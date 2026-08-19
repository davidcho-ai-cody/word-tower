using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ItemService
{
    private List<ItemData> items = new List<ItemData>();

    public void Initialize()
    {
        string itemPath = Path.Combine(
            Application.dataPath,
            "Data/Items/items.json"
        );

        if (!File.Exists(itemPath))
        {
            Debug.LogError("items.json 파일을 찾을 수 없습니다.");
            items = new List<ItemData>();
            return;
        }

        string itemJson = File.ReadAllText(itemPath);
        ItemDataList itemList =
            JsonUtility.FromJson<ItemDataList>(itemJson);

        items = itemList != null && itemList.items != null
            ? itemList.items
            : new List<ItemData>();

        Debug.Log($"Item Data Loaded: {items.Count}");
    }

    public ItemData GetItem(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return items.FirstOrDefault(item => item.id == id);
    }

    public List<ItemData> GetItemsByType(ItemType itemType)
    {
        return items
            .Where(item => item.GetItemType() == itemType)
            .ToList();
    }

    public List<ItemData> GetAllItems()
    {
        return new List<ItemData>(items);
    }
}
