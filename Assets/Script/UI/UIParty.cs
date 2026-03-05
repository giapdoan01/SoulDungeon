using Mirror;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class UIParty : MonoBehaviour
{
    public static UIParty Instance { get; private set; }

    [Header("Status")]
    public TextMeshProUGUI txtStatus;

    [Header("Panel — Chờ server phản hồi")]
    public GameObject panelWaiting;

    [Header("Panel — Nhập code")]
    public GameObject     panelJoinInput;
    public TMP_InputField inputCode;
    public Button       btnConfirmJoin;

    [Header("Panel — Phòng chờ")]
    public GameObject      panelRoom;
    public TextMeshProUGUI txtPartyCode;
    public GameObject      btnStart;  
    public GameObject      btnLeave;

    [Header("Card")]
    public GameObject cardPlayerPrefab;
    public Transform  cardContainer;

    // ── State ─────────────────────────────────────────────────────
    private readonly List<GameObject> spawnedCards = new();
    private string myName      = "";
    private bool   isLeader    = false;
    private int    myConnId    = -1;   // server gửi về, không tự lấy từ client
    private string pendingCode = "";

    void Awake() => Instance = this;
    void Start()
    {
        btnConfirmJoin.onClick.AddListener(OnClickConfirmJoin);
        btnStart.GetComponent<Button>().onClick.AddListener(OnClickStart);
        btnLeave.GetComponent<Button>().onClick.AddListener(OnClickLeave);
    }

    // ── Subscribe / Unsubscribe ───────────────────────────────────
    void OnEnable()
    {
        if (MyNetworkManager.Instance == null) return;
        MyNetworkManager.Instance.OnConnected     += HandleConnected;
        MyNetworkManager.Instance.OnPartyCreated  += HandlePartyCreated;
        MyNetworkManager.Instance.OnPartyUpdated  += HandlePartyUpdated;
        MyNetworkManager.Instance.OnPartyKicked   += HandlePartyKicked;
        MyNetworkManager.Instance.OnPartyStarting += HandlePartyStarting;
        MyNetworkManager.Instance.OnError         += HandleError;
    }

    void OnDisable()
    {
        if (MyNetworkManager.Instance == null) return;
        MyNetworkManager.Instance.OnConnected     -= HandleConnected;
        MyNetworkManager.Instance.OnPartyCreated  -= HandlePartyCreated;
        MyNetworkManager.Instance.OnPartyUpdated  -= HandlePartyUpdated;
        MyNetworkManager.Instance.OnPartyKicked   -= HandlePartyKicked;
        MyNetworkManager.Instance.OnPartyStarting -= HandlePartyStarting;
        MyNetworkManager.Instance.OnError         -= HandleError;
    }

    // ══════════════════════════════════════════════════════════════
    // PUBLIC API — Gọi từ UIManager
    // ══════════════════════════════════════════════════════════════

    public void StartCreateParty(string playerName)
    {
        myName   = playerName;
        isLeader = true;
        ResetUI();
        panelWaiting.SetActive(true);
        SetStatus("Đang tạo phòng...");
        NetworkManager.singleton.StartClient();
    }

    public void ShowJoinInput(string playerName)
    {
        myName   = playerName;
        isLeader = false;
        ResetUI();
        panelJoinInput.SetActive(true);
        SetStatus("Nhập mã phòng...");
    }

    public void LeaveAndGoHome()
    {
        if (NetworkClient.isConnected)
        {
            NetworkClient.Send(new MsgLeaveParty());
            NetworkManager.singleton.StopClient();
        }
        ResetUI();
    }

    // ══════════════════════════════════════════════════════════════
    // BUTTON CALLBACKS — kéo vào Inspector
    // ══════════════════════════════════════════════════════════════

    public void OnClickConfirmJoin()
    {
        string code = inputCode.text.ToUpper().Trim();
        if (string.IsNullOrEmpty(code))
        {
            SetStatus("Vui lòng nhập mã phòng!");
            return;
        }
        pendingCode = code;
        panelJoinInput.SetActive(false);
        panelWaiting.SetActive(true);
        SetStatus("Đang kết nối...");
        NetworkManager.singleton.StartClient();
    }

    public void OnClickStart()
    {
        if (!isLeader) return;
        NetworkClient.Send(new MsgStartParty());
        Debug.Log("[Party] Leader sent MsgStartParty.");
    }

    public void OnClickLeave()
    {
        NetworkClient.Send(new MsgLeaveParty());
        NetworkManager.singleton.StopClient();
        UIManager.Instance.ShowHome();
    }

    // ══════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ══════════════════════════════════════════════════════════════

    // Gọi khi TCP connect xong → gửi message tương ứng lên server
    private void HandleConnected()
    {
        if (isLeader)
        {
            NetworkClient.Send(new MsgCreateParty { playerName = myName });
            Debug.Log("[Party] Sent MsgCreateParty.");
        }
        else
        {
            NetworkClient.Send(new MsgJoinParty { partyCode = pendingCode, playerName = myName });
            Debug.Log($"[Party] Sent MsgJoinParty code={pendingCode}.");
        }
    }

    // Server xác nhận tạo phòng → nhận partyCode + myConnId
    private void HandlePartyCreated(string code, int connId)
    {
        myConnId = connId; 

        panelWaiting.SetActive(false);
        panelRoom.SetActive(true);
        txtPartyCode.text = $"Mã phòng: {code}";
        btnLeave.SetActive(true);
        SetStatus("Chờ người chơi vào...");
        Debug.Log($"[Party] Created: {code} | myConnId={myConnId}");
    }

    // Server broadcast danh sách member → nhận members + leaderConnId + myConnId
    private void HandlePartyUpdated(PartyMemberData[] members, int leaderConnId, int connId)
    {
        myConnId = connId; 
        isLeader = myConnId == leaderConnId;

        panelWaiting.SetActive(false);
        panelRoom.SetActive(true);
        btnLeave.SetActive(true);
        btnStart.SetActive(isLeader);

        // Rebuild toàn bộ card
        ClearCards();
        foreach (var m in members)
        {
            bool isSelf   = m.connId == myConnId;
            bool showKick = isLeader && m.connId != leaderConnId; // leader thấy Kick trên card member
            SpawnCard(m.playerName, m.connId, isSelf, showKick);
        }

        SetStatus(isLeader
            ? $"Phòng của bạn — {members.Length}/{MyNetworkManager.Instance.playersPerMatch} người"
            : $"Chờ leader bắt đầu — {members.Length}/{MyNetworkManager.Instance.playersPerMatch} người");
    }

    private void HandlePartyKicked()
    {
        NetworkManager.singleton.StopClient();
        ResetUI();
        SetStatus("Bạn đã bị kick khỏi phòng.");
        Debug.Log("[Party] Kicked by leader.");
    }

    private void HandlePartyStarting()
    {
        btnStart.SetActive(false);
        btnLeave.SetActive(false);
        SetStatus("Đang vào trận...");
        Debug.Log("[Party] Match starting!");
    }

    private void HandleError(string error)
    {
        SetStatus(error);
        // Join thất bại → quay lại ô nhập code
        if (!isLeader)
        {
            panelWaiting.SetActive(false);
            panelJoinInput.SetActive(true);
        }
    }

    // ══════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════

    private void SpawnCard(string playerName, int connId, bool isSelf, bool showKick)
    {
        var go   = Instantiate(cardPlayerPrefab, cardContainer);
        var card = go.GetComponent<CardPlayer>();

        card.SetInfo(playerName, connId, showKick, showKick ? KickMember : null);

        if (isSelf)
        {
            var outline = go.GetComponent<UnityEngine.UI.Outline>();
            if (outline != null) outline.enabled = true;
        }
        spawnedCards.Add(go);
    }

    private void KickMember(int targetConnId)
    {
        NetworkClient.Send(new MsgKickMember { targetConnId = targetConnId });
        Debug.Log($"[Party] Kick sent → connId={targetConnId}");
    }

    private void ClearCards()
    {
        foreach (var c in spawnedCards) if (c != null) Destroy(c);
        spawnedCards.Clear();
    }

    private void ResetUI()
    {
        ClearCards();
        myConnId = -1;
        panelWaiting.SetActive(false);
        panelJoinInput.SetActive(false);
        panelRoom.SetActive(false);
        btnStart.SetActive(false);
        btnLeave.SetActive(false);
        if (txtPartyCode) txtPartyCode.text = "";
        if (inputCode)    inputCode.text    = "";
        pendingCode = "";
    }

    public void SetStatus(string msg)
    {
        if (txtStatus) txtStatus.text = msg;
    }
}
