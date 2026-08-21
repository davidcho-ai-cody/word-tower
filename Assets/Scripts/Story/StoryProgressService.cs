using System;
using System.IO;
using UnityEngine;

public class StoryProgressService
{
    private const string SaveFileName = "wordtower_story_progress.json";
    public const string OpeningStoryId = "opening";

    public string GetSavePath()
    {
        return Path.Combine(
            Application.persistentDataPath,
            SaveFileName
        );
    }

    public bool TryLoad(out StoryProgressData storyProgress)
    {
        storyProgress = null;
        string savePath = GetSavePath();

        if (!File.Exists(savePath))
            return false;

        try
        {
            string json = File.ReadAllText(savePath);
            storyProgress = JsonUtility.FromJson<StoryProgressData>(json);

            if (storyProgress == null)
                return false;

            EnsureDefaults(storyProgress);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Story Progress를 불러오지 못했습니다.\n" +
                $"{savePath}\n{exception.Message}"
            );

            storyProgress = null;
            return false;
        }
    }

    public StoryProgressData LoadOrCreate()
    {
        if (TryLoad(out StoryProgressData storyProgress))
            return storyProgress;

        storyProgress = new StoryProgressData();
        EnsureDefaults(storyProgress);
        return storyProgress;
    }

    public void Save(StoryProgressData storyProgress)
    {
        if (storyProgress == null)
            return;

        EnsureDefaults(storyProgress);

        try
        {
            string json = JsonUtility.ToJson(storyProgress, true);
            File.WriteAllText(GetSavePath(), json);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Story Progress를 저장하지 못했습니다.\n" +
                $"{GetSavePath()}\n{exception.Message}"
            );
        }
    }

    public void MarkOpeningStorySeen()
    {
        StoryProgressData storyProgress = LoadOrCreate();
        storyProgress.hasSeenOpeningStory = true;
        UnlockStory(storyProgress, OpeningStoryId);
        Save(storyProgress);
    }

    private void UnlockStory(
        StoryProgressData storyProgress,
        string storyId
    )
    {
        if (storyProgress.unlockedStoryIds.Contains(storyId))
            return;

        storyProgress.unlockedStoryIds.Add(storyId);
    }

    private void EnsureDefaults(StoryProgressData storyProgress)
    {
        if (storyProgress.unlockedStoryIds == null)
            storyProgress.unlockedStoryIds = new System.Collections.Generic.List<string>();
    }
}
