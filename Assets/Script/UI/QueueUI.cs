// QueueUI.cs
// Nội dung bên trong QueuePanel — chỉ xử lý display, KHÔNG quản lý panel visibility.
// UIManager chịu trách nhiệm mở/đóng QueuePanel.
//
// Hierarchy gợi ý (bên trong QueuePanel):
//   QueuePanel
//   ├── QueueView
//   │   ├── Txt_Status        "Đang tìm trận..."
//   │   └── Btn_CancelQueue
//   └── MatchFoundView        ← ẩn lúc đầu
//       ├── Txt_Countdown
//       ├── PlayerList        ← spawn CardPlayer
//       └── Btn_CancelMatch
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QueueUI : MonoBehaviour
{
    #region Inspector References
    [Header("Views")]
    [SerializeField] private GameObject _queueView;
    [SerializeField] private GameObject _matchFoundView;

    [Header("Queue View")]
    [SerializeField] private TMP_Text _txtStatus;
    [SerializeField] private Button   _btnCancelQueue;

    [Header("Match Found View")]
    [SerializeField] private TMP_Text  _txtCountdown;
    [SerializeField] private Transform _playerListContainer;
    [SerializeField] private CardPlayer _cardPlayerPrefab;
    [SerializeField] private Button    _btnCancelMatch;
    #endregion

    #region Private State
    private MatchmakingRoomManager _mgr;
    private Coroutine _countdownCoroutine;
    private readonly List<CardPlayer> _spawnedCards = new();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _btnCancelQueue.onClick.AddListener(() => _mgr?.LeaveQueue());
        _btnCancelMatch.onClick.AddListener(() => _mgr?.CancelMatch());
    }

    // OnEnable gọi mỗi khi QueuePanel bật lên — reset về trạng thái "searching"
    private void OnEnable()
    {
        _mgr = MatchmakingRoomManager.Instance;
        SubscribeEvents();
        ShowQueueView();
        SetStatus("Đang tìm trận...");
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
        StopCountdown();
        ClearPlayerCards();
    }
    #endregion

    #region Event Subscription
    private void SubscribeEvents()
    {
        if (_mgr == null) return;
        _mgr.OnQueueJoined    += HandleQueueJoined;
        _mgr.OnQueueError     += HandleQueueError;
        _mgr.OnMatchFound     += HandleMatchFound;
        _mgr.OnMatchCancelled += HandleMatchCancelled;
    }

    private void UnsubscribeEvents()
    {
        if (_mgr == null) return;
        _mgr.OnQueueJoined    -= HandleQueueJoined;
        _mgr.OnQueueError     -= HandleQueueError;
        _mgr.OnMatchFound     -= HandleMatchFound;
        _mgr.OnMatchCancelled -= HandleMatchCancelled;
    }
    #endregion

    #region Message Handlers
    private void HandleQueueJoined(QueueJoinedMsg msg)
    {
        SetStatus($"Đang tìm trận... (#{msg.position})");
    }

    private void HandleQueueError(QueueErrorMsg msg)
    {
        SetStatus($"Lỗi: {msg.message}");
    }

    private void HandleMatchFound(MatchFoundMsg msg)
    {
        if (msg.isPartyMatch) return; // PartyUI xử lý

        Debug.Log($"[QueueUI] matchFound — countdown:{msg.countdown}, players:{msg.players?.Length}, isPartyMatch:{msg.isPartyMatch}");
        Debug.Log($"[QueueUI] _matchFoundView={_matchFoundView}, _txtCountdown={_txtCountdown}, _cardPlayerPrefab={_cardPlayerPrefab}");

        SpawnPlayerCards(msg.players);
        ShowMatchFoundView();
        StartCountdown(msg.countdown);
    }

    private void HandleMatchCancelled(MatchCancelledMsg msg)
    {
        // UIManager sẽ đóng panel khi queue bị huỷ.
        // Dừng countdown để tránh nhấp nháy.
        StopCountdown();
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

    #region CardPlayer
    private void SpawnPlayerCards(PlayerInfo[] players)
    {
        ClearPlayerCards();
        if (players == null) return;
        foreach (var p in players)
        {
            CardPlayer card = Instantiate(_cardPlayerPrefab, _playerListContainer);
            card.SetInfo(p.username);
            _spawnedCards.Add(card);
        }
    }

    private void ClearPlayerCards()
    {
        foreach (var c in _spawnedCards)
            if (c != null) Destroy(c.gameObject);
        _spawnedCards.Clear();
    }
    #endregion

    #region View Helpers
    private void ShowQueueView()
    {
        _queueView     .SetActive(true);
        _matchFoundView.SetActive(false);
    }

    private void ShowMatchFoundView()
    {
        _queueView     .SetActive(false);
        _matchFoundView.SetActive(true);
    }

    private void SetStatus(string text)
    {
        if (_txtStatus != null) _txtStatus.text = text;
    }
    #endregion
}
