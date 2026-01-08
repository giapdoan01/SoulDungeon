using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CharacterItemSelected : MonoBehaviour
{
    public CharacterItemModels characterItemModels;
    public List<SpriteRenderer> characterEyeRenderers;
    public List<SpriteRenderer> characterHairRenderers;
    public List<SpriteRenderer> characterBodyRenderers;
    public List<SpriteRenderer> characterFaceHairRenderers;
    public List<SpriteRenderer> characterClothRenderers;
    public List<SpriteRenderer> characterPantRenderers;
    public List<SpriteRenderer> characterHelmetRenderers;
    public List<SpriteRenderer> characterBackRenderers;
    public MenuSelectItem menuSelectItem;

    private void Awake()
    {
        menuSelectItem.OnEyeSelect += SetEyeSprite;
        menuSelectItem.OnHairSelect += SetHairSprite;
        menuSelectItem.OnBodySelect += SetBodySprite;
        menuSelectItem.OnFaceHairSelect += SetFaceHairSprite;
        menuSelectItem.OnClothSelect += SetClothSprite;
        menuSelectItem.OnPantSelect += SetPantSprite;
        menuSelectItem.OnHelmetSelect += SetHelmetSprite;
        menuSelectItem.OnBackSelect += SetBackSprite;
    }

    private void Start()
    {
        
    }

    private void OnDestroy()
    {
        menuSelectItem.OnEyeSelect -= SetEyeSprite;
        menuSelectItem.OnHairSelect -= SetHairSprite;
        menuSelectItem.OnBodySelect -= SetBodySprite;
        menuSelectItem.OnFaceHairSelect -= SetFaceHairSprite;
        menuSelectItem.OnClothSelect -= SetClothSprite;
        menuSelectItem.OnPantSelect -= SetPantSprite;
        menuSelectItem.OnHelmetSelect -= SetHelmetSprite;
        menuSelectItem.OnBackSelect -= SetBackSprite;
    }   

    public void SetEyeSprite(int index)
    {
        for(int i = 0; i < characterEyeRenderers.Count; i++)
        {
            Sprite sprite = characterItemModels.EyesBackAndEyesFrontList[i].GetSpriteByIndex(index);
            if (sprite != null)
            {
                characterEyeRenderers[i].sprite = sprite;
                Sprite menuImageEyeButton = menuSelectItem.eyeImages.GetSpriteByIndex(index);
                // menuSelectItem.eyeImageButton.sprite = menuImageEyeButton;
                SetSizeImage.Instance.SetSizeSprite(menuSelectItem.eyeImageButton, menuImageEyeButton);
            }
        }
    }
    public void SetHairSprite(int index)
    {
        for(int i = 0; i < characterHairRenderers.Count; i++)
        {
            Sprite sprite = characterItemModels.HairList[i].GetSpriteByIndex(index);
            if (sprite != null)
            {
                characterHairRenderers[i].sprite = sprite;
                Sprite menuImageHairButton = menuSelectItem.hairImages.GetSpriteByIndex(index);
                // menuSelectItem.hairImageButton.sprite = menuImageHairButton;
                SetSizeImage.Instance.SetSizeSprite(menuSelectItem.hairImageButton, menuImageHairButton);
            }
        }
    }
    public void SetBodySprite(int index)
    {
        for(int i = 0; i < characterBodyRenderers.Count; i++)
        {
            Sprite sprite = characterItemModels.BodyList[i].GetSpriteByIndex(index);
            if (sprite != null)
            {
                characterBodyRenderers[i].sprite = sprite;
                Sprite menuImageBodyButton = menuSelectItem.bodyImages.GetSpriteByIndex(index);
                // menuSelectItem.bodyImageButton.sprite = menuImageBodyButton;
                SetSizeImage.Instance.SetSizeSprite(menuSelectItem.bodyImageButton, menuImageBodyButton);
            }
        }
    }
    public void SetFaceHairSprite(int index)
    {
        for(int i = 0; i < characterFaceHairRenderers.Count; i++)
        {
            Sprite sprite = characterItemModels.FaceHairList[i].GetSpriteByIndex(index);
            if (sprite != null)
            {
                characterFaceHairRenderers[i].sprite = sprite;
                Sprite menuImageFaceHairButton = menuSelectItem.faceHairImages.GetSpriteByIndex(index);
                // menuSelectItem.faceHairImageButton.sprite = menuImageFaceHairButton;
                SetSizeImage.Instance.SetSizeSprite(menuSelectItem.faceHairImageButton, menuImageFaceHairButton);
            }
        }
    }
    public void SetClothSprite(int index)
    {
        for(int i = 0; i < characterClothRenderers.Count; i++)
        {
            Sprite sprite = characterItemModels.ClothList[i].GetSpriteByIndex(index);
            if (sprite != null)
            {
                characterClothRenderers[i].sprite = sprite;
                Sprite menuImageClothButton = menuSelectItem.clothImages.GetSpriteByIndex(index);
                // menuSelectItem.clothImageButton.sprite = menuImageClothButton;
                SetSizeImage.Instance.SetSizeSprite(menuSelectItem.clothImageButton, menuImageClothButton);
            }
        }
    }
    public void SetPantSprite(int index)
    {
        for(int i = 0; i < characterPantRenderers.Count; i++)
        {
            Sprite sprite = characterItemModels.PantList[i].GetSpriteByIndex(index);
            if (sprite != null)
            {
                characterPantRenderers[i].sprite = sprite;
                Sprite menuImagePantButton = menuSelectItem.pantImages.GetSpriteByIndex(index);
                // menuSelectItem.pantImageButton.sprite = menuImagePantButton;
                SetSizeImage.Instance.SetSizeSprite(menuSelectItem.pantImageButton, menuImagePantButton);
            }
        }
    }
    public void SetHelmetSprite(int index)
    {
        for(int i = 0; i < characterHelmetRenderers.Count; i++)
        {
            Sprite sprite = characterItemModels.HelmetList[i].GetSpriteByIndex(index);
            if (sprite != null)
            {
                characterHelmetRenderers[i].sprite = sprite;
                Sprite menuImageHelmetButton = menuSelectItem.helmetImages.GetSpriteByIndex(index);
                // menuSelectItem.helmetImageButton.sprite = menuImageHelmetButton;
                SetSizeImage.Instance.SetSizeSprite(menuSelectItem.helmetImageButton, menuImageHelmetButton);
            }
        }
    }
    public void SetBackSprite(int index)
    {
        for(int i = 0; i < characterBackRenderers.Count; i++)
        {
            Sprite sprite = characterItemModels.BackList[i].GetSpriteByIndex(index);
            if (sprite != null)
            {
                characterBackRenderers[i].sprite = sprite;
                Sprite menuImageBackButton = menuSelectItem.backImages.GetSpriteByIndex(index);
                // menuSelectItem.backImageButton.sprite = menuImageBackButton;
                SetSizeImage.Instance.SetSizeSprite(menuSelectItem.backImageButton, menuImageBackButton);
            }
        }
    }
}
