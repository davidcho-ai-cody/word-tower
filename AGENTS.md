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
- 앱 진입 씬: Assets/Scenes/StudioSplashScene.unity
- 공식 전투 씬: Assets/Scenes/BattleScene.unity
- 목표 Build Scene 순서: Index 0 StudioSplashScene, Index 1 OpeningScene, Index 2 TitleScene, Index 3 StoryScene, Index 4 StoryPlaybackScene, Index 5 ShopScene, Index 6 BattleScene
- 중복 루트 씬 Assets/BattleScene.unity는 삭제됐으며 다시 생성하거나 사용하지 않는다.
- 초기 런타임은 AI/API 없이 로컬 단어 데이터로 동작한다. Windows Editor는 SQLite DB, Android 1차 APK는 JSON 단어 데이터를 사용한다.

WordTower는 단순 단어 퀴즈가 아니라 전투 언어가 끝말잇기인 RPG를 지향한다. 전투 손맛, 보이는 성장, 단어 선택의 전략성을 보존한다.

현재 전체 Scene Flow:

    StudioSplashScene
    → OpeningScene (최초 1회만)
    → TitleScene
    → BattleScene
    → ShopScene
    → BattleScene
    → HOME
    → TitleScene

Title STORY 흐름:

    TitleScene
    → StoryScene
    → StoryPlaybackScene (해금 Story 다시보기)
    → StoryScene

Opening Story를 이미 본 경우:

    StudioSplashScene
    → TitleScene

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

- LevelText: {playerLevel}
- PlayerName: LV.{playerLevel} 용사
- ExpText: {exp} / {requiredExp}
- ExpBarFill: EXP 진행률 fillAmount
- GoldText: {gold:N0}
- LevelUpTextEffect(): LevelUpOverlay로 도달 레벨과 실제 HP/ATK 증가량을 표시
- LevelUpOverlay는 BattleSceneBuilder가 생성
- 상단 PlayerName은 과거 Scene의 `LV.1 용사` 정적 문자열만 사용해 레벨업 후 갱신되지 않았다. 현재는 BattleManager가 PlayerName TMP_Text를 참조하고 `UpdateUI()`에서 하단 LevelText와 같은 PlayerProgressData.playerLevel 기준으로 동기화한다.

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
- Android 1차 APK 데이터: Assets/StreamingAssets/Data/Words/words.json

WordData는 [Table("words")]로 실제 words 테이블과 매핑한다.

Windows Editor에서는 기존 words.db와 sqlite3.dll을 사용한다. Android 런타임에서는 Android ARM64 SQLite 네이티브 플러그인이 아직 확정되지 않았으므로, 1차 APK 호환을 위해 StreamingAssets의 words.json을 UnityWebRequest로 읽어 메모리 조회한다. words.db는 원본/검증용으로 유지하며, 운영용 대규모 DB 단계에서는 Android SQLite 플러그인과 persistentDataPath 복사 구조를 다시 검토한다.

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

RuntimeDataLoader — Assets/Scripts/RuntimeDataLoader.cs:

- Android StreamingAssets 텍스트 데이터를 UnityWebRequest로 로드
- Editor/Standalone에서는 Assets/Data를 우선 읽고 StreamingAssets를 fallback으로 사용
- Android 1차 APK에서 floors.json, monsters.json, items.json, words.json 로딩에 사용

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

## 8.2 Shop System 1단계 — Fullscreen ShopScene 전환

구현:

- BattleScene의 ShopButton은 팝업을 열지 않고 현재 진행을 저장한 뒤 `ShopScene`으로 이동한다.
- ShopScene은 StoryScene처럼 독립적인 9:16 전체화면 메뉴로 운영한다.
- ShopSceneManager가 ItemService / PlayerProgressService / SaveService를 사용해 기존 구매, 장착, Gold, Save 규칙을 재사용한다.
- ShopSceneBuilder가 ShopCanvas / Background / Header / Tabs / ItemScrollView / BackButton / PNG Template을 생성한다.
- BattleSceneBuilder는 BattleScene에 ShopButton만 생성하고, 더 이상 BattleScene 내부 ShopPanel을 생성하지 않는다.

구현 파일:

- Assets/Scripts/Shop/ShopSceneManager.cs
- Assets/Scripts/Editor/ShopSceneBuilder.cs
- Assets/Scenes/ShopScene.unity는 Unity Editor에서 `WordTower → Build Shop Scene` 실행으로 생성한다.

Shop UI 구조:

- BattleScene: ShopButton
- ShopScene / ShopCanvas / ShopRoot
- Background: `Assets/Art/UI/Shop/shop_background.png`
- Header: GoldText, MessageText
- TabArea: WeaponTab, ArmorTab, AccessoryTab
- ItemScrollView / Viewport / Content
- BackButton
- ItemCardTemplate / ItemCardEquippedTemplate / BuyButtonTemplate

탭:

- Weapon: 1차 실제 콘텐츠. `wood_sword_01`, `iron_sword_01`을 표시한다.
- Armor: UI 전환만 제공하며 1차에서는 `준비 중` 표시
- Accessory: UI 전환만 제공하며 1차에서는 `준비 중` 표시

Shop PNG:

- `Assets/Art/UI/Shop/shop_background.png`
- `Assets/Art/UI/Shop/shop_tab_active.png`
- `Assets/Art/UI/Shop/shop_tab_inactive.png`
- `Assets/Art/UI/Shop/shop_item_card.png`
- `Assets/Art/UI/Shop/shop_item_card_equipped.png`
- `Assets/Art/UI/Shop/shop_buy_button.png`

Shop 카드:

- 미장착 카드는 `shop_item_card.png`, 장착중 카드는 `shop_item_card_equipped.png`를 사용한다.
- 카드에는 아이템 Sprite, 이름, ATK, 가격, 상태 버튼을 TMP/UI로 표시한다.
- 미보유는 `구매`, 보유 미장착은 `장착`, 현재 장비는 비활성 `장착 중` 버튼으로 표시한다.

BattleScene에서 ShopScene으로 이동할 수 있는 상태:

- battleEnded가 false
- Scene 전환 중이 아님
- WordInput과 AttackButton이 interactable
- VictoryPanel이 표시 중이 아님

공격 애니메이션, 몬스터 턴, 사망 연출, Victory 처리 중에는 입력 버튼이 잠겨 있으므로 ShopButton도 비활성화된다. Shop 진입 시 BattleManager는 SaveGame() 후 `SceneManager.LoadScene("ShopScene")`을 호출한다.

구매 규칙:

- ItemService.GetItem(id)로 아이템 존재 확인
- 이미 ownedItemIds에 있으면 재구매 불가
- Gold가 부족하면 구매하지 않고 ShopMessage에 안내
- Gold가 충분하면 Gold 차감, ownedItemIds 추가, UI 즉시 갱신, SaveGame() 호출
- price = 0인 기본 장비는 기본 ownedItemIds에 포함되므로 구매 대상이 아니다.

구매와 장착은 분리한다. 미보유 아이템은 구매 버튼, 보유한 미장착 아이템은 장착 버튼, 현재 장비는 비활성 장착 중 버튼으로 표시한다. Weapon/Armor만 타입에 맞게 장착할 수 있으며 장착 변경은 즉시 Save하고 Shop UI와 장비 외형을 갱신한다.

ShopScene의 BackButton과 Android Back/Escape는 Save 후 `BattleScene`으로 복귀한다. BattleScene 복귀 후 BattleManager가 Save를 다시 읽고 장착 무기 외형과 Final Attack을 반영한다.

ShopScene UI 표시 안정화:

- `shop_background.png`에 이미 포함된 SHOP 로고와 겹치지 않도록 별도 SHOP Title TMP와 Subtitle TMP는 생성하지 않는다.
- GoldText와 MessageText만 Header에 동적 TMP로 유지한다.
- WeaponTab / ArmorTab / AccessoryTab은 각각 `Background` Image와 Full Stretch `Label` TMP를 자식으로 둔다.
- BackButton도 `Background` Image와 Full Stretch `Label("닫기")` TMP 구조를 사용한다.
- 아이템 목록 Content 이름은 `ShopItemListContent`로 고정해 런타임 검색 충돌을 피한다.
- 초기 무기 탭에는 `WoodenSwordCard`, `IronSwordCard` 2개가 표시되어야 한다.
- Viewport에는 Stencil 기반 일반 `Mask`를 사용하지 않고 `RectMask2D`만 사용한다. 일반 Mask가 TMP와 카드 렌더링을 통째로 가리는 문제가 확인됐다.
- 카드/버튼 PNG는 inactive Template을 `GameObject.Find()`로 찾지 않고, ShopSceneBuilder가 ShopSceneManager에 Sprite 참조를 직렬화해 전달한다.
- Shop 아이템 카드는 새 가로형 `shop_item_card.png` / `shop_item_card_equipped.png` 기준으로 940x320 크기를 사용한다.
- 카드 내부는 왼쪽 무기 아이콘, 중앙 이름/설명, 하단 ATK/Gold, 우측 상태 버튼 구조로 배치한다.

주의:

- Unity batchmode가 라이선스 재연결에서 지연될 수 있다. Scene 파일이 없는 PC에서는 Unity Editor에서 `WordTower → Build Shop Scene`을 수동 실행해 `Assets/Scenes/ShopScene.unity`와 Build Settings 반영을 완료한다.
- OpeningSceneBuilder / StorySceneBuilder / StoryPlaybackSceneBuilder / StudioSplashSceneBuilder의 Build Settings 배열은 ShopScene을 StoryPlaybackScene과 BattleScene 사이에 포함하도록 갱신한다.
- TitleSceneBuilder는 기존처럼 TitleScene만 독립 생성하며 Build Settings를 변경하지 않는다.

Equipment Visual:

- Weapon은 ItemData.spritePath를 기존 Weapon Image에 적용한다.
- Armor는 ItemData.characterSpritePath를 기존 Body Image에 적용한다.
- 장착 직후, Save Load 후 초기화, Save Reset 시 현재 equipped ID 기준으로 외형을 갱신한다.
- Sprite 교체 시 기존 RectTransform을 유지해 위치, 크기, 공격/피격 애니메이션을 보존한다.
- 경로가 비어 있거나 Sprite 로드에 실패하면 현재 표시 중인 Sprite를 유지하고 Warning만 남긴다.
- Editor에서는 UnityEditor.AssetDatabase를 우선 사용하고, Android/Player에서는 Assets/Resources/Art/Sprites 아래 복제된 Sprite를 Resources.Load로 읽는다.

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

### 9.2 Studio Splash 1차 — 구현 완료

Studio Splash는 게임 실행 직후 매번 약 3초 동안 `PLAY YOUR NEXT WORLD` 브랜드 로고와 영어 음성을 재생한 뒤, StoryProgress에 따라 Opening 또는 Title로 분기하는 최초 진입 Scene이다.

구현:

- Assets/Scripts/Brand/StudioSplashManager.cs
- Assets/Scripts/Editor/StudioSplashSceneBuilder.cs
- Assets/Scenes/StudioSplashScene.unity

브랜드 에셋:

- Assets/Art/Brand/Studio/play_your_next_world_logo.png
- Assets/Audio/Brand/Studio/play_your_next_world_voice.mp3

기본 흐름:

    StudioSplashScene
    → StoryProgressService.LoadOrCreate()
    → hasSeenOpeningStory == false ? OpeningScene : TitleScene

연출:

- 1차 버전은 PLAY YOUR NEXT WORLD 전체 로고 PNG 1개를 사용한다.
- 영어 음성 `Play Your Next World`를 재생한다.
- 첫 프레임 로고 Flicker를 막기 위해 Awake에서 Canvas와 Alpha/Scale 초기값을 설정한다.
- 0.0~0.35초는 어두운 배경과 약한 청백색 Glow를 유지한다.
- 0.35~0.85초는 로고 Alpha 0→1, Scale 0.92→1.03, LightSweep 이동을 적용한다.
- 0.85~1.15초는 로고 Scale 1.03→1.0으로 안정화한다.
- 약 1.05초부터 영어 음성을 재생하며 음성이 끝나기 전 다음 Scene으로 넘어가지 않는다.
- 마지막 약 0.35초는 전체 Fade Out 후 다음 Scene으로 이동한다.
- 1차 StudioSplash에는 터치 Skip을 넣지 않고 Android Back/Escape도 별도로 처리하지 않는다.

Builder 실행 후 목표 Build Scene 순서:

1. Assets/Scenes/StudioSplashScene.unity
2. Assets/Scenes/OpeningScene.unity
3. Assets/Scenes/TitleScene.unity
4. Assets/Scenes/StoryScene.unity
5. Assets/Scenes/StoryPlaybackScene.unity
6. Assets/Scenes/ShopScene.unity
7. Assets/Scenes/BattleScene.unity

`StudioSplashSceneBuilder`는 `WordTower → Build Studio Splash Scene` 메뉴로 StudioSplashScene을 생성하고 위 Build Scene 순서를 설정한다. `OpeningSceneBuilder`, `StorySceneBuilder`, `StoryPlaybackSceneBuilder`, `ShopSceneBuilder`도 같은 Build Scene 순서를 사용하도록 맞춘다.

향후 고도화 후보:

- X/O 개별 등장
- X slash / O orbit
- Particle System
- Bloom / Shader
- Whoosh / Impact SFX
- Studio BGM sting

### 9.3 Opening Story — 구현 완료 및 2차 연출 튜닝 완료

오프닝 스토리는 WordTower 최초 실행 시 1회만 자동 재생하고, 완료 또는 Skip 후 TitleScene으로 진입하는 구조다. 기존 gameplay save(`wordtower_save.json`)와 분리된 `wordtower_story_progress.json`을 사용해 오프닝 시청 여부를 저장한다.

구현:

- Assets/Scripts/Story/StoryProgressData.cs
- Assets/Scripts/Story/StoryProgressService.cs
- Assets/Scripts/Story/OpeningStoryManager.cs
- Assets/Scripts/Editor/OpeningSceneBuilder.cs
- Assets/Scripts/Editor/OpeningStoryDebugMenu.cs
- Assets/Scenes/OpeningScene.unity

오프닝 에셋:

- Assets/Art/Opening/opening_01_peaceful_world.png
- Assets/Art/Opening/opening_02_words_disappear.png
- Assets/Art/Opening/opening_03_demon_king_steals_words.png
- Assets/Art/Opening/opening_04_world_without_words.png
- Assets/Art/Opening/opening_05_hero_awakens.png
- Assets/Art/Opening/opening_06_toward_word_tower.png
- Assets/Art/Opening/opening_07_demon_kings_plan.png
- Assets/Art/Opening/opening_08_adventure_begins.png
- Assets/Audio/BGM/Opening/wordtower_opening_theme.mp3

2차 튜닝 Timeline:

- Cut 1: 0.0 ~ 3.8
- Cut 2: 3.8 ~ 6.2
- Cut 3: 6.2 ~ 9.7
- Cut 4: 9.7 ~ 14.0
- Cut 5: 14.0 ~ 17.4
- Cut 6: 17.4 ~ 21.2
- Cut 7: 21.2 ~ 26.0
- Cut 8: 26.0 ~ 30.8

OpeningStoryManager는 `AudioSource.time`을 우선 기준으로 컷과 Motion을 동기화한다. 오프닝 음악은 0초부터 바로 재생하되 시작 볼륨 0에서 약 1.3초 동안 원래 AudioSource 볼륨까지 Fade In하고, 자연 종료 마지막 약 0.9초에는 Fade Out한다. SKIP 버튼과 Android Back/Escape는 반응성을 위해 긴 Audio Fade Out을 기다리지 않고 같은 완료 처리로 연결되어 `hasSeenOpeningStory = true` 저장 후 TitleScene으로 이동한다.

컷 전환은 약 0.3초 Cross Fade를 유지하되, 다음 컷을 Fade 시작 시점부터 alpha 0 상태로 준비하고 Zoom/Pan Motion도 함께 진행한다. Cross Fade 중에는 outgoing cut과 incoming cut이 모두 움직이며, 전환 완료 시 incoming cut의 Motion progress를 reset하지 않고 Audio Timeline 기준으로 이어간다. 새 컷 초반 약 0.45초는 Motion을 약하게 적용해 핵심 스토리 문장 가독성을 확보한다. Cut 8은 마지막 약 1.8초 동안 Motion을 멈춰 `WORD TOWER` 로고와 모험 시작 이미지를 안정적으로 보여준다.

OpeningSceneBuilder는 `WordTower → Build Opening Scene` 메뉴로 OpeningScene을 생성하고 Build Scene 순서를 아래처럼 맞춘다.

1. Assets/Scenes/StudioSplashScene.unity
2. Assets/Scenes/OpeningScene.unity
3. Assets/Scenes/TitleScene.unity
4. Assets/Scenes/StoryScene.unity
5. Assets/Scenes/StoryPlaybackScene.unity
6. Assets/Scenes/ShopScene.unity
7. Assets/Scenes/BattleScene.unity

Editor 전용 디버그 메뉴 `WordTower → Debug → Reset Opening Story`는 `wordtower_story_progress.json`만 삭제해 다음 Play에서 Opening Story를 다시 최초 실행 상태로 재생하게 한다. gameplay save인 `wordtower_save.json`은 건드리지 않는다.

향후 TitleScene STORY 메뉴에서는 `forceReplay` 또는 별도 Story 선택 진입을 통해 이미 본 Opening도 다시 재생할 수 있게 확장한다. `opening_08_adventure_begins.png`는 향후 TitleScene 배경과 Seamless Transition 후보로 남긴다.

### 9.4 STORY 메뉴 1차 / UI 2차 튜닝 — Builder 반영 완료

TitleScene의 STORY 버튼은 `StoryScene` 이동으로 연결됐다. STORY 메뉴 1차는 프롤로그 다시보기와 10층 단위 잠금 챕터 목록을 제공하는 구조다.

구현:

- Assets/Scripts/Story/StoryMenuManager.cs
- Assets/Scripts/Editor/StorySceneBuilder.cs
- Assets/Scenes/StoryScene.unity

STORY 배경:

- Assets/Art/UI/Story/wordtower_story_background.png

StorySceneBuilder 메뉴:

    WordTower → Build Story Scene

StoryScene UI:

- 상단 `STORY`
- 서브타이틀 `되찾은 단어의 기록`
- 진행도 `되찾은 단어 0 / 10`
- PrologueCard: unlocked
- Chapter10Card ~ Chapter100Card: `???` locked
- 세로 ScrollView 기반 목록
- BackButton으로 TitleScene 복귀

2차 UI 튜닝 생성 값:

- 카드 높이는 176 → 200으로 늘려 텍스트 여백을 확보했다.
- 카드 간격은 22 → 27로 늘려 잠긴 챕터 카드들이 덜 붙어 보이게 했다.
- ScrollView 하단 padding은 36 → 88로 늘려 마지막 카드 아래로 배경 하단부가 보이도록 했다.
- Chapter header, `???`, 설명, `LOCK` 텍스트 크기와 밝기를 높여 모바일 9:16 화면에서 가독성을 개선했다.
- PrologueCard의 `PROLOGUE`, 제목, 설명 텍스트 크기를 키웠다.
- Progress 값 `0 / 10`은 30 → 35로 키우고 Gold/Bold 강조를 유지한다.
- ScrollView 구조와 StoryMenuManager 기능 로직은 변경하지 않았다.

3차 UI 컬러 튜닝:

- Prologue와 Chapter 01은 같은 Unlocked Story 시각 규칙을 사용한다.
- Unlocked Story 카드는 밝은 베이지 대신 Dark Purple/Navy 배경과 얇은 Gold Border를 사용한다.
- `PROLOGUE`, `CHAPTER 01`, `10F`, `PLAY`, Play icon 등 주요 accent는 Warm Gold 계열로 통일한다.
- `빼앗긴 단어`, `사랑` 같은 해금 카드 제목은 Warm Ivory 계열을 사용한다.
- 해금 카드 설명은 Soft Ivory 계열로 Locked description보다 밝게 표시한다.
- Chapter 01 사랑 카드는 같은 Dark Purple/Navy 계열 안에서 아주 약한 Rose accent만 추가한다.
- Chapter 02~10 Locked 카드는 기존 Dark Navy / Muted Purple / Dim Text 상태를 유지한다.
- Prologue replay, Chapter 01 replay, StoryProgress, Progress 1/10, Scene 이동 기능 로직은 변경하지 않았다.

현재 Unity Editor에서 `WordTower → Build Story Scene`을 실행하면 위 생성 값이 `Assets/Scenes/StoryScene.unity`에 반영된다.

프롤로그 다시보기:

    StoryScene
    → PrologueCard 클릭
    → OpeningStoryManager.RequestReplay()
    → OpeningScene
    → 자연 종료 / SKIP / Back
    → TitleScene

Opening 다시보기는 `hasSeenOpeningStory`를 false로 되돌리지 않는다. gameplay save와 `wordtower_story_progress.json` 초기화도 하지 않는다. 다음 앱 실행 시 Opening이 자동 재생되지 않아야 하며, 최초 시청 상태 초기화는 기존 Editor Debug 메뉴 `WordTower → Debug → Reset Opening Story`만 담당한다.

Build Settings 목표 순서:

1. Assets/Scenes/StudioSplashScene.unity
2. Assets/Scenes/OpeningScene.unity
3. Assets/Scenes/TitleScene.unity
4. Assets/Scenes/StoryScene.unity
5. Assets/Scenes/StoryPlaybackScene.unity
6. Assets/Scenes/ShopScene.unity
7. Assets/Scenes/BattleScene.unity

StorySceneBuilder나 Story UI 생성 값이 바뀐 경우 실제 Scene 반영을 위해 Unity Editor에서 `WordTower → Build Story Scene`을 실행해야 한다. Scene YAML 직접 수정은 하지 않는다.

세계관 확장 방향:

- 10층마다 마왕에게 빼앗긴 중요한 단어 하나를 되찾는다.
- 핵심 단어 후보: 사랑, 우정, 희망, 배려, 꿈, 용기, 믿음, 웃음, 가족, 마음
- 100층의 마지막 핵심은 `마음`이다.
- 모든 단어를 되찾으면 마왕의 힘 또는 마왕성이 무너지는 방향이다.
- 1차 UI에서는 잠긴 단어명을 공개하지 않고 `???`만 표시한다.

### 9.5 10층 Slime King 클리어 스토리 — 소스/Builder 추가

10층 Slime King을 최초로 처치하면 일반 Victory보다 먼저 8컷 Story Playback을 재생하고, 첫 번째 빼앗긴 단어 `사랑`을 되찾는다. 다시보기와 최초 클리어 보상은 분리한다.

구현:

- Assets/Scripts/Story/StoryCatalog.cs
- Assets/Scripts/Story/StoryPlaybackManager.cs
- Assets/Scripts/Editor/StoryPlaybackSceneBuilder.cs
- Assets/Scenes/StoryPlaybackScene.unity은 Builder 실행으로 생성한다.

StoryPlaybackSceneBuilder 메뉴:

    WordTower → Build Story Playback Scene

Story ID / Keyword:

- Story ID: `floor_10_clear`
- Keyword ID: `love`
- 표시명: `사랑`
- 의미: `누군가를 소중하게 생각하는 마음`

8컷 이미지:

- Assets/Art/Story/Floor10/floor10_01_battle_end.png
- Assets/Art/Story/Floor10/floor10_02_mysterious_light.png
- Assets/Art/Story/Floor10/floor10_03_love_crystal.png
- Assets/Art/Story/Floor10/floor10_04_hero_holds_love.png
- Assets/Art/Story/Floor10/floor10_05_slimes_reunite.png
- Assets/Art/Story/Floor10/floor10_06_slime_king_remembers.png
- Assets/Art/Story/Floor10/floor10_07_hero_next_journey.png
- Assets/Art/Story/Floor10/floor10_08_demon_king.png

10층 최초 클리어 흐름:

    Slime King HP 0
    → SlimeDeathSequence()
    → floor_10_clear 미해금이면 StoryPlaybackScene
    → 8컷 Story / SKIP 허용
    → floor_10_clear unlock 저장
    → 사랑 획득 Overlay, 1 / 10
    → BattleScene 복귀
    → 기존 WinBattle() 보상 지급
    → VictoryPanel

이미 `floor_10_clear`가 해금된 경우 10층 승리 시 Story를 자동 재생하지 않고 기존 Victory 흐름으로 바로 진행한다. Story replay는 보상, Gold, EXP, Save 진행도, unlock을 중복 적용하지 않는다.

8컷 대사/타이밍:

1. 전투의 끝, 약 3.0초: `마침내... / 슬라임킹을 쓰러뜨렸다.`
2. 이상한 빛, 약 3.2초: `용사: ...이건 뭐지?`와 빛 내레이션
3. 사랑의 결정, 약 3.2초: `용사: 이 글자는... / 사랑`
4. 결정을 바라보는 용사, 약 3.5초: `용사: 사랑...?`
5. 슬라임들의 기쁨, 약 3.5초: `슬라임킹: 이... 따뜻한 느낌은...`
6. 마음을 되찾은 슬라임킹, 약 4.0초: `그게... 사랑이었어.`
7. 용사의 깨달음, 약 4.2초: 단어가 아니라 마음을 빼앗겼다는 깨달음
8. 마왕의 독백, 약 4.0초: 아직 아홉 개가 남아 있다는 암시

StoryPlayback 연출:

- Opening Story의 Cross Fade / Zoom / Pan 패턴을 재사용한다.
- Cross Fade는 약 0.3초이며 outgoing / incoming 이미지 모두 Motion을 유지한다.
- 하단 DialoguePanel은 TMP 기반 SpeakerName / DialogueText를 사용한다.
- Story 완료 또는 Skip 시 최초 클리어일 때만 `빼앗긴 단어를 되찾았습니다 / 사랑 / 1 / 10` Overlay를 표시한다.

StoryProgress:

- `StoryProgressData.unlockedStoryIds`에 `floor_10_clear`를 1회만 추가한다.
- Floor Story 진행도는 `floor_*_clear` 형태의 unlock 수로 계산한다.
- 10층 클리어 후 STORY 메뉴 Progress는 `1 / 10`이다.
- Chapter 01은 `CHAPTER 01 / 10F / 사랑 / 되찾은 첫 번째 단어 / ▶` 상태로 해금된다.

Replay:

    StoryScene
    → Chapter 01 클릭
    → StoryPlaybackManager.RequestReplay(floor_10_clear)
    → StoryPlaybackScene
    → 자연 종료 / SKIP / Back
    → StoryScene

Replay는 StoryProgress, Battle 진행, Victory 보상에 영향을 주지 않는다. 향후 20층~100층 Story도 `floor_20_clear`, `floor_30_clear` 같은 동일 패턴으로 확장한다.

Build Settings 목표 순서:

1. Assets/Scenes/StudioSplashScene.unity
2. Assets/Scenes/OpeningScene.unity
3. Assets/Scenes/TitleScene.unity
4. Assets/Scenes/StoryScene.unity
5. Assets/Scenes/StoryPlaybackScene.unity
6. Assets/Scenes/ShopScene.unity
7. Assets/Scenes/BattleScene.unity

StoryPlaybackSceneBuilder나 StoryPlayback UI 생성 값이 바뀐 경우 실제 Scene 반영을 위해 Unity Editor에서 `WordTower → Build Story Playback Scene`을 실행해야 한다. Scene YAML 직접 수정은 하지 않는다.

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

Editor에서는 BattleManager.ApplyMonsterSprite()가 JSON spritePath를 UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>()로 우선 로드한다.

Android/Player에서는 같은 JSON spritePath를 Resources 경로로 변환해 Assets/Resources/Art/Sprites 아래 복제 Sprite를 로드한다. 장기적으로는 Resources 복제 대신 Addressables 또는 직렬화 참조 구조로 정리하는 작업이 TODO다.

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
- LEVEL UP! Overlay

시각 테스트된 타이밍과 공용 오브젝트 이름은 관련 작업이 아니면 변경하지 않는다.

### 13.1 Battle HUD / Level Up Overlay — PNG 적용 완료

BattleScene의 전투 HUD는 WordTower 전용 PNG가 디자인을 담당하고, Unity TMP/UI가 실제 데이터를 표시하는 구조다.

적용 에셋:

- `Assets/Art/UI/Battle/battle_screen_frame.png`
- `Assets/Art/UI/Battle/battle_hud_frame.png`
- `Assets/Art/UI/Battle/levelup_magic_circle.png`

Builder 기준 Hierarchy:

    BattleCanvas
    ├─ Background
    ├─ 기존 전투 UI
    ├─ StatusPanel
    │  ├─ HudFrameImage
    │  ├─ LevelLabel
    │  ├─ LevelText
    │  ├─ ExpLabel
    │  ├─ ExpBarBackground
    │  │  └─ ExpBarFill
    │  ├─ ExpText
    │  ├─ GoldLabel
    │  └─ GoldText
    ├─ BattleScreenDecoration
    └─ LevelUpOverlay
       ├─ DimBackground
       └─ LevelUpContent
          ├─ MagicCircleImage
          ├─ LevelUpText
          ├─ NewLevelText
          ├─ HpIncreaseText
          └─ AtkIncreaseText

HUD 원칙:

- PNG는 Dark Navy / Purple / Antique Gold / Purple Jewel 장식과 프레임을 담당한다.
- `LevelText`, `ExpText`, `GoldText`는 BattleManager의 PlayerProgress 기반 값으로 갱신한다.
- PNG에는 숫자와 진행 데이터가 박혀 있지 않다.
- 기존 임시 `HudOuterBorder`, `HudInnerBorder`, Corner Accent, Level icon shape, Gold coin shape는 더 이상 생성하지 않는다.
- `BattleScreenDecoration`은 `Image.raycastTarget = false`라 입력을 막지 않는다.
- `battle_screen_frame.png`는 전체 화면 Overscan 장식이 아니라 전투/입력 영역을 감싸는 Screen Frame으로 사용한다.
- Screen Frame은 `anchorMin=(0,0)`, `anchorMax=(1,1)`, `offsetMin=(14,210)`, `offsetMax=(-14,-14)`, `preserveAspect=false` 기준으로 하단 HUD 영역 위에서 끝난다.
- 기존 `offsetMin=(-28,-28)`, `offsetMax=(28,28)` Overscan 방식은 폐기했다.
- `battle_hud_frame.png`는 원본 비율을 유지하고 하단 `StatusPanel`에 배치하는 상태 HUD 전용 프레임이다.
- Screen Frame 하단 장식과 HUD Frame은 겹치지 않게 별도 세로 영역으로 분리한다.

EXP Gauge:

- `ExpBarFill`은 PNG 중앙 게이지 안쪽에 배치된 Unity Image다.
- EXP 비율은 `Mathf.Clamp01((float)exp / requiredExp)` 기준이다.
- `Image.fillAmount` 값은 갱신하되 실제 표시 폭은 `ExpBarFill` RectTransform의 `anchorMax.x`가 ratio를 직접 반영한다.
- `0 / required`는 fill을 숨기고, 25/50/100%는 실제 폭이 달라져야 한다.

Level Up Overlay:

- `levelup_magic_circle.png`가 Gold/Purple 마법진, Glow, 보석 장식을 담당한다.
- `LEVEL UP!`, `LV. X`, `HP +N`, `ATK +N`은 Unity TMP 4개로 분리해 표시한다.
- Level Up TMP는 MagicCircleImage 중심을 기준으로 `LEVEL UP!` / `LV. X` / `HP +N` / `ATK +N` 4단 중앙 정렬을 사용한다. `LV. X`는 중앙에서 가장 크게 강조한다.
- `LevelUpContent`는 화면 중앙 `740 x 740` 기준점이며 `MagicCircleImage`와 같은 중심을 사용한다. TMP 로컬 위치는 `LEVEL UP! (0,+135)`, `LV. X (0,+15)`, `HP (0,-80)`, `ATK (0,-125)`이다.
- Level Up TMP 최종 기준은 `LEVEL UP!` font 72 / rect 560x90, `LV. X` font 88 / rect 460x110, `HP` font 32 / rect 360x52, `ATK` font 32 / rect 360x52이며 세부 좌표는 모두 LevelUpContent 기준 local anchoredPosition이다.
- Level Up TMP는 모두 anchor/pivot center, Auto Size OFF, Word Wrap OFF, Overflow, margin 0을 사용해 줄바꿈과 클리핑을 방지한다.
- 레벨업 시 기존 PlayerProgressService 결과를 사용하고, 새 레벨과 실제 증가량을 표시한다.

    hpIncrease = newMaxHp - oldMaxHp
    attackIncrease = newAttack - oldAttack

- 연출은 fullscreen Dim 위에서 `LevelUpContent` scale pop/pulse, MagicCircleImage rotation/alpha fade, 공통 CanvasGroup fade로 구성한다. TMP 텍스트는 회전하지 않는다.
- Level Up TMP 개별 Fade는 간헐적 미표시를 막기 위해 사용하지 않는다. LevelUp 시작 시 `LEVEL UP!`, `LV. X`, `HP +N`, `ATK +N` alpha를 모두 1로 초기화하고, 이후 표시 중에는 TMP 개별 alpha를 다시 변경하지 않는다.
- Level Up Coroutine은 중복 실행 시 이전 연출을 중단하고 새 연출 상태를 재초기화해 이전 alpha 상태가 다음 레벨업에 영향을 주지 않게 한다.
- Save Reset과 Debug Floor 이동은 `StopAllCoroutines()` 이후 LevelUp 전용 상태를 다시 초기화한다. Overlay는 비활성, CanvasGroup alpha는 0, TMP alpha는 1, LevelUp Coroutine handle은 null 상태가 기본이다.
- 모든 Level Up 텍스트가 함께 보이는 hold 구간을 최소 약 0.7초 확보한다.
- 한 보상에서 여러 레벨이 올라도 최종 Level 기준으로 한 번만 표시하고, 누적 HP/ATK 증가량을 보여준다.
- 레벨업이 발생하면 `LevelUpOverlay`가 끝난 뒤 `VictoryPanel`을 표시해 두 UI가 동시에 보이지 않게 한다.
- 레벨업이 없으면 기존처럼 VictoryPanel을 바로 표시한다.

기존 단어 입력, 공격, EXP/Gold/Level 계산, Save/Load, Shop, HOME, Story, 10층 Slime King Story 흐름은 변경하지 않는다. 향후 Shop, 도감, 설정 UI도 같은 Dark Navy / Gold / Purple Jewel 팔레트를 확장 적용한다.

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

TitleScene 1차 리뉴얼 — 구현 완료:

- TitleScene은 `Assets/Art/UI/Title/wordtower_title_main.png`를 메인 배경 비주얼로 사용하는 9:16 모바일 타이틀 화면으로 리뉴얼됐다.
- 배경 이미지는 Scene에 합성된 버튼/로고가 아니라 순수 배경으로 사용하고, `WORD TOWER` 타이틀, 서브타이틀, 메뉴 버튼은 Unity UI로 분리한다.
- `TitleHeader` 아래 TMP_Text 기반 `WordTowerTitle`과 `Subtitle`을 배치한다. 향후 투명 PNG 로고가 확정되면 `WordTowerTitle` 영역만 교체할 수 있게 유지한다.
- `MainMenu` 아래 기존 `StartButton`을 가장 큰 Primary 버튼으로 유지한다. Save가 없으면 `게임 시작`, Save가 있으면 `이어하기`로 표시한다.
- `SecondaryMenu`에는 `StoryButton`, `CollectionButton`, `SettingsButton`을 둔다. STORY는 StoryScene 이동으로 연결됐고, 도감과 설정은 아직 placeholder로 Console log만 남긴다.
- 기존 종료 기능은 `QuitButton`으로 유지하되 메인 메뉴 우선순위를 방해하지 않는 작은 보조 버튼으로 배치한다.
- `VersionText`는 `Application.version` 기반으로 하단 중앙에 작게 표시한다.
- 배경 위 UI 가독성을 위해 `ReadabilityOverlay`를 사용한다. 배경 이미지 자체를 과도하게 가리지 않는다.
- `Assets/Scripts/Title/TitleManager.cs`가 Save 존재 확인, 시작 버튼 문구, BattleScene 이동, 종료와 TitleScene의 Android Back/Escape를 담당한다.
- TitleManager는 STORY 버튼의 StoryScene 이동과 도감/설정 placeholder 버튼 이벤트도 담당한다.
- 실제 Save Load는 기존처럼 BattleScene 진입 후 `BattleManager.LoadGame()`이 담당한다.
- `Assets/Scripts/Editor/TitleSceneBuilder.cs`는 `WordTower → Build Title Scene` 메뉴로 TitleScene만 독립 생성한다. BattleScene, BattleSceneBuilder, OpeningSceneBuilder, StudioSplashSceneBuilder를 호출하지 않는다.
- TitleManager는 Scene 참조가 비어 있어도 최소 Title UI를 런타임에 생성하는 fallback을 갖는다. TitleSceneBuilder로 재생성하면 정적 UI 참조를 연결해 사용한다.
- TitleScene의 `게임 시작` / `이어하기`는 BattleScene 이동이 정상 동작하는 구조를 유지한다.
- STORY는 StoryScene 이동으로 연결됐으며, StorySceneBuilder 실행 후 프롤로그 다시보기 UI와 연결된다. 도감 / 설정은 현재 placeholder다.
- BattleScene HOME/Back은 구현되어 있다. BattleSceneBuilder가 상단 `HomeButton`을 생성하고 BattleManager의 `ReturnToTitle()`이 현재 진행을 저장한 뒤 TitleScene으로 이동한다.
- BattleScene Back 우선순위는 Android 키보드 닫기 → Shop 닫기 → Victory 이동 차단 → 일반 상태 Save 후 TitleScene 이동이다. 한 입력에서는 한 단계만 처리한다.
- HOME/Back Scene 전환은 `isSceneTransitioning`으로 중복 실행을 막는다.
- VictoryPanel 활성 상태에서는 같은 층 보상 중복 획득을 막기 위해 HOME/Back 이동을 금지한다.
- 10층 Chapter Clear 후 TitleScene으로 나가는 흐름은 Chapter Clear 단계에서 별도 설계한다.

BattleScene 초기 화면 표시:

- 이어하기 진입 시 Scene에 저장된 1층 기본 Visual이 Android StreamingAssets 비동기 데이터와 Save 적용 전에 렌더링되어 약 0.2초 노출되는 플리커가 발견됐다.
- 이는 초록 슬라임뿐 아니라 FloorTitle, Level/EXP/Gold, HP, 기본 장비와 Monster Scale을 포함한 BattleCanvas 전체의 초기 Visual 문제다.
- BattleManager는 `Start()`에서 `FindUI()` 직후 첫 `yield` 전에 `BattleCanvas.enabled = false`로 렌더링을 숨긴다.
- Floor/Monster/Word/Item 로드, Save와 PlayerProgress 적용, 장비 외형, Monster Sprite/Scale, `SetupBattle()`과 Idle 재개가 모두 끝난 뒤 Canvas를 표시한다.
- 초기화 예외 시에도 `finally`에서 Canvas를 다시 표시해 영구 빈 화면을 방지한다.
- Scene과 BattleSceneBuilder는 이 해결을 위해 수정하거나 재실행하지 않는다.
- 회귀 검증은 5층 이상 Save, 장착 장비, 9/10층 Sprite/Scale, Save 없는 새 게임과 Android Release APK에서 수행한다.

BattleScene은 기존 전투, 저장, SHOP → ShopScene, HOME → TitleScene 복귀 구조를 유지한다. BattleSceneBuilder 기준으로 전투 씬에는 `BattleCanvas`, `ShopButton`, `HomeButton`, `AudioManager`가 포함되며 ShopPanel은 더 이상 생성하지 않는다.

Scene Builder 운영 원칙:

- StudioSplash / Opening / Title / Story / Shop / Battle Scene은 모두 Builder 기반으로 관리한다.
- Builder 코드가 변경된 경우 실제 Scene 반영을 위해 Unity Editor의 해당 `WordTower → Build ... Scene` 메뉴 실행이 필요할 수 있다.
- Scene YAML을 직접 수정하지 않는다.
- Hierarchy에만 수동 추가한 중요 UI는 Builder 재실행 시 사라질 수 있으므로 Builder와 런타임 참조를 함께 갱신한다.

Assets/Scripts/Editor/BattleSceneBuilder.cs가 전투 UI의 소스 오브 트루스다.

공식 BattleScene은 Assets/Scenes/BattleScene.unity 하나만 사용한다. 과거 중복으로 존재했던 Assets/BattleScene.unity와 meta는 삭제됐으며, 루트 BattleScene을 다시 만들거나 Build Settings에 추가하지 않는다.

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
- FloorDebugPanel
- DebugSaveResetButton

BattleSceneBuilder 수정 후 반드시 실행:

    WordTower → Build Battle Scene

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

현재 Android 개발 환경:

- Unity 6.5 (6000.5.8f1)
- Android Build Support / Android SDK & NDK Tools / OpenJDK 설치 완료
- Android Build Profile: Assets/Settings/Build Profiles/Android APK.asset
- Main Scene: Assets/Scenes/BattleScene.unity
- Orientation: Portrait
- Scripting Backend: IL2CPP
- Target Architecture: ARM64
- Minimum API Level: Android 8.0 / API 26
- Target API Level: Automatic
- 테스트용 Company Name: DavidCho
- 테스트용 Android Package Name: com.davidcho.wordtower

DavidCho와 com.davidcho.wordtower는 첫 실기기 테스트용 임시 값이다. 정식 출시 전 게임 스튜디오/브랜드명을 결정한 뒤 변경한다.

현재 확인:

- Windows Unity Editor에서 sqlite3 연결
- Editor에서 JSON spritePath 기반 이미지 교체
- Android 1차 APK에서 words.json 기반 단어 조회
- Android/Player에서 Resources 기반 몬스터/장비 Sprite 교체
- Android 1차 APK 빌드와 Galaxy 실기기 설치/실행 확인
- Android 일반 Release APK 빌드 완료
- Galaxy 실기기 최종 회귀 테스트와 실제 전투 플레이 정상 확인
- Android에서는 WordInput을 직접 선택할 때만 소프트 키보드를 연다. `TouchScreenKeyboard.area`의 Android 상단 원점 좌표를 Unity 화면 좌표로 변환한 뒤 `ScreenPointToLocalPointInRectangle()`으로 Canvas 부모 로컬 좌표를 구해 WordBattlePanel을 키보드 위로 이동하며, Canvas 상단을 넘지 않도록 제한한다.
- 정상 공격/Done/입력 취소/전투 상태 전환 시 키보드와 패널 위치를 복구하며, 잘못된 입력은 키보드와 올라간 패널을 유지해 바로 수정할 수 있게 한다.
- Galaxy + Samsung Keyboard 실기기에서 WordBattlePanel이 키보드 바로 위로 이동하고, 키보드 닫힘 시 원위치로 복귀하는 것을 확인했다.
- Development Build용 `WT KEYBOARD DEBUG` Overlay와 `[WT_KEYBOARD]` Logcat 진단 코드는 해결 확인 후 제거했다.
- Unity Editor C# compile error 해결, Console Error 0, Play Mode 정상
- Editor에서 Word DB 연결, Item/Floor 로드, 몬스터/Hero Sprite, 단어 판정, 공격, 다음 Floor 이동 정상 확인

Android TODO:

- 운영용 DB 단계에서 StreamingAssets DB를 Application.persistentDataPath로 복사
- Android SQLite 네이티브 설정 검증
- Resources 복제 Sprite를 Addressables 또는 직렬화 참조 구조로 개선

모바일 키보드 위치와 제조사별 키보드 영역은 Editor에서 완전히 검증할 수 없으므로 APK 실기기 확인을 Definition of Done에 포함한다.

### 17.1 Android 모바일 키보드 대응 인수인계

첫 Android APK 빌드와 Galaxy 실기기 설치/실행은 성공했다. 공식 Scene은 `Assets/Scenes/BattleScene.unity`이며, 모바일 키보드 대응 과정에서 Scene을 수정하거나 `BattleSceneBuilder`를 실행하지 않았다.

현재 Android 키보드 UX:

- 평상시 소프트 키보드는 닫혀 있다.
- 사용자가 WordInput을 직접 터치할 때만 키보드를 표시한다.
- 정상 공격 또는 Done/Enter 제출 후 입력 포커스를 해제하고 키보드를 닫는다.
- 다음 플레이어 턴에 키보드를 자동으로 다시 열지 않는다.
- 잘못된 입력에서는 키보드와 올라간 WordBattlePanel을 유지해 바로 수정할 수 있게 한다.
- 키보드 종료, 입력 취소, Shop, 다음 층, 전투 상태 전환 시 WordBattlePanel을 원래 `anchoredPosition`으로 복구한다.

Galaxy + Samsung Keyboard에서 최종 확인한 값:

    Screen = 1080x2340
    keyboard area = x:0, y:1314, width:1080, height:1026
    focused = True
    visible = True
    keyboardExists = True
    active = True
    status = Visible
    keyboardTopY = 1026
    panelCurrent.y ≈ 546.29

진단 결과 키보드 감지와 상태 전달은 정상이었다. 기존 코드는 `TouchScreenKeyboard.area.yMax`를 키보드 상단으로 잘못 해석했다. 위 실기기 값에서 `yMax`는 `1314 + 1026 = 2340`, 즉 화면 최상단이 되어 `panelCurrent.y`가 약 `1736`까지 증가했고 WordBattlePanel이 화면 위로 사라졌다.

작성 완료한 좌표 수정:

- Android 키보드 영역의 상단 원점 좌표를 `keyboardTopScreenY = Screen.height - keyboardArea.y`로 Unity의 하단 원점 화면 좌표로 변환한다.
- 위 실기기 값에서 `keyboardTopScreenY = 2340 - 1314 = 1026`이다.
- `RectTransformUtility.ScreenPointToLocalPointInRectangle()`으로 키보드 상단 Screen 좌표를 WordBattlePanel 부모 RectTransform의 로컬 좌표로 변환한다.
- 매 프레임 WordBattlePanel 원본 `anchoredPosition`과 원본 패널 하단을 기준으로 실제 겹치는 만큼만 이동해 누적 이동을 방지한다.
- 부모 Canvas RectTransform의 상단을 기준으로 최종 Y를 clamp하여 패널이 화면 상단을 넘지 않게 한다.
- 화면 픽셀 이동량을 `Canvas.scaleFactor`로 단순히 나누던 방식은 제거했다.

현재 상태:

- 키보드 좌표 수정은 Galaxy 실기기에서 해결 완료로 확인했다.
- Android 일반 Release APK 빌드와 Galaxy 최종 회귀 테스트를 완료했고 실제 전투 플레이도 정상 확인했다.
- WordInput과 AttackButton이 Samsung Keyboard 위에 정상 노출된다.
- 키보드를 닫으면 WordBattlePanel이 `(0,0)` 원위치로 정상 복귀한다.
- 다음 턴에 키보드가 자동으로 다시 열리지 않는다.
- 해결 확인 후 Development Build용 `WT KEYBOARD DEBUG` Overlay와 `[WT_KEYBOARD]` Logcat 진단 코드는 제거했다.

Android 1차 실기기 대응은 완료됐다. 이후 Android 작업은 운영용 DB/SQLite와 Resources 대체 구조처럼 장기 배포 구조 개선 단계에서 진행한다.

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
- 장착 Weapon/Armor의 Editor/Android Sprite 교체와 Load/Reset 외형 복원
- Android 1차 APK용 StreamingAssets JSON 데이터 로딩
- Android Build Profile과 Android Player Settings 1차 구성
- Android 소프트 키보드 기반 WordBattlePanel 자동 이동과 자동 재호출 방지 해결 완료(Galaxy + Samsung Keyboard 실기기 확인)
- 상단 PlayerName과 하단 LevelText의 PlayerProgressData 기반 레벨 표시 동기화
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
- StudioSplashScene 1차 구현과 매 실행 진입 흐름
- Opening Story 8컷, BGM, Audio Fade In/Out, Cross Fade, Zoom/Pan 연출
- Opening 최초 1회 자동 재생과 Debug Reset
- TitleScene 리뉴얼 Scene / Builder / TitleManager 메뉴 구조
- Title 배경 `wordtower_title_main.png` 기반 9:16 UI 구조
- Title STORY 버튼의 StoryScene 이동 코드
- StoryScene 1차 StoryMenuManager / StorySceneBuilder 소스
- Opening Story 다시보기용 `OpeningStoryManager.RequestReplay()`
- Title 도감 / 설정 placeholder 버튼
- StudioSplash → Opening/Title → Battle 전체 Scene Flow

---

## 19. 현재 미구현 / TODO

- 10층 클리어 후 존재하지 않는 11층 이동 방어와 챕터 완료 처리
- 보스 전용 패턴 및 isBoss 기반 전투 분기
- 인벤토리 UI
- 11층 이후 확정 콘텐츠
- StorySceneBuilder / StoryPlaybackSceneBuilder 실행과 Unity Scene 검증
- Title STORY → StoryScene → Prologue / Chapter 01 다시보기 실제 Scene 검증
- 20층 / 30층 / ... Story 해금 데이터 확장
- 도감 UI
- 설정 UI
- Studio Splash X/O 분리 애니메이션 고도화
- Title WORD TOWER 투명 PNG 로고 고도화
- Battle UI/연출 고도화
- 운영용 대규모 한국어 단어 DB
- 단어 뜻/도감/부적절 단어 관리
- 운영용 Android SQLite와 대규모 DB 로딩 구조
- Resources 복제 Sprite의 장기 로딩 구조 개선
- 튜토리얼
- Gold, Shop Buy, Equip, UI Button 실제 SFX
- 실제 BGM AudioClip과 BGM 재생 로직
- Settings/Volume UI와 Audio Mixer
- Chapter Clear Sequence, Illustration Unlock, Gallery
- Boss/Chapter Clear 전용 Jingle 또는 Music
- 프로덕션 UI 폴리시

---

## 20. 현재 권장 다음 작업

StudioSplash, Opening Story, TitleScene 리뉴얼, BattleScene HOME/Shop/전투 흐름, STORY 메뉴, 10층 Slime King Story Playback 소스까지 기본 구조가 갖춰졌다. 다음 시작점은 Unity Editor에서 StorySceneBuilder와 StoryPlaybackSceneBuilder를 실행하고 Title/Story/Battle 통합 흐름을 검증하는 작업이다.

권장 순서:

1. Unity Editor에서 `WordTower → Build Story Scene` 실행
2. Unity Editor에서 `WordTower → Build Story Playback Scene` 실행
3. Build Settings가 StudioSplash, Opening, Title, Story, StoryPlayback, Battle 순서인지 확인
4. Title → STORY → StoryScene → Prologue → Opening 다시보기 → Title 흐름 검증
5. 10층 Slime King 최초 클리어 → StoryPlayback → 사랑 1/10 → Victory 흐름 검증
6. StoryScene Chapter 01 replay와 보상 중복 없음 검증
7. SKIP / Android Back / BackButton 회귀 확인
8. 20층 / 30층 / ... Story 데이터 확장
9. 도감 UI
10. 설정 UI
11. Studio Splash X/O 분리 애니메이션 고도화
12. Title WORD TOWER 투명 PNG 로고 고도화
13. Battle UI/연출 고도화

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
