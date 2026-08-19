public class PlayerProgressService
{
    public PlayerProgressData Data { get; private set; }

    public int PlayerLevel => Data.playerLevel;
    public int Exp => Data.exp;
    public int RequiredExp => Data.requiredExp;
    public int Gold => Data.gold;
    public int PlayerMaxHp => Data.playerMaxHp;
    public int PlayerAttack => Data.playerAttack;
    public string EquippedWeaponId => Data.equippedWeaponId;
    public string EquippedArmorId => Data.equippedArmorId;

    public PlayerProgressService(PlayerProgressData data = null)
    {
        SetData(data);
    }

    public void SetData(PlayerProgressData data)
    {
        Data = data ?? new PlayerProgressData();
        NormalizeData();
    }

    public void Reset()
    {
        SetData(new PlayerProgressData());
    }

    private void NormalizeData()
    {
        if (Data.playerLevel <= 0)
            Data.playerLevel = 1;

        if (Data.requiredExp <= 0)
            Data.requiredExp = 100;

        if (Data.playerMaxHp <= 0)
            Data.playerMaxHp = 100;

        if (Data.playerAttack <= 0)
            Data.playerAttack = 20;

        if (Data.exp < 0)
            Data.exp = 0;

        if (Data.gold < 0)
            Data.gold = 0;

        if (string.IsNullOrEmpty(Data.equippedWeaponId))
            Data.equippedWeaponId = PlayerProgressData.DefaultWeaponId;

        if (string.IsNullOrEmpty(Data.equippedArmorId))
            Data.equippedArmorId = PlayerProgressData.DefaultArmorId;

        EnsureOwnedItem(PlayerProgressData.DefaultWeaponId);
        EnsureOwnedItem(PlayerProgressData.DefaultArmorId);
        RemoveDuplicateOwnedItems();
    }

    public bool AddExp(int amount)
    {
        if (amount <= 0)
            return false;

        Data.exp += amount;
        return CheckLevelUp();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        Data.gold += amount;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0)
            return false;

        if (Data.gold < amount)
            return false;

        Data.gold -= amount;
        return true;
    }

    public bool AddOwnedItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        if (OwnsItem(itemId))
            return false;

        EnsureOwnedItem(itemId);
        return true;
    }

    public void EnsureOwnedItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        if (Data.ownedItemIds == null)
            Data.ownedItemIds = new System.Collections.Generic.List<string>();

        if (!Data.ownedItemIds.Contains(itemId))
            Data.ownedItemIds.Add(itemId);
    }

    public bool OwnsItem(string itemId)
    {
        return Data.ownedItemIds != null &&
            Data.ownedItemIds.Contains(itemId);
    }

    public bool TryEquipItem(ItemData item)
    {
        if (item == null || !OwnsItem(item.id))
            return false;

        switch (item.GetItemType())
        {
            case ItemType.Weapon:
                Data.equippedWeaponId = item.id;
                return true;

            case ItemType.Armor:
                Data.equippedArmorId = item.id;
                return true;

            default:
                return false;
        }
    }

    private void RemoveDuplicateOwnedItems()
    {
        if (Data.ownedItemIds == null)
            return;

        Data.ownedItemIds =
            new System.Collections.Generic.List<string>(
                new System.Collections.Generic.HashSet<string>(
                    Data.ownedItemIds
                )
            );
    }

    private bool CheckLevelUp()
    {
        bool didLevelUp = false;

        while (Data.exp >= Data.requiredExp)
        {
            Data.exp -= Data.requiredExp;
            Data.playerLevel++;
            didLevelUp = true;

            Data.playerMaxHp += 10;
            Data.playerAttack += 2;

            Data.requiredExp += 20 + (Data.playerLevel * 10);
        }

        return didLevelUp;
    }
}
