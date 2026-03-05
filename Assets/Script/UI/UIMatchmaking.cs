using Mirror;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class UIMatchmaking : MonoBehaviour
{
    public static UIMatchmaking Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI txtStatus;
    public TextMeshProUGUI txtCountdown;
    public GameObject      btnCancel;

    [Header("Card")]
    public GameObject cardPlayerPrefab;
    public Transform  cardContainer;

    private readonly List<GameObject> spawnedCards = new();
    private Coroutine countdownCoroutine;
    private bool      isInCountdown = false;
    private string    myName        = "";

    void Awake() => Instance = this;

    // ── Subscribe / Unsubscribe ───────────────────────────────────
    void OnEnable()
    {
        if (MyNetworkManager.Instance == null) return;
        MyNetworkManager.Instance.OnConnected      += HandleConnected;
        MyNetworkManager.Instance.OnMatchFound     += HandleMatchFound;
        MyNetworkManager.Instance.OnMatchCancelled += HandleMatchCancelled;
    }

    void OnDisable()
    {
        if (MyNetworkManager.Instance == null) return;
        MyNetworkManager.Instance.OnConnected      -= HandleConnected;
        MyNetworkManager.Instance.OnMatchFound     -= HandleMatchFound;
        MyNetworkManager.Instance.OnMatchCancelled -= HandleMatchCancelled;
    }

    // ══════════════════════════════════════════════════════════════
    // PUBLIC API
    // ══════════════════════════════════════════════════════════════

    public void StartFindMatch(string playerName)
    {
        myName = playerName;
        ClearCards();
        isInCountdown = false;
        txtCountdown.gameObject.SetActive(false);
        txtCountdown.text = "";
        btnCancel.SetActive(true);
        SetStatus("Đang tìm đối thủ...");
        SpawnCard(myName, isSelf: true);
        NetworkManager.singleton.StartClient();
    }

    public void CancelFindMatch()
    {
        StopCountdown();
        if (isInCountdown) NetworkClient.Send(new MsgCancelMatch());
        isInCountdown = false;
        ClearCards();
        txtCountdown.gameObject.SetActive(false);
        btnCancel.SetActive(true);
        NetworkManager.singleton.StopClient();
    }

    // ══════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ══════════════════════════════════════════════════════════════

    private void HandleConnected()
    {
        NetworkClient.Send(new MsgJoinQueue { playerName = myName });
        Debug.Log("[Matchmaking] Sent MsgJoinQueue.");
    }

    private void HandleMatchFound(string[] playerNames)
    {
        ClearCards();
        for (int i = 0; i < playerNames.Length; i++)
            SpawnCard(playerNames[i], isSelf: i == 0);
        SetStatus("Đã tìm thấy trận!");
        StopCountdown();
        countdownCoroutine = StartCoroutine(CountdownRoutine(15));
    }

    private void HandleMatchCancelled()
    {
        StopCountdown();
        isInCountdown = false;
        txtCountdown.gameObject.SetActive(false);
        txtCountdown.text = "";
        btnCancel.SetActive(true);

        // Xoá card đối thủ, giữ card mình
        for (int i = spawnedCards.Count - 1; i >= 1; i--)
        {
            if (spawnedCards[i] != null) Destroy(spawnedCards[i]);
            spawnedCards.RemoveAt(i);
        }
        SetStatus("Đối thủ đã huỷ. Đang tìm lại...");
    }

    // ══════════════════════════════════════════════════════════════
    // COUNTDOWN
    // ══════════════════════════════════════════════════════════════

    private IEnumerator CountdownRoutine(int seconds)
    {
        isInCountdown = true;
        txtCountdown.gameObject.SetActive(true);
        for (int i = seconds; i > 0; i--)
        {
            txtCountdown.text = $"Vào trận sau: {i}s";
            yield return new WaitForSeconds(1f);
        }
        isInCountdown = false;
        txtCountdown.text = "Đang vào trận...";
        btnCancel.SetActive(false);
        countdownCoroutine = null;
    }

    private void StopCountdown()
    {
        if (countdownCoroutine == null) return;
        StopCoroutine(countdownCoroutine);
        countdownCoroutine = null;
    }

    // ══════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════

    private void SpawnCard(string playerName, bool isSelf)
    {
        var go   = Instantiate(cardPlayerPrefab, cardContainer);
        var card = go.GetComponent<CardPlayer>();
        card.SetInfo(playerName); // Matchmaking: không cần Kick
        if (isSelf)
        {
            var outline = go.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null) outline.enabled = true;
        }
        spawnedCards.Add(go);
    }

    private void ClearCards()
    {
        foreach (var c in spawnedCards) if (c != null) Destroy(c);
        spawnedCards.Clear();
    }

    public void SetStatus(string msg) { if (txtStatus) txtStatus.text = msg; }
}
