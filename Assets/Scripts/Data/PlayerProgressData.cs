using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProgressData
{
    public const string DefaultWeaponId = "wood_sword_01";
    public const string DefaultArmorId = "beginner_armor_01";

    public int playerLevel = 1;
    public int exp = 0;
    public int requiredExp = 100;
    public int gold = 0;
    public int playerMaxHp = 100;
    public int playerAttack = 20;
    public string equippedWeaponId = DefaultWeaponId;
    public string equippedArmorId = DefaultArmorId;
    public List<string> ownedItemIds = new List<string>
    {
        DefaultWeaponId,
        DefaultArmorId
    };
}
