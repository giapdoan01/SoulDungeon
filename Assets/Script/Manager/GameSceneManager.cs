// GameSceneManager.cs
// Nằm trong GameScene. Lắng nghe GameRoomManager events để:
//   1. Spawn character cho từng player khi vào phòng
//   2. Despawn khi player rời
//   3. Hiển thị trạng thái trận (waiting ready / playing)
//
// Inspector cần set:
//   - CharacterPrefabs[]  : mảng prefab theo characterIndex
//   - SpawnPoints[]       : vị trí spawn cho mỗi slot (index 0 = local, index 1 = remote,...)
//   - StatusText          : TMP_Text hiển thị trạng thái
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    [Header("Prefabs & Spawn")]
    [SerializeField] private GameObject[]  _characterPrefabs;
    [SerializeField] private Transform[]   _spawnPoints;

    [Header("UI")]
    [SerializeField] private TMP_Text      _statusText;

    // sessionId → NetworkedCharacter
    private readonly Dictionary<string, NetworkedCharacter> _characters = new();

    // ── Lifecycle ──────────────────────────────────────────────────
    private void Start()
    {
        var mgr = GameRoomManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[GameScene] GameRoomManager.Instance is null!");
            return;
        }

        mgr.OnGameReady    += HandleGameReady;
        mgr.OnAllReady     += HandleAllReady;
        mgr.OnPlayerLeft   += HandlePlayerLeft;
        mgr.OnPlayerAdded  += HandlePlayerAdded;
        mgr.OnPlayerRemoved += HandlePlayerRemoved;

        SetStatus("Đang chờ người chơi...");
    }

    private void OnDestroy()
    {
        var mgr = GameRoomManager.Instance;
        if (mgr == null) return;

        mgr.OnGameReady     -= HandleGameReady;
        mgr.OnAllReady      -= HandleAllReady;
        mgr.OnPlayerLeft    -= HandlePlayerLeft;
        mgr.OnPlayerAdded   -= HandlePlayerAdded;
        mgr.OnPlayerRemoved -= HandlePlayerRemoved;
    }

    // ── GameRoom Events ────────────────────────────────────────────
    private void HandleGameReady(GameReadyMsg msg)
    {
        Debug.Log($"[GameScene] gameReady — mySessionId:{msg.sessionId}");
        SetStatus("Đang chờ tất cả chọn nhân vật...");
    }

    private void HandleAllReady(AllReadyMsg msg)
    {
        Debug.Log($"[GameScene] allReady — bắt đầu trận {msg.matchId}");
        SetStatus("Đang chiến!");
    }

    private void HandlePlayerLeft(PlayerLeftGameMsg msg)
    {
        Debug.Log($"[GameScene] Player rời: {msg.username}");
        SetStatus($"{msg.username} đã rời trận.");
    }

    // OnAdd từ state — spawn character
    private void HandlePlayerAdded(string sessionId, PlayerGameStateSchema playerState)
    {
        if (_characters.ContainsKey(sessionId)) return;

        bool isLocal = sessionId == GameRoomManager.Instance?.SessionId;

        // Chọn spawn point theo thứ tự: local player luôn ở slot 0
        int slot = isLocal ? 0 : GetNextRemoteSlot();
        Vector3 spawnPos = slot < _spawnPoints.Length
            ? _spawnPoints[slot].position
            : Vector3.zero;

        // Chọn prefab theo characterIndex (fallback về index 0 nếu out of range)
        int prefabIdx = playerState.characterIndex;
        if (prefabIdx >= _characterPrefabs.Length) prefabIdx = 0;
        GameObject prefab = _characterPrefabs[prefabIdx];

        GameObject go = Instantiate(prefab, spawnPos, Quaternion.identity);
        go.name = $"Character_{playerState.username}_{(isLocal ? "LOCAL" : "REMOTE")}";

        var nc = go.GetComponent<NetworkedCharacter>();
        if (nc == null) nc = go.AddComponent<NetworkedCharacter>();

        nc.Init(sessionId, isLocal);
        _characters[sessionId] = nc;

        Debug.Log($"[GameScene] Spawned {go.name}");
    }

    // OnRemove từ state — despawn character
    private void HandlePlayerRemoved(string sessionId, PlayerGameStateSchema _)
    {
        if (!_characters.TryGetValue(sessionId, out var nc)) return;
        if (nc != null) Destroy(nc.gameObject);
        _characters.Remove(sessionId);
        Debug.Log($"[GameScene] Despawned character for {sessionId}");
    }

    // ── Helpers ────────────────────────────────────────────────────
    private int _remoteSlotCounter = 1;
    private int GetNextRemoteSlot() => _remoteSlotCounter++;

    private void SetStatus(string text)
    {
        if (_statusText != null) _statusText.text = text;
    }
}
