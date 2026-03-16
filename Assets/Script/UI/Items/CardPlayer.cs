using UnityEngine;
using TMPro;
using System;

public class CardPlayer : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text   txtName;
    public GameObject btnKick;

    private string         _sessionId;
    private Action<string> _onKickClicked;

    // ── Queue / MatchFound: không cần Kick ───────────────────────
    public void SetInfo(string name)
    {
        txtName.text = name;
        if (btnKick != null) btnKick.SetActive(false);
    }

    // ── Party: leader truyền callback kick ───────────────────────
    public void SetInfo(string name, string sessionId, bool showKick, Action<string> kickCallback)
    {
        txtName.text   = name;
        _sessionId     = sessionId;
        _onKickClicked = kickCallback;
        if (btnKick != null) btnKick.SetActive(showKick);
    }

    // Kéo vào OnClick của Btn_Kick trong Inspector
    public void OnClickKick() => _onKickClicked?.Invoke(_sessionId);
}
