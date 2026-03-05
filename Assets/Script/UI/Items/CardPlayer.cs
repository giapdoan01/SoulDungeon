using UnityEngine;
using TMPro;
using System;

public class CardPlayer : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text   txtName;
    public GameObject btnKick; 

    private int         connId;
    private Action<int> onKickClicked;

    // ── Matchmaking: không cần Kick ──────────────────────────────
    public void SetInfo(string name)
    {
        txtName.text = name;
        if (btnKick != null) btnKick.SetActive(false);
    }

    // ── Party: leader truyền callback kick ───────────────────────
    public void SetInfo(string name, int connectionId, bool showKick, Action<int> kickCallback)
    {
        txtName.text  = name;
        connId        = connectionId;
        onKickClicked = kickCallback;
        if (btnKick != null) btnKick.SetActive(showKick);
    }

    // Kéo vào OnClick của Btn_Kick trong Inspector
    public void OnClickKick() => onKickClicked?.Invoke(connId);
}
