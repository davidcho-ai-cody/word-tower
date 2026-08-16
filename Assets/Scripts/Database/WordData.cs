using SQLite;

// ========================================
// words.db 의 words 테이블 매핑
// ========================================
public class WordData
{
    [PrimaryKey]
    public int id { get; set; }

    public string word { get; set; }

    public string first_char { get; set; }

    public string last_char { get; set; }

    public string meaning { get; set; }

    public int level { get; set; }

    public string category { get; set; }

    public int is_active { get; set; }
}