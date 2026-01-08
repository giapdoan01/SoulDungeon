using System;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class PanelListItem : MonoBehaviour 
{
    public GameObject panel;
    public Transform container;
    public GameObject itemPrefab;
    public MenuSelectItem menuSelectItem;

    void Start()
    {
        panel.SetActive(false);
    }

    public void ShowPanel(ListItem listItem, ItemSelectType type)
    {
        panel.SetActive(true);
        PopulateItem(itemPrefab, listItem, type);  
    }
    public void HidePanel()
    {
        panel.SetActive(false);
        ClearItems();
    }

    public void PopulateItem(GameObject itemPrefab, ListItem listItem, ItemSelectType type)
    {
        ClearItems();

        if (listItem == null || listItem.itemSprites == null)
        {
            Debug.LogWarning("Invalid listItem");
            return;
        }

        for(int i = 0; i < listItem.itemSprites.Count; i++)
        {
            GameObject item = Instantiate(itemPrefab, container);
            item.name = $"EyeItem_{i}";

            ItemImage itemImage = item.GetComponent<ItemImage>();
            if (itemImage != null)
            {
                itemImage.setupImage(listItem.itemSprites[i]);
            }

            Button button = item.GetComponent<Button>();
            if (button != null)
            {
                int index = i;
                button.onClick.AddListener(() => 
                {
                    menuSelectItem.SelectItem(index, type);
                });
            }
        }

        Debug.Log($"Populated {listItem.itemSprites.Count} eye items");
    }

    private void ClearItems()
    {
        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}
