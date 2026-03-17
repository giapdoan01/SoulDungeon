// UIManager.cs
// Điều phối màn hình lobby:
//   - Auto-connect Colyseus khi scene load
//   - 3 nút chính: FindMatch / CreateParty / JoinParty
//   - Mở / đóng QueuePanel và PartyPanel
//
// Hierarchy gợi ý:
//   UIManager (GameObject)
//   ├── MainView
//   │   ├── Txt_ConnectionStatus
//   │   ├── Btn_FindMatch
//   │   ├── Btn_CreateParty
//   │   ├── Input_InviteCode
//   │   └── Btn_JoinParty
//   ├── QueuePanel      ← chứa QueueUI
//   └── PartyPanel      ← chứa PartyUI
using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private enum ActivePanel { None, Queue, Party }

    #region Inspector References
    [Header("Views")]
    [SerializeField] private GameObject _mainView;
    [SerializeField] private GameObject _queuePanel;
    [SerializeField] private GameObject _partyPanel;

    [Header("Main View — Buttons")]
    [SerializeField] private Button _btnFindMatch;
    [SerializeField] private Button _btnCreateParty;
    [SerializeField] private Button _btnJoinParty;

    [Header("Main View — Join by Code")]
    [SerializeField] private TMP_InputField _inputInviteCode;

    [Header("Main View — Status")]
    [SerializeField] private TMP_Text _txtConnectionStatus;

    [Header("Scene")]
    [SerializeField] private string _battleSceneName = "BattleScene";
    #endregion

    #region Private State
    private MatchmakingRoomManager _mgr;
    private ActivePanel _activePanel = ActivePanel.None;
    #endregion

    #region Unity Lifecycle
    private async void Start()
    {
        _mgr = MatchmakingRoomManager.Instance;
        if (_mgr == null)
        {
            Debug.LogError("[UIManager] MatchmakingRoomManager không tìm thấy trong scene!");
            return;
        }

        SubscribeEvents();
        RegisterButtons();

        // Ban đầu ẩn hết panels, hiện MainView với buttons bị khoá
        _queuePanel.SetActive(false);
        _partyPanel.SetActive(false);
        _mainView.SetActive(true);
        SetLobbyButtons(interactable: false);
        SetStatus("Đang kết nối server...");

        // Auto-connect ngay khi vào scene
        try
        {
            await _mgr.ConnectAsync();
            SetStatus("Sẵn sàng");
            SetLobbyButtons(interactable: true);
        }
        catch (Exception e)
        {
            SetStatus($"Kết nối thất bại: {e.Message}");
            Debug.LogError($"[UIManager] ConnectAsync failed: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }
    #endregion

    #region Button Setup
    private void RegisterButtons()
    {
        _btnFindMatch  .onClick.AddListener(OnClickFindMatch);
        _btnCreateParty.onClick.AddListener(OnClickCreateParty);
        _btnJoinParty  .onClick.AddListener(OnClickJoinParty);
    }
    #endregion

    #region Button Handlers
    private void OnClickFindMatch()
    {
        if (!_mgr.IsConnected) return;
        _mgr.JoinQueue();
        OpenPanel(ActivePanel.Queue);
    }

    private void OnClickCreateParty()
    {
        if (!_mgr.IsConnected) return;
        _mgr.CreateParty(maxMembers: 2);
        OpenPanel(ActivePanel.Party);
    }

    private void OnClickJoinParty()
    {
        if (!_mgr.IsConnected) return;
        string code = _inputInviteCode.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            SetStatus("Vui lòng nhập mã mời.");
            return;
        }
        _mgr.JoinPartyByCode(code);
        OpenPanel(ActivePanel.Party);
    }
    #endregion

    #region Event Subscription
    private void SubscribeEvents()
    {
        _mgr.OnQueueLeft      += HandleQueueLeft;
        _mgr.OnMatchStart     += HandleMatchStart;
        _mgr.OnMatchCancelled += HandleMatchCancelled;
        _mgr.OnPartyLeft      += HandlePartyLeft;
        _mgr.OnKicked         += HandleKicked;
        _mgr.OnPartyExpired   += HandlePartyExpired;
    }

    private void UnsubscribeEvents()
    {
        if (_mgr == null) return;
        _mgr.OnQueueLeft      -= HandleQueueLeft;
        _mgr.OnMatchStart     -= HandleMatchStart;
        _mgr.OnMatchCancelled -= HandleMatchCancelled;
        _mgr.OnPartyLeft      -= HandlePartyLeft;
        _mgr.OnKicked         -= HandleKicked;
        _mgr.OnPartyExpired   -= HandlePartyExpired;
    }
    #endregion

    #region MMRoom Event Handlers
    private void HandleQueueLeft() => ReturnToLobby();

    private async void HandleMatchStart(MatchStartMsg msg)
    {
        Debug.Log($"[UIManager] MatchStart — roomId:{msg.roomId}");
        SetStatus("Đang kết nối phòng chiến...");

        string mmSessionId = MatchmakingRoomManager.Instance?.SessionId;
        try
        {
            await GameRoomManager.Instance.ConnectAsync(msg.roomId, mmSessionId);
            // ConnectAsync thành công → load BattleScene
            // GameSceneManager sẽ catch-up state ngay khi Start() chạy
            SceneManager.LoadScene(_battleSceneName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[UIManager] GameRoom connect failed: {e.Message}");
            SetStatus("Kết nối phòng chiến thất bại!");
            ReturnToLobby();
        }
    }

    private void HandleMatchCancelled(MatchCancelledMsg msg)
    {
        // Queue match bị huỷ → đóng QueuePanel về lobby
        // Party match bị huỷ → UIManager giữ PartyPanel mở (PartyUI tự cập nhật)
        if (_activePanel == ActivePanel.Queue)
        {
            SetStatus("Trận bị huỷ. Tìm lại nhé!");
            ReturnToLobby();
        }
    }

    private void HandlePartyLeft()           => ReturnToLobby();
    private void HandleKicked(KickedMsg _)   => ReturnToLobby();
    private void HandlePartyExpired(PartyExpiredMsg _) => ReturnToLobby();
    #endregion

    #region Panel Management
    private void OpenPanel(ActivePanel panel)
    {
        _mainView.SetActive(false);
        _queuePanel.SetActive(panel == ActivePanel.Queue);
        _partyPanel.SetActive(panel == ActivePanel.Party);
        _activePanel = panel;
    }

    private void ReturnToLobby()
    {
        _queuePanel.SetActive(false);
        _partyPanel.SetActive(false);
        _mainView.SetActive(true);
        _activePanel = ActivePanel.None;
        SetLobbyButtons(interactable: true);
        SetStatus("Sẵn sàng");
    }
    #endregion

    #region Helpers
    private void SetStatus(string text)
    {
        if (_txtConnectionStatus != null)
            _txtConnectionStatus.text = text;
    }

    private void SetLobbyButtons(bool interactable)
    {
        _btnFindMatch  .interactable = interactable;
        _btnCreateParty.interactable = interactable;
        _btnJoinParty  .interactable = interactable;
    }
    #endregion
}
