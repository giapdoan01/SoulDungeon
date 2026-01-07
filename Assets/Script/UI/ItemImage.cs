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

        if (itemImage != null)
        {
            itemImage.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("No Image component found on ItemImage");
        }
    }
}
