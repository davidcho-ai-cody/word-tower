using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SQLite;

public class WordService
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private List<WordData> words = new List<WordData>();
#else
    private SQLiteConnection db;
#endif

    public IEnumerator Initialize()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        string wordsJson = null;
        string loadError = null;

        yield return RuntimeDataLoader.LoadStreamingAssetText(
            "Data/Words/words.json",
            text => wordsJson = text,
            error => loadError = error
        );

        if (!string.IsNullOrEmpty(loadError))
        {
            Debug.LogError("words.json load failed: " + loadError);
            words = new List<WordData>();
            yield break;
        }

        WordDataJsonList wordList =
            JsonUtility.FromJson<WordDataJsonList>(wordsJson);

        words = wordList != null && wordList.words != null
            ? wordList.words.Select(word => word.ToWordData()).ToList()
            : new List<WordData>();

        Debug.Log($"Word JSON Loaded: {words.Count}");
#else
        string dbPath = Path.Combine(
            Application.streamingAssetsPath,
            "Data/Words/words.db"
        );

        db = new SQLiteConnection(dbPath);

        Debug.Log("Word DB Connected : " + dbPath);
#endif

        yield break;
    }

    public bool IsValidWord(string word)
    {
        return GetActiveWords()
            .Any(
                w =>
                    w.word == word &&
                    w.is_active == 1
            );
    }

    public WordData GetWord(string word)
    {
        return GetActiveWords()
            .FirstOrDefault(
                w =>
                    w.word == word &&
                    w.is_active == 1
            );
    }

    public WordData GetMonsterWord(
        string startChar,
        int minLevel,
        int maxLevel,
        HashSet<string> usedWords
    )
    {
        List<WordData> candidates =
            GetActiveWords()
            .Where(
                w =>
                    w.first_char == startChar &&
                    w.level >= minLevel &&
                    w.level <= maxLevel &&
                    w.is_active == 1
            )
            .ToList();

        candidates = candidates
            .Where(w => !usedWords.Contains(w.word))
            .ToList();

        if (candidates.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, candidates.Count);

        return candidates[index];
    }

    public bool IsOneShotForPlayer(
        string word,
        HashSet<string> usedWords
    )
    {
        if (string.IsNullOrEmpty(word))
            return false;

        string lastChar = word[word.Length - 1].ToString();

        int candidateCount =
            GetActiveWords()
            .Where(
                w =>
                    w.first_char == lastChar &&
                    w.is_active == 1
            )
            .ToList()
            .Count(
                w => !usedWords.Contains(w.word)
            );

        return candidateCount == 0;
    }

    public bool IsOneShotWord(
        string word,
        int minLevel,
        int maxLevel,
        HashSet<string> usedWords
    )
    {
        if (string.IsNullOrEmpty(word))
            return false;

        string lastChar = word[word.Length - 1].ToString();

        int candidateCount =
            GetActiveWords()
            .Where(
                w =>
                    w.first_char == lastChar &&
                    w.level >= minLevel &&
                    w.level <= maxLevel &&
                    w.is_active == 1
            )
            .ToList()
            .Count(
                w => !usedWords.Contains(w.word)
            );

        return candidateCount == 0;
    }

    public WordData GetRandomStartWord(
        int minLevel,
        int maxLevel,
        HashSet<string> usedWords
    )
    {
        List<WordData> candidates =
            GetActiveWords()
            .Where(
                w =>
                    w.level >= minLevel &&
                    w.level <= maxLevel &&
                    w.is_active == 1
            )
            .ToList();

        candidates = candidates
            .Where(w => !usedWords.Contains(w.word))
            .ToList();

        if (candidates.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, candidates.Count);

        return candidates[index];
    }

    public WordData GetRandomStartWordForPlayer(
        HashSet<string> usedWords
    )
    {
        List<WordData> availableWords =
            GetActiveWords()
            .Where(w => w.is_active == 1)
            .ToList()
            .Where(w => !usedWords.Contains(w.word))
            .ToList();

        List<WordData> candidates = availableWords
            .Where(
                startWord => availableWords.Any(
                    nextWord =>
                        nextWord.word != startWord.word &&
                        nextWord.first_char == startWord.last_char
                )
            )
            .ToList();

        if (candidates.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(0, candidates.Count);

        return candidates[index];
    }

    public void Close()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        words.Clear();
#else
        db?.Close();
        db = null;
#endif
    }

    private IEnumerable<WordData> GetActiveWords()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return words;
#else
        if (db == null)
            return Enumerable.Empty<WordData>();

        return db.Table<WordData>();
#endif
    }

    [Serializable]
    private class WordDataJsonList
    {
        public List<WordDataJson> words;
    }

    [Serializable]
    private class WordDataJson
    {
        public int id;
        public string word;
        public string first_char;
        public string last_char;
        public string meaning;
        public int level;
        public string category;
        public int is_active;

        public WordData ToWordData()
        {
            return new WordData
            {
                id = id,
                word = word,
                first_char = first_char,
                last_char = last_char,
                meaning = meaning,
                level = level,
                category = category,
                is_active = is_active
            };
        }
    }
}
