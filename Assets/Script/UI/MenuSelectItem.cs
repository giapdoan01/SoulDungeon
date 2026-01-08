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
    public Image bodyImageButton;
    [Header("Face Hair Selection")]
    public ListItem faceHairImages;
    public Button openListFaceHairButton;
    public Image faceHairImageButton;
    [Header("Cloth Selection")]
    public ListItem clothImages;
    public Button openListClothButton;
    public Image clothImageButton;
    [Header("Pant Selection")]
    public ListItem pantImages;
    public Button openListPantButton;
    public Image pantImageButton;
    [Header("Helmet Selection")]
    public ListItem helmetImages;
    public Button openListHelmetButton;
    public Image helmetImageButton;
    [Header("Back Selection")]
    public ListItem backImages;
    public Button openListBackButton;
    public Image backImageButton;
    [Header("Panel List")]
    public PanelListItem panelListItem;

    public Action<int> OnEyeSelect;
    public Action<int> OnHairSelect;
    public Action<int> OnBodySelect;
    public Action<int> OnFaceHairSelect;
    public Action<int> OnClothSelect;
    public Action<int> OnPantSelect;
    public Action<int> OnHelmetSelect;
    public Action<int> OnBackSelect;
    void Start()
    {
        openListEyeButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(eyeImages, ItemSelectType.Eye);
        });
        openListHairButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(hairImages, ItemSelectType.Hair);
        });
        openListBodyButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(bodyImages, ItemSelectType.Body);
        });
        openListFaceHairButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(faceHairImages, ItemSelectType.FaceHair);
        });
        openListClothButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(clothImages, ItemSelectType.Cloth);
        });
        openListPantButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(pantImages, ItemSelectType.Pant);
        });
        openListHelmetButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(helmetImages, ItemSelectType.Helmet);
        });
        openListBackButton.onClick.AddListener(() => 
        {
            panelListItem.ShowPanel(backImages, ItemSelectType.Back);
        });
        SelectEyeIndex(0);
        SelectHairIndex(0);
        SelectBodyIndex(0);
        SelectFaceHairIndex(0);
        SelectClothIndex(0);
        SelectPantIndex(0);
        SelectHelmetIndex(0);
        SelectBackIndex(0);
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

    private void SelectFaceHairIndex(int faceHairIndex)
    {
        OnFaceHairSelect?.Invoke(faceHairIndex);
    }
    private void SelectClothIndex(int clothIndex)
    {
        OnClothSelect?.Invoke(clothIndex);
    }
    private void SelectPantIndex(int pantIndex)
    {
        OnPantSelect?.Invoke(pantIndex);
    }
    private void SelectHelmetIndex(int helmetIndex)
    {
        OnHelmetSelect?.Invoke(helmetIndex);
    }
    private void SelectBackIndex(int backIndex)
    {
        OnBackSelect?.Invoke(backIndex);
    }
    public void SelectItem(int itemIndex, ItemSelectType type)
    {
        switch(type)
        {
            case ItemSelectType.Eye:
                OnEyeSelect?.Invoke(itemIndex);
                break;
            case ItemSelectType.Hair:
                OnHairSelect?.Invoke(itemIndex);
                break;
            case ItemSelectType.Body:
                OnBodySelect?.Invoke(itemIndex);
                break;
            case ItemSelectType.FaceHair:
                OnFaceHairSelect?.Invoke(itemIndex);
                break;
            case ItemSelectType.Cloth:
                OnClothSelect?.Invoke(itemIndex); 
                break;
            case ItemSelectType.Pant:
                OnPantSelect?.Invoke(itemIndex);
                break;
            case ItemSelectType.Helmet:
                OnHelmetSelect?.Invoke(itemIndex);
                break;
            case ItemSelectType.Back:
                OnBackSelect?.Invoke(itemIndex);
                break;
            default:
                Debug.LogWarning("Unknown ItemSelectType");
                break;
        }
    }

}
public enum ItemSelectType
{
    Eye,
    Hair,
    Body,
    FaceHair,
    Cloth,
    Pant,
    Helmet,
    Back
}