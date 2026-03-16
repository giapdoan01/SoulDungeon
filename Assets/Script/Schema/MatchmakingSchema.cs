// MatchmakingSchema.cs
// Tất cả data classes cho messages trao đổi với MatchmakingRoom (Colyseus)
using System;

#region Shared

[Serializable]
public class PlayerInfo
{
    public string sessionId;
    public string username;
}

[Serializable]
public class PartyMemberInfo
{
    public string sessionId;
    public string username;
    public bool isLeader;
}

[Serializable]
public class PartyData
{
    public string id;
    public string inviteCode;
    public string leaderId;
    public PartyMemberInfo[] members;
    public string status;
    public int maxMembers;
    public double createdAt;
}

// Dùng cho messages không có body (queueLeft, partyLeft)
[Serializable]
public class EmptyMsg { }

#endregion

#region Welcome

[Serializable]
public class WelcomeMsg
{
    public string message;
    public string sessionId;
}

#endregion

#region Queue Messages

[Serializable]
public class QueueJoinedMsg
{
    public int position;
    public float estimatedWait;
}

[Serializable]
public class QueueErrorMsg
{
    public string message;
}

#endregion

#region Party Messages

[Serializable]
public class PartyCreatedMsg
{
    public bool success;
    public string partyId;
    public string inviteCode;
    public PartyData party;
}

[Serializable]
public class PartyErrorMsg
{
    public string message;
}

[Serializable]
public class JoinPartyResultMsg
{
    public bool success;
    public string reason;
    public PartyData party;
}

[Serializable]
public class LeadershipTransferredMsg
{
    public string message;
}

[Serializable]
public class KickResultMsg
{
    public bool success;
    public string reason;
}

[Serializable]
public class KickedMsg
{
    public string reason;
}

[Serializable]
public class StartMatchResultMsg
{
    public bool success;
    public string reason;
}

[Serializable]
public class PartyExpiredMsg
{
    public string message;
}

#endregion

#region Match Messages

[Serializable]
public class MatchFoundMsg
{
    public string matchId;
    public int countdown;
    public PlayerInfo[] players;
    public PlayerInfo[] opponents;
    public bool isPartyMatch;
}

[Serializable]
public class MatchStartMsg
{
    public string matchId;
    public string roomId;
    public PlayerInfo[] players;
}

[Serializable]
public class MatchCancelledMsg
{
    public string reason;
    public string cancelledBy;
}

[Serializable]
public class CancelMatchResultMsg
{
    public bool success;
    public string reason;
}

[Serializable]
public class MatchErrorMsg
{
    public string reason;
}

#endregion
