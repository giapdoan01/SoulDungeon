using Colyseus.Schema;

public class MatchmakingState : Schema
{
    [Type(0, "map", typeof(MapSchema<MmPlayer>))]
    public MapSchema<MmPlayer> players = new MapSchema<MmPlayer>();

    [Type(1, "map", typeof(MapSchema<QueueEntry>))]
    public MapSchema<QueueEntry> queue = new MapSchema<QueueEntry>();

    [Type(2, "map", typeof(MapSchema<Party>))]
    public MapSchema<Party> parties = new MapSchema<Party>();

    [Type(3, "number")]
    public int onlineCount = 0;

    [Type(4, "number")]
    public int queueCount = 0;
}

public class MmPlayer : Schema
{
    [Type(0, "string")]
    public string sessionId = "";

    [Type(1, "string")]
    public string name = "";

    [Type(2, "number")]
    public int level = 1;

    [Type(3, "string")]
    public string status = "idle";

    [Type(4, "number")]
    public int lastPingMs = 0;
}

public class QueueEntry : Schema
{
    [Type(0, "string")]
    public string sessionId = "";

    [Type(1, "number")]
    public long joinedAt = 0;

    [Type(2, "number")]
    public int mmr = 0;
}

public class Party : Schema
{
    [Type(0, "string")]
    public string code = "";

    [Type(1, "string")]
    public string hostId = "";

    [Type(2, "array", typeof(ArraySchema<string>))]
    public ArraySchema<string> members = new ArraySchema<string>();

    [Type(3, "number")]
    public int maxPlayers = 4;

    [Type(4, "number")]
    public long createdAt = 0;

    [Type(5, "boolean")]
    public bool locked = false;
}
