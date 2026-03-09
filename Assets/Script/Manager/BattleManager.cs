using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject      playerPrefab;
    [SerializeField] private Transform[]     spawnPoints;

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    void Awake()
    {
        Instance = this;
    }

    // ══════════════════════════════════════════════════════════════
    // SPAWN — Được gọi bởi MyNetworkManager.OnServerReady()
    //         khi TẤT CẢ client trong match đã ready
    // ══════════════════════════════════════════════════════════════

    public void SpawnPlayers(List<NetworkConnectionToClient> conns)
    {
        for (int i = 0; i < conns.Count; i++)
        {
            if (i >= spawnPoints.Length)
            {
                Debug.LogWarning($"[BattleManager] Không đủ SpawnPoint cho player {i}!");
                break;
            }

            var go = Instantiate(
                playerPrefab,
                spawnPoints[i].position,
                Quaternion.identity
            );

            // Spawn với đúng owner → client đó nhận isLocalPlayer = true
            NetworkServer.Spawn(go, conns[i]);

            Debug.Log($"[BattleManager] Spawned player {i} " +
                      $"(connId={conns[i].connectionId}) " +
                      $"at {spawnPoints[i].position}");
        }
    }
}
