using Mirror;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class MyNetworkManager : NetworkManager
{
    public static MyNetworkManager Instance { get; private set; }

    [Header("Match Settings")]
    public string battleScene      = "BattleScene";
    public int    playersPerMatch  = 2;
    public float  countdownSeconds = 15f;

    // ══════════════════════════════════════════════════════════════
    // EVENTS — UI subscribe vào đây
    // ══════════════════════════════════════════════════════════════

    public event Action                          OnConnected;
    public event Action                          OnDisconnected;
    public event Action<string[]>                OnMatchFound;
    public event Action                          OnMatchCancelled;
    public event Action<string, int>             OnPartyCreated;   // (partyCode, myConnId)
    public event Action<PartyMemberData[], int, int> OnPartyUpdated; // (members, leaderConnId, myConnId)
    public event Action                          OnPartyKicked;
    public event Action                          OnPartyStarting;
    public event Action<string>                  OnError;

    // ── Server State ──────────────────────────────────────────────
    private readonly Queue<NetworkConnectionToClient>                    matchmakingQueue = new();
    private readonly Dictionary<NetworkConnectionToClient, string>       queueNames       = new();
    private readonly Dictionary<string, List<NetworkConnectionToClient>> pendingMatches   = new();
    private readonly Dictionary<NetworkConnectionToClient, string>       connToMatch      = new();
    private readonly Dictionary<string, List<NetworkConnectionToClient>> parties          = new();
    private readonly Dictionary<NetworkConnectionToClient, string>       playerParty      = new();
    private readonly Dictionary<NetworkConnectionToClient, string>       playerNames      = new();

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
        NetworkServer.RegisterHandler<MsgJoinQueue>  (OnReceiveJoinQueue);
        NetworkServer.RegisterHandler<MsgCancelMatch>(OnReceiveCancelMatch);
        NetworkServer.RegisterHandler<MsgCreateParty>(OnReceiveCreateParty);
        NetworkServer.RegisterHandler<MsgJoinParty>  (OnReceiveJoinParty);
        NetworkServer.RegisterHandler<MsgKickMember> (OnReceiveKickMember);
        NetworkServer.RegisterHandler<MsgStartParty> (OnReceiveStartParty);
        NetworkServer.RegisterHandler<MsgLeaveParty> (OnReceiveLeaveParty);
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

    // ══════════════════════════════════════════════════════════════
    // CLIENT CALLBACKS → Invoke Events
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

        // Gửi MsgPartyCreated kèm connId của chính leader
        conn.Send(new MsgPartyCreated
        {
            partyCode = code,
            myConnId  = (int)conn.connectionId
        });

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
        if (!playerParty.ContainsKey(conn)) return;
        string partyId = playerParty[conn];
        if (!parties.ContainsKey(partyId))  return;
        if (parties[partyId][0] != conn)    return; // chỉ leader

        // Tìm target theo connId
        NetworkConnectionToClient target = null;
        foreach (var c in parties[partyId])
        {
            if ((int)c.connectionId == msg.targetConnId)
            {
                target = c;
                break;
            }
        }
        if (target == null) return;

        target.Send(new MsgPartyKicked());
        HandleLeaveParty(target);
        BroadcastPartyUpdate(partyId);
        Debug.Log($"[Server] Kicked connId={msg.targetConnId} from party {partyId}");
    }

    private void OnReceiveStartParty(NetworkConnectionToClient conn, MsgStartParty msg)
    {
        if (!playerParty.ContainsKey(conn)) return;
        string partyId = playerParty[conn];
        if (!parties.ContainsKey(partyId))  return;
        if (parties[partyId][0] != conn)    return; // chỉ leader

        var members = new List<NetworkConnectionToClient>(parties[partyId]);

        // Báo tất cả chuẩn bị vào trận
        foreach (var c in members)
        {
            c.Send(new MsgPartyStarting());
            playerParty.Remove(c);
        }
        parties.Remove(partyId);

        Debug.Log($"[Server] Party {partyId} → {battleScene}");
        ServerChangeScene(battleScene);
    }

    private void OnReceiveLeaveParty(NetworkConnectionToClient conn, MsgLeaveParty msg)
    {
        if (!playerParty.ContainsKey(conn)) return;
        string partyId = playerParty[conn];
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
            var names   = new List<string>();

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

        // Mỗi client nhận: index 0 = tên mình, index 1+ = đối thủ
        for (int i = 0; i < players.Count; i++)
        {
            var ordered = new string[names.Count];
            ordered[0] = names[i];
            int k = 1;
            for (int j = 0; j < names.Count; j++)
                if (j != i) ordered[k++] = names[j];

            players[i].Send(new MsgMatchFound { playerNames = ordered });
        }

        StartCoroutine(CountdownMatch(matchId));
    }

    private IEnumerator CountdownMatch(string matchId)
    {
        yield return new WaitForSeconds(countdownSeconds);
        if (!pendingMatches.ContainsKey(matchId)) yield break;

        var players = pendingMatches[matchId];
        foreach (var conn in players) connToMatch.Remove(conn);
        pendingMatches.Remove(matchId);

        Debug.Log($"[Match] {matchId} → {battleScene}");
        ServerChangeScene(battleScene);
    }

    private void CancelPendingMatch(NetworkConnectionToClient conn)
    {
        if (!connToMatch.ContainsKey(conn)) return;
        string matchId = connToMatch[conn];
        if (!pendingMatches.ContainsKey(matchId)) return;

        var players = new List<NetworkConnectionToClient>(pendingMatches[matchId]);
        foreach (var c in players) connToMatch.Remove(c);
        pendingMatches.Remove(matchId);

        foreach (var c in players)
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
        string partyId      = GeneratePartyCode();
        parties[partyId]    = new List<NetworkConnectionToClient> { leader };
        playerParty[leader] = partyId;
        return partyId;
    }

    private bool JoinParty(string partyId, NetworkConnectionToClient conn)
    {
        partyId = partyId.ToUpper().Trim();
        if (!parties.ContainsKey(partyId))             return false;
        if (parties[partyId].Count >= playersPerMatch) return false;
        if (parties[partyId].Contains(conn))           return false;

        HandleLeaveParty(conn);
        parties[partyId].Add(conn);
        playerParty[conn] = partyId;

        BroadcastPartyUpdate(partyId);
        return true;
    }

    private void HandleLeaveParty(NetworkConnectionToClient conn)
    {
        if (!playerParty.ContainsKey(conn)) return;
        string partyId = playerParty[conn];
        playerParty.Remove(conn);
        if (!parties.ContainsKey(partyId)) return;
        parties[partyId].Remove(conn);
        if (parties[partyId].Count == 0) parties.Remove(partyId);
    }

    private void BroadcastPartyUpdate(string partyId)
    {
        if (!parties.ContainsKey(partyId)) return;

        var members      = parties[partyId];
        int leaderConnId = (int)members[0].connectionId;

        // Build data array
        var data = new PartyMemberData[members.Count];
        for (int i = 0; i < members.Count; i++)
            data[i] = new PartyMemberData
            {
                connId     = (int)members[i].connectionId,
                playerName = playerNames.ContainsKey(members[i])
                             ? playerNames[members[i]]
                             : $"Player_{members[i].connectionId}"
            };

        // ✅ Gửi riêng từng người — myConnId là connId của chính họ
        foreach (var c in members)
        {
            c.Send(new MsgPartyUpdated
            {
                members      = data,
                leaderConnId = leaderConnId,
                myConnId     = (int)c.connectionId  // mỗi người nhận đúng connId của mình
            });
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

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
