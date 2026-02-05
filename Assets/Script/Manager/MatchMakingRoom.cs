using UnityEngine;
using Colyseus;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class MatchMakingRoom : MonoBehaviour
{
  // ==================== SINGLETON ====================
  public static MatchMakingRoom Instance { get; private set; }

  // ==================== COLYSEUS ====================
  private ColyseusClient MMclient;
  private ColyseusRoom<MatchMakingState> MMRoom;
  public string WebSocketUrl = "ws://localhost:3001";

  // ==================== EVENTS ====================
  public event Action<int> OnOnlineCountChanged;
  public event Action<int> OnQueueCountChanged;
  public event Action<int> OnPartyCountChanged;
  public event Action<bool> OnConnectionChanged;
  
  // ==================== QUEUE EVENTS ====================
  public event Action<Dictionary<string, object>> OnQueueJoined;
  public event Action OnQueueLeft;
  public event Action<string> OnQueueError;
  
  // ==================== PARTY EVENTS (UPDATED) ====================
  public event Action<string, string> OnPartyCreated; // (partyId, inviteCode)
  public event Action<Dictionary<string, object>> OnJoinPartyResult; // Full party data
  public event Action<Dictionary<string, object>> OnPartyUpdate; // Party state updates
  public event Action OnPartyLeft;
  public event Action<string> OnPartyError; // (errorMessage)
  public event Action<string> OnKicked; // (reason)
  public event Action<bool> OnKickResult; // (success)
  public event Action OnLeadershipTransferred;
  public event Action<string> OnPartyExpired; // (message)
  public event Action<bool, string> OnStartMatchResult; // (success, gameRoomId/reason)
  
  // ==================== MATCH EVENTS ====================
  public event Action<Dictionary<string, object>> OnMatchFound; // (matchData)

  // ==================== PROPERTIES ====================
  public bool IsConnected => MMRoom != null;
  public string SessionId => MMRoom?.SessionId ?? "";
  
  //  UPDATED PARTY PROPERTIES
  public string CurrentPartyId { get; private set; } = "";
  public bool IsInParty => !string.IsNullOrEmpty(CurrentPartyId);
  public bool IsPartyLeader { get; private set; } = false;
  public string CurrentInviteCode { get; private set; } = "";
  public List<Dictionary<string, object>> PartyMembers { get; private set; } = new List<Dictionary<string, object>>();

  // ==================== UNITY LIFECYCLE ====================
  void Awake()
  {
      if (Instance == null)
      {
          Instance = this;
          DontDestroyOnLoad(gameObject);
      }
      else
      {
          Destroy(gameObject);
      }
  }

  async void Start()
  {
      await Connect();
  }

  void OnDestroy()
  {
      try
      {
          if (MMRoom != null)
          {
              MMRoom.OnStateChange -= HandleStateChange;
              UnregisterServerMessages();
              _ = MMRoom.Leave();
          }
      }
      catch (Exception e)
      {
          Debug.LogError($"[MM] Error during OnDestroy: {e.Message}\n{e.StackTrace}");
      }
  }

  // ==================== CONNECTION ====================
  async Task Connect()
  {
      try
      {
          Debug.Log("[MM] Connecting...");

          MMclient = new ColyseusClient(WebSocketUrl);

          var options = new Dictionary<string, object>{
              { "username", AuthManager.Instance?.CurrentUser?.username ?? "Player" },
              { "level", 1 } //  Default level 1
          };
          Debug.Log($"[MM] Join options: username={options["username"]}, level={options["level"]}");

          MMRoom = await MMclient.JoinOrCreate<MatchMakingState>("matchmaking", options);

          Debug.Log($"[MM] Connected! SessionId: {MMRoom.SessionId}");

          SetupRoomEvents();
          RegisterServerMessages();
          OnConnectionChanged?.Invoke(true);
      }
      catch (Exception e)
      {
          Debug.LogError($"[MM] Connect failed: {e.Message}\n{e.StackTrace}");
          OnConnectionChanged?.Invoke(false);
      }
  }

  // ==================== SETUP EVENTS ====================
  void SetupRoomEvents()
  {
      try
      {
          MMRoom.OnStateChange += HandleStateChange;

          MMRoom.OnLeave += (code) =>
          {
              Debug.Log($"[MM] Left room. Code: {code}");
              ResetPartyState();
              OnConnectionChanged?.Invoke(false);
          };

          MMRoom.OnError += (code, message) =>
          {
              Debug.LogError($"[MM] Error {code}: {message}");
          };
      }
      catch (Exception e)
      {
          Debug.LogError($"[MM] Error setting up room events: {e.Message}\n{e.StackTrace}");
      }
  }
  
  void RegisterServerMessages()
  {
      try
      {
          // Basic messages
          MMRoom.OnMessage<Dictionary<string, object>>("welcome", SafeHandleMessage(OnWelcomeMessage, "welcome"));
          
          // Queue messages
          MMRoom.OnMessage<Dictionary<string, object>>("queueJoined", SafeHandleMessage(OnQueueJoinedMessage, "queueJoined"));
          MMRoom.OnMessage<Dictionary<string, object>>("queueLeft", SafeHandleMessage(OnQueueLeftMessage, "queueLeft"));
          MMRoom.OnMessage<Dictionary<string, object>>("queueError", SafeHandleMessage(OnQueueErrorMessage, "queueError"));
          
          //  UPDATED PARTY MESSAGES
          MMRoom.OnMessage<Dictionary<string, object>>("partyCreated", SafeHandleMessage(OnPartyCreatedMessage, "partyCreated"));
          MMRoom.OnMessage<Dictionary<string, object>>("joinPartyResult", SafeHandleMessage(OnJoinPartyResultMessage, "joinPartyResult"));
          MMRoom.OnMessage<Dictionary<string, object>>("partyUpdate", SafeHandleMessage(OnPartyUpdateMessage, "partyUpdate"));
          MMRoom.OnMessage<Dictionary<string, object>>("partyLeft", SafeHandleMessage(OnPartyLeftMessage, "partyLeft"));
          MMRoom.OnMessage<Dictionary<string, object>>("partyError", SafeHandleMessage(OnPartyErrorMessage, "partyError"));
          MMRoom.OnMessage<Dictionary<string, object>>("kicked", SafeHandleMessage(OnKickedMessage, "kicked"));
          MMRoom.OnMessage<Dictionary<string, object>>("kickResult", SafeHandleMessage(OnKickResultMessage, "kickResult"));
          MMRoom.OnMessage<Dictionary<string, object>>("leadershipTransferred", SafeHandleMessage(OnLeadershipTransferredMessage, "leadershipTransferred"));
          MMRoom.OnMessage<Dictionary<string, object>>("partyExpired", SafeHandleMessage(OnPartyExpiredMessage, "partyExpired"));
          MMRoom.OnMessage<Dictionary<string, object>>("startMatchResult", SafeHandleMessage(OnStartMatchResultMessage, "startMatchResult"));
          
          // Match messages
          MMRoom.OnMessage<Dictionary<string, object>>("matchFound", SafeHandleMessage(OnMatchFoundMessage, "matchFound"));
      }
      catch (Exception e)
      {
          Debug.LogError($"[MM] Error registering server messages: {e.Message}\n{e.StackTrace}");
      }
  }
  
  // Helper để bọc các message handler với xử lý lỗi
  private Action<Dictionary<string, object>> SafeHandleMessage(
      Action<Dictionary<string, object>> handler, 
      string messageType)
  {
      return (message) => {
          try {
              Debug.Log($"[MM] Received {messageType} message");
              handler(message);
          } catch (Exception e) {
              Debug.LogError($"[MM] Error handling {messageType} message: {e.Message}\n{e.StackTrace}");
              LogMessageContents(message, messageType);
          }
      };
  }
  
  void LogMessageContents(Dictionary<string, object> message, string messageType)
  {
      if (message != null) {
          Debug.LogError($"[MM] {messageType} message content keys: {string.Join(", ", message.Keys)}");
          foreach (var key in message.Keys) {
              try {
                  var value = message[key];
                  Debug.LogError($"[MM] - {key}: {value} (Type: {value?.GetType().Name ?? "null"})");
              } catch {
                  Debug.LogError($"[MM] - {key}: <Error accessing value>");
              }
          }
      } else {
          Debug.LogError($"[MM] {messageType} message is null");
      }
  }
  
  void UnregisterServerMessages()
  {
      // Colyseus handles this automatically
  }

  // ==================== SERVER MESSAGE HANDLERS ====================
  
  void OnWelcomeMessage(Dictionary<string, object> message)
  {
      if (message.TryGetValue("message", out object welcomeMsg))
      {
          Debug.Log($"[MM] Received welcome: {welcomeMsg}");
      }
  }
  
  // ==================== QUEUE MESSAGE HANDLERS ====================
  
  void OnQueueJoinedMessage(Dictionary<string, object> message)
  {
      Debug.Log("[MM] Joined queue");
      OnQueueJoined?.Invoke(message);
  }
  
  void OnQueueLeftMessage(Dictionary<string, object> message)
  {
      Debug.Log("[MM] Left queue");
      OnQueueLeft?.Invoke();
  }
  
  void OnQueueErrorMessage(Dictionary<string, object> message)
  {
      string errorMsg = GetStringValue(message, "message", "Unknown queue error");
      Debug.LogError($"[MM] Queue error: {errorMsg}");
      OnQueueError?.Invoke(errorMsg);
  }
  
  // ==================== PARTY MESSAGE HANDLERS (UPDATED) ====================
  
  void OnPartyCreatedMessage(Dictionary<string, object> message)
  {
      try {
          bool success = GetBoolValue(message, "success", false);
          if (!success) {
              Debug.LogError("[MM] Failed to create party");
              return;
          }
          
          string partyId = GetStringValue(message, "partyId", "");
          string inviteCode = GetStringValue(message, "inviteCode", "");
          
          if (string.IsNullOrEmpty(partyId) || string.IsNullOrEmpty(inviteCode)) {
              Debug.LogError("[MM] Invalid party created data");
              return;
          }
          
          //  UPDATE STATE
          CurrentPartyId = partyId;
          CurrentInviteCode = inviteCode;
          IsPartyLeader = true;
          
          // Update party members from party data
          if (message.TryGetValue("party", out object partyObj) && partyObj is Dictionary<string, object> partyData) {
              UpdatePartyMembers(partyData);
          }
          
          Debug.Log($"[MM] Party created. ID: {partyId}, Invite Code: {inviteCode}");
          OnPartyCreated?.Invoke(partyId, inviteCode);
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error in OnPartyCreatedMessage: {e.Message}");
      }
  }
  
  void OnJoinPartyResultMessage(Dictionary<string, object> message)
  {
      try {
          bool success = GetBoolValue(message, "success", false);
          
          if (success) {
              if (message.TryGetValue("party", out object partyObj) && partyObj is Dictionary<string, object> partyData) {
                  string partyId = GetStringValue(partyData, "id", "");
                  string inviteCode = GetStringValue(partyData, "inviteCode", "");
                  string leaderId = GetStringValue(partyData, "leaderId", "");
                  
                  //  UPDATE STATE
                  CurrentPartyId = partyId;
                  CurrentInviteCode = inviteCode;
                  IsPartyLeader = (leaderId == SessionId);
                  
                  UpdatePartyMembers(partyData);
                  
                  Debug.Log($"[MM] Joined party. ID: {partyId}, IsLeader: {IsPartyLeader}");
              }
          } else {
              string reason = GetStringValue(message, "reason", "Unknown error");
              Debug.LogError($"[MM] Failed to join party: {reason}");
          }
          
          OnJoinPartyResult?.Invoke(message);
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error in OnJoinPartyResultMessage: {e.Message}");
      }
  }
  
  void OnPartyUpdateMessage(Dictionary<string, object> message)
  {
      try {
          string partyId = GetStringValue(message, "id", "");
          if (partyId == CurrentPartyId) {
              string leaderId = GetStringValue(message, "leaderId", "");
              IsPartyLeader = (leaderId == SessionId);
              
              UpdatePartyMembers(message);
              
              Debug.Log($"[MM] Party updated. Members: {PartyMembers.Count}, IsLeader: {IsPartyLeader}");
          }
          
          OnPartyUpdate?.Invoke(message);
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error in OnPartyUpdateMessage: {e.Message}");
      }
  }
  
  void OnPartyLeftMessage(Dictionary<string, object> message)
  {
      Debug.Log("[MM] Left party");
      ResetPartyState();
      OnPartyLeft?.Invoke();
  }
  
  void OnPartyErrorMessage(Dictionary<string, object> message)
  {
      string errorMsg = GetStringValue(message, "message", "Unknown party error");
      Debug.LogError($"[MM] Party error: {errorMsg}");
      OnPartyError?.Invoke(errorMsg);
  }
  
  void OnKickedMessage(Dictionary<string, object> message)
  {
      string reason = GetStringValue(message, "reason", "Unknown reason");
      Debug.LogWarning($"[MM] You were kicked: {reason}");
      ResetPartyState();
      OnKicked?.Invoke(reason);
  }
  
  void OnKickResultMessage(Dictionary<string, object> message)
  {
      bool success = GetBoolValue(message, "success", false);
      Debug.Log($"[MM] Kick result: {(success ? "Success" : "Failed")}");
      OnKickResult?.Invoke(success);
  }
  
  void OnLeadershipTransferredMessage(Dictionary<string, object> message)
  {
      string msg = GetStringValue(message, "message", "You are now the party leader!");
      Debug.Log($"[MM] Leadership transferred: {msg}");
      IsPartyLeader = true;
      OnLeadershipTransferred?.Invoke();
  }
  
  void OnPartyExpiredMessage(Dictionary<string, object> message)
  {
      string msg = GetStringValue(message, "message", "Party expired");
      Debug.LogWarning($"[MM] Party expired: {msg}");
      ResetPartyState();
      OnPartyExpired?.Invoke(msg);
  }
  
  void OnStartMatchResultMessage(Dictionary<string, object> message)
  {
      bool success = GetBoolValue(message, "success", false);
      
      if (success) {
          string gameRoomId = GetStringValue(message, "gameRoomId", "");
          Debug.Log($"[MM] Match starting! GameRoom: {gameRoomId}");
          OnStartMatchResult?.Invoke(true, gameRoomId);
      } else {
          string reason = GetStringValue(message, "reason", "Unknown error");
          Debug.LogError($"[MM] Failed to start match: {reason}");
          OnStartMatchResult?.Invoke(false, reason);
      }
  }
  
  // ==================== MATCH MESSAGE HANDLERS ====================
  
  void OnMatchFoundMessage(Dictionary<string, object> message)
  {
      Debug.Log("[MM] Match found!");
      
      try {
          bool isPartyMatch = GetBoolValue(message, "isPartyMatch", false);
          string gameRoomId = GetStringValue(message, "gameRoomId", "");
          
          if (isPartyMatch) {
              Debug.Log($"[MM] Party match found! GameRoom: {gameRoomId}");
          } else {
              Debug.Log($"[MM] Queue match found! GameRoom: {gameRoomId}");
          }
          
          OnMatchFound?.Invoke(message);
      } 
      catch (Exception e) {
          Debug.LogError($"[MM] Error processing match data: {e.Message}\n{e.StackTrace}");
      }
  }

  // ==================== STATE CHANGE HANDLER ====================
  void HandleStateChange(MatchMakingState state, bool isFirstState)
  {
      try {
          int online = (int)state.onlineCount;
          int queue = (int)state.queueCount;
          int party = (int)state.partyCount;

          Debug.Log($"[MM] State changed - Online: {online}, Queue: {queue}, Parties: {party}");

          OnOnlineCountChanged?.Invoke(online);
          OnQueueCountChanged?.Invoke(queue);
          OnPartyCountChanged?.Invoke(party);
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error handling state change: {e.Message}\n{e.StackTrace}");
      }
  }

  // ==================== HELPER METHODS ====================
  
  void ResetPartyState()
  {
      CurrentPartyId = "";
      CurrentInviteCode = "";
      IsPartyLeader = false;
      PartyMembers.Clear();
  }
  
  void UpdatePartyMembers(Dictionary<string, object> partyData)
  {
      PartyMembers.Clear();
      
      if (partyData.TryGetValue("members", out object membersObj)) {
          if (membersObj is List<object> membersList) {
              foreach (var memberObj in membersList) {
                  if (memberObj is Dictionary<string, object> memberData) {
                      PartyMembers.Add(memberData);
                  }
              }
          }
      }
  }
  
  string GetStringValue(Dictionary<string, object> dict, string key, string defaultValue = "")
  {
      if (dict.TryGetValue(key, out object value) && value is string stringValue) {
          return stringValue;
      }
      return defaultValue;
  }
  
  bool GetBoolValue(Dictionary<string, object> dict, string key, bool defaultValue = false)
  {
      if (dict.TryGetValue(key, out object value) && value is bool boolValue) {
          return boolValue;
      }
      return defaultValue;
  }
  
  int GetIntValue(Dictionary<string, object> dict, string key, int defaultValue = 0)
  {
      if (dict.TryGetValue(key, out object value)) {
          if (value is int intValue) return intValue;
          if (value is float floatValue) return (int)floatValue;
          if (value is double doubleValue) return (int)doubleValue;
      }
      return defaultValue;
  }

  // ==================== PUBLIC METHODS ====================
  
  // ==================== QUEUE METHODS ====================
  public int GetOnlineCount()
  {
      if (MMRoom == null || MMRoom.State == null) return 0;
      return (int)MMRoom.State.onlineCount;
  }

  public int GetQueueCount()
  {
      if (MMRoom == null || MMRoom.State == null) return 0;
      return (int)MMRoom.State.queueCount;
  }
  
  public int GetPartyCount()
  {
      if (MMRoom == null || MMRoom.State == null) return 0;
      return (int)MMRoom.State.partyCount;
  }

  public void JoinQueue()
  {
      try {
          if (MMRoom == null) {
              Debug.LogError("[MM] Cannot join queue: Room is null");
              return;
          }
          
          Debug.Log("[MM] Sending joinQueue message");
          MMRoom.Send("joinQueue");
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error joining queue: {e.Message}\n{e.StackTrace}");
      }
  }

  public void LeaveQueue()
  {
      try {
          if (MMRoom == null) {
              Debug.LogError("[MM] Cannot leave queue: Room is null");
              return;
          }
          
          MMRoom.Send("leaveQueue");
          Debug.Log("[MM] Left queue");
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error leaving queue: {e.Message}");
      }
  }
  
  // ==================== PARTY METHODS (UPDATED) ====================
  
  public void CreateParty(int maxMembers = 4)
  {
      try {
          if (MMRoom == null) {
              Debug.LogError("[MM] Cannot create party: Room is null");
              return;
          }
          
          var data = new Dictionary<string, object> {
              { "maxMembers", maxMembers }
          };
          
          MMRoom.Send("createParty", data);
          Debug.Log($"[MM] Creating party with max {maxMembers} members...");
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error creating party: {e.Message}");
      }
  }
  
  //  UPDATED METHOD NAME
  public void JoinPartyByCode(string inviteCode)
  {
      try {
          if (MMRoom == null) {
              Debug.LogError("[MM] Cannot join party: Room is null");
              return;
          }
          
          if (string.IsNullOrEmpty(inviteCode)) {
              Debug.LogError("[MM] Cannot join party: Invite code is empty");
              return;
          }
          
          var data = new Dictionary<string, object> {
              { "inviteCode", inviteCode.ToUpper() }
          };
          
          MMRoom.Send("joinPartyByCode", data); //  UPDATED MESSAGE NAME
          Debug.Log($"[MM] Joining party with invite code: {inviteCode}");
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error joining party: {e.Message}");
      }
  }
  
  public void LeaveParty()
  {
      try {
          if (MMRoom == null) {
              Debug.LogError("[MM] Cannot leave party: Room is null");
              return;
          }
          
          if (string.IsNullOrEmpty(CurrentPartyId)) {
              Debug.LogError("[MM] Cannot leave party: Not in a party");
              return;
          }
          
          MMRoom.Send("leaveParty");
          Debug.Log("[MM] Leaving party...");
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error leaving party: {e.Message}");
      }
  }
  
  //  NEW METHOD
  public void KickPlayer(string sessionId)
  {
      try {
          if (MMRoom == null) {
              Debug.LogError("[MM] Cannot kick player: Room is null");
              return;
          }
          
          if (!IsPartyLeader) {
              Debug.LogError("[MM] Cannot kick player: Not the party leader");
              return;
          }
          
          if (string.IsNullOrEmpty(sessionId)) {
              Debug.LogError("[MM] Cannot kick player: Invalid session ID");
              return;
          }
          
          var data = new Dictionary<string, object> {
              { "sessionId", sessionId }
          };
          
          MMRoom.Send("kickPlayer", data);
          Debug.Log($"[MM] Kicking player: {sessionId}");
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error kicking player: {e.Message}");
      }
  }
  
  //  UPDATED METHOD NAME
  public void StartPartyMatch()
  {
      try {
          if (MMRoom == null) {
              Debug.LogError("[MM] Cannot start party match: Room is null");
              return;
          }
          
          if (string.IsNullOrEmpty(CurrentPartyId)) {
              Debug.LogError("[MM] Cannot start party match: Not in a party");
              return;
          }
          
          if (!IsPartyLeader) {
              Debug.LogError("[MM] Cannot start party match: Not the party leader");
              return;
          }
          
          MMRoom.Send("startPartyMatch"); //  UPDATED MESSAGE NAME
          Debug.Log("[MM] Starting party match...");
      }
      catch (Exception e) {
          Debug.LogError($"[MM] Error starting party match: {e.Message}");
      }
  }
  
  // ==================== UTILITY METHODS ====================
  
  public string GetCurrentInviteCode()
  {
      return CurrentInviteCode;
  }
  
  public int GetPartyMemberCount()
  {
      return PartyMembers.Count;
  }
  
  public List<Dictionary<string, object>> GetPartyMembers()
  {
      return new List<Dictionary<string, object>>(PartyMembers);
  }
  
  public bool CanStartMatch()
  {
      return IsPartyLeader && IsInParty && PartyMembers.Count >= 2;
  }
  
  public bool CanKickPlayers()
  {
      return IsPartyLeader && IsInParty;
  }
}