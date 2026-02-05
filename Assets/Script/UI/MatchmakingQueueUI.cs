using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Collections;

public class MatchmakingQueueUI : MonoBehaviour
{
    [Header("Main Panel")]
    public TMP_Text onlineCountText;
    public TMP_Text queueCountText;
    public TMP_Text statusText;
    public Button joinQueueButton;
    public GameObject loadingSpinner;

    [Header("Queue Panel")]
    public GameObject queuePanel;
    public TMP_Text queueStatusText;
    public Button cancelQueueButton;

    [Header("Player Card")]
    public GameObject playerCardPrefab;
    public Transform playerCardContainer;
    public Transform opponentCardContainer;

    private GameObject currentPlayerCard;
    private GameObject opponentPlayerCard;
    private bool isMatchFound = false;
    private bool isInQueue = false;

    // ==================== UNITY LIFECYCLE ====================
    void Start()
    {
        //  VALIDATE UI COMPONENTS FIRST
        ValidateUIComponents();

        SubscribeToEvents();

        joinQueueButton.onClick.AddListener(OnJoinQueueClicked);
        cancelQueueButton.onClick.AddListener(OnCancelQueueClicked);

        UpdateConnectionStatus(false);

        // Ẩn queue panel ban đầu
        if (queuePanel != null)
            queuePanel.SetActive(false);
    }

    //  NEW: Validate UI components
    void ValidateUIComponents()
    {
        Debug.Log("[UI] === VALIDATING UI COMPONENTS ===");

        if (playerCardPrefab == null)
            Debug.LogError("[UI]  playerCardPrefab is not assigned!");
        else
            Debug.Log($"[UI]  playerCardPrefab OK: {playerCardPrefab.name}");

        if (playerCardContainer == null)
            Debug.LogError("[UI]  playerCardContainer is not assigned!");
        else
            Debug.Log($"[UI]  playerCardContainer OK: {playerCardContainer.name} (Active: {playerCardContainer.gameObject.activeInHierarchy})");

        if (opponentCardContainer == null)
            Debug.LogError("[UI]  opponentCardContainer is not assigned!");
        else
            Debug.Log($"[UI]  opponentCardContainer OK: {opponentCardContainer.name} (Active: {opponentCardContainer.gameObject.activeInHierarchy})");

        if (queuePanel == null)
            Debug.LogError("[UI]  queuePanel is not assigned!");
        else
            Debug.Log($"[UI]  queuePanel OK: {queuePanel.name}");
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();

        joinQueueButton.onClick.RemoveListener(OnJoinQueueClicked);
        cancelQueueButton.onClick.RemoveListener(OnCancelQueueClicked);
    }

    // ==================== EVENT SUBSCRIPTION ====================
    void SubscribeToEvents()
    {
        if (MatchMakingRoom.Instance != null)
        {
            // Basic events
            MatchMakingRoom.Instance.OnOnlineCountChanged += OnOnlineCountChanged;
            MatchMakingRoom.Instance.OnQueueCountChanged += OnQueueCountChanged;
            MatchMakingRoom.Instance.OnConnectionChanged += OnConnectionChanged;

            // Queue events
            MatchMakingRoom.Instance.OnQueueJoined += OnQueueJoinedEvent;
            MatchMakingRoom.Instance.OnQueueLeft += OnQueueLeftEvent;
            MatchMakingRoom.Instance.OnQueueError += OnQueueErrorEvent;

            // Match events
            MatchMakingRoom.Instance.OnMatchFound += OnMatchFound;
        }
        else
        {
            Debug.LogWarning("[UI] MatchMakingRoom.Instance is null in SubscribeToEvents");
        }
    }

    void UnsubscribeFromEvents()
    {
        if (MatchMakingRoom.Instance != null)
        {
            MatchMakingRoom.Instance.OnOnlineCountChanged -= OnOnlineCountChanged;
            MatchMakingRoom.Instance.OnQueueCountChanged -= OnQueueCountChanged;
            MatchMakingRoom.Instance.OnConnectionChanged -= OnConnectionChanged;

            MatchMakingRoom.Instance.OnQueueJoined -= OnQueueJoinedEvent;
            MatchMakingRoom.Instance.OnQueueLeft -= OnQueueLeftEvent;
            MatchMakingRoom.Instance.OnQueueError -= OnQueueErrorEvent;

            MatchMakingRoom.Instance.OnMatchFound -= OnMatchFound;
        }
    }

    // ==================== EVENT HANDLERS ====================
    void OnOnlineCountChanged(int count)
    {
        Debug.Log($"[UI] Online count updated: {count}");
        if (onlineCountText != null)
            onlineCountText.text = $"Online: {count}";
    }

    void OnQueueCountChanged(int count)
    {
        Debug.Log($"[UI] Queue count updated: {count}");
        if (queueCountText != null)
            queueCountText.text = $"In Queue: {count}";
    }

    void OnConnectionChanged(bool isConnected)
    {
        Debug.Log($"[UI] Connection status: {isConnected}");
        UpdateConnectionStatus(isConnected);
    }

    void OnQueueJoinedEvent(Dictionary<string, object> data)
    {
        Debug.Log("[UI] Queue joined successfully");
        isInQueue = true;
        ShowQueuePanel();

        if (data != null)
        {
            if (data.TryGetValue("position", out object pos))
            {
                Debug.Log($"[UI] Queue position: {pos}");
                if (queueStatusText != null)
                {
                    queueStatusText.text = $"Đang tìm trận... (Vị trí: {pos})";
                }
            }

            if (data.TryGetValue("estimatedWait", out object wait))
            {
                Debug.Log($"[UI] Estimated wait: {wait}s");
            }
        }
    }

    void OnQueueLeftEvent()
    {
        Debug.Log("[UI] Queue left successfully");
        isInQueue = false;
        HideQueuePanel();
    }

    void OnQueueErrorEvent(string error)
    {
        Debug.LogError($"[UI] Queue error: {error}");
        isInQueue = false;

        if (queueStatusText != null)
        {
            queueStatusText.text = $"Lỗi: {error}";
        }

        // Show error for a few seconds then hide
        Invoke(nameof(HideQueuePanel), 3f);
    }

    //  FIXED: OnMatchFound - Đảm bảo cả 2 player đều thấy match
    void OnMatchFound(Dictionary<string, object> matchData)
    {
        Debug.Log("[UI] === MATCH FOUND EVENT RECEIVED ===");

        if (matchData == null)
        {
            Debug.LogError("[UI] Match data is null!");
            return;
        }

        try
        {
            bool isPartyMatch = GetBoolValue(matchData, "isPartyMatch", false);
            string gameRoomId = GetStringValue(matchData, "gameRoomId", "");

            Debug.Log($"[UI] Match type: {(isPartyMatch ? "Party" : "Queue")}, GameRoom: {gameRoomId}");

            //  FORCE SHOW QUEUE PANEL cho cả 2 player (kể cả Player 2)
            if (queuePanel != null)
            {
                if (!queuePanel.activeSelf)
                {
                    Debug.Log("[UI] Force showing queue panel for match found");
                    queuePanel.SetActive(true);

                    //  Tạo player card nếu chưa có (cho Player 2)
                    if (currentPlayerCard == null)
                    {
                        CreatePlayerCard();
                    }
                }
            }

            //  UPDATE STATE cho cả 2 player
            isMatchFound = true;
            isInQueue = false;

            //  UPDATE STATUS TEXT cho cả 2 player
            if (queueStatusText != null)
            {
                queueStatusText.text = isPartyMatch ? "Trận đấu nhóm đã sẵn sàng!" : "Đã tìm được trận!";
            }

            //  DISABLE CANCEL BUTTON
            if (cancelQueueButton != null)
                cancelQueueButton.interactable = false;

            // Handle different match types
            if (isPartyMatch)
            {
                HandlePartyMatch(matchData);
            }
            else
            {
                HandleQueueMatch(matchData);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UI] Error processing match data: {e.Message}\n{e.StackTrace}");
        }
    }

    //  FIXED: HandlePartyMatch
    void HandlePartyMatch(Dictionary<string, object> matchData)
    {
        Debug.Log("[UI] Handling party match");

        if (matchData.TryGetValue("members", out object membersObj) && membersObj is List<object> membersList)
        {
            Debug.Log($"[UI] Party match with {membersList.Count} members");

            // Create cards for all party members
            foreach (var memberObj in membersList)
            {
                if (memberObj is Dictionary<string, object> memberData)
                {
                    string username = GetStringValue(memberData, "username", "Unknown");
                    float level = GetFloatValue(memberData, "level", 1);
                    bool isLeader = GetBoolValue(memberData, "isLeader", false);

                    Debug.Log($"[UI] Party member: {username} (Lv.{level}) {(isLeader ? "[Leader]" : "")}");
                }
            }

            //  UPDATE STATUS for party match
            if (queueStatusText != null)
            {
                queueStatusText.text = $"Trận đấu nhóm với {membersList.Count} thành viên!";
            }
        }
    }

    //  FIXED: HandleQueueMatch - Xử lý IndexedDictionary từ Colyseus
    void HandleQueueMatch(Dictionary<string, object> matchData)
    {
        Debug.Log("[UI] === HANDLING QUEUE MATCH ===");

        //  LOG TOÀN BỘ matchData
        if (matchData != null)
        {
            Debug.Log($"[UI] matchData keys: {string.Join(", ", matchData.Keys)}");
            foreach (var kvp in matchData)
            {
                Debug.Log($"[UI] - {kvp.Key}: {kvp.Value} (Type: {kvp.Value?.GetType().Name})");
            }
        }

        if (matchData.TryGetValue("opponent", out object opponentObj))
        {
            Debug.Log($"[UI] Found opponent object: {opponentObj} (Type: {opponentObj?.GetType().Name})");

            try
            {
                string opponentName = "Unknown";
                int opponentLevel = 1;

                //  METHOD 1: Try cast to Dictionary first
                if (opponentObj is Dictionary<string, object> directDict)
                {
                    Debug.Log("[UI] Opponent is Dictionary - parsing directly");
                    opponentName = GetStringValue(directDict, "username", "Unknown");
                    opponentLevel = (int)GetFloatValue(directDict, "level", 1);
                }
                //  METHOD 2: Try cast to IDictionary
                else if (opponentObj is IDictionary<string, object> iDict)
                {
                    Debug.Log("[UI] Opponent is IDictionary - converting");
                    var tempDict = new Dictionary<string, object>();
                    foreach (var kvp in iDict)
                    {
                        tempDict[kvp.Key] = kvp.Value;
                    }
                    opponentName = GetStringValue(tempDict, "username", "Unknown");
                    opponentLevel = (int)GetFloatValue(tempDict, "level", 1);
                }
                //  METHOD 3: Try generic IDictionary (for IndexedDictionary)
                else if (opponentObj is IDictionary genericDict)
                {
                    Debug.Log("[UI] Opponent is generic IDictionary - extracting values");

                    if (genericDict.Contains("username"))
                    {
                        var usernameValue = genericDict["username"];
                        if (usernameValue != null)
                        {
                            opponentName = usernameValue.ToString();
                        }
                    }

                    if (genericDict.Contains("level"))
                    {
                        var levelValue = genericDict["level"];
                        if (levelValue != null)
                        {
                            if (float.TryParse(levelValue.ToString(), out float levelFloat))
                            {
                                opponentLevel = (int)levelFloat;
                            }
                        }
                    }
                }
                //  METHOD 4: Reflection fallback
                else
                {
                    Debug.Log("[UI] Using reflection to access opponent data");

                    var opponentType = opponentObj.GetType();

                    // Try to get username
                    var usernameProperty = opponentType.GetProperty("username");
                    if (usernameProperty != null)
                    {
                        var usernameValue = usernameProperty.GetValue(opponentObj);
                        if (usernameValue != null)
                        {
                            opponentName = usernameValue.ToString();
                        }
                    }

                    // Try to get level
                    var levelProperty = opponentType.GetProperty("level");
                    if (levelProperty != null)
                    {
                        var levelValue = levelProperty.GetValue(opponentObj);
                        if (levelValue != null)
                        {
                            if (float.TryParse(levelValue.ToString(), out float levelFloat))
                            {
                                opponentLevel = (int)levelFloat;
                            }
                        }
                    }
                }

                Debug.Log($"[UI] Opponent parsed: {opponentName} (Lv.{opponentLevel})");

                //  TẠO OPPONENT CARD
                CreateOpponentCard(opponentName, opponentLevel);

                //  CẬP NHẬT STATUS VỚI TÊN ĐỐI THỦ
                if (queueStatusText != null)
                {
                    queueStatusText.text = $"Đối thủ: {opponentName} (Lv.{opponentLevel})";
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[UI] Error processing opponent: {e.Message}");

                //  ULTIMATE FALLBACK
                CreateOpponentCard("Opponent", 1);
                if (queueStatusText != null)
                {
                    queueStatusText.text = "Đối thủ: Opponent (Lv.1)";
                }
            }
        }
        else
        {
            Debug.LogError("[UI] No 'opponent' key found in matchData!");
            if (queueStatusText != null)
            {
                queueStatusText.text = "Đã tìm được trận đấu!";
            }
        }
    }

    // ==================== BUTTON HANDLERS ====================
    void OnJoinQueueClicked()
    {
        Debug.Log("[UI] Join Queue button clicked");

        if (MatchMakingRoom.Instance == null)
        {
            Debug.LogError("[UI] MatchMakingRoom instance is null!");
            return;
        }

        if (isInQueue)
        {
            Debug.LogWarning("[UI] Already in queue!");
            return;
        }

        if (isMatchFound)
        {
            Debug.LogWarning("[UI] Match already found!");
            return;
        }

        // Send join queue request
        MatchMakingRoom.Instance.JoinQueue();

        // Don't show queue panel immediately, wait for server response
        Debug.Log("[UI] Queue request sent, waiting for server response...");
    }

    void OnCancelQueueClicked()
    {
        Debug.Log("[UI] Cancel Queue button clicked");

        if (isMatchFound)
        {
            Debug.LogWarning("[UI] Cannot cancel - match already found!");
            return;
        }

        if (!isInQueue)
        {
            Debug.LogWarning("[UI] Not in queue!");
            HideQueuePanel(); // Hide panel anyway
            return;
        }

        if (MatchMakingRoom.Instance != null)
        {
            MatchMakingRoom.Instance.LeaveQueue();
            // Don't hide panel immediately, wait for server response
        }
        else
        {
            Debug.LogError("[UI] MatchMakingRoom instance is null!");
            // Force hide panel if no connection
            isInQueue = false;
            HideQueuePanel();
        }
    }

    // ==================== UI UPDATES ====================
    void UpdateConnectionStatus(bool isConnected)
    {
        if (statusText != null)
        {
            if (isConnected)
            {
                statusText.text = "Connected";
                statusText.color = Color.green;

                if (joinQueueButton != null)
                    joinQueueButton.interactable = true;

                if (loadingSpinner != null)
                    loadingSpinner.SetActive(false);
            }
            else
            {
                statusText.text = "Connecting...";
                statusText.color = Color.yellow;

                if (joinQueueButton != null)
                    joinQueueButton.interactable = false;

                if (loadingSpinner != null)
                    loadingSpinner.SetActive(true);

                // Reset queue state on disconnect
                isInQueue = false;
                isMatchFound = false;
                HideQueuePanel();
            }
        }
    }

    void ShowQueuePanel()
    {
        if (queuePanel != null)
        {
            queuePanel.SetActive(true);

            // Set initial status
            if (queueStatusText != null)
                queueStatusText.text = "Đang tìm trận...";

            // Enable cancel button
            if (cancelQueueButton != null)
                cancelQueueButton.interactable = true;

            // Clear old opponent card
            if (opponentPlayerCard != null)
            {
                Destroy(opponentPlayerCard);
                opponentPlayerCard = null;
            }

            // Create player card
            CreatePlayerCard();
        }
    }

    void HideQueuePanel()
    {
        if (queuePanel != null)
            queuePanel.SetActive(false);

        // Clean up cards
        if (currentPlayerCard != null)
        {
            Destroy(currentPlayerCard);
            currentPlayerCard = null;
        }

        if (opponentPlayerCard != null)
        {
            Destroy(opponentPlayerCard);
            opponentPlayerCard = null;
        }

        // Reset states
        isMatchFound = false;
        isInQueue = false;
    }

    void CreatePlayerCard()
    {
        if (playerCardPrefab == null || playerCardContainer == null)
        {
            Debug.LogWarning("[UI] Player card prefab or container is missing");
            return;
        }

        // Clean up old card
        if (currentPlayerCard != null)
            Destroy(currentPlayerCard);

        // Create new card
        currentPlayerCard = Instantiate(playerCardPrefab, playerCardContainer);

        // Set player info
        PlayerCardUI playerCard = currentPlayerCard.GetComponent<PlayerCardUI>();
        if (playerCard != null)
        {
            string playerName = "Player";
            int playerLevel = 1;

            if (AuthManager.Instance?.CurrentUser != null)
            {
                playerName = AuthManager.Instance.CurrentUser.username;
            }

            playerCard.SetPlayerInfo(playerName, playerLevel);
            Debug.Log($"[UI] Player card created: {playerName} (Lv.{playerLevel})");
        }
        else
        {
            // Fallback to direct text update
            TMP_Text playerNameText = currentPlayerCard.GetComponentInChildren<TMP_Text>();
            if (playerNameText != null)
            {
                string playerName = AuthManager.Instance?.CurrentUser?.username ?? "Player";
                playerNameText.text = playerName;
                Debug.Log($"[UI] Player card created with fallback: {playerName}");
            }
        }
    }

    //  ENHANCED: CreateOpponentCard với debug chi tiết
    void CreateOpponentCard(string opponentName, int opponentLevel)
    {
        Debug.Log($"[UI] === CREATING OPPONENT CARD ===");
        Debug.Log($"[UI] Opponent: {opponentName} (Level {opponentLevel})");

        //  CHECK PREFAB & CONTAINER
        if (playerCardPrefab == null)
        {
            Debug.LogError("[UI]  playerCardPrefab is NULL!");
            return;
        }
        Debug.Log($"[UI]  playerCardPrefab OK: {playerCardPrefab.name}");

        if (opponentCardContainer == null)
        {
            Debug.LogError("[UI]  opponentCardContainer is NULL!");
            return;
        }
        Debug.Log($"[UI]  opponentCardContainer OK: {opponentCardContainer.name} (Active: {opponentCardContainer.gameObject.activeInHierarchy})");

        //  FORCE ENABLE CONTAINER
        if (!opponentCardContainer.gameObject.activeInHierarchy)
        {
            Debug.Log("[UI] Force enabling opponentCardContainer");
            opponentCardContainer.gameObject.SetActive(true);
        }

        // Clean up old card
        if (opponentPlayerCard != null)
        {
            Debug.Log("[UI] Destroying old opponent card");
            Destroy(opponentPlayerCard);
            opponentPlayerCard = null;
        }

        //  GET PREFAB'S ORIGINAL SCALE BEFORE INSTANTIATE
        Vector3 originalScale = playerCardPrefab.transform.localScale;
        Debug.Log($"[UI] Prefab original scale: {originalScale}");

        // Create new card
        Debug.Log("[UI] Instantiating new opponent card...");
        opponentPlayerCard = Instantiate(playerCardPrefab, opponentCardContainer);

        if (opponentPlayerCard == null)
        {
            Debug.LogError("[UI]  Failed to instantiate opponent card!");
            return;
        }
        Debug.Log($"[UI]  Opponent card instantiated: {opponentPlayerCard.name}");

        //  KEEP ORIGINAL SCALE & SET POSITION
        opponentPlayerCard.transform.localPosition = Vector3.zero;
        opponentPlayerCard.transform.localScale = originalScale; //  Giữ nguyên scale gốc (0.3, 0.3, 0.3)
        opponentPlayerCard.SetActive(true);

        Debug.Log($"[UI] Applied scale: {opponentPlayerCard.transform.localScale}");

        // Set opponent info
        PlayerCardUI playerCard = opponentPlayerCard.GetComponent<PlayerCardUI>();
        if (playerCard != null)
        {
            Debug.Log("[UI] Setting opponent info via PlayerCardUI component");
            playerCard.SetPlayerInfo(opponentName, opponentLevel);
            Debug.Log($"[UI]  Opponent card created successfully: {opponentName} (Lv.{opponentLevel})");
        }
        else
        {
            Debug.LogWarning("[UI] No PlayerCardUI component found, using fallback");
            // Fallback to direct text update
            TMP_Text playerNameText = opponentPlayerCard.GetComponentInChildren<TMP_Text>();
            if (playerNameText != null)
            {
                playerNameText.text = $"{opponentName} (Lv.{opponentLevel})";
                Debug.Log($"[UI]  Opponent card created with fallback: {opponentName}");
            }
            else
            {
                Debug.LogError("[UI]  Could not find TMP_Text in opponent card!");
            }
        }

        //  FINAL CHECK
        Debug.Log($"[UI] Final check - opponentPlayerCard active: {opponentPlayerCard.activeInHierarchy}");
        Debug.Log($"[UI] Final check - parent: {opponentPlayerCard.transform.parent?.name}");
        Debug.Log($"[UI] Final check - position: {opponentPlayerCard.transform.position}");
        Debug.Log($"[UI] Final check - scale: {opponentPlayerCard.transform.localScale}");
        Debug.Log($"[UI] Final check - children count in container: {opponentCardContainer.childCount}");
    }

    // ==================== HELPER METHODS ====================

    string GetStringValue(Dictionary<string, object> dict, string key, string defaultValue = "")
    {
        if (dict != null && dict.TryGetValue(key, out object value) && value != null)
        {
            return value.ToString();
        }
        return defaultValue;
    }

    bool GetBoolValue(Dictionary<string, object> dict, string key, bool defaultValue = false)
    {
        if (dict != null && dict.TryGetValue(key, out object value) && value is bool boolValue)
        {
            return boolValue;
        }
        return defaultValue;
    }

    float GetFloatValue(Dictionary<string, object> dict, string key, float defaultValue = 0f)
    {
        if (dict != null && dict.TryGetValue(key, out object value))
        {
            if (value is float floatValue) return floatValue;
            if (value is double doubleValue) return (float)doubleValue;
            if (value is int intValue) return (float)intValue;
            if (float.TryParse(value.ToString(), out float parsedValue)) return parsedValue;
        }
        return defaultValue;
    }
}
