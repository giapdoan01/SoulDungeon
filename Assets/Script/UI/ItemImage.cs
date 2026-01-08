using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemImage : MonoBehaviour 
{
    public Image itemImage;

    public void setupImage(Sprite sprite)
    {
        if (itemImage == null)
        {
            itemImage = GetComponent<Image>();
        }
        SetSizeImage.Instance.SetSizeSprite(itemImage, sprite);
    }
}
