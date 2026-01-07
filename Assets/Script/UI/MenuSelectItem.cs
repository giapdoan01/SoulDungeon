using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSelectItem : MonoBehaviour
{
    [Header("Eye Selection")]
    public ListItem eyeImages;
    public Button openListEyeButton;
    public Image eyeImageButton;
    [Header("Hair Selection")]
    public ListItem hairImages;
    public Button openListHairButton;
    public Image hairImageButton;
    [Header("Body Selection")]
    public ListItem bodyImages;
    public Button openListBodyButton;
    [Header("Panel List")]
    public PanelListItem panelListItem;

    public Action<int> OnEyeSelect;
    public Action<int> OnHairSelect;
    public Action<int> OnBodySelect;

    void Start()
    {
        openListEyeButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(eyeImages, MenuSelectType.Eye);
        });
        openListHairButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(hairImages, MenuSelectType.Hair);
        });
    }

    public void SelectEyeIndex(int eyeIndex)
    {
        OnEyeSelect?.Invoke(eyeIndex);
    }

    public void SelectHairIndex(int hairIndex)
    {
        OnHairSelect?.Invoke(hairIndex);
    }

    public void SelectBodyIndex(int bodyIndex)
    {
        OnBodySelect?.Invoke(bodyIndex);
    }
    public void SelectItem(int itemIndex, MenuSelectType type)
    {
        switch(type)
        {
            case MenuSelectType.Eye:
                OnEyeSelect?.Invoke(itemIndex);
                break;
            case MenuSelectType.Hair:
                OnHairSelect?.Invoke(itemIndex);
                break;
            case MenuSelectType.Body:
                OnBodySelect?.Invoke(itemIndex);
                break;
            default:
                Debug.LogWarning("Unknown MenuSelectType");
                break;
        }
    }
}
public enum MenuSelectType
{
    Eye,
    Hair,
    Body
}