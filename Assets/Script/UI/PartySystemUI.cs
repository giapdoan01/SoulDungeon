// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// public class PartySystemUI : MonoBehaviour
// {
//     [Header("Party Creation Panel")]
//     public GameObject createPartyPanel;
//     public Button createPartyButton;
//     public TMP_Text partyCountText;

//     [Header("Invite Panel")]
//     public GameObject invitePanel;
//     public TMP_Text inviteCodeText;
//     public Button copyInviteCodeButton;

//     [Header("Join Party Panel")]
//     public GameObject joinPartyPanel;
//     public TMP_InputField inviteCodeInput;
//     public Button joinPartyButton;

//     [Header("Party Management")]
//     public GameObject partyManagementPanel;
//     public TMP_Text partyStatusText;
//     public TMP_Text memberListText;
//     public Button startGameButton;
//     public Button leavePartyButton;

//     [Header("Notifications")]
//     public GameObject notificationPanel;
//     public TMP_Text notificationText;

//     private string currentInviteCode = "";

//     // ==================== UNITY LIFECYCLE ====================
//     void Start()
//     {
//         SubscribeToEvents();
//         SetupButtons();
        
//         // Default view
//         ShowCreateJoinPanel();
//     }

//     void OnDestroy()
//     {
//         UnsubscribeFromEvents();
//     }

//     // ==================== EVENT SUBSCRIPTION ====================
//     void SubscribeToEvents()
//     {
//         if (MatchMakingRoom.Instance != null)
//         {
//             MatchMakingRoom.Instance.OnPartyCountChanged += OnPartyCountChanged;
//             MatchMakingRoom.Instance.OnPartyCreated += OnPartyCreated;
//             MatchMakingRoom.Instance.OnPartyJoined += OnPartyJoined;
//             MatchMakingRoom.Instance.OnPartyLeft += OnPartyLeft;
//             MatchMakingRoom.Instance.OnPartyMemberJoined += OnPartyMemberJoined;
//             MatchMakingRoom.Instance.OnPartyMemberLeft += OnPartyMemberLeft;
//             MatchMakingRoom.Instance.OnPartyLeaderChanged += OnPartyLeaderChanged;
//             MatchMakingRoom.Instance.OnPartyError += OnPartyError;
//             MatchMakingRoom.Instance.OnMatchFound += OnMatchFound;
//         }
//     }

//     void UnsubscribeFromEvents()
//     {
//         if (MatchMakingRoom.Instance != null)
//         {
//             MatchMakingRoom.Instance.OnPartyCountChanged -= OnPartyCountChanged;
//             MatchMakingRoom.Instance.OnPartyCreated -= OnPartyCreated;
//             MatchMakingRoom.Instance.OnPartyJoined -= OnPartyJoined;
//             MatchMakingRoom.Instance.OnPartyLeft -= OnPartyLeft;
//             MatchMakingRoom.Instance.OnPartyMemberJoined -= OnPartyMemberJoined;
//             MatchMakingRoom.Instance.OnPartyMemberLeft -= OnPartyMemberLeft;
//             MatchMakingRoom.Instance.OnPartyLeaderChanged -= OnPartyLeaderChanged;
//             MatchMakingRoom.Instance.OnPartyError -= OnPartyError;
//             MatchMakingRoom.Instance.OnMatchFound -= OnMatchFound;
//         }
//     }

//     void SetupButtons()
//     {
//         createPartyButton.onClick.AddListener(OnCreatePartyClicked);
//         copyInviteCodeButton.onClick.AddListener(OnCopyInviteCodeClicked);
//         joinPartyButton.onClick.AddListener(OnJoinPartyClicked);
//         startGameButton.onClick.AddListener(OnStartGameClicked);
//         leavePartyButton.onClick.AddListener(OnLeavePartyClicked);
//     }

//     // ==================== EVENT HANDLERS ====================
//     void OnPartyCountChanged(int count)
//     {
//         partyCountText.text = $"Active Parties: {count}";
//     }

//     void OnPartyCreated(string partyId, string inviteCode)
//     {
//         currentInviteCode = inviteCode;
//         inviteCodeText.text = inviteCode;
        
//         ShowPartyManagementPanel();
//         UpdatePartyStatus("Party Created! Waiting for players to join...");
//         ShowNotification("Party created! Share the invite code.");
//     }

//     void OnPartyJoined(string partyId, string leaderUsername)
//     {
//         ShowPartyManagementPanel();
//         UpdatePartyStatus($"Joined party led by {leaderUsername}");
//         ShowNotification($"You've joined {leaderUsername}'s party!");
        
//         // Hide start game button since not leader
//         startGameButton.gameObject.SetActive(false);
//     }

//     void OnPartyLeft()
//     {
//         ShowCreateJoinPanel();
//         ShowNotification("You've left the party");
//     }

//     void OnPartyMemberJoined(string username)
//     {
//         UpdateMemberList();
//         ShowNotification($"{username} has joined the party!");
//     }

//     void OnPartyMemberLeft(string username)
//     {
//         UpdateMemberList();
//         ShowNotification($"{username} has left the party");
//     }

//     void OnPartyLeaderChanged(bool isLeader)
//     {
//         startGameButton.gameObject.SetActive(isLeader);
        
//         if (isLeader)
//         {
//             UpdatePartyStatus("You are now the party leader");
//             ShowNotification("You are now the party leader!");
//         }
//     }

//     void OnPartyError(string errorMessage)
//     {
//         ShowNotification($"Error: {errorMessage}", Color.red);
//     }
    
//     void OnMatchFound(Dictionary<string, object> matchData)
//     {
//         var opponentData = matchData["opponent"] as Dictionary<string, object>;
//         string opponentName = opponentData["username"] as string;
//         string gameRoomId = matchData["gameRoomId"] as string;
        
//         ShowNotification($"Game starting with {opponentName}!", Color.green);
        
//         // Thực hiện chuyển scene hoặc setup battle
//         // ...
//     }

//     // ==================== BUTTON HANDLERS ====================
//     void OnCreatePartyClicked()
//     {
//         MatchMakingRoom.Instance.CreateParty();
//     }

//     void OnCopyInviteCodeClicked()
//     {
//         GUIUtility.systemCopyBuffer = currentInviteCode;
//         ShowNotification("Invite code copied to clipboard!");
//     }

//     void OnJoinPartyClicked()
//     {
//         string code = inviteCodeInput.text.Trim();
//         if (string.IsNullOrEmpty(code))
//         {
//             ShowNotification("Please enter an invite code", Color.yellow);
//             return;
//         }
        
//         MatchMakingRoom.Instance.JoinParty(code);
//     }

//     void OnStartGameClicked()
//     {
//         MatchMakingRoom.Instance.StartPartyGame();
//     }

//     void OnLeavePartyClicked()
//     {
//         MatchMakingRoom.Instance.LeaveParty();
//     }

//     // ==================== UI UPDATES ====================
//     void ShowCreateJoinPanel()
//     {
//         createPartyPanel.SetActive(true);
//         invitePanel.SetActive(false);
//         joinPartyPanel.SetActive(true);
//         partyManagementPanel.SetActive(false);
//     }

//     void ShowPartyManagementPanel()
//     {
//         createPartyPanel.SetActive(false);
//         invitePanel.SetActive(true);
//         joinPartyPanel.SetActive(false);
//         partyManagementPanel.SetActive(true);
        
//         // Hiển thị/ẩn nút bắt đầu game tùy theo vai trò leader
//         startGameButton.gameObject.SetActive(MatchMakingRoom.Instance.IsPartyLeader);
        
//         UpdateMemberList();
//     }

//     void UpdatePartyStatus(string status)
//     {
//         partyStatusText.text = status;
//     }
    
//     void UpdateMemberList()
//     {
//         // Trong version thực tế, bạn sẽ lấy danh sách thành viên từ state
//         // Đây là phiên bản giả định đơn giản
//         if (memberListText != null)
//         {
//             memberListText.text = "Members:\n";
            
//             // Implement logic to show actual members
//             // For example:
//             // foreach (var member in MatchMakingRoom.Instance.GetPartyMembers())
//             // {
//             //     memberListText.text += $"• {member.username}" + (member.isLeader ? " (Leader)" : "") + "\n";
//             // }
//         }
//     }

//     void ShowNotification(string message, Color color = default)
//     {
//         if (color == default)
//             color = Color.white;
            
//         notificationPanel.SetActive(true);
//         notificationText.text = message;
//         notificationText.color = color;
        
//         CancelInvoke(nameof(HideNotification));
//         Invoke(nameof(HideNotification), 3f);
//     }

//     void HideNotification()
//     {
//         notificationPanel.SetActive(false);
//     }
// }