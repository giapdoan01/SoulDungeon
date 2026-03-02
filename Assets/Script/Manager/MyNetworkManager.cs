using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class MyNetworkManager : NetworkManager
{
    public static MyNetworkManager Instance { get; private set; }

    [Header("Match Settings")]
    public string battleScene = "BattleScene";
    public int playersPerMatch = 2;

    private readonly Queue<NetworkConnectionToClient> matchmakingQueue = new();
    private readonly Dictionary<string, List<NetworkConnectionToClient>> parties = new();
    private readonly Dictionary<NetworkConnectionToClient, string> playerParty = new();

    // ── Lifecycle ────────────────────────────────────────────────────
    public override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    // ── Server Start → Đăng ký handlers ─────────────────────────────
    public override void OnStartServer()
    {
        base.OnStartServer();

        // Đăng ký nhận messages từ client
        NetworkServer.RegisterHandler<MsgJoinQueue>(OnReceiveJoinQueue);
        NetworkServer.RegisterHandler<MsgCreateParty>(OnReceiveCreateParty);
        NetworkServer.RegisterHandler<MsgJoinParty>(OnReceiveJoinParty);

        Debug.Log("[Server] Started. Handlers registered.");
    }

    // ── Server Callbacks ─────────────────────────────────────────────
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"[Server] Player {conn.connectionId} connected. " +
                  $"Online: {NetworkServer.connections.Count}");
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        RemoveFromQueue(conn);
        LeaveParty(conn);
        base.OnServerDisconnect(conn);
        Debug.Log($"[Server] Player {conn.connectionId} disconnected.");
    }

    // ── Client Callbacks ─────────────────────────────────────────────
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[Client] Connected to server.");

        // Đăng ký nhận messages từ server
        NetworkClient.RegisterHandler<MsgPartyCreated>(OnReceivePartyCreated);
        NetworkClient.RegisterHandler<MsgError>(OnReceiveError);

        // Báo UIManager biết đã kết nối
        UIManager.Instance?.OnClientConnected();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("[Client] Disconnected.");
        UIManager.Instance?.ShowHome();
    }

    // ══════════════════════════════════════════════════════════════
    // SERVER MESSAGE HANDLERS
    // ══════════════════════════════════════════════════════════════

    private void OnReceiveJoinQueue(NetworkConnectionToClient conn, MsgJoinQueue msg)
    {
        Debug.Log($"[Server] Player {conn.connectionId} wants to join queue.");
        JoinQueue(conn);
    }

    private void OnReceiveCreateParty(NetworkConnectionToClient conn, MsgCreateParty msg)
    {
        Debug.Log($"[Server] Player {conn.connectionId} wants to create party.");
        string code = CreateParty(conn);

        // Gửi mã party về cho client
        conn.Send(new MsgPartyCreated { partyCode = code });
    }

    private void OnReceiveJoinParty(NetworkConnectionToClient conn, MsgJoinParty msg)
    {
        Debug.Log($"[Server] Player {conn.connectionId} wants to join party [{msg.partyCode}].");
        bool success = JoinParty(msg.partyCode, conn);

        if (!success)
            conn.Send(new MsgError { message = $"Không tìm thấy phòng [{msg.partyCode}]!" });
    }

    // ══════════════════════════════════════════════════════════════
    // CLIENT MESSAGE HANDLERS
    // ══════════════════════════════════════════════════════════════

    private void OnReceivePartyCreated(MsgPartyCreated msg)
    {
        Debug.Log($"[Client] Party created: [{msg.partyCode}]");
        UIManager.Instance?.SetPartyCode(msg.partyCode);
        UIManager.Instance?.SetStatus("Chờ bạn bè vào phòng...");
    }

    private void OnReceiveError(MsgError msg)
    {
        Debug.Log($"[Client] Error: {msg.message}");
        UIManager.Instance?.SetStatus(msg.message);
    }

    // ══════════════════════════════════════════════════════════════
    // MATCHMAKING QUEUE
    // ══════════════════════════════════════════════════════════════

    private void JoinQueue(NetworkConnectionToClient conn)
    {
        if (matchmakingQueue.Contains(conn)) return;

        matchmakingQueue.Enqueue(conn);
        Debug.Log($"[Queue] Player {conn.connectionId} joined. " +
                  $"Queue: {matchmakingQueue.Count}/{playersPerMatch}");

        TryStartMatch();
    }

    private void TryStartMatch()
    {
        while (matchmakingQueue.Count >= playersPerMatch)
        {
            var players = new List<NetworkConnectionToClient>();
            for (int i = 0; i < playersPerMatch; i++)
                players.Add(matchmakingQueue.Dequeue());

            StartMatch(players);
        }
    }

    private void RemoveFromQueue(NetworkConnectionToClient conn)
    {
        var temp = new Queue<NetworkConnectionToClient>();
        foreach (var c in matchmakingQueue)
            if (c != conn) temp.Enqueue(c);

        matchmakingQueue.Clear();
        foreach (var c in temp)
            matchmakingQueue.Enqueue(c);
    }

    // ══════════════════════════════════════════════════════════════
    // PARTY SYSTEM
    // ══════════════════════════════════════════════════════════════

    private string CreateParty(NetworkConnectionToClient leader)
    {
        LeaveParty(leader);

        string partyId = GeneratePartyCode();
        parties[partyId] = new List<NetworkConnectionToClient> { leader };
        playerParty[leader] = partyId;

        Debug.Log($"[Party] Created [{partyId}] by Player {leader.connectionId}");
        return partyId;
    }

    private bool JoinParty(string partyId, NetworkConnectionToClient conn)
    {
        partyId = partyId.ToUpper().Trim();

        if (!parties.ContainsKey(partyId))         return false;
        if (parties[partyId].Count >= playersPerMatch) return false;
        if (parties[partyId].Contains(conn))        return false;

        LeaveParty(conn);
        parties[partyId].Add(conn);
        playerParty[conn] = partyId;

        Debug.Log($"[Party] Player {conn.connectionId} joined [{partyId}]. " +
                  $"Size: {parties[partyId].Count}/{playersPerMatch}");

        if (parties[partyId].Count == playersPerMatch)
            StartMatch(new List<NetworkConnectionToClient>(parties[partyId]));

        return true;
    }

    private void LeaveParty(NetworkConnectionToClient conn)
    {
        if (!playerParty.ContainsKey(conn)) return;

        string partyId = playerParty[conn];
        playerParty.Remove(conn);

        if (!parties.ContainsKey(partyId)) return;
        parties[partyId].Remove(conn);

        if (parties[partyId].Count == 0)
            parties.Remove(partyId);
    }

    // ══════════════════════════════════════════════════════════════
    // MATCH
    // ══════════════════════════════════════════════════════════════

    private void StartMatch(List<NetworkConnectionToClient> players)
    {
        string matchId = System.Guid.NewGuid().ToString()[..8];
        Debug.Log($"[Match] Starting [{matchId}] with {players.Count} players.");
        ServerChangeScene(battleScene);
    }

    // ── Helpers ──────────────────────────────────────────────────────
    private string GeneratePartyCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string code;
        do
        {
            code = "";
            for (int i = 0; i < 6; i++)
                code += chars[Random.Range(0, chars.Length)];
        }
        while (parties.ContainsKey(code));
        return code;
    }
}
