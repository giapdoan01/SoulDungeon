using Mirror;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MyNetworkManager : NetworkManager
{
    public static MyNetworkManager Instance { get; private set; }

    [Header("Match Settings")]
    public string battleScene = "BattleScene";
    public int playersPerMatch = 2;
    public float countdownSeconds = 15f;


    // ══════════════════════════════════════════════════════════════
    // EVENTS
    // ══════════════════════════════════════════════════════════════

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string[]> OnMatchFound;
    public event Action OnMatchCancelled;
    public event Action<string, int> OnPartyCreated;
    public event Action<PartyMemberData[], int, int> OnPartyUpdated;
    public event Action OnPartyKicked;
    public event Action OnPartyStarting;
    public event Action<string> OnError;

    // ══════════════════════════════════════════════════════════════
    // SERVER STATE
    // ══════════════════════════════════════════════════════════════

    private readonly Queue<NetworkConnectionToClient> matchmakingQueue = new();
    private readonly Dictionary<NetworkConnectionToClient, string> queueNames = new();
    private readonly Dictionary<string, List<NetworkConnectionToClient>> pendingMatches = new();
    private readonly Dictionary<NetworkConnectionToClient, string> connToMatch = new();
    private readonly Dictionary<string, List<NetworkConnectionToClient>> parties = new();
    private readonly Dictionary<NetworkConnectionToClient, string> playerParty = new();
    public readonly Dictionary<NetworkConnectionToClient, string> playerNames = new();
    private readonly Dictionary<string, List<NetworkConnectionToClient>> matchPlayers = new();

    // matchId đang chờ tất cả client ready để spawn
    private string pendingMatchId;

    // Đếm số client đã ready trong battle scene theo matchId
    private readonly Dictionary<string, List<NetworkConnectionToClient>> readyInMatch = new();

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    public override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<MsgJoinQueue>(OnReceiveJoinQueue);
        NetworkServer.RegisterHandler<MsgCancelMatch>(OnReceiveCancelMatch);
        NetworkServer.RegisterHandler<MsgCreateParty>(OnReceiveCreateParty);
        NetworkServer.RegisterHandler<MsgJoinParty>(OnReceiveJoinParty);
        NetworkServer.RegisterHandler<MsgKickMember>(OnReceiveKickMember);
        NetworkServer.RegisterHandler<MsgStartParty>(OnReceiveStartParty);
        NetworkServer.RegisterHandler<MsgLeaveParty>(OnReceiveLeaveParty);
        Debug.Log("[Server] Started.");
    }

    // ══════════════════════════════════════════════════════════════
    // SERVER CALLBACKS
    // ══════════════════════════════════════════════════════════════

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        RemoveFromQueue(conn);
        CancelPendingMatch(conn);
        HandleLeaveParty(conn);
        playerNames.Remove(conn);
        base.OnServerDisconnect(conn);
    }

    /// <summary>
    /// Gọi khi MỘT client đã load scene xong và gửi Ready.
    /// Đây là thời điểm chính xác để spawn player cho client đó.
    /// </summary>
    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        // Chỉ xử lý khi đang ở battle scene
        if (networkSceneName != battleScene) return;
        if (string.IsNullOrEmpty(pendingMatchId)) return;
        if (!matchPlayers.TryGetValue(pendingMatchId, out var players)) return;

        // Client này có thuộc match đang chờ không?
        if (!players.Contains(conn)) return;

        // Ghi nhận client này đã ready
        if (!readyInMatch.ContainsKey(pendingMatchId))
            readyInMatch[pendingMatchId] = new List<NetworkConnectionToClient>();

        if (readyInMatch[pendingMatchId].Contains(conn)) return; // tránh double
        readyInMatch[pendingMatchId].Add(conn);

        Debug.Log($"[Server] Client {conn.connectionId} ready " +
                  $"({readyInMatch[pendingMatchId].Count}/{players.Count})");

        // Khi TẤT CẢ client trong match đã ready → spawn
        if (readyInMatch[pendingMatchId].Count >= players.Count)
        {
            SpawnMatchPlayers(pendingMatchId, players);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // SPAWN — Gọi khi tất cả client đã ready
    // ══════════════════════════════════════════════════════════════

    private void SpawnMatchPlayers(string matchId, List<NetworkConnectionToClient> conns)
    {
        // Lấy spawnPoints từ BattleManager
        var battleManager = BattleManager.Instance;
        if (battleManager == null)
        {
            Debug.LogError("[Server] BattleManager.Instance is null khi spawn!");
            return;
        }

        battleManager.SpawnPlayers(conns);

        // Dọn dẹp
        matchPlayers.Remove(matchId);
        readyInMatch.Remove(matchId);
        pendingMatchId = null;

        Debug.Log($"[Server] Match {matchId} — tất cả player đã được spawn.");
    }

    // ══════════════════════════════════════════════════════════════
    // CLIENT CALLBACKS
    // ══════════════════════════════════════════════════════════════

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        NetworkClient.RegisterHandler<MsgMatchFound>
            (msg => OnMatchFound?.Invoke(msg.playerNames));
        NetworkClient.RegisterHandler<MsgMatchCancelled>
            (msg => OnMatchCancelled?.Invoke());
        NetworkClient.RegisterHandler<MsgPartyCreated>
            (msg => OnPartyCreated?.Invoke(msg.partyCode, msg.myConnId));
        NetworkClient.RegisterHandler<MsgPartyUpdated>
            (msg => OnPartyUpdated?.Invoke(msg.members, msg.leaderConnId, msg.myConnId));
        NetworkClient.RegisterHandler<MsgPartyKicked>
            (msg => OnPartyKicked?.Invoke());
        NetworkClient.RegisterHandler<MsgPartyStarting>
            (msg => OnPartyStarting?.Invoke());
        NetworkClient.RegisterHandler<MsgError>
            (msg => OnError?.Invoke(msg.message));

        OnConnected?.Invoke();
    }

    // Mirror's NetworkManager.OnStartClient() tự động register playerPrefab.
    // KHÔNG cần gọi lại RegisterPrefab thủ công (gây lỗi duplicate).

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        OnDisconnected?.Invoke();
    }

    // ══════════════════════════════════════════════════════════════
    // SERVER HANDLERS — Matchmaking
    // ══════════════════════════════════════════════════════════════

    private void OnReceiveJoinQueue(NetworkConnectionToClient conn, MsgJoinQueue msg)
    {
        playerNames[conn] = msg.playerName;
        JoinQueue(conn, msg.playerName);
    }

    private void OnReceiveCancelMatch(NetworkConnectionToClient conn, MsgCancelMatch msg)
    {
        CancelPendingMatch(conn);
    }

    // ══════════════════════════════════════════════════════════════
    // SERVER HANDLERS — Party
    // ══════════════════════════════════════════════════════════════

    private void OnReceiveCreateParty(NetworkConnectionToClient conn, MsgCreateParty msg)
    {
        playerNames[conn] = msg.playerName;
        string code = CreateParty(conn);
        conn.Send(new MsgPartyCreated { partyCode = code, myConnId = (int)conn.connectionId });
        BroadcastPartyUpdate(playerParty[conn]);
    }

    private void OnReceiveJoinParty(NetworkConnectionToClient conn, MsgJoinParty msg)
    {
        playerNames[conn] = msg.playerName;
        bool ok = JoinParty(msg.partyCode, conn);
        if (!ok)
            conn.Send(new MsgError { message = $"Không tìm thấy phòng [{msg.partyCode}]!" });
    }

    private void OnReceiveKickMember(NetworkConnectionToClient conn, MsgKickMember msg)
    {
        if (!playerParty.TryGetValue(conn, out var partyId)) return;
        if (!parties.TryGetValue(partyId, out var members)) return;
        if (members[0] != conn) return;

        NetworkConnectionToClient target = null;
        foreach (var c in members)
            if ((int)c.connectionId == msg.targetConnId) { target = c; break; }
        if (target == null) return;

        target.Send(new MsgPartyKicked());
        HandleLeaveParty(target);
        BroadcastPartyUpdate(partyId);
    }

    private void OnReceiveStartParty(NetworkConnectionToClient conn, MsgStartParty msg)
    {
        if (!playerParty.TryGetValue(conn, out var partyId)) return;
        if (!parties.TryGetValue(partyId, out var members)) return;
        if (members[0] != conn) return;

        var players = new List<NetworkConnectionToClient>(members);
        foreach (var c in players)
        {
            c.Send(new MsgPartyStarting());
            playerParty.Remove(c);
        }
        parties.Remove(partyId);

        string matchId = Guid.NewGuid().ToString()[..8];
        matchPlayers[matchId] = players;
        pendingMatchId = matchId;

        Debug.Log($"[Server] Party {partyId} → {battleScene} (matchId={matchId})");
        ServerChangeScene(battleScene);
    }

    private void OnReceiveLeaveParty(NetworkConnectionToClient conn, MsgLeaveParty msg)
    {
        if (!playerParty.TryGetValue(conn, out var partyId)) return;
        HandleLeaveParty(conn);
        if (parties.ContainsKey(partyId))
            BroadcastPartyUpdate(partyId);
    }

    // ══════════════════════════════════════════════════════════════
    // MATCHMAKING QUEUE
    // ══════════════════════════════════════════════════════════════

    private void JoinQueue(NetworkConnectionToClient conn, string playerName)
    {
        if (matchmakingQueue.Contains(conn)) return;
        matchmakingQueue.Enqueue(conn);
        queueNames[conn] = playerName;
        Debug.Log($"[Queue] {playerName} joined. {matchmakingQueue.Count}/{playersPerMatch}");
        TryStartMatch();
    }

    private void TryStartMatch()
    {
        while (matchmakingQueue.Count >= playersPerMatch)
        {
            var players = new List<NetworkConnectionToClient>();
            var names = new List<string>();
            for (int i = 0; i < playersPerMatch; i++)
            {
                var conn = matchmakingQueue.Dequeue();
                players.Add(conn);
                names.Add(queueNames[conn]);
                queueNames.Remove(conn);
            }
            StartPendingMatch(players, names);
        }
    }

    private void RemoveFromQueue(NetworkConnectionToClient conn)
    {
        var temp = new Queue<NetworkConnectionToClient>();
        foreach (var c in matchmakingQueue)
            if (c != conn) temp.Enqueue(c);
        matchmakingQueue.Clear();
        foreach (var c in temp) matchmakingQueue.Enqueue(c);
        queueNames.Remove(conn);
    }

    // ══════════════════════════════════════════════════════════════
    // PENDING MATCH
    // ══════════════════════════════════════════════════════════════

    private void StartPendingMatch(List<NetworkConnectionToClient> players, List<string> names)
    {
        string matchId = Guid.NewGuid().ToString()[..8];
        pendingMatches[matchId] = new List<NetworkConnectionToClient>(players);
        foreach (var conn in players) connToMatch[conn] = matchId;

        for (int i = 0; i < players.Count; i++)
        {
            var ordered = new string[names.Count];
            ordered[0] = names[i];
            int k = 1;
            for (int j = 0; j < names.Count; j++)
                if (j != i) ordered[k++] = names[j];
            players[i].Send(new MsgMatchFound { playerNames = ordered });
        }

        StartCoroutine(CountdownMatch(matchId, players));
    }

    private IEnumerator CountdownMatch(string matchId, List<NetworkConnectionToClient> players)
    {
        yield return new WaitForSeconds(countdownSeconds);
        if (!pendingMatches.ContainsKey(matchId)) yield break;

        foreach (var conn in players) connToMatch.Remove(conn);
        pendingMatches.Remove(matchId);

        string newMatchId = Guid.NewGuid().ToString()[..8];
        matchPlayers[newMatchId] = players;
        pendingMatchId = newMatchId;

        Debug.Log($"[Match] {newMatchId} → {battleScene}");
        ServerChangeScene(battleScene);
    }

    private void CancelPendingMatch(NetworkConnectionToClient conn)
    {
        if (!connToMatch.TryGetValue(conn, out var matchId)) return;
        if (!pendingMatches.TryGetValue(matchId, out var players)) return;

        var snapshot = new List<NetworkConnectionToClient>(players);
        foreach (var c in snapshot) connToMatch.Remove(c);
        pendingMatches.Remove(matchId);

        foreach (var c in snapshot)
        {
            if (c == conn) continue;
            c.Send(new MsgMatchCancelled());
            string name = playerNames.ContainsKey(c) ? playerNames[c] : $"Player_{c.connectionId}";
            JoinQueue(c, name);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // PARTY SYSTEM
    // ══════════════════════════════════════════════════════════════

    private string CreateParty(NetworkConnectionToClient leader)
    {
        HandleLeaveParty(leader);
        string partyId = GeneratePartyCode();
        parties[partyId] = new List<NetworkConnectionToClient> { leader };
        playerParty[leader] = partyId;
        return partyId;
    }

    private bool JoinParty(string partyId, NetworkConnectionToClient conn)
    {
        partyId = partyId.ToUpper().Trim();
        if (!parties.TryGetValue(partyId, out var members)) return false;
        if (members.Count >= playersPerMatch) return false;
        if (members.Contains(conn)) return false;

        HandleLeaveParty(conn);
        members.Add(conn);
        playerParty[conn] = partyId;
        BroadcastPartyUpdate(partyId);
        return true;
    }

    private void HandleLeaveParty(NetworkConnectionToClient conn)
    {
        if (!playerParty.TryGetValue(conn, out var partyId)) return;
        playerParty.Remove(conn);
        if (!parties.TryGetValue(partyId, out var members)) return;
        members.Remove(conn);
        if (members.Count == 0) parties.Remove(partyId);
    }

    private void BroadcastPartyUpdate(string partyId)
    {
        if (!parties.TryGetValue(partyId, out var members)) return;
        int leaderConnId = (int)members[0].connectionId;
        var data = new PartyMemberData[members.Count];
        for (int i = 0; i < members.Count; i++)
            data[i] = new PartyMemberData
            {
                connId = (int)members[i].connectionId,
                playerName = playerNames.ContainsKey(members[i])
                             ? playerNames[members[i]]
                             : $"Player_{members[i].connectionId}"
            };
        foreach (var c in members)
            c.Send(new MsgPartyUpdated
            {
                members = data,
                leaderConnId = leaderConnId,
                myConnId = (int)c.connectionId
            });
    }

    private string GeneratePartyCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        string code;
        do
        {
            code = "";
            for (int i = 0; i < 6; i++)
                code += chars[UnityEngine.Random.Range(0, chars.Length)];
        }
        while (parties.ContainsKey(code));
        return code;
    }
}
