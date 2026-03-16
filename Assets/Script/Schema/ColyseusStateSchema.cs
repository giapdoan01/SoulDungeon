// ColyseusStateSchema.cs
// C# mirror của server MatchmakingState.js — phải khớp ĐÚNG thứ tự field với server.
// Dùng để Colyseus SDK decode state patches, tránh "refId not found" errors.
//
// Field index dựa theo thứ tự type() declaration bên server:
//   QueueEntry  : 0=sessionId, 1=username, 2=joinedAt
//   MmPlayer    : 0=sessionId, 1=username, 2=status, 3=partyId
//   PartyMember : 0=sessionId, 1=username, 2=isLeader
//   Party       : 0=id, 1=inviteCode, 2=leaderId, 3=members, 4=createdAt, 5=status, 6=maxMembers
//   MmState     : 0=players, 1=queue, 2=parties, 3=onlineCount, 4=queueCount, 5=partyCount
using Colyseus.Schema;

public partial class QueueEntrySchema : Schema
{
    [Type(0, "string")]  public string sessionId = default;
    [Type(1, "string")]  public string username  = default;
    [Type(2, "float64")] public double joinedAt  = default;
}

public partial class MmPlayerSchema : Schema
{
    [Type(0, "string")] public string sessionId = default;
    [Type(1, "string")] public string username  = default;
    [Type(2, "string")] public string status    = default;
    [Type(3, "string")] public string partyId   = default;
}

public partial class PartyMemberSchema : Schema
{
    [Type(0, "string")]  public string sessionId = default;
    [Type(1, "string")]  public string username  = default;
    [Type(2, "boolean")] public bool   isLeader  = default;
}

public partial class PartySchema : Schema
{
    [Type(0, "string")]  public string id         = default;
    [Type(1, "string")]  public string inviteCode = default;
    [Type(2, "string")]  public string leaderId   = default;
    [Type(3, "array", typeof(ArraySchema<PartyMemberSchema>))]
    public ArraySchema<PartyMemberSchema> members = new ArraySchema<PartyMemberSchema>();
    [Type(4, "float64")] public double createdAt  = default;
    [Type(5, "string")]  public string status     = default;
    [Type(6, "number")]  public float  maxMembers = default;
}

public partial class MatchmakingStateSchema : Schema
{
    [Type(0, "map", typeof(MapSchema<MmPlayerSchema>))]
    public MapSchema<MmPlayerSchema>  players    = new MapSchema<MmPlayerSchema>();

    [Type(1, "array", typeof(ArraySchema<QueueEntrySchema>))]
    public ArraySchema<QueueEntrySchema> queue   = new ArraySchema<QueueEntrySchema>();

    [Type(2, "map", typeof(MapSchema<PartySchema>))]
    public MapSchema<PartySchema>     parties    = new MapSchema<PartySchema>();

    [Type(3, "number")] public float onlineCount = default;
    [Type(4, "number")] public float queueCount  = default;
    [Type(5, "number")] public float partyCount  = default;
}
