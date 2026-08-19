# AGENTS.md — WordTower Development Guide

> WordTower의 지속 프로젝트 컨텍스트다. 새 Codex 세션이나 다른 PC에서 작업할 때 먼저 읽는다.
> 문서와 실제 코드/데이터가 충돌하면 실제 구현이 우선이다.

---

## 1. 프로젝트 개요

- 프로젝트: WordTower
- 장르: 2D 모바일 끝말잇기 RPG
- 엔진/언어: Unity 2D / C#
- 화면: 모바일 우선, 세로 9:16
- 목표: 100층 마왕성을 한국어 끝말잇기 전투로 공략
- 현재 확정·구현 콘텐츠: 1~10층 슬라임 챕터
- 메인 씬: Assets/Scenes/BattleScene.unity
- 초기 런타임은 AI/API 없이 SQLite 단어 DB로 동작

WordTower는 단순 단어 퀴즈가 아니라 전투 언어가 끝말잇기인 RPG를 지향한다. 전투 손맛, 보이는 성장, 단어 선택의 전략성을 보존한다.

---

## 2. 현재 핵심 게임 루프

1. floors.json에서 현재 FloorData를 찾는다.
2. monsterId로 monsters.json의 MonsterData를 찾는다.
3. 이름, HP, 공격력, 보상, 단어 난이도, 이미지, 표시 배율을 적용한다.
4. currentWord = "사과"로 전투를 시작한다.
5. 플레이어 단어의 시작 글자, DB 등록/활성 상태, 미사용 여부를 검증한다.
6. 플레이어 공격과 몬스터 반격을 진행한다.
7. 양쪽 모두 한방단어 크리티컬을 사용할 수 있다.
8. 몬스터 HP가 0이면 사망 연출 후 Victory 보상을 지급한다.
9. 다음 층에서 새 데이터를 로드하고 전투 상태를 초기화한다.

새 층 초기화:

- 플레이어/몬스터 HP 완전 회복
- currentWord를 "사과"로 초기화
- usedWords 초기화 후 "사과" 등록
- 몬스터 위치, 회전, 데이터 배율 복구
- 입력창과 공격 버튼 활성화
- EXP, Gold, Level 유지

---

## 3. 플레이어 기본값과 데미지

- Level: 1
- Max HP / HP: 100 / 100
- Attack: 20
- EXP / 필요 EXP: 0 / 100
- Gold: 0
- 플레이어 일반 데미지: playerAttack
- 플레이어 한방단어: playerAttack * 2
- 몬스터 일반 데미지: 현재 몬스터 attack
- 몬스터 한방단어: 현재 몬스터 attack * 2

Level, EXP, Gold, Max HP, Attack은 PlayerProgressData / PlayerProgressService에서 관리한다. BattleManager는 전투 중 현재 HP(playerHp)를 별도로 들고, 공격력과 Max HP는 PlayerProgressService에서 조회해 사용한다.

플레이어 한방단어는 Critical Impact 이미지와 CRITICAL! 텍스트 연출을 사용한다.

---

## 4. 플레이어 진행/레벨업 시스템 — 구현 완료

구현:

- Assets/Scripts/Data/PlayerProgressData.cs
- Assets/Scripts/PlayerProgressService.cs
- BattleManager.cs는 현재 전투와 UI 연결만 담당

PlayerProgressData 필드:

    public int playerLevel = 1;
    public int exp = 0;
    public int requiredExp = 100;
    public int gold = 0;
    public int playerMaxHp = 100;
    public int playerAttack = 20;
    public string equippedWeaponId = "wood_sword_01";
    public string equippedArmorId = "beginner_armor_01";
    public List<string> ownedItemIds;

PlayerProgressService 역할:

- EXP 추가
- Gold 추가
- 레벨업 판정
- 필요 경험치 증가
- 레벨업 시 Max HP / Attack 증가
- 현재 진행 상태 조회

Victory 연결 흐름:

    WinBattle()
    → PlayerProgressService.AddExp(currentMonsterData.expReward)
    → PlayerProgressService.AddGold(currentMonsterData.goldReward)
    → Save / UpdateUI()
    → VictoryPanel / Victory SFX
    → 레벨업 발생 시 짧은 간격 후 LevelUp SFX + LevelUpTextEffect()

레벨업 규칙:

- while (exp >= requiredExp)로 여러 레벨 상승 가능
- exp -= requiredExp이므로 초과 EXP 이월
- 레벨업마다 playerLevel +1, playerMaxHp +10, playerAttack +2
- 다음 필요 EXP: requiredExp += 20 + (playerLevel * 10)

초기 EXP 곡선:

- Lv.1 → 2: 100
- Lv.2 → 3: 140
- Lv.3 → 4: 190
- Lv.4 → 5: 250
- Lv.5 → 6: 320

UI:

- LevelText: LV.{playerLevel}
- ExpText: EXP {exp} / {requiredExp}
- LevelUpTextEffect(): LEVEL UP!과 도달 레벨을 팝업으로 표시하고 상승/페이드아웃
- LevelUpText는 BattleSceneBuilder가 생성

주의:

- 레벨업 시 Max HP는 증가하지만 현재 HP를 즉시 추가 회복하지는 않는다.
- 다음 층 초기화에서 새 Max HP까지 완전 회복한다.
- playerHp는 현재 전투 중 변하는 값이므로 BattleManager에 남아 있다.

---

## 4.1 Save / Load 시스템 — 구현 완료

구현:

- Assets/Scripts/Data/SaveData.cs
- Assets/Scripts/SaveService.cs

저장 방식:

- JSON 파일 기반
- 경로: Application.persistentDataPath / wordtower_save.json
- PlayerPrefs는 주요 진행 데이터 저장에 사용하지 않는다.
- saveVersion = 1을 저장하지만 마이그레이션 시스템은 아직 없다.

SaveData 저장 항목:

- playerProgress: playerLevel, exp, requiredExp, gold, playerMaxHp, playerAttack, equippedWeaponId, equippedArmorId, ownedItemIds
- currentFloor: 현재 이어서 시작할 층
- highestFloor: 정상 플레이로 도달한 최고 층

저장하지 않는 항목:

- playerHp
- 몬스터 현재 HP
- currentWord
- usedWords
- 공격/피격/코루틴/애니메이션 진행 상태
- VictoryPanel 표시 상태

게임 시작 시 Save 파일이 있으면 PlayerProgressData와 currentFloor/highestFloor를 불러온 뒤 해당 층의 새 전투로 시작한다. Save 파일이 없거나 손상되면 기본 PlayerProgressData와 1층으로 시작한다.

자동 저장 시점:

- Victory 보상 지급과 레벨업 판정 완료 후
- 정상 다음 층 진입 후 currentFloor/highestFloor 갱신 완료 후

Debug Floor 이동은 EXP/Gold/Level/highestFloor를 변경하지 않고 Save 파일도 덮어쓰지 않는다. 개발 테스트 중 10층으로 이동해도 실제 진행도가 오염되지 않아야 한다.

Save Reset:

- SaveService.DeleteSave()
- 개발용 FloorDebugPanel의 DebugSaveResetButton에서 호출
- 저장 파일 삭제, PlayerProgress 기본값 복구, currentFloor/highestFloor 1로 복구, 1층 새 전투 시작

---

## 5. Word Engine과 SQLite

구조:

    BattleManager
      → WordService
        → sqlite-net / SQLite.cs
          → words.db

주요 파일:

- DB: Assets/StreamingAssets/Data/Words/words.db
- 생성 도구: Tools/create_word_db.py
- 서비스: Assets/Scripts/Database/WordService.cs
- 매핑: Assets/Scripts/Database/WordData.cs
- sqlite-net: Assets/Scripts/Database/SQLite.cs
- Windows Editor DLL: Assets/Plugins/x86_64/sqlite3.dll

WordData는 [Table("words")]로 실제 words 테이블과 매핑한다.

현재 WordService 메서드:

    IsValidWord(string word)
    GetWord(string word)
    GetMonsterWord(string startChar, int minLevel, int maxLevel, HashSet<string> usedWords)
    IsOneShotWord(string word, int minLevel, int maxLevel, HashSet<string> usedWords)
    IsOneShotForPlayer(string word, HashSet<string> usedWords)
    GetRandomStartWord(int minLevel, int maxLevel, HashSet<string> usedWords)
    GetRandomStartWordForPlayer(HashSet<string> usedWords)

몬스터 단어 선택 조건:

- first_char == requiredChar
- level >= wordLevelMin
- level <= wordLevelMax
- is_active == 1
- usedWords에 없음

플레이어 입력에는 단어 난이도 제한을 적용하지 않는다. 현재 words.db는 엔진 검증용 소규모 테스트 데이터이며 운영용 어휘 DB가 아니다.

---

## 6. 사용 단어 규칙

각 전투는 HashSet<string> usedWords를 사용한다.

- 시작 단어 "사과" 즉시 등록
- 검증을 통과한 플레이어 단어 등록
- 몬스터 단어 등록
- 한방단어 후 새 제시어 등록
- 같은 전투에서 재사용 불가
- 새 층과 Floor Debug 이동 시 초기화

---

## 7. 양방향 한방단어 — 구현 완료

한방단어는 즉시 승리/패배가 아니다. 양쪽 모두 2배 크리티컬 데미지를 주고 안전한 새 제시어로 전투를 계속한다.

### 7.1 플레이어 한방단어

판정:

    WordService.IsOneShotWord(
        word,
        currentMonsterData.wordLevelMin,
        currentMonsterData.wordLevelMax,
        usedWords
    )

판정 조건:

- 플레이어 단어의 마지막 글자로 시작
- 활성/미사용 단어
- 현재 몬스터의 wordLevelMin~wordLevelMax 범위

흐름:

    플레이어 단어
    → 몬스터 난이도 범위에서 후속 단어 없음
    → playerAttack * 2
    → Critical VFX / CRITICAL! 표시
    → 몬스터 반격 없음
    → GetRandomStartWordForPlayer()로 새 제시어 선택
    → 플레이어 턴으로 계속

### 7.2 몬스터 한방단어

판정:

    WordService.IsOneShotForPlayer(monsterWord, usedWords)

판정 조건:

- 몬스터 단어의 마지막 글자로 시작
- DB 전체 활성 단어
- 미사용 단어
- 플레이어 단어 난이도 제한 없음

흐름:

    몬스터 단어 선택 및 usedWords 등록
    → 플레이어가 이어갈 활성/미사용 단어 없음
    → slimeAttack * 2
    → 플레이어 피격
    → 막힌 마지막 글자를 입력하도록 두지 않음
    → GetRandomStartWordForPlayer()로 새 제시어 선택
    → 추가 몬스터 공격 없이 플레이어 턴

GetRandomStartWordForPlayer()는 활성·미사용 단어 중 마지막 글자로 실제 후속 활성·미사용 단어가 있는 후보만 선택한다. 능력 → 력처럼 새 제시어 자체가 전투를 막는 상황을 방지한다.

---

## 8. 데이터 구조

FloorData — Assets/Scripts/Data/FloorData.cs:

- floor
- monsterId
- title
- isBoss

데이터: Assets/Data/Floors/floors.json

MonsterData — Assets/Scripts/Data/MonsterData.cs:

- id
- name
- maxHp
- attack
- expReward
- goldReward
- wordLevelMin
- wordLevelMax
- visualScale
- spritePath

데이터: Assets/Data/Monsters/monsters.json

몬스터 스탯, 보상, 난이도, 이미지, 배율을 BattleManager에 하드코딩하지 않는다.

ItemData — Assets/Scripts/Data/ItemData.cs:

- id
- name
- type
- price
- attackBonus
- defenseRate
- spritePath
- characterSpritePath
- description

데이터: Assets/Data/Items/items.json

현재 지원 ItemType:

- Weapon
- Armor

ItemService — Assets/Scripts/ItemService.cs:

- items.json 로드
- GetItem(string id)
- GetItemsByType(ItemType itemType)
- GetAllItems()

BattleManager는 items.json을 직접 읽지 않고 ItemService를 초기화한다.

---

## 8.1 아이템 / 장비 데이터 — 기반 구현 완료

현재 등록 아이템:

| id | 이름 | 타입 | 가격 | ATK 보너스 | defenseRate | 아트 상태 |
|---|---|---|---:|---:|---:|---|
| wood_sword_01 | 나무검 | Weapon | 0 | 0 | 0 | 실제 weapon_wood_sword_01.png 사용 |
| beginner_armor_01 | 초보자 방어구 | Armor | 0 | 0 | 0 | 실제 hero_beginner_01.png 사용 |
| iron_sword_01 | 철검 | Weapon | 100 | 5 | 0 | weapon_iron_sword_01.png 연결 |
| leather_armor_01 | 가죽 갑옷 | Armor | 120 | 0 | 0.10 | hero_leather_armor_01.png 연결 |

기본 장비:

- equippedWeaponId = wood_sword_01
- equippedArmorId = beginner_armor_01
- ownedItemIds 기본값에는 위 두 기본 장비가 포함된다.

PlayerProgressService는 ownedItemIds에 기본 장비가 포함되도록 보정하고 중복을 제거한다. BattleManager는 ItemService 초기화 후 저장된 장착 ID의 존재 여부, ItemType, 보유 여부를 검증한다. 잘못된 무기/방어구 ID는 각각 wood_sword_01 / beginner_armor_01로 복구하고 수정된 진행 상태를 저장한다.

스탯 역할:

- 레벨업은 기본 Max HP와 기본 Attack을 성장시킨다.
- Weapon은 attackBonus로 플레이어 공격력을 증가시킨다.
- Armor는 Max HP를 증가시키지 않고 defenseRate로 몬스터에게 받는 최종 Damage를 비율 감소시킨다.

장비 전투 규칙:

- Final Attack = 레벨업으로 성장하는 Base Attack + 장착 Weapon의 attackBonus
- 플레이어 일반 공격과 한방단어 Critical 모두 Final Attack을 사용한다.
- Armor는 현재 HP와 Max HP에 영향을 주지 않는다.
- 몬스터 일반/크리티컬 Raw Damage에 장착 Armor의 defenseRate를 적용한다.
- 피격 Damage는 Mathf.RoundToInt(rawDamage * (1 - defenseRate))로 계산하며 최소 1이다.
- 장비 교체는 Base Attack이나 HP를 직접 변경하지 않으므로 반복 교체해도 보너스가 누적되지 않는다.

Shop 구매와 장착은 분리되어 있다. 구매는 Gold 차감과 ownedItemIds 추가만 수행하며, 사용자가 보유 아이템의 장착 버튼을 눌러야 equippedWeaponId / equippedArmorId가 변경된다.

---

## 8.2 Shop System 1단계 — 구현 완료

구현:

- BattleScene 위에 Modal Shop Panel을 띄운다.
- BattleSceneBuilder가 ShopButton / ShopPanel / 탭 / 아이템 목록 컨테이너를 생성한다.
- BattleManager가 ItemService와 PlayerProgressService를 사용해 아이템 목록과 구매 상태를 표시한다.

Shop UI 구조:

- ShopButton
- ShopPanel
- ShopTitle
- ShopCloseButton
- ShopCurrentGold
- ShopMessage
- ShopTabWeapon
- ShopTabArmor
- ShopTabAccessory
- ShopTabEtc
- ShopItemListContent

탭:

- Weapon: 현재 데이터 있음
- Armor: 현재 데이터 있음
- Accessory: 향후 확장용, 현재 비활성
- Etc: 향후 확장용, 현재 비활성

Shop을 열 수 있는 상태:

- battleEnded가 false
- Shop이 이미 열려 있지 않음
- WordInput과 AttackButton이 interactable
- VictoryPanel이 표시 중이 아님

공격 애니메이션, 몬스터 턴, 사망 연출, Victory 처리 중에는 입력 버튼이 잠겨 있으므로 ShopButton도 비활성화된다. Time.timeScale은 사용하지 않는다.

Shop이 열리면 WordInput과 AttackButton을 잠그고, 닫으면 전투가 종료되지 않은 경우 플레이어 입력 상태로 복구한다.

구매 규칙:

- ItemService.GetItem(id)로 아이템 존재 확인
- 이미 ownedItemIds에 있으면 재구매 불가
- Gold가 부족하면 구매하지 않고 ShopMessage에 안내
- Gold가 충분하면 Gold 차감, ownedItemIds 추가, UI 즉시 갱신, SaveGame() 호출
- price = 0인 기본 장비는 기본 ownedItemIds에 포함되므로 구매 대상이 아니다.

구매와 장착은 분리한다. 미보유 아이템은 구매 버튼, 보유한 미장착 아이템은 장착 버튼, 현재 장비는 비활성 장착 중 버튼으로 표시한다. Weapon/Armor만 타입에 맞게 장착할 수 있으며 장착 변경은 즉시 Save하고 Shop UI와 장비 외형을 갱신한다.

Equipment Visual:

- Weapon은 ItemData.spritePath를 기존 Weapon Image에 적용한다.
- Armor는 ItemData.characterSpritePath를 기존 Body Image에 적용한다.
- 장착 직후, Save Load 후 초기화, Save Reset 시 현재 equipped ID 기준으로 외형을 갱신한다.
- Sprite 교체 시 기존 RectTransform을 유지해 위치, 크기, 공격/피격 애니메이션을 보존한다.
- 경로가 비어 있거나 Sprite 로드에 실패하면 현재 표시 중인 Sprite를 유지하고 Warning만 남긴다.
- 현재 Unity Editor 프로토타입은 UnityEditor.AssetDatabase를 사용하며 Android 런타임 로딩은 별도 TODO다.

---

## 9. 1~10층 슬라임 챕터 — 구현 완료

| 층 | monsterId | 이름 | HP | ATK | EXP | Gold | 단어 Lv. | 배율 | isBoss | spritePath |
|---:|---|---|---:|---:|---:|---:|---|---:|---|---|
| 1 | slime_green | 초록 슬라임 | 100 | 10 | 20 | 10 | 1~1 | 1.0 | false | Assets/Art/Sprites/Monsters/Slime/slime_green_idle_01.png |
| 2 | slime_green | 초록 슬라임 | 100 | 10 | 20 | 10 | 1~1 | 1.0 | false | 동일 |
| 3 | slime_blue | 파란 슬라임 | 140 | 13 | 30 | 15 | 1~2 | 1.0 | false | Assets/Art/Sprites/Monsters/Slime/slime_blue_idle_01.png |
| 4 | slime_blue | 파란 슬라임 | 140 | 13 | 30 | 15 | 1~2 | 1.0 | false | 동일 |
| 5 | slime_red | 빨간 슬라임 | 160 | 16 | 35 | 18 | 1~2 | 1.0 | false | Assets/Art/Sprites/Monsters/Slime/slime_red_idle_01.png |
| 6 | slime_yellow | 노란 슬라임 | 150 | 15 | 40 | 20 | 2~2 | 1.0 | false | Assets/Art/Sprites/Monsters/Slime/slime_yellow_idle_01.png |
| 7 | slime_poison | 독 슬라임 | 180 | 17 | 45 | 22 | 2~3 | 1.0 | false | Assets/Art/Sprites/Monsters/Slime/slime_poison_idle_01.png |
| 8 | slime_armor | 철갑 슬라임 | 220 | 16 | 50 | 25 | 2~3 | 1.0 | false | Assets/Art/Sprites/Monsters/Slime/slime_armor_idle_01.png |
| 9 | slime_elite | 엘리트 슬라임 | 240 | 20 | 60 | 30 | 2~3 | 1.20 | false | Assets/Art/Sprites/Monsters/Slime/slime_elite_idle_01.png |
| 10 | slime_king | 슬라임 킹 | 350 | 24 | 100 | 60 | 2~4 | 1.70 | true | Assets/Art/Sprites/Monsters/Slime/slime_king_idle_01.png |

슬라임 킹은 첫 보스로 확정됐지만 isBoss 기반 보스 전용 전투 패턴은 아직 없다.

### 9.1 Chapter Clear 공통 기획 — 확정 방향, 미구현

각 Chapter의 마지막 Boss를 처치하면 일반 Floor Victory와 구분되는 Chapter Clear Sequence를 실행한다. Chapter 1에서는 10층 Slime King 처치가 해당 트리거다.

기본 흐름:

    Final Boss 마지막 공격
    → Boss Death
    → 짧은 여운
    → Battle 화면 Fade Out
    → Chapter Clear Illustration Fade In
    → Chapter Title / Complete UI
    → 짧은 Story Text
    → 다음 Chapter 이동

정확한 연출 시간은 아직 확정하지 않는다. 현재 코드와 Scene에는 Chapter Clear 분기가 구현되어 있지 않다.

Chapter Clear Illustration 원칙:

- 일반 Gameplay는 밝고 귀여운 2D SD Character를 유지한다.
- Chapter Ending은 SD가 아닌 Full Character 비율의 고전 판타지 책 삽화 같은 고품질 Illustration으로 대비를 만든다.
- Chapter 1은 Hero가 Slime King을 처치하는 역동적인 순간을 기록한 판타지 삽화 방향이다. 현재 Concept Sample은 최종 게임 Asset 확정본이 아니다.
- Artwork에는 가능하면 문자를 직접 넣지 않고 CHAPTER, Title, COMPLETE 등의 Text는 Unity UI로 분리해 Animation, Localization, 재사용성을 보존한다.
- Story Text는 Chapter 종료와 다음 모험을 암시하는 1~2문장 정도로 짧게 사용한다. 구체 문구는 아직 확정하지 않는다.

수집 확장 방향:

- Chapter Clear 시 해당 Illustration을 Unlock한다.
- 향후 `모험의 기록` 또는 Gallery에서 획득 Illustration을 다시 감상할 수 있게 한다.
- 미획득 항목은 Silhouette, Lock, Unknown 등으로 표현할 수 있다.
- Gallery와 Unlock 저장 구조는 아직 구현하지 않는다.

Audio 방향:

- 일반 Victory는 현재 `victory_01.wav`를 사용한다.
- Chapter Clear는 향후 전용 Jingle 또는 Music으로 일반 Victory와 구분한다.
- Boss/Chapter Clear 전용 Audio는 아직 제작·연결하지 않는다.

### 10층 이후 방향 — 미확정 후보

- 11~20층: 스켈레톤 계열 후보
- 이후: 머드맨, 골렘, 고블린, 오크 계열 검토

아직 JSON에 확정 구현된 콘텐츠가 아니다.

---

## 10. 슬라임 이미지와 폴더 원칙

색상별 폴더를 만들지 않고 아래 폴더에서 파일명으로 구분한다.

    Assets/Art/Sprites/Monsters/Slime/
    ├─ slime_green_idle_01.png
    ├─ slime_blue_idle_01.png
    ├─ slime_red_idle_01.png
    ├─ slime_yellow_idle_01.png
    ├─ slime_poison_idle_01.png
    ├─ slime_armor_idle_01.png
    ├─ slime_elite_idle_01.png
    └─ slime_king_idle_01.png

Editor에서는 BattleManager.ApplyMonsterSprite()가 JSON spritePath를 UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>()로 로드한다.

이 방식은 Android 런타임에서 작동하지 않는다. Resources, Addressables 또는 직렬화 참조로 교체하는 작업이 TODO다.

---

## 11. 몬스터 표시 배율

MonsterData.visualScale과 monsters.json으로 관리한다.

- 1~8층: 1.0
- 9층 엘리트 슬라임: 1.20
- 10층 슬라임 킹: 1.70

구현:

- BattleManager.ApplyMonsterVisualScale()
- LoadFloorAndMonsterData()에서 이미지 로드 후 적용
- ResetBattleForNextFloor()에서 사망/층 이동 후 해당 몬스터 배율 복구

공격/피격 애니메이션은 배율을 덮어쓰지 않는다. 사망 시 축소되지만 새 전투 초기화에서 데이터 배율로 복구한다.

---

## 12. 개발용 Floor Debug — 구현 완료

생성: BattleSceneBuilder.CreateFloorDebugPanel()

기능:

- 현재 Debug 층 표시
- 이전 층
- 다음 층
- 10층 바로가기
- Save Reset

런타임 조건:

    #if UNITY_EDITOR || DEVELOPMENT_BUILD

일반 Release 빌드에서는 FloorDebugPanel을 숨긴다.

DebugMoveToFloor(int targetFloor):

1. FloorDataExists()로 층 존재 확인
2. 없는 층으로 이동하지 않음
3. 진행 중 코루틴 중단
4. VictoryPanel과 잔여 Critical/Impact/LevelUp 효과 숨김
5. 플레이어 위치와 무기 회전 복구
6. currentFloor 변경 및 Floor/Monster 재로드
7. ResetBattleForNextFloor()로 새 전투 초기화

Debug 이동 시:

- EXP/Gold 보상 없음
- 누적 EXP, Gold, Level 유지
- highestFloor 갱신 없음
- Save 파일 덮어쓰기 없음
- 플레이어 HP는 Max HP까지 회복
- usedWords와 제시어 초기화
- 몬스터 HP, 이미지, 스탯, 위치, 회전, 배율 복구
- 9/10층 배율, 보스 표시, 레벨업/전투 연출 반복 테스트에 사용

---

## 13. 전투 연출 — 구현 완료

- Hero Procedural Idle: Frame Sprite 없이 PlayerPlaceholder 부모의 Position/Scale을 미세하게 변화시켜 Body와 Weapon을 함께 움직인다.
- Hero Idle은 저장된 기준 Transform을 중심으로 계산하며 공격/피격 중 일시 중지하고 연출 종료 후 기준값으로 복구·재개한다.
- Slime Procedural Idle: 단일 Sprite의 RectTransform에 미세한 Squash & Stretch와 Y 이동을 적용한다.
- Slime Idle은 MonsterData.visualScale이 적용된 Base Scale을 보존하며 일반/Elite/King별 속도와 변화 폭을 다르게 사용한다.
- Slime 공격/피격/사망 중에는 Idle을 중지하고, 생존 연출 종료 또는 새 Floor 초기화 후 기준 Transform에서 재개한다.
- Hero와 Slime Idle은 독립적으로 동작하며 Frame Animation은 아직 사용하지 않는다.
- Monster Attack Anticipation: Slime Idle을 중지하고 Base Scale 기준으로 짧게 Squash한 뒤 기존 Rush를 실행한다.
- 일반/Elite/King은 공격 준비 속도와 압축 폭이 다르며, 이 연출은 기존 공격 거리와 Damage 계산에 영향을 주지 않는다.
- HeroShadow와 MonsterShadow는 BattleSceneBuilder가 생성하는 반투명 타원형 UI Ground Shadow다.
- Shadow는 Procedural Idle에 미세하게 반응하고 공격/피격 중 캐릭터의 X 이동을 추적하며, Monster Shadow는 사망 연출과 함께 축소·페이드된다.
- Floor 및 Floor Debug 이동 시 Shadow의 위치, 크기, Alpha를 초기화하고 Elite/King에는 몬스터 등급에 맞는 기준 크기를 적용한다.
- 플레이어 전진 공격: +110f X 기준
- 무기 스윙: 20° → -55° 기준
- 몬스터 돌진: -85f X 기준
- 일반/크리티컬 타격 이미지
- 피격 넉백과 흔들림
- 데미지 숫자 상승/페이드아웃
- 몬스터 사망: 점프, 회전, 축소, 드리프트
- 플레이어 한방단어 CRITICAL! 텍스트
- LEVEL UP! 텍스트

시각 테스트된 타이밍과 공용 오브젝트 이름은 관련 작업이 아니면 변경하지 않는다.

### Audio System 7단계

- `Assets/Scripts/Audio/AudioManager.cs`가 Scene 단위 AudioManager와 SFX/BGM AudioSource를 관리한다.
- SFX는 `SfxId`와 `PlayOneShot()`을 사용하며 HeroAttack, MonsterHit, Critical, MonsterSquash, MonsterAttack, MonsterDeath, LevelUp, Victory 이벤트가 BattleManager에 연결되어 있다.
- SFX/BGM AudioSource는 분리되어 있고 BattleSceneBuilder가 Hierarchy를 재생성한다. Main Camera의 기존 AudioListener를 사용한다.
- HeroAttack은 `hero_attack_01.flac` Whoosh를 Swing 시작에 재생하고, MonsterHit은 `monster_hit_01.wav` Slime Impact를 실제 타격 시점에 재생한다. 두 역할과 호출 시점을 분리한다.
- Critical은 `critical_01.mp3`를 실제 타격 레이어로 사용한다. Player Critical에서는 HeroAttack 이후 MonsterHit과 Critical을 함께 재생하고, Monster Critical에서도 같은 Critical Clip을 사용한다.
- Monster 공격은 `monster_squash_01.wav`의 Anticipation 압축음과 `monster_attack_01.mp3`의 Rush/Hero Impact 음으로 역할을 분리한다. Monster Critical은 여기에 공용 `critical_01.mp3`를 겹쳐 재생한다.
- MonsterDeath는 `monster_death_01.wav`를 Slime 사망 애니메이션 시작에 재생한다. 1~10층 Slime은 같은 Clip을 사용하며, Victory는 별도의 전투 결과 이벤트로 유지한다.
- Victory는 Reward 폴더의 `victory_01.wav`를 VictoryPanel 표시 시 재생한다. 1~10층이 같은 Jingle을 사용하며, Boss/Chapter Clear 전용 Jingle은 아직 없다.
- LevelUp은 Reward 폴더의 `level_up_01.wav`를 실제 레벨 상승 시에만 재생한다. 한 Reward에서 여러 레벨이 올라도 한 번만 울리며, Victory와 구분되도록 SFX와 기존 팝업을 함께 짧게 늦춘다.
- Save Load와 Reset은 LevelUp 이벤트가 아니므로 재생하지 않는다.
- Shop Buy, Equip, Button Click, Gold/UI SFX와 Settings/Volume UI, Audio Mixer는 TODO다.
- BGM AudioSource 기반만 있으며 실제 BGM은 아직 제작·재생하지 않는다.

---

## 14. Hero 및 장비 아트 방향

- 방어구/몸: 부위별 AI 오버레이 대신 완성된 전신 스프라이트 교체
- 무기: 별도 레이어
- 액세서리: 필요할 때 별도 레이어
- 캐릭터는 오른쪽 몬스터 방향을 보는 포즈 유지

현재 주요 파일:

    Assets/Art/Sprites/Hero/Body/hero_body_base.png
    Assets/Art/Sprites/Hero/Body/hero_beginner_01.png
    Assets/Art/Sprites/Hero/Weapon/weapon_wood_sword_01.png
    Assets/Art/Sprites/Hero/Body/hero_leather_armor_01.png
    Assets/Art/Sprites/Hero/Weapon/weapon_iron_sword_01.png

나무검 기준 배치:

    Pos X 110 / Pos Y 5 / Width 150 / Height 150 / Rotation 0

장비 구매, 장착, 스탯 적용, 저장과 Editor용 외형 교체는 구현되어 있다. 인벤토리는 TODO다. 철검과 가죽 갑옷 이미지는 items.json에 연결되어 있다.

---

## 15. BattleSceneBuilder 규칙

Assets/Scripts/Editor/BattleSceneBuilder.cs가 전투 UI의 소스 오브 트루스다.

주요 생성 오브젝트:

- BattleCanvas
- AudioManager / SFX AudioSource / BGM AudioSource
- PlayerPlaceholder / Hero 레이어 / SlimePlaceholder
- HP UI / WordBattlePanel
- StatusPanel / LevelText / ExpText / GoldText
- VictoryPanel
- ImpactEffect / CriticalImpactEffect / CriticalText
- LevelUpText
- ShopButton
- ShopPanel
- FloorDebugPanel
- DebugSaveResetButton

Builder 수정 후 반드시 실행:

    WordTower → Build Battle Scene

Hierarchy에만 수동 추가한 중요 UI는 Builder 재실행 시 사라질 수 있다.

알려진 정리 항목:

- BuildBattleScene() 초반 ClearScene() / CreateEventSystem() 호출이 두 번 반복된다. 현재 동작을 깨지는 않지만 추후 작은 정리 대상이다.

---

## 16. 폰트와 Sprite Import

현재 한글 TMP 폰트:

    Assets/Fonts/NotoSansKR-Regular.otf
    Assets/Fonts/NotoSansKR-Regular SDF.asset

- Variable Font 기반 NotoSansKR-VF SDF로 되돌리지 않는다.
- Builder는 NotoSansKR-Regular SDF.asset을 로드한다.
- SpriteImportProcessor는 Assets/Art/Sprites 아래 PNG에 Sprite/Single, PPU 100, Alpha, Point, Clamp, Compression None을 적용한다.
- 현재 실제 필터는 Point다. 부드러운 아트가 거칠면 Bilinear를 별도로 검토한다.

---

## 17. 플랫폼 주의사항

현재 확인:

- Windows Unity Editor에서 sqlite3 연결
- Editor에서 JSON spritePath 기반 이미지 교체

Android TODO:

- StreamingAssets DB를 Application.persistentDataPath로 복사
- Android SQLite 네이티브 설정 검증
- UnityEditor.AssetDatabase 없는 런타임 이미지 로딩

Windows Editor 성공을 Android 준비 완료로 간주하지 않는다.

---

## 18. 현재 구현 완료 기능

- SQLite 단어 DB 연결과 유효 단어 검사
- 사용 단어 중복 방지
- 몬스터 단어 난이도 필터
- 플레이어 일반 공격과 몬스터 반격
- 플레이어/몬스터 양방향 한방단어 크리티컬
- 양방향 한방단어 후 안전한 새 제시어
- HP, 데미지, 공격/피격/넉백/사망 연출
- 일반/크리티컬 VFX와 데미지 숫자
- VictoryPanel, EXP, Gold 보상
- 레벨, 필요 EXP 증가, 초과 EXP 이월
- 레벨업 Max HP/Attack 증가
- PlayerProgressData / PlayerProgressService 기반 플레이어 진행 데이터 분리
- JSON 기반 Save/Load와 Save Reset
- ItemData / items.json / ItemService 기반 아이템 데이터 구조
- PlayerProgress 기반 장착 장비 ID와 ownedItemIds 저장
- BattleScene Modal Shop 1단계와 Gold 구매
- 보유 Weapon/Armor 장착 및 교체
- Weapon attackBonus 기반 일반/크리티컬 Final Attack
- Armor defenseRate 기반 몬스터 일반/크리티컬 피해 감소
- 장비 변경 Save/Load, Reset 기본 장비, 잘못된 장비 ID fallback
- 장착 Weapon/Armor의 Editor용 Sprite 교체와 Load/Reset 외형 복원
- LV/EXP UI와 LEVEL UP 연출
- JSON 기반 Floor/Monster 로딩
- 1~10층 슬라임 챕터
- 10층 슬라임 킹 보스 플래그
- 몬스터별 이미지와 표시 배율
- 다음 층 전투 초기화
- Editor/Development Build용 Floor Debug UI
- 정적 한글 폰트 기반 TMP 표시
- Sprite 자동 Import
- Hero/Slime Procedural Idle, 등급별 Slime Idle 차이, Attack Anticipation, Hit Flash, Ground Shadow 연동
- Scene 단위 AudioManager와 SFX/BGM AudioSource 분리, SfxId/PlayOneShot 기반 중첩 재생
- HeroAttack, MonsterHit, Critical, MonsterSquash, MonsterAttack, MonsterDeath 실제 Combat SFX
- Victory와 LevelUp 실제 Reward SFX 및 다중 레벨업 1회 재생

---

## 19. 현재 미구현 / TODO

- 10층 클리어 후 존재하지 않는 11층 이동 방어와 챕터 완료 처리
- 보스 전용 패턴 및 isBoss 기반 전투 분기
- 인벤토리 UI
- 11층 이후 확정 콘텐츠
- 운영용 대규모 한국어 단어 DB
- 단어 뜻/도감/부적절 단어 관리
- Android SQLite와 런타임 이미지 로딩
- 타이틀/메뉴/튜토리얼
- Gold, Shop Buy, Equip, UI Button 실제 SFX
- 실제 BGM AudioClip과 BGM 재생 로직
- Settings/Volume UI와 Audio Mixer
- Chapter Clear Sequence, Illustration Unlock, Gallery
- Boss/Chapter Clear 전용 Jingle 또는 Music
- 프로덕션 UI 폴리시

---

## 20. 현재 권장 다음 작업

전투 Polish, Equipment Visual, 핵심 Combat/Reward SFX까지 구현됐다. 다음 작업 후보는 아래 순서이며 개발 상황에 따라 조정할 수 있다.

권장 순서:

1. Shop Buy SFX
2. Equip SFX
3. Button Click SFX
4. Slime Chapter Clear Sequence 구현
5. Chapter Clear Illustration 최종 제작 및 적용
6. BGM 방향성 기획 및 구현

BGM은 임시 곡을 먼저 붙이기보다 WordTower 고유의 음악적 Identity를 정한 후 진행한다. Main/Title Theme, Battle BGM, Boss BGM, Chapter Clear Music이 완전히 분리된 곡이 아니라 공통 Melody 또는 Motif를 공유하는 방향을 검토한다. 현재는 기획 단계다.

---

## 21. Git 및 작업 규칙

- 작업 전 git status 확인
- 사용자의 기존 변경 보존
- Unity .meta를 에셋과 함께 커밋
- 의미 있는 마일스톤에서 commit/push
- 파괴적인 Git 명령 금지
- TMP Font Asset은 Unity가 바꿀 수 있으므로 discard 전 확인

다른 PC에서 시작:

    cd D:\Projects\WordTower
    git status
    git pull

저장:

    git status
    git add .
    git commit -m "Meaningful commit message"
    git push

---

## 22. Codex 작업 원칙

1. 이 문서를 먼저 읽는다.
2. 관련 실제 파일을 확인한 뒤 수정한다.
3. 메서드명과 데이터 구조를 추측하지 않는다.
4. 작고 검증 가능한 변경을 선호한다.
5. 전투 연출, DB 필터, JSON 흐름을 보존한다.
6. 공용 씬 오브젝트 이름을 함부로 변경하지 않는다.
7. Builder UI 변경 시 Builder와 BattleManager를 함께 확인한다.
8. 불필요한 런타임 프레임워크/API를 도입하지 않는다.
9. 변경 파일, 동작, 테스트 방법을 설명한다.
10. 컴파일뿐 아니라 다음 턴, Victory, 다음 층, DB 연결까지 확인한다.

협업 선호:

- 이론보다 실제 빌드 진행 중심
- 정확한 파일/메서드/테스트 체크포인트
- PowerShell 사용
- 작은 단위 시각 테스트
- 과도한 일괄 리팩터링 금지

---

## 23. AGENTS.md 관리 원칙

다음 변화가 발생하면 같은 작업에서 AGENTS.md도 업데이트한다.

- 새로운 핵심 시스템 구현
- 게임 핵심 규칙 변경
- 데이터 구조 변경
- 주요 폴더 구조 변경
- 챕터/몬스터 구성 확정
- 중요한 밸런스 규칙 확정
- 기존 TODO 기능 구현 완료
- 다른 Codex 세션이 반드시 알아야 하는 설계 결정

과도하게 기록하지 않는 항목:

- 단순 버그 수정
- 사소한 위치/크기 조정
- 일시적인 테스트 값
- 작업 과정의 세부 로그

이 문서는 작업 일지가 아니라 현재 구조, 확정 규칙, 구현 상태, 중요한 TODO를 빠르게 이해하기 위한 지속 컨텍스트로 유지한다.

---

## 24. Definition of Done

- Unity 컴파일 오류와 런타임 예외 없음
- DB 연결 정상
- 입력과 다음 턴 정상
- 일반/크리티컬 공격 정상
- Victory와 보상 정상
- 레벨업/UI 정상
- 다음 층 또는 Debug 이동 정상
- 몬스터 이미지/배율 정상
- 한글 글리프 누락 없음
- Builder 재생성 후 필요한 UI 유지

    Make it work
    → Test it
    → Make it fun
    → Commit it
    → Refactor only when useful
