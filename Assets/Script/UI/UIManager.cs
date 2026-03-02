using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject panelHome;
    public GameObject panelMatchmaking;

    [Header("Matchmaking UI")]
    public TextMeshProUGUI txtStatus;
    public TextMeshProUGUI txtPartyCode;
    public TMP_InputField  inputPartyCode;
    public Button          btnJoinParty;

    private enum Mode { None, FindMatch, CreateParty, JoinParty }
    private Mode currentMode = Mode.None;

    // ── Lifecycle ────────────────────────────────────────────────────
    void Awake() => Instance = this;
    void Start()  => ShowHome();

    // ══════════════════════════════════════════════════════════════
    // PANEL CONTROL
    // ══════════════════════════════════════════════════════════════

    public void ShowHome()
    {
        panelHome.SetActive(true);
        panelMatchmaking.SetActive(false);
        currentMode = Mode.None;
    }

    private void ShowMatchmaking(string status)
    {
        panelHome.SetActive(false);
        panelMatchmaking.SetActive(true);

        txtStatus.text = status;
        txtPartyCode.gameObject.SetActive(false);
        inputPartyCode.gameObject.SetActive(false);
        btnJoinParty.gameObject.SetActive(false);
    }

    private void ShowJoinPartyInput()
    {
        panelHome.SetActive(false);
        panelMatchmaking.SetActive(true);

        txtStatus.text = "Nhập mã phòng của bạn bè:";
        txtPartyCode.gameObject.SetActive(false);
        inputPartyCode.gameObject.SetActive(true);   // ← hiện input
        btnJoinParty.gameObject.SetActive(true);     // ← hiện nút Join
    }

    // ══════════════════════════════════════════════════════════════
    // BUTTON CALLBACKS — HOME
    // ══════════════════════════════════════════════════════════════

    public void OnClickFindMatch()
    {
        currentMode = Mode.FindMatch;
        ShowMatchmaking("Đang kết nối...");
        NetworkManager.singleton.StartClient();
    }

    public void OnClickCreateParty()
    {
        currentMode = Mode.CreateParty;
        ShowMatchmaking("Đang tạo phòng...");
        NetworkManager.singleton.StartClient();
    }

    public void OnClickOpenJoinParty()
    {
        // Chỉ hiện UI nhập mã, chưa kết nối
        currentMode = Mode.JoinParty;
        ShowJoinPartyInput();
    }

    // ══════════════════════════════════════════════════════════════
    // BUTTON CALLBACKS — MATCHMAKING
    // ══════════════════════════════════════════════════════════════

    public void OnClickJoinParty()
    {
        string code = inputPartyCode.text.ToUpper().Trim();

        if (string.IsNullOrEmpty(code))
        {
            txtStatus.text = "Vui lòng nhập mã phòng!";
            return;
        }

        ShowMatchmaking($"Đang vào phòng [{code}]...");
        NetworkManager.singleton.StartClient();
    }

    public void OnClickCancel()
    {
        NetworkManager.singleton.StopHost();
        NetworkManager.singleton.StopClient();
        ShowHome();
    }

    // ══════════════════════════════════════════════════════════════
    // GỌI KHI CLIENT ĐÃ KẾT NỐI → Gửi message lên server
    // ══════════════════════════════════════════════════════════════

    public void OnClientConnected()
    {
        Debug.Log($"[UI] Connected. Sending mode: {currentMode}");

        switch (currentMode)
        {
            case Mode.FindMatch:
                NetworkClient.Send(new MsgJoinQueue());
                SetStatus("Đang tìm đối thủ...");
                break;

            case Mode.CreateParty:
                NetworkClient.Send(new MsgCreateParty());
                SetStatus("Đang tạo phòng...");
                break;

            case Mode.JoinParty:
                string code = inputPartyCode.text.ToUpper().Trim();
                NetworkClient.Send(new MsgJoinParty { partyCode = code });
                SetStatus($"Đang vào phòng [{code}]...");
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════
    // UPDATE UI TỪ SERVER
    // ══════════════════════════════════════════════════════════════

    public void SetPartyCode(string code)
    {
        txtPartyCode.gameObject.SetActive(true);
        txtPartyCode.text = $"Mã phòng: {code}";
    }

    public void SetStatus(string status)
    {
        txtStatus.text = status;
    }
}
