using Mirror;

// ── Client → Server ───────────────────────────────────────────────
public struct MsgJoinQueue   : NetworkMessage { public string playerName; }
public struct MsgCancelMatch : NetworkMessage { }
public struct MsgCreateParty : NetworkMessage { public string playerName; }
public struct MsgJoinParty   : NetworkMessage { public string partyCode; public string playerName; }
public struct MsgKickMember  : NetworkMessage { public int targetConnId; }
public struct MsgStartParty  : NetworkMessage { }
public struct MsgLeaveParty  : NetworkMessage { }

// ── Server → Client ───────────────────────────────────────────────
public struct MsgMatchFound     : NetworkMessage { public string[] playerNames; }
public struct MsgMatchCancelled : NetworkMessage { }

public struct MsgPartyCreated : NetworkMessage
{
    public string partyCode;
    public int    myConnId;   // server gửi connId của chính client đó
}

public struct MsgPartyUpdated : NetworkMessage
{
    public PartyMemberData[] members;
    public int leaderConnId;
    public int myConnId;      // server gửi connId của chính client đó
}

public struct MsgPartyKicked   : NetworkMessage { }
public struct MsgPartyStarting : NetworkMessage { }
public struct MsgError         : NetworkMessage { public string message; }

// ── Data ──────────────────────────────────────────────────────────
public struct PartyMemberData : NetworkMessage
{
    public int    connId;
    public string playerName;
}
