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

        EnsureOwnedItem(Data.equippedWeaponId);
        EnsureOwnedItem(Data.equippedArmorId);
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
