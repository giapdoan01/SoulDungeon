// GameRoomManager.cs
// Singleton quản lý kết nối tới GameRoom.
// Gọi ConnectAsync(roomId) sau khi nhận matchStart từ MatchmakingRoom.
// Các script game đọc State.players để render, gọi SendMove / SendSetCharacter để điều khiển.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Colyseus;
using Colyseus.Schema;
using UnityEngine;

public class GameRoomManager : MonoBehaviour
{
    #region Singleton
    public static GameRoomManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        _ = DisconnectAsync();
    }
    #endregion

    #region Configuration
    [Header("Server")]
    [SerializeField] private string _serverUrl = "ws://localhost:3001";
    #endregion

    #region State
    private ColyseusClient              _client;
    private ColyseusRoom<GameStateSchema> _room;
    private StateCallbackStrategy<GameStateSchema> _callbacks;  // giữ ref tránh GC

    /// <summary>SessionId trong GameRoom (dùng để identify bản thân trong State.players).</summary>
    public string SessionId   { get; private set; }
    public bool   IsConnected => _room != null;

    // Cache các message quan trọng để GameSceneManager catch-up khi Start() chạy
    // trễ hơn so với lúc message được nhận (do scene loading delay)
    public AllReadyMsg  CachedAllReady  { get; private set; }
    public GameReadyMsg CachedGameReady { get; private set; }

    /// <summary>Truy cập trực tiếp GameState để đọc vị trí / characterIndex của tất cả player.</summary>
    public GameStateSchema State => _room?.State;
    #endregion

    #region Events
    /// <summary>GameRoom xác nhận kết nối — nhận được sessionId của mình.</summary>
    public event Action<GameReadyMsg>     OnGameReady;

    /// <summary>Tất cả player đã setCharacter xong, trận bắt đầu.</summary>
    public event Action<AllReadyMsg>      OnAllReady;

    /// <summary>Một player rời phòng.</summary>
    public event Action<PlayerLeftGameMsg> OnPlayerLeft;

    /// <summary>Server broadcast vị trí mới của remote player (thay schema sync cho movement).</summary>
    public event Action<PlayerMovedMsg> OnPlayerMoved;

    /// <summary>State.players thêm player mới (dùng để spawn character).</summary>
    public event Action<string, PlayerGameStateSchema> OnPlayerAdded;

    /// <summary>State.players xoá player (dùng để despawn character).</summary>
    public event Action<string, PlayerGameStateSchema> OnPlayerRemoved;
    #endregion

    #region Connect / Disconnect
    /// <summary>
    /// Kết nối vào GameRoom.
    /// roomId     = từ MatchStartMsg.roomId
    /// mmSessionId = MatchmakingRoomManager.Instance.SessionId (dùng để server validate)
    /// </summary>
    public async Task ConnectAsync(string roomId, string mmSessionId)
    {
        if (_room != null) await DisconnectAsync();

        try
        {
            _client = new ColyseusClient(_serverUrl);
            _room   = await _client.JoinById<GameStateSchema>(roomId, new Dictionary<string, object>
            {
                { "mmSessionId", mmSessionId }
            });

            RegisterHandlers();
            Debug.Log($"[GameRoom] Connected — roomId:{roomId} sessionId:{_room.SessionId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameRoom] ConnectAsync failed: {e.Message}");
            _room = null;
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_room == null) return;
        try { await _room.Leave(); } catch (Exception e) { Debug.LogWarning($"[GameRoom] Leave: {e.Message}"); }
        finally
        {
            _room           = null;
            SessionId       = null;
            CachedGameReady = null;
            CachedAllReady  = null;
            SessionId  = null;
            Debug.Log("[GameRoom] Disconnected.");
        }
    }
    #endregion

    #region Register Handlers
    private void RegisterHandlers()
    {
        // Custom messages
        _room.OnMessage<GameReadyMsg>("gameReady", msg =>
        {
            SessionId       = msg.sessionId;
            CachedGameReady = msg;   // cache lai cho GameSceneManager catch-up
            Debug.Log($"[GameRoom] gameReady — mySessionId:{SessionId}");
            OnGameReady?.Invoke(msg);
        });
        _room.OnMessage<AllReadyMsg>("allReady", msg =>
        {
            CachedAllReady = msg;    // cache lai — quan trong! GameSceneManager co the chua Start() kip
            OnAllReady?.Invoke(msg);
        });
        _room.OnMessage<PlayerLeftGameMsg>("playerLeft",   msg => OnPlayerLeft?.Invoke(msg));
        _room.OnMessage<PlayerMovedMsg>   ("playerMoved",  msg => OnPlayerMoved?.Invoke(msg));

        // Schema state — subscribe player add/remove (API đúng cho SDK này)
        _callbacks = Callbacks.Get(_room);
        _callbacks.OnAdd   (state => state.players, (key, player) => OnPlayerAdded?.Invoke(key, player));
        _callbacks.OnRemove(state => state.players, (key, player) => OnPlayerRemoved?.Invoke(key, player));
    }
    #endregion

    #region Send — Character
    /// <summary>Chọn character trước khi trận bắt đầu.</summary>
    public void SendSetCharacter(int characterIndex)
    {
        if (_room == null) return;
        _ = _room.Send("setCharacter", new Dictionary<string, object>
        {
            { "characterIndex", characterIndex }
        });
    }
    #endregion

    #region Send — Movement
    /// <summary>
    /// Gửi vị trí + hướng nhìn lên server. Gọi trong FixedUpdate hoặc khi có thay đổi.
    /// facing: góc degrees (0 = phải, 180 = trái).
    /// </summary>
    public void SendMove(float x, float y, float facing, float speed)
    {
        if (_room == null || State?.status != "playing") return;
        _ = _room.Send("move", new Dictionary<string, object>
        {
            { "x",      x      },
            { "y",      y      },
            { "facing", facing },
            { "speed",  speed  }   // [ANIMATION] 0=dung, 1=di chuyen
        });
    }
    #endregion

    #region Helpers
    /// <summary>Lấy PlayerGameStateSchema của bản thân (dùng sessionId của GameRoom).</summary>
    public PlayerGameStateSchema GetMyPlayer()
    {
        if (State?.players == null || SessionId == null) return null;
        return State.players.ContainsKey(SessionId) ? State.players[SessionId] : null;
    }

    /// <summary>Lấy PlayerGameStateSchema của player khác (remote).</summary>
    public PlayerGameStateSchema GetPlayer(string sessionId)
    {
        if (State?.players == null) return null;
        return State.players.ContainsKey(sessionId) ? State.players[sessionId] : null;
    }
    #endregion
}
