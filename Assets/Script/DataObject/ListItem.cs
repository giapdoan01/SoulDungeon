using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ListItem", menuName = "Scriptable Objects/ListItem")]
public class ListItem : ScriptableObject
{
    public List<Sprite> itemSprites;

    public bool IsValidIndex(int index)
    {
        return index >= 0 && index < itemSprites.Count;
    }

    public Sprite GetSpriteByIndex(int index)
    {
        if (IsValidIndex(index))
        {
            return itemSprites[index];
        }
        else
        {
            Debug.LogWarning("Index out of range: " + index);
            return null;
        }
    }
}
