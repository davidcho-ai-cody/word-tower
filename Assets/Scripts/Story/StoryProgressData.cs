using System;
using System.Collections.Generic;

[Serializable]
public class StoryProgressData
{
    public int saveVersion = 1;
    public bool hasSeenOpeningStory = false;
    public List<string> unlockedStoryIds = new List<string>();
}
