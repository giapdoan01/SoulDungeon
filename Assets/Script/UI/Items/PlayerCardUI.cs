using UnityEngine;
using TMPro;

public class PlayerCardUI : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text playerLevelText;
    
    public void SetPlayerInfo(string name, int level = 1)
    {
        if (playerNameText != null)
            playerNameText.text = name;
            
        if (playerLevelText != null)
            playerLevelText.text = $"Lv.{level}";
    }
}