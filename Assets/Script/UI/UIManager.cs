using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject panelHome;
    public GameObject panelMatchmaking;
    public GameObject panelParty;

    [Header("Button")]
    public Button btnFindMatch;
    public Button btnCreateParty;
    public Button btnJoinParty;
    public Button btnMatchMakingCancel;
    public Button btnPartyCancel;

    private enum ActiveFlow { None, Matchmaking, Party }
    private ActiveFlow activeFlow = ActiveFlow.None;

    void Awake() => Instance = this;
    void Start()
    {
        ShowHome();
        btnFindMatch.onClick.AddListener(OnClickFindMatch);
        btnCreateParty.onClick.AddListener(OnClickCreateParty);
        btnJoinParty.onClick.AddListener(OnClickOpenJoinParty);
        btnMatchMakingCancel.onClick.AddListener(OnClickCancel);
        btnPartyCancel.onClick.AddListener(OnClickCancel);
    }

    // ── Subscribe / Unsubscribe ───────────────────────────────────
    void OnEnable()
    {
        if (MyNetworkManager.Instance == null) return;
        MyNetworkManager.Instance.OnDisconnected += HandleDisconnected;
    }

    void OnDisable()
    {
        if (MyNetworkManager.Instance == null) return;
        MyNetworkManager.Instance.OnDisconnected -= HandleDisconnected;
    }

    // ══════════════════════════════════════════════════════════════
    // PANEL CONTROL
    // ══════════════════════════════════════════════════════════════

    public void ShowHome()
    {
        panelHome.SetActive(true);
        panelMatchmaking.SetActive(false);
        panelParty.SetActive(false);
        activeFlow = ActiveFlow.None;
    }

    // ══════════════════════════════════════════════════════════════
    // BUTTON CALLBACKS
    // ══════════════════════════════════════════════════════════════

    public void OnClickFindMatch()
    {
        activeFlow = ActiveFlow.Matchmaking;
        panelHome.SetActive(false);
        panelMatchmaking.SetActive(true);
        UIMatchmaking.Instance.StartFindMatch(AuthManager.Instance.currentUser.username);
    }

    public void OnClickCreateParty()
    {
        activeFlow = ActiveFlow.Party;
        panelHome.SetActive(false);
        panelParty.SetActive(true);
        UIParty.Instance.StartCreateParty(AuthManager.Instance.currentUser.username);
    }

    public void OnClickOpenJoinParty()
    {
        activeFlow = ActiveFlow.Party;
        panelHome.SetActive(false);
        panelParty.SetActive(true);
        UIParty.Instance.ShowJoinInput(AuthManager.Instance.currentUser.username);
    }

    public void OnClickCancel()
    {
        switch (activeFlow)
        {
            case ActiveFlow.Matchmaking: UIMatchmaking.Instance.CancelFindMatch(); break;
            case ActiveFlow.Party:       UIParty.Instance.LeaveAndGoHome();        break;
        }
        ShowHome();
    }

    // ══════════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ══════════════════════════════════════════════════════════════

    // Bị disconnect ngoài ý muốn → về Home
    private void HandleDisconnected() => ShowHome();
}
