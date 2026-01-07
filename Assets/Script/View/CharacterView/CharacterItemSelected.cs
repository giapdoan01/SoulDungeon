using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CharacterItemSelected : MonoBehaviour
{
    public CharacterItemModels characterItemModels;
    public List<SpriteRenderer> characterEyeRenderers;
    public List<SpriteRenderer> characterHairRenderers;
    public List<SpriteRenderer> characterBodyRenderers;
    public MenuSelectItem menuSelectItem;

    private void Start()
    {
        menuSelectItem.OnEyeSelect += SetEyeSprite;
        menuSelectItem.OnHairSelect += SetHairSprite;
        menuSelectItem.OnBodySelect += SetBodySprite;
    }

    private void OnDestroy()
    {
        menuSelectItem.OnEyeSelect -= SetEyeSprite;
        menuSelectItem.OnHairSelect -= SetHairSprite;
        menuSelectItem.OnBodySelect -= SetBodySprite;
    }   

    public void SetEyeSprite(int index)
    {
        for(int i = 0; i < characterEyeRenderers.Count; i++)
        {
            Sprite sprite = characterItemModels.EyesBackAndEyesFrontList[i].GetSpriteByIndex(index);
            if (sprite != null)
            {
                characterEyeRenderers[i].sprite = sprite;
                menuSelectItem.eyeImageButton.sprite = sprite;
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
                menuSelectItem.hairImageButton.sprite = sprite;
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
            }
        }
    }
}
