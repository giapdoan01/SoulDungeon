// MatchmakingRoomManager.cs
// Singleton quản lý kết nối tới Colyseus MatchmakingRoom.
// Chỉ xử lý networking, không đụng UI.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Colyseus;
using Colyseus.Schema;
using UnityEngine;

// MatchmakingStateSchema được định nghĩa trong ColyseusStateSchema.cs

public class MatchmakingRoomManager : MonoBehaviour
{
    #region Singleton
    public static MatchmakingRoomManager Instance { get; private set; }

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
    [SerializeField] private string _roomName  = "matchmaking";
    #endregion

    #region State
    private ColyseusClient                     _client;
    private ColyseusRoom<MatchmakingStateSchema> _room;

    public string SessionId  { get; private set; }
    public bool   IsConnected => _room != null;
    #endregion

    #region Events — Welcome
    public event Action<WelcomeMsg> OnWelcome;
    #endregion

    #region Events — Queue
    public event Action<QueueJoinedMsg> OnQueueJoined;
    public event Action<QueueErrorMsg>  OnQueueError;
    public event Action                 OnQueueLeft;
    #endregion

    #region Events — Party
    public event Action<PartyCreatedMsg>      OnPartyCreated;
    public event Action<PartyErrorMsg>        OnPartyError;
    public event Action<JoinPartyResultMsg>   OnJoinPartyResult;
    public event Action<PartyData>            OnPartyUpdate;
    public event Action                       OnPartyLeft;
    public event Action<LeadershipTransferredMsg> OnLeadershipTransferred;
    public event Action<KickResultMsg>        OnKickResult;
    public event Action<KickedMsg>            OnKicked;
    public event Action<StartMatchResultMsg>  OnStartMatchResult;
    public event Action<PartyExpiredMsg>      OnPartyExpired;
    #endregion

    #region Events — Match
    public event Action<MatchFoundMsg>      OnMatchFound;
    public event Action<MatchStartMsg>      OnMatchStart;
    public event Action<MatchCancelledMsg>  OnMatchCancelled;
    public event Action<CancelMatchResultMsg> OnCancelMatchResult;
    public event Action<MatchErrorMsg>      OnMatchError;
    #endregion

    #region Connect / Disconnect
    /// <summary>
    /// Kết nối tới MatchmakingRoom. Gọi sau khi đã login thành công.
    /// </summary>
    public async Task ConnectAsync()
    {
        if (_room != null)
        {
            Debug.LogWarning("[MMRoom] Already connected.");
            return;
        }

        try
        {
            string username = AuthManager.Instance?.CurrentUser?.username ?? "Player";

            _client = new ColyseusClient(_serverUrl);
            _room   = await _client.JoinOrCreate<MatchmakingStateSchema>(_roomName, new Dictionary<string, object>
            {
                { "username", username }
            });

            RegisterHandlers();
            Debug.Log("[MMRoom] Connected to matchmaking room.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MMRoom] ConnectAsync failed: {e.Message}");
            _room = null;
            throw;
        }
    }

    /// <summary>
    /// Ngắt kết nối khỏi MatchmakingRoom.
    /// </summary>
    public async Task DisconnectAsync()
    {
        if (_room == null) return;
        try { await _room.Leave(); }
        catch (Exception e) { Debug.LogWarning($"[MMRoom] Leave error: {e.Message}"); }
        finally
        {
            _room      = null;
            SessionId  = null;
            Debug.Log("[MMRoom] Disconnected.");
        }
    }
    #endregion

    #region Register Message Handlers
    private void RegisterHandlers()
    {
        // Welcome
        _room.OnMessage<WelcomeMsg>("welcome", msg =>
        {
            SessionId = msg.sessionId;
            Debug.Log($"[MMRoom] Welcome — sessionId: {SessionId}");
            OnWelcome?.Invoke(msg);
        });

        // Queue
        _room.OnMessage<QueueJoinedMsg>("queueJoined",   msg => OnQueueJoined?.Invoke(msg));
        _room.OnMessage<QueueErrorMsg> ("queueError",    msg => OnQueueError?.Invoke(msg));
        _room.OnMessage<EmptyMsg>      ("queueLeft",     _   => OnQueueLeft?.Invoke());

        // Party
        _room.OnMessage<PartyCreatedMsg>     ("partyCreated",          msg => OnPartyCreated?.Invoke(msg));
        _room.OnMessage<PartyErrorMsg>       ("partyError",            msg => OnPartyError?.Invoke(msg));
        _room.OnMessage<JoinPartyResultMsg>  ("joinPartyResult",       msg => OnJoinPartyResult?.Invoke(msg));
        _room.OnMessage<PartyData>           ("partyUpdate",           msg => OnPartyUpdate?.Invoke(msg));
        _room.OnMessage<EmptyMsg>            ("partyLeft",             _   => OnPartyLeft?.Invoke());
        _room.OnMessage<LeadershipTransferredMsg>("leadershipTransferred", msg => OnLeadershipTransferred?.Invoke(msg));
        _room.OnMessage<KickResultMsg>       ("kickResult",            msg => OnKickResult?.Invoke(msg));
        _room.OnMessage<KickedMsg>           ("kicked",                msg => OnKicked?.Invoke(msg));
        _room.OnMessage<StartMatchResultMsg> ("startMatchResult",      msg => OnStartMatchResult?.Invoke(msg));
        _room.OnMessage<PartyExpiredMsg>     ("partyExpired",          msg => OnPartyExpired?.Invoke(msg));

        // Match
        _room.OnMessage<MatchFoundMsg>      ("matchFound",        msg => OnMatchFound?.Invoke(msg));
        _room.OnMessage<MatchStartMsg>      ("matchStart",        msg => OnMatchStart?.Invoke(msg));
        _room.OnMessage<MatchCancelledMsg>  ("matchCancelled",    msg => OnMatchCancelled?.Invoke(msg));
        _room.OnMessage<CancelMatchResultMsg>("cancelMatchResult",msg => OnCancelMatchResult?.Invoke(msg));
        _room.OnMessage<MatchErrorMsg>      ("matchError",        msg => OnMatchError?.Invoke(msg));
    }
    #endregion

    #region Send — Internal helper
    private async void Send(string type, object data = null)
    {
        if (_room == null)
        {
            Debug.LogWarning($"[MMRoom] Cannot send '{type}': not connected.");
            return;
        }
        try
        {
            if (data == null) await _room.Send(type);
            else              await _room.Send(type, data);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MMRoom] Send('{type}') error: {e.Message}");
        }
    }
    #endregion

    #region Send — Queue
    public void JoinQueue()  => Send("joinQueue");
    public void LeaveQueue() => Send("leaveQueue");
    #endregion

    #region Send — Party
    public void CreateParty(int maxMembers = 2) =>
        Send("createParty", new Dictionary<string, object> { { "maxMembers", maxMembers } });

    public void JoinPartyByCode(string inviteCode) =>
        Send("joinPartyByCode", new Dictionary<string, object> { { "inviteCode", inviteCode } });

    public void LeaveParty()  => Send("leaveParty");

    public void KickPlayer(string sessionId) =>
        Send("kickPlayer", new Dictionary<string, object> { { "sessionId", sessionId } });

    public void StartPartyMatch() => Send("startPartyMatch");
    #endregion

    #region Send — Match
    public void CancelMatch() => Send("cancelMatch");
    #endregion
}
