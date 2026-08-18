public class PlayerProgressService
{
    public PlayerProgressData Data { get; private set; }

    public int PlayerLevel => Data.playerLevel;
    public int Exp => Data.exp;
    public int RequiredExp => Data.requiredExp;
    public int Gold => Data.gold;
    public int PlayerMaxHp => Data.playerMaxHp;
    public int PlayerAttack => Data.playerAttack;

    public PlayerProgressService(PlayerProgressData data = null)
    {
        Data = data ?? new PlayerProgressData();
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
