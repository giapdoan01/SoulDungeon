// PartyUI.cs
// Nội dung bên trong PartyPanel — chỉ xử lý display, KHÔNG quản lý panel visibility.
// UIManager chịu trách nhiệm mở/đóng PartyPanel (và cũng đã gọi CreateParty/JoinPartyByCode).
//
// Hierarchy gợi ý (bên trong PartyPanel):
//   PartyPanel
//   ├── LoadingView           ← hiện trong lúc chờ server xác nhận party
//   │   └── Txt_Loading
//   ├── InPartyView           ← hiện khi đã có party data
//   │   ├── Txt_InviteCode
//   │   ├── MemberList        ← spawn CardPlayer
//   │   ├── Btn_LeaveParty
//   │   └── Btn_StartMatch    ← chỉ leader thấy
//   └── MatchFoundView        ← hiện khi matchFound
//       ├── Txt_Countdown
//       ├── PlayerList        ← spawn CardPlayer
//       └── Btn_CancelMatch
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PartyUI : MonoBehaviour
{
    #region Inspector References
    [Header("Views")]
    [SerializeField] private GameObject _loadingView;
    [SerializeField] private GameObject _inPartyView;
    [SerializeField] private GameObject _matchFoundView;

    [Header("Loading View")]
    [SerializeField] private TMP_Text _txtLoading;

    [Header("In Party View")]
    [SerializeField] private TMP_Text  _txtInviteCode;
    [SerializeField] private Transform _memberListContainer;
    [SerializeField] private Button    _btnLeaveParty;
    [SerializeField] private Button    _btnStartMatch;

    [Header("Match Found View")]
    [SerializeField] private TMP_Text  _txtCountdown;
    [SerializeField] private Transform _playerListContainer;
    [SerializeField] private Button    _btnCancelMatch;

    [Header("Prefab")]
    [SerializeField] private CardPlayer _cardPlayerPrefab;
    #endregion

    #region Private State
    private MatchmakingRoomManager _mgr;
    private PartyData _currentParty;
    private bool      _isLeader;
    private Coroutine _countdownCoroutine;
    private readonly List<CardPlayer> _memberCards = new();
    private readonly List<CardPlayer> _matchCards  = new();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _btnLeaveParty .onClick.AddListener(() => _mgr?.LeaveParty());
        _btnStartMatch .onClick.AddListener(() => _mgr?.StartPartyMatch());
        _btnCancelMatch.onClick.AddListener(() => _mgr?.CancelMatch());
    }

    // OnEnable gọi mỗi khi PartyPanel bật lên
    private void OnEnable()
    {
        _mgr = MatchmakingRoomManager.Instance;
        SubscribeEvents();
        _currentParty = null;
        _isLeader     = false;
        ShowLoadingView("Đang chờ party...");
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StopCountdown();
        ClearMemberCards();
        ClearMatchCards();
    }
    #endregion

    #region Event Subscription
    private void SubscribeEvents()
    {
        if (_mgr == null) return;
        _mgr.OnPartyCreated          += HandlePartyCreated;
        _mgr.OnPartyError            += HandlePartyError;
        _mgr.OnJoinPartyResult       += HandleJoinPartyResult;
        _mgr.OnPartyUpdate           += HandlePartyUpdate;
        _mgr.OnLeadershipTransferred += HandleLeadershipTransferred;
        _mgr.OnKickResult            += HandleKickResult;
        _mgr.OnStartMatchResult      += HandleStartMatchResult;
        _mgr.OnMatchFound            += HandleMatchFound;
        _mgr.OnMatchCancelled        += HandleMatchCancelled;
    }

    private void UnsubscribeEvents()
    {
        if (_mgr == null) return;
        _mgr.OnPartyCreated          -= HandlePartyCreated;
        _mgr.OnPartyError            -= HandlePartyError;
        _mgr.OnJoinPartyResult       -= HandleJoinPartyResult;
        _mgr.OnPartyUpdate           -= HandlePartyUpdate;
        _mgr.OnLeadershipTransferred -= HandleLeadershipTransferred;
        _mgr.OnKickResult            -= HandleKickResult;
        _mgr.OnStartMatchResult      -= HandleStartMatchResult;
        _mgr.OnMatchFound            -= HandleMatchFound;
        _mgr.OnMatchCancelled        -= HandleMatchCancelled;
    }
    #endregion

    #region Message Handlers
    private void HandlePartyCreated(PartyCreatedMsg msg)
    {
        if (!msg.success) { ShowLoadingView($"Lỗi tạo party"); return; }
        ApplyPartyData(msg.party, isLeader: true);
        ShowInPartyView();
    }

    private void HandlePartyError(PartyErrorMsg msg)
    {
        ShowLoadingView($"Lỗi: {msg.message}");
    }

    private void HandleJoinPartyResult(JoinPartyResultMsg msg)
    {
        if (!msg.success) { ShowLoadingView($"Không vào được party: {msg.reason}"); return; }
        string mySessionId = _mgr?.SessionId;
        ApplyPartyData(msg.party, isLeader: msg.party?.leaderId == mySessionId);
        ShowInPartyView();
    }

    private void HandlePartyUpdate(PartyData party)
    {
        if (_currentParty == null) return; // Không phải panel của mình
        string mySessionId = _mgr?.SessionId;
        ApplyPartyData(party, isLeader: party.leaderId == mySessionId);
    }

    private void HandleLeadershipTransferred(LeadershipTransferredMsg msg)
    {
        // partyUpdate sẽ theo sau — UI tự cập nhật qua HandlePartyUpdate
        Debug.Log($"[PartyUI] {msg.message}");
    }

    private void HandleKickResult(KickResultMsg msg)
    {
        if (!msg.success)
            Debug.LogWarning($"[PartyUI] Kick thất bại: {msg.reason}");
    }

    private void HandleStartMatchResult(StartMatchResultMsg msg)
    {
        if (!msg.success)
            Debug.LogWarning($"[PartyUI] StartMatch thất bại: {msg.reason}");
    }

    private void HandleMatchFound(MatchFoundMsg msg)
    {
        if (!msg.isPartyMatch) return; // QueueUI xử lý

        SpawnMatchCards(msg.players);
        ShowMatchFoundView();
        StartCountdown(msg.countdown);
    }

    private void HandleMatchCancelled(MatchCancelledMsg msg)
    {
        // Chỉ xử lý nếu PartyUI đang hiện MatchFoundView (tức là party match bị huỷ).
        // Queue match bị huỷ UIManager đã xử lý đóng QueuePanel.
        if (!_matchFoundView.activeSelf) return;

        // Party vẫn còn (server restore về "waiting") → hiện lại party view
        StopCountdown();
        ClearMatchCards();
        if (_currentParty != null) ShowInPartyView();
        else                       ShowLoadingView("Trận bị huỷ.");
    }
    #endregion

    #region Apply Party Data
    private void ApplyPartyData(PartyData party, bool isLeader)
    {
        _currentParty = party;
        _isLeader     = isLeader;

        if (_txtInviteCode != null)
            _txtInviteCode.text = $"Mã mời: {party.inviteCode}";

        _btnStartMatch.gameObject.SetActive(isLeader);
        RefreshMemberCards(party);
    }

    private void RefreshMemberCards(PartyData party)
    {
        ClearMemberCards();
        if (party?.members == null) return;

        string mySessionId = _mgr?.SessionId;
        foreach (var m in party.members)
        {
            bool canKick = _isLeader && m.sessionId != mySessionId;
            CardPlayer card = Instantiate(_cardPlayerPrefab, _memberListContainer);
            card.SetInfo(
                name:         m.isLeader ? $"[Leader] {m.username}" : m.username,
                sessionId:    m.sessionId,
                showKick:     canKick,
                kickCallback: sid => _mgr?.KickPlayer(sid)
            );
            _memberCards.Add(card);
        }
    }
    #endregion

    #region Countdown
    private void StartCountdown(int seconds)
    {
        StopCountdown();
        _countdownCoroutine = StartCoroutine(CountdownCoroutine(seconds));
    }

    private void StopCountdown()
    {
        if (_countdownCoroutine == null) return;
        StopCoroutine(_countdownCoroutine);
        _countdownCoroutine = null;
    }

    private IEnumerator CountdownCoroutine(int seconds)
    {
        int remaining = seconds;
        while (remaining > 0)
        {
            _txtCountdown.text = remaining.ToString();
            yield return new WaitForSeconds(1f);
            remaining--;
        }
        _txtCountdown.text = "0";
    }
    #endregion

    #region Card Helpers
    private void SpawnMatchCards(PlayerInfo[] players)
    {
        ClearMatchCards();
        if (players == null) return;
        foreach (var p in players)
        {
            CardPlayer card = Instantiate(_cardPlayerPrefab, _playerListContainer);
            card.SetInfo(p.username);
            _matchCards.Add(card);
        }
    }

    private void ClearMemberCards()
    {
        foreach (var c in _memberCards)
            if (c != null) Destroy(c.gameObject);
        _memberCards.Clear();
    }

    private void ClearMatchCards()
    {
        foreach (var c in _matchCards)
            if (c != null) Destroy(c.gameObject);
        _matchCards.Clear();
    }
    #endregion

    #region View Helpers
    private void ShowLoadingView(string message = "Đang tải...")
    {
        _loadingView   .SetActive(true);
        _inPartyView   .SetActive(false);
        _matchFoundView.SetActive(false);
        if (_txtLoading != null) _txtLoading.text = message;
    }

    private void ShowInPartyView()
    {
        _loadingView   .SetActive(false);
        _inPartyView   .SetActive(true);
        _matchFoundView.SetActive(false);
    }

    private void ShowMatchFoundView()
    {
        _loadingView   .SetActive(false);
        _inPartyView   .SetActive(false);
        _matchFoundView.SetActive(true);
    }
    #endregion
}
