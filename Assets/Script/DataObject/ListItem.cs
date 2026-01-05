using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ListItem", menuName = "Scriptable Objects/ListItem")]
public class ListItem : ScriptableObject
{
    public List<Sprite> itemSprites;
}
