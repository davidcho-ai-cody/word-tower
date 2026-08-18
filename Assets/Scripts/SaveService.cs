using System;
using System.IO;
using UnityEngine;

public class SaveService
{
    private const string SaveFileName = "wordtower_save.json";

    public string GetSavePath()
    {
        return Path.Combine(
            Application.persistentDataPath,
            SaveFileName
        );
    }

    public bool HasSave()
    {
        return File.Exists(GetSavePath());
    }

    public void Save(SaveData saveData)
    {
        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(GetSavePath(), json);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Save 파일을 저장하지 못했습니다.\n" +
                $"{GetSavePath()}\n{exception.Message}"
            );
        }
    }

    public bool TryLoad(out SaveData saveData)
    {
        saveData = null;

        string savePath = GetSavePath();

        if (!File.Exists(savePath))
            return false;

        try
        {
            string json = File.ReadAllText(savePath);
            saveData = JsonUtility.FromJson<SaveData>(json);

            return saveData != null &&
                saveData.playerProgress != null;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Save 파일을 불러오지 못했습니다. 기본 진행으로 시작합니다.\n" +
                $"{savePath}\n{exception.Message}"
            );

            saveData = null;
            return false;
        }
    }

    public void DeleteSave()
    {
        string savePath = GetSavePath();

        try
        {
            if (File.Exists(savePath))
                File.Delete(savePath);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"Save 파일을 삭제하지 못했습니다.\n" +
                $"{savePath}\n{exception.Message}"
            );
        }
    }
}
