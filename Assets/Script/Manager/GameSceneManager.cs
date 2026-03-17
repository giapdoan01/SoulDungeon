// GameSceneManager.cs
//
// VAN DE TUNG GAP:
//   allReady message co the fire TRUOC khi scene BattleScene load xong.
//   → GameSceneManager.Start() chua chay → chua subscribe OnAllReady → event bi lost.
//   → 1 client bi ket man hinh loading mai mai.
//
// GIAI PHAP:
//   GameRoomManager cache lai tat ca message quan trong (CachedAllReady, CachedGameReady).
//   Trong Start(), kiem tra cache truoc → xu ly ngay neu da co → khong bi miss.
//
// LUONG CHINH:
//   1. Hien loading overlay
//   2. Nhan allReady (truc tiep hoac tu cache) → spawn tat ca player
//   3. Tắt loading overlay khi du 2 character duoc spawn
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    [Header("Prefabs & Spawn")]
    [SerializeField] private GameObject[] _characterPrefabs;
    [SerializeField] private Transform[]  _spawnPoints;

    [Header("UI")]
    [SerializeField] private TMP_Text   _statusText;
    [SerializeField] private GameObject _loadingOverlay;

    private readonly Dictionary<string, NetworkedCharacter> _characters = new();
    private int _remoteSlotCounter = 1;

    // ── Lifecycle ──────────────────────────────────────────────────
    private void Start()
    {
        var mgr = GameRoomManager.Instance;
        if (mgr == null)
        {
            Debug.LogError("[GameScene] GameRoomManager.Instance is null!");
            return;
        }

        // Subscribe events truoc
        mgr.OnAllReady      += HandleAllReady;
        mgr.OnPlayerLeft    += HandlePlayerLeft;
        mgr.OnPlayerRemoved += HandlePlayerRemoved;

        ShowLoading("Dang cho nguoi choi vao tran...");

        // CATCH-UP: Kiem tra xem allReady da duoc nhan chua (truoc khi scene load xong)
        // Neu co → xu ly ngay, khong can doi event fire lan nua
        if (mgr.CachedAllReady != null)
        {
            Debug.Log("[GameScene] allReady da co trong cache → xu ly ngay");
            HandleAllReady(mgr.CachedAllReady);
        }
    }

    private void OnDestroy()
    {
        var mgr = GameRoomManager.Instance;
        if (mgr == null) return;
        mgr.OnAllReady      -= HandleAllReady;
        mgr.OnPlayerLeft    -= HandlePlayerLeft;
        mgr.OnPlayerRemoved -= HandlePlayerRemoved;
    }

    // ── Events ─────────────────────────────────────────────────────

    // allReady: server xac nhan du player va gui danh sach day du
    private void HandleAllReady(AllReadyMsg msg)
    {
        Debug.Log($"[GameScene] HandleAllReady | players:{msg.players?.Length ?? 0}");

        if (msg.players != null && msg.players.Length > 0)
        {
            foreach (var snapshot in msg.players)
                SpawnFromSnapshot(snapshot);
        }
        else
        {
            // Fallback: neu deserialization cua players array bi loi
            // thu doc tu state (co the van co du lieu)
            Debug.LogWarning("[GameScene] msg.players rong, fallback to state");
            var mgr = GameRoomManager.Instance;
            if (mgr != null)
                mgr.State?.players?.ForEach((key, p) => SpawnFromState(key, p));
        }

        // TAT LOADING du ket qua spawn nhu the nao
        // Neu spawn that bai se co debug log de biet ly do
        HideLoading();
        SetStatus("Dang chien!");
    }

    private void HandlePlayerLeft(PlayerLeftGameMsg msg)
    {
        SetStatus($"{msg.username} da roi tran.");
    }

    private void HandlePlayerRemoved(string sessionId, PlayerGameStateSchema _)
    {
        if (!_characters.TryGetValue(sessionId, out var nc)) return;
        if (nc != null) Destroy(nc.gameObject);
        _characters.Remove(sessionId);
    }

    // ── Spawn ──────────────────────────────────────────────────────

    // Spawn tu PlayerSnapshot (tu allReady message — du lieu dang tin cay)
    private void SpawnFromSnapshot(PlayerSnapshot snapshot)
    {
        if (_characters.ContainsKey(snapshot.sessionId)) return;

        var mgr = GameRoomManager.Instance;
        bool isLocal = mgr != null && snapshot.sessionId == mgr.SessionId;

        Spawn(snapshot.sessionId, snapshot.username, snapshot.characterIndex, isLocal);
    }

    // Spawn tu State (fallback)
    private void SpawnFromState(string sessionId, PlayerGameStateSchema p)
    {
        if (_characters.ContainsKey(sessionId)) return;

        var mgr = GameRoomManager.Instance;
        bool isLocal = mgr != null && sessionId == mgr.SessionId;

        Spawn(sessionId, p.username, p.characterIndex, isLocal);
    }

    // Ham spawn thuc su — dung chung cho ca 2 nguon du lieu
    private void Spawn(string sessionId, string username, int characterIndex, bool isLocal)
    {
        int slot = isLocal ? 0 : _remoteSlotCounter++;
        Vector3 spawnPos = slot < _spawnPoints.Length ? _spawnPoints[slot].position : Vector3.zero;

        int prefabIdx = (characterIndex >= 0 && characterIndex < _characterPrefabs.Length)
            ? characterIndex : 0;

        GameObject go = Instantiate(_characterPrefabs[prefabIdx], spawnPos, Quaternion.identity);
        go.name = $"Character_{username}_{(isLocal ? "LOCAL" : "REMOTE")}";

        if (!go.TryGetComponent<NetworkedCharacter>(out var nc))
            nc = go.AddComponent<NetworkedCharacter>();

        nc.Init(sessionId, isLocal);
        _characters[sessionId] = nc;

        Debug.Log($"[GameScene] Spawned {go.name} | slot:{slot} | isLocal:{isLocal}");
    }

    // ── UI ─────────────────────────────────────────────────────────

    private void ShowLoading(string msg)
    {
        if (_loadingOverlay != null) _loadingOverlay.SetActive(true);
        SetStatus(msg);
    }

    private void HideLoading()
    {
        if (_loadingOverlay != null) _loadingOverlay.SetActive(false);
    }

    private void SetStatus(string text)
    {
        if (_statusText != null) _statusText.text = text;
    }
}
