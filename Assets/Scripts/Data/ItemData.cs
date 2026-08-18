using System;
using System.Collections.Generic;

public enum ItemType
{
    Unknown,
    Weapon,
    Armor
}

[Serializable]
public class ItemData
{
    public string id;
    public string name;
    public string type;
    public int price;
    public int attackBonus;
    public float defenseRate;
    public string spritePath;
    public string characterSpritePath;
    public string description;

    public ItemType GetItemType()
    {
        if (Enum.TryParse(type, true, out ItemType itemType))
            return itemType;

        return ItemType.Unknown;
    }
}

[Serializable]
public class ItemDataList
{
    public List<ItemData> items;
}
