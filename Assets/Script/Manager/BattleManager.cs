using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform[] spawnPoints;

    // Map connId → GameObject để cleanup khi disconnect
    private readonly Dictionary<int, GameObject> spawnedPlayers = new();

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ══════════════════════════════════════════════════════════════
    // SPAWN
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gọi từ MyNetworkManager.SpawnMatchPlayers()
    /// khi TẤT CẢ client trong match đã ready.
    /// </summary>
    [Server]
    public void SpawnPlayers(List<NetworkConnectionToClient> conns)
    {
        GameObject prefab = NetworkManager.singleton.playerPrefab;
        if (prefab == null)
        {
            Debug.LogError("[BattleManager] playerPrefab is null!");
            return;
        }

        if (conns == null || conns.Count == 0)
        {
            Debug.LogError("[BattleManager] conns rỗng!");
            return;
        }

        for (int i = 0; i < conns.Count; i++)
        {
            var conn = conns[i];

            // Guard: connection còn sống không?
            if (conn == null || !conn.isReady)
            {
                Debug.LogWarning($"[BattleManager] conn[{i}] null hoặc chưa ready, bỏ qua.");
                continue;
            }

            // Guard: đã spawn rồi thì không spawn lại
            if (spawnedPlayers.ContainsKey((int)conn.connectionId))
            {
                Debug.LogWarning($"[BattleManager] connId={conn.connectionId} đã được spawn, bỏ qua.");
                continue;
            }

            // Lấy spawnPoint — nếu thiếu thì dùng Vector3.zero
            Vector3 spawnPos = i < spawnPoints.Length
                ? spawnPoints[i].position
                : Vector3.zero;

            var go = Instantiate(prefab, spawnPos, Quaternion.identity);

            // NetworkServer.Spawn(go, conn) → set owner cho conn
            // → client của conn nhận isLocalPlayer = true
            // → KHÔNG dùng AddPlayerForConnection (conflict với autoCreatePlayer)
            NetworkServer.Spawn(go, conn);

            spawnedPlayers[(int)conn.connectionId] = go;

            Debug.Log($"[BattleManager] Spawned player {i} " +
                      $"(connId={conn.connectionId}) at {spawnPos}");
        }

        Debug.Log($"[BattleManager] Tổng spawn: {spawnedPlayers.Count}/{conns.Count}");
    }

    // ══════════════════════════════════════════════════════════════
    // CLEANUP — Gọi từ MyNetworkManager.OnServerDisconnect()
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Destroy player object khi client disconnect giữa chừng.
    /// </summary>
    [Server]
    public void OnPlayerDisconnected(NetworkConnectionToClient conn)
    {
        int connId = (int)conn.connectionId;

        if (!spawnedPlayers.TryGetValue(connId, out var go))
            return;

        spawnedPlayers.Remove(connId);

        if (go != null)
        {
            NetworkServer.Destroy(go);
            Debug.Log($"[BattleManager] Destroyed player (connId={connId})");
        }
    }

    // ══════════════════════════════════════════════════════════════
    // QUERY
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy GameObject của player theo connId.
    /// Dùng cho các hệ thống khác (combat, UI, v.v.)
    /// </summary>
    public bool TryGetPlayerObject(int connId, out GameObject go)
        => spawnedPlayers.TryGetValue(connId, out go);

    public int PlayerCount => spawnedPlayers.Count;
}
