// GameStateSchema.cs
// C# mirror của server GameState.js — field index phải khớp đúng server.
//
// PlayerGameState : 0=sessionId, 1=username, 2=characterIndex, 3=x, 4=y, 5=facing, 6=isReady
// GameState       : 0=players, 1=matchId, 2=status
//
// Message classes cho GameRoom cũng nằm ở đây.
using System;
using Colyseus.Schema;

// ─── Schema ──────────────────────────────────────────────────────────────────

public partial class PlayerGameStateSchema : Schema
{
    [Colyseus.Schema.Type(0, "string")]  public string sessionId       = default;
    [Colyseus.Schema.Type(1, "string")]  public string username        = default;
    [Colyseus.Schema.Type(2, "int32")]   public int    characterIndex  = default;
    [Colyseus.Schema.Type(3, "float32")] public float  x              = default;
    [Colyseus.Schema.Type(4, "float32")] public float  y              = default;
    [Colyseus.Schema.Type(5, "float32")] public float  facing         = default;
    [Colyseus.Schema.Type(6, "boolean")] public bool   isReady        = default;
}

public partial class GameStateSchema : Schema
{
    [Colyseus.Schema.Type(0, "map", typeof(MapSchema<PlayerGameStateSchema>))]
    public MapSchema<PlayerGameStateSchema> players = new MapSchema<PlayerGameStateSchema>();

    [Colyseus.Schema.Type(1, "string")] public string matchId = default;
    [Colyseus.Schema.Type(2, "string")] public string status  = default;
}

// ─── Message Data Classes ─────────────────────────────────────────────────────

[Serializable]
public class GameReadyMsg
{
    public string matchId;
    public string sessionId;  // GameRoom sessionId — dùng để tự identify trong state
}

[Serializable]
public class PlayerSnapshot
{
    public string sessionId;
    public string username;
    public int    characterIndex;
}

[Serializable]
public class AllReadyMsg
{
    public string           matchId;
    public PlayerSnapshot[] players;  // snapshot day du — spawn tu day, khong phu thuoc schema sync
}

[Serializable]
public class PlayerMovedMsg
{
    public string sessionId;
    public float  x;
    public float  y;
    public float  facing;
    public float  speed;   // 0 = dung (IDLE), >0 = dang di chuyen (MOVE)
}

[Serializable]
public class PlayerLeftGameMsg
{
    public string sessionId;
    public string username;
}
