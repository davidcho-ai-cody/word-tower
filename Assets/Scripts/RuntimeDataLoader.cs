using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class RuntimeDataLoader
{
    public static IEnumerator LoadDataText(
        string relativePath,
        Action<string> onLoaded,
        Action<string> onError = null
    )
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        yield return LoadStreamingAssetText(relativePath, onLoaded, onError);
#else
        string dataPath = Path.Combine(Application.dataPath, relativePath);

        if (File.Exists(dataPath))
        {
            onLoaded?.Invoke(File.ReadAllText(dataPath));
            yield break;
        }

        yield return LoadStreamingAssetText(relativePath, onLoaded, onError);
#endif
    }

    public static IEnumerator LoadStreamingAssetText(
        string relativePath,
        Action<string> onLoaded,
        Action<string> onError = null
    )
    {
        string path = CombineStreamingAssetPath(relativePath);

#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest request = UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onLoaded?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onError?.Invoke($"{path}\n{request.error}");
            }
        }
#else
        if (File.Exists(path))
        {
            onLoaded?.Invoke(File.ReadAllText(path));
        }
        else
        {
            onError?.Invoke(path);
        }

        yield break;
#endif
    }

    private static string CombineStreamingAssetPath(string relativePath)
    {
        string normalizedRelativePath = relativePath.Replace("\\", "/");

#if UNITY_ANDROID && !UNITY_EDITOR
        return Application.streamingAssetsPath.TrimEnd('/') +
            "/" +
            normalizedRelativePath;
#else
        return Path.Combine(
            Application.streamingAssetsPath,
            normalizedRelativePath
        );
#endif
    }
}
