using UnityEngine;
using Colyseus;
using System.Collections.Generic;

public class SimpleMatchmakingTest : MonoBehaviour
{
    private ColyseusClient client;
    private ColyseusRoom<MatchmakingState> room;
    private string statusText = "Initializing...";
    private bool isConnected = false;

    async void Start()
    {
        Debug.Log("🧪 Simple Matchmaking Test Started");
        statusText = "Connecting...";

        try
        {
            // ✅ CRITICAL FIX: Use ws:// not http://
            Debug.Log("🔌 Connecting to ws://localhost:3001");
            client = new ColyseusClient("ws://localhost:3001");
            statusText = "Client created";
            Debug.Log("✅ Client created");

            // Join matchmaking room
            Debug.Log("🚀 Joining matchmaking...");
            var options = new Dictionary<string, object>
            {
                { "name", "TestPlayer" },
                { "level", 5 }
            };

            room = await client.JoinOrCreate<MatchmakingState>("matchmaking", options);
            
            Debug.Log($"✅ Joined! RoomId: {room.RoomId}");
            Debug.Log($"✅ SessionId: {room.SessionId}");
            statusText = $"Connected to {room.RoomId}";
            isConnected = true;

            // Listen to state changes
            room.OnStateChange += (state, isFirst) =>
            {
                if (isFirst)
                {
                    Debug.Log("📊 Initial state received");
                }
                Debug.Log($"📊 State: Online={state.onlineCount}, Queue={state.queueCount}");
            };

            // Listen to messages
            room.OnMessage<WelcomeMessage>("welcome", (msg) =>
            {
                Debug.Log($"👋 Welcome: {msg.message}");
                Debug.Log($"   SessionId: {msg.sessionId}");
                Debug.Log($"   Online: {msg.onlineCount}, Queue: {msg.queueCount}");
                statusText = $"Welcome! Online: {msg.onlineCount}";
            });

            room.OnMessage<QueueJoinedMessage>("queue:joined", (msg) =>
            {
                Debug.Log($"🎯 Joined Queue!");
                Debug.Log($"   Position: {msg.position}");
                Debug.Log($"   Wait: {msg.estimatedWait}s");
                statusText = $"In Queue - Position: {msg.position}";
            });

            room.OnMessage<QueueLeftMessage>("queue:left", (msg) =>
            {
                Debug.Log($"🚪 Left Queue! Reason: {msg.reason}");
                statusText = "Left Queue";
            });

            room.OnMessage<ErrorMessage>("error", (msg) =>
            {
                Debug.LogError($"❌ Server Error: {msg.message}");
                statusText = $"Error: {msg.message}";
            });

            // Connection events
            room.OnLeave += (code) =>
            {
                Debug.Log($"🚪 Left room. Code: {code}");
                isConnected = false;
                statusText = "Disconnected";
            };

            room.OnError += (code, message) =>
            {
                Debug.LogError($"❌ Room Error [{code}]: {message}");
                statusText = $"Error: {message}";
            };

            Debug.Log("✅ All listeners registered");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Connection Error: {e.Message}");
            Debug.LogError($"Type: {e.GetType().Name}");
            Debug.LogError($"Stack: {e.StackTrace}");
            statusText = $"Error: {e.Message}";
            
            // Helpful hints
            if (e.Message.Contains("StringLiteral"))
            {
                Debug.LogError("⚠️ HINT: Schema mismatch! Check MatchmakingState.cs");
            }
            if (e.Message.Contains("refused"))
            {
                Debug.LogError("⚠️ HINT: Server not running? Check 'npm start'");
            }
            if (e.Message.Contains("404"))
            {
                Debug.LogError("⚠️ HINT: Room 'matchmaking' not defined on server!");
            }
        }
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        
        GUILayout.Label("🎮 MATCHMAKING TEST", new GUIStyle(GUI.skin.label) 
        { 
            fontSize = 16, 
            fontStyle = FontStyle.Bold 
        });

        GUILayout.Space(10);

        // Connection status
        if (isConnected)
        {
            GUILayout.Label("✅ CONNECTED", new GUIStyle(GUI.skin.label) 
            { 
                normal = { textColor = Color.green },
                fontSize = 14,
                fontStyle = FontStyle.Bold
            });
        }
        else
        {
            GUILayout.Label("❌ DISCONNECTED", new GUIStyle(GUI.skin.label) 
            { 
                normal = { textColor = Color.red },
                fontSize = 14,
                fontStyle = FontStyle.Bold
            });
        }

        GUILayout.Label($"Status: {statusText}", new GUIStyle(GUI.skin.label) 
        { 
            fontSize = 12,
            wordWrap = true
        });

        GUILayout.Space(20);

        // Buttons
        GUI.enabled = room != null;
        
        if (GUILayout.Button("JOIN QUEUE", GUILayout.Height(50)))
        {
            Debug.Log("📤 Sending: queue:join");
            room.Send("queue:join");
            statusText = "Joining queue...";
        }

        GUILayout.Space(10);

        if (GUILayout.Button("LEAVE QUEUE", GUILayout.Height(50)))
        {
            Debug.Log("📤 Sending: queue:leave");
            room.Send("queue:leave");
            statusText = "Leaving queue...";
        }

        GUILayout.Space(10);

        if (GUILayout.Button("DISCONNECT", GUILayout.Height(50)))
        {
            Debug.Log("🚪 Disconnecting...");
            room.Leave();
            room = null;
            isConnected = false;
            statusText = "Disconnected";
        }
        
        GUI.enabled = true;

        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        if (room != null)
        {
            Debug.Log("🚪 OnDestroy: Leaving room...");
            room.Leave();
        }
    }

    void OnApplicationQuit()
    {
        if (room != null)
        {
            Debug.Log("🚪 OnApplicationQuit: Leaving room...");
            room.Leave();
        }
    }
}

// Message classes
[System.Serializable]
public class WelcomeMessage
{
    public string message;
    public string sessionId;
    public int onlineCount;
    public int queueCount;
}

[System.Serializable]
public class QueueJoinedMessage
{
    public int position;
    public int estimatedWait;
}

[System.Serializable]
public class QueueLeftMessage
{
    public string reason;
}

[System.Serializable]
public class ErrorMessage
{
    public string message;
}
