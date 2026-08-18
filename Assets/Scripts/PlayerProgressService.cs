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
        SetData(data);
    }

    public void SetData(PlayerProgressData data)
    {
        Data = data ?? new PlayerProgressData();
        NormalizeData();
    }

    public void Reset()
    {
        Data = new PlayerProgressData();
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
