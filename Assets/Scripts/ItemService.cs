using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemService
{
    private List<ItemData> items = new List<ItemData>();

    public IEnumerator Initialize()
    {
        string itemJson = null;
        string loadError = null;

        yield return RuntimeDataLoader.LoadDataText(
            "Data/Items/items.json",
            text => itemJson = text,
            error => loadError = error
        );

        if (!string.IsNullOrEmpty(loadError))
        {
            Debug.LogError("items.json load failed: " + loadError);
            items = new List<ItemData>();
            yield break;
        }

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
