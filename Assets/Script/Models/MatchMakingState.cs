using Colyseus.Schema;

// ==================== QueueEntry ====================
public class QueueEntry : Schema
{
[Type(0, "string")] public string sessionId = "";
[Type(1, "string")] public string username = "";
[Type(2, "number")] public float level = 1;
[Type(3, "float64")] public double joinedAt = 0;
}

// ==================== MmPlayer ====================
public class MmPlayer : Schema
{
[Type(0, "string")] public string sessionId = "";
[Type(1, "string")] public string username = "";
[Type(2, "number")] public float level = 1;
[Type(3, "string")] public string status = "idle"; 
[Type(4, "string")] public string partyId = ""; 
}

// ==================== PartyMember ====================
public class PartyMember : Schema
{
[Type(0, "string")] public string sessionId = "";
[Type(1, "string")] public string username = "";
[Type(2, "number")] public float level = 1;
[Type(3, "boolean")] public bool isLeader = false;
}

// ==================== Party ====================
public class Party : Schema
{
[Type(0, "string")] public string id = "";
[Type(1, "string")] public string inviteCode = "";
[Type(2, "string")] public string leaderId = "";
[Type(3, "array", typeof(ArraySchema<PartyMember>))] public ArraySchema<PartyMember> members = new ArraySchema<PartyMember>();
[Type(4, "float64")] public double createdAt = 0;
[Type(5, "string")] public string status = "waiting"; 
[Type(6, "number")] public float maxMembers = 2; 
}

// ==================== MatchMakingState (FIXED ORDER) ====================
public class MatchMakingState : Schema
{
[Type(0, "map", typeof(MapSchema<MmPlayer>))]
public MapSchema<MmPlayer> players = new MapSchema<MmPlayer>();

[Type(1, "array", typeof(ArraySchema<QueueEntry>))]
public ArraySchema<QueueEntry> queue = new ArraySchema<QueueEntry>();

[Type(2, "map", typeof(MapSchema<Party>))]
public MapSchema<Party> parties = new MapSchema<Party>();

[Type(3, "number")] 
public float onlineCount = 0;

[Type(4, "number")] 
public float queueCount = 0;

[Type(5, "number")] 
public float partyCount = 0;
}