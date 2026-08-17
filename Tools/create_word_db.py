import sqlite3
from pathlib import Path

# ========================================
# WordTower 테스트 단어 DB 생성
# ========================================

PROJECT_ROOT = Path(__file__).resolve().parent.parent

DB_PATH = (
    PROJECT_ROOT
    / "Assets"
    / "StreamingAssets"
    / "Data"
    / "Words"
    / "words.db"
)

DB_PATH.parent.mkdir(parents=True, exist_ok=True)

# 기존 테스트 DB는 다시 생성
if DB_PATH.exists():
    DB_PATH.unlink()

conn = sqlite3.connect(DB_PATH)
cursor = conn.cursor()

# ========================================
# 단어 테이블
# ========================================

cursor.execute("""
CREATE TABLE words (
    id INTEGER PRIMARY KEY AUTOINCREMENT,

    word TEXT NOT NULL UNIQUE,

    first_char TEXT NOT NULL,
    last_char TEXT NOT NULL,

    meaning TEXT,

    level INTEGER NOT NULL DEFAULT 1,

    category TEXT,

    is_active INTEGER NOT NULL DEFAULT 1
)
""")

# 끝말잇기 검색 속도를 위한 인덱스
cursor.execute("""
CREATE INDEX idx_words_first_char
ON words(first_char)
""")

cursor.execute("""
CREATE INDEX idx_words_level
ON words(level)
""")

# ========================================
# 테스트 단어
# 일부러 끝말잇기가 이어지도록 구성
# ========================================

words = [
    ("사과", "과", "사과나무의 열매", 1, "음식"),
    ("과자", "자", "간식으로 먹는 식품", 1, "음식"),
    ("자동차", "차", "도로를 달리는 탈것", 1, "교통"),
    ("차표", "표", "교통수단 이용권", 1, "생활"),
    ("표지", "지", "책이나 문서의 겉면", 1, "생활"),
    ("지갑", "갑", "돈이나 카드를 넣는 물건", 1, "생활"),
    ("표범", "범", "고양잇과의 동물", 2, "동물"),
    ("범인", "인", "범죄를 저지른 사람", 2, "일반"),
    ("인사", "사", "서로 예의를 표현하는 행동", 1, "생활"),

    ("사슴", "슴", "뿔이 있는 초식동물", 1, "동물"),
    ("슴새", "새", "바닷새의 한 종류", 3, "동물"),
    ("새우", "우", "물에 사는 갑각류", 1, "동물"),
    ("우산", "산", "비를 막는 도구", 1, "생활"),
    ("산책", "책", "가볍게 걸어 다니는 일", 1, "생활"),
    ("책상", "상", "책을 읽거나 일을 하는 가구", 1, "생활"),
    ("상자", "자", "물건을 담는 용기", 1, "생활"),

    ("거울", "울", "모습을 비추는 물건", 1, "생활"),
    ("울음", "음", "우는 소리나 행동", 1, "일반"),
    ("음악", "악", "소리로 표현하는 예술", 1, "예술"),
    ("악기", "기", "음악을 연주하는 도구", 1, "예술"),
    ("기차", "차", "철도를 따라 달리는 교통수단", 1, "교통"),

    ("학교", "교", "학생이 공부하는 곳", 1, "생활"),
    ("교실", "실", "수업을 하는 방", 1, "생활"),
    ("실내", "내", "건물의 안쪽", 1, "생활"),
    ("내일", "일", "오늘의 다음 날", 1, "시간"),
    ("일기", "기", "하루 일을 기록한 글", 1, "생활"),

    ("바다", "다", "넓고 큰 소금물", 1, "자연"),
    ("다리", "리", "몸을 지탱하거나 건너는 구조물", 1, "일반"),
    ("리본", "본", "장식용 끈", 2, "생활"),
    ("본능", "능", "타고난 행동 성향", 3, "일반"),
    ("능력", "력", "어떤 일을 해낼 수 있는 힘", 2, "일반"),
]

for word, last_char, meaning, level, category in words:
    cursor.execute("""
        INSERT INTO words (
            word,
            first_char,
            last_char,
            meaning,
            level,
            category
        )
        VALUES (?, ?, ?, ?, ?, ?)
    """, (
        word,
        word[0],
        last_char,
        meaning,
        level,
        category
    ))

conn.commit()

count = cursor.execute(
    "SELECT COUNT(*) FROM words"
).fetchone()[0]

conn.close()

print(f"Word DB created: {DB_PATH}")
print(f"Inserted words: {count}")