using System;

[Serializable]
public class SaveData
{
    public int saveVersion = 1;
    public int currentFloor = 1;
    public int highestFloor = 1;
    public PlayerProgressData playerProgress = new PlayerProgressData();
}
