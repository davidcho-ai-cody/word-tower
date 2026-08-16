using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using SQLite;

public class WordService
{
    private SQLiteConnection db;

    // ========================================
    // DB 초기화
    // ========================================
    public void Initialize()
    {
        string dbPath = Path.Combine(
            Application.streamingAssetsPath,
            "Data/Words/words.db"
        );

        db = new SQLiteConnection(dbPath);

        Debug.Log("Word DB Connected : " + dbPath);
    }


    // ========================================
    // 실제 등록된 단어인지 확인
    // ========================================
    public bool IsValidWord(string word)
    {
        if (db == null)
            return false;

        WordData result = db.Table<WordData>()
            .FirstOrDefault(
                w =>
                    w.word == word &&
                    w.is_active == 1
            );

        return result != null;
    }


    // ========================================
    // 단어 정보 조회
    // ========================================
    public WordData GetWord(string word)
    {
        if (db == null)
            return null;

        return db.Table<WordData>()
            .FirstOrDefault(
                w =>
                    w.word == word &&
                    w.is_active == 1
            );
    }


    // ========================================
    // 몬스터가 사용할 단어 선택
    //
    // startChar : 시작 글자
    // minLevel  : 몬스터 최소 단어 난이도
    // maxLevel  : 몬스터 최대 단어 난이도
    // ========================================
    public WordData GetMonsterWord(
        string startChar,
        int minLevel,
        int maxLevel,
        HashSet<string> usedWords
    )
    {
        if (db == null)
            return null;

        List<WordData> candidates =
            db.Table<WordData>()
            .Where(
                w =>
                    w.first_char == startChar &&
                    w.level >= minLevel &&
                    w.level <= maxLevel &&
                    w.is_active == 1
            )
            .ToList();

        // 이미 사용한 단어 제거
        candidates = candidates
            .Where(w => !usedWords.Contains(w.word))
            .ToList();

        if (candidates.Count == 0)
            return null;

        int index = Random.Range(0, candidates.Count);

        return candidates[index];
    }


    // ========================================
    // DB 종료
    // ========================================
    public void Close()
    {
        db?.Close();
        db = null;
    }
}