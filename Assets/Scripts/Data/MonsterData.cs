using System;
using System.Collections.Generic;

[Serializable]
public class MonsterData
{
    public string id;
    public string name;

    public int maxHp;
    public int attack;

    public int expReward;
    public int goldReward;

    public int wordLevelMin;
    public int wordLevelMax;

    public float visualScale = 1f;
    public string spritePath;
}

[Serializable]
public class MonsterDataList
{
    public List<MonsterData> monsters;
}
