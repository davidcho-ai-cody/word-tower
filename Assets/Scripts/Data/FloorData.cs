using System;
using System.Collections.Generic;

[Serializable]
public class FloorData
{
    public int floor;
    public string monsterId;
    public string title;
    public bool isBoss;
}

[Serializable]
public class FloorDataList
{
    public List<FloorData> floors;
}