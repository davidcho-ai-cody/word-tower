using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class OpeningStoryDebugMenu
{
    [MenuItem("WordTower/Debug/Reset Opening Story")]
    public static void ResetOpeningStory()
    {
        StoryProgressService storyProgressService =
            new StoryProgressService();
        string storyProgressPath = storyProgressService.GetSavePath();

        try
        {
            if (!File.Exists(storyProgressPath))
            {
                Debug.Log(
                    "[WordTower] Opening story progress is already reset."
                );
                return;
            }

            File.Delete(storyProgressPath);
            Debug.Log("[WordTower] Opening story progress reset.");
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[WordTower] Failed to reset opening story progress.\n" +
                $"{storyProgressPath}\n{exception.Message}"
            );
        }
    }
}
