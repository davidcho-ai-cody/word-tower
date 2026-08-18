# AGENTS.md — WordTower Development Guide

> This file is the persistent project context for Codex and other coding agents working on **WordTower**.
> Read this file before making changes. Preserve existing working behavior unless the task explicitly asks to change it.

---

## 1. Project Overview

**Project name:** WordTower  
**Genre:** 2D mobile word-chain RPG  
**Core mechanic:** Korean 끝말잇기 (word-chain battle)  
**Target platform:** Mobile first  
**Screen orientation:** Portrait, 9:16  
**Engine:** Unity 2D  
**Primary language:** C#  
**Development style:** Production-first / vibe coding. The user can read C# reasonably well but does not want line-by-line programming lessons unless requested.

The game concept is:

- The player climbs a **100-floor demon tower**.
- Each floor contains a monster.
- Battles are resolved through Korean word-chain gameplay.
- Successful words trigger attacks and reduce HP.
- Monsters answer using words from the word database.
- Defeating monsters rewards EXP and Gold.
- Gold will later be spent in shops for weapons and armor.
- Equipment should visibly change the hero’s appearance.
- The game should initially work without AI/API calls to avoid token/API cost and simplify launch.
- AI features may be added later if the game gets traction.

The current MVP focuses on making the first few floors fully playable before scaling toward 100 floors.

---

## 2. Core Game Philosophy

WordTower should feel like a **real RPG whose combat language happens to be 끝말잇기**, not merely a vocabulary quiz.

Important design values:

1. **Fast and satisfying combat**
   - Player attack animation
   - Weapon swing
   - Hit effects
   - Knockback
   - Damage numbers
   - Critical effects
   - Monster counterattacks
   - Death animation

2. **Visible progression**
   - EXP
   - Level
   - Gold
   - Weapons
   - Armor
   - Stronger monsters
   - Increasing word difficulty

3. **Words should matter mechanically**
   - Valid words are checked against SQLite.
   - Used words cannot be reused.
   - Monster vocabulary difficulty depends on monster data.
   - “One-shot words” are rewarded as critical attacks rather than instant wins.

4. **Do not trivialize the RPG system**
   - One-shot words must NOT instantly defeat monsters.
   - Otherwise children could memorize a few one-shot words and bypass HP, gear, progression, and combat systems.

---

## 3. Current Working Gameplay Loop

Current intended gameplay loop:

1. Floor data loads from JSON.
2. Monster data loads from JSON.
3. Current monster image, name, HP, attack, reward, and word difficulty are applied.
4. Battle starts with the starting word `"사과"`.
5. Player enters a Korean word.
6. System validates:
   - correct starting character
   - word exists in SQLite DB
   - word is active
   - word was not already used
7. System checks whether the player word is a one-shot word.
8. Player attacks.
9. If normal word:
   - normal damage
   - normal slash effect
   - monster selects a valid word from DB
   - monster counterattacks
10. If one-shot word:
   - critical damage x2
   - critical slash effect
   - `CRITICAL!` text effect
   - monster does NOT counterattack
   - DB selects a new random starting word
   - battle continues
11. If monster HP reaches 0:
   - monster death animation
   - Victory panel
   - EXP and Gold reward
12. Player presses “다음 층”.
13. `currentFloor++`
14. Next floor JSON data is loaded.
15. Next battle begins.

---

## 4. Current Combat Values

Current baseline values:

### Player
- Max HP: `100`
- Base Attack: `20`

### Normal attack
- Damage: `20`

### One-shot / Critical attack
- Damage multiplier: `2x`
- Current critical damage: `40`

### Green Slime
- EXP reward: `20`
- Gold reward: `10`
- Current word level range: typically Lv.1 only

### Blue Slime
- EXP reward: `30`
- Gold reward: `15`
- Current word range: Lv.1–2

Current floor plan in the prototype:

- Floor 1: Green Slime
- Floor 2: Green Slime
- Floor 3: Blue Slime
- Floor 4 was discussed as likely Blue Slime for early progression testing

With current rewards:

- Floor 1: +20 EXP
- Floor 2: +20 EXP
- Floor 3: +30 EXP
- Floor 4: +30 EXP
- Total = 100 EXP

Therefore the first level-up is naturally expected around Floor 4 if Lv.1 → Lv.2 requires 100 EXP.

---

## 5. Planned Level-Up Direction

Level-up logic is NOT fully implemented yet.

Recommended initial rules:

- Lv.1 → Lv.2: 100 EXP
- Later required EXP should increase gradually.
- Suggested early curve:
  - Lv.1 → Lv.2: 100
  - Lv.2 → Lv.3: 140
  - Lv.3 → Lv.4: 190
  - Lv.4 → Lv.5: 250

Suggested initial level-up bonuses:

- Max HP +10
- Attack +2
- Overflow EXP should carry over
- Display a `LEVEL UP!` effect

Do not hardcode long-term progression prematurely. Start with a simple, testable formula.

---

## 6. One-Shot Word / Critical System

This design decision is IMPORTANT.

A one-shot word is a valid word whose final character has no valid continuation available to the current monster under its allowed difficulty range.

Example:

- Monster: `오리`
- Player: `리튬`
- Monster would need a word starting with `튬`
- If none exists, it is a one-shot word

### Final design decision

One-shot words do **NOT** cause instant victory.

Instead:

- Deal critical damage: base attack x2
- Show critical slash effect
- Show `CRITICAL!`
- Monster counterattack is skipped
- A new starting word is selected from DB
- Battle continues

Reason:
- Prevent abuse by repeatedly memorizing elemental-symbol style one-shot words
- Preserve HP, weapons, armor, progression, and RPG balance
- Still reward player knowledge strongly

Current implemented behavior:
- `isCriticalAttack`
- `currentAttackDamage`
- critical = `playerAttack * 2`
- new word is selected with `GetRandomStartWord(...)`

---

## 7. Word Engine Architecture

The word system is the core engine.

Architecture:

```text
BattleManager
    ↓
WordService
    ↓
SQLite.cs / sqlite-net
    ↓
words.db
```

### SQLite database location

Source database:

```text
Assets/StreamingAssets/Data/Words/words.db
```

### DB generation tool

```text
Tools/create_word_db.py
```

Current development workflow recreates the test DB from seed words in the Python script.

When the script prints:

```text
Inserted words: 32
```

that means the DB was rebuilt and contains 32 total words — not 32 additional words.

Run:

```powershell
cd D:\Projects\WordTower
python .\Tools\create_word_db.py
```

---

## 8. Word Database Schema

Current `words` table concept:

```sql
CREATE TABLE words (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    word TEXT NOT NULL UNIQUE,
    first_char TEXT NOT NULL,
    last_char TEXT NOT NULL,
    meaning TEXT,
    level INTEGER NOT NULL DEFAULT 1,
    category TEXT,
    is_active INTEGER NOT NULL DEFAULT 1
);
```

Indexes currently planned/used:

```sql
CREATE INDEX idx_words_first_char
ON words(first_char);

CREATE INDEX idx_words_level
ON words(level);
```

### WordData mapping

`WordData.cs` must map to the actual SQLite table:

```csharp
[Table("words")]
public class WordData
```

This is required because sqlite-net otherwise tries to query a table named `WordData`.

---

## 9. WordService Responsibilities

`WordService` currently handles / should handle:

- DB initialization
- DB close
- Check if a word is valid
- Retrieve word information
- Select monster word
- Respect monster word difficulty
- Exclude already-used words
- Detect one-shot words
- Select a random restart word after a one-shot

Important methods include conceptually:

```csharp
IsValidWord(string word)

GetWord(string word)

GetMonsterWord(
    string startChar,
    int minLevel,
    int maxLevel,
    HashSet<string> usedWords
)

IsOneShotWord(
    string word,
    int minLevel,
    int maxLevel,
    HashSet<string> usedWords
)

GetRandomStartWord(
    int minLevel,
    int maxLevel,
    HashSet<string> usedWords
)
```

Monster word selection MUST filter:

```text
first_char == requiredChar
level >= minLevel
level <= maxLevel
is_active == 1
not already used
```

Do not remove difficulty filtering unless explicitly requested.

---

## 10. Used Word Rules

Each battle tracks:

```csharp
HashSet<string> usedWords
```

Rules:

- Starting word `"사과"` is added immediately.
- Player words are added after validation.
- Monster words are added after selection.
- Used words cannot be reused.
- When advancing to a new floor, `usedWords` is reset.
- New floor starts again with `"사과"` for now.

This may later be changed to randomized floor start words, but currently preserve this behavior unless asked.

---

## 11. Test Word Chain Examples

Current small test DB has been expanded specifically to verify chaining.

Known test flow:

```text
사과
→ 과자
→ 자동차
→ 차표
→ 표지
→ 지갑
```

At one point `갑` had no continuation, which was intentionally useful for testing one-shot logic.

Some words added during testing include:

- 표지
- 지갑

There are also early seed words such as:

- 사과
- 과자
- 자동차
- 차표
- 표범
- 범인
- 인사
- 사슴
- 슴새
- 새우
- 우산
- 산책
- 책상
- 상자
- 자전거
- 거울
- 울음
- 음악
- 악기
- 기차
- 학교
- 교실
- 실내
- 내일
- 일기
- 바다
- 다리
- 리본
- 본능
- 능력

Do NOT treat the current 30–40 word DB as production vocabulary. It exists only to validate engine behavior.

Long term, the word list may grow toward tens of thousands or more, likely generated from structured external word data into SQLite rather than hardcoded Python lists.

---

## 12. Monster / Floor Data Architecture

Game content is data-driven.

### Folder structure

```text
Assets/Data/
├─ Monsters/
│  └─ monsters.json
└─ Floors/
   └─ floors.json
```

### MonsterData fields

Conceptually:

```text
id
name
maxHp
attack
expReward
goldReward
wordLevelMin
wordLevelMax
spritePath
```

### FloorData fields

Conceptually:

```text
floor
monsterId
title
isBoss
```

### Relationship

```text
FloorData
   ↓ monsterId
MonsterData
   ↓
HP / Attack / Reward / Word Level / Sprite
```

Do not hardcode monster stats inside BattleManager if the value belongs in `monsters.json`.

Do not hardcode floor-specific monster assignments inside BattleManager if they belong in `floors.json`.

---

## 13. Current Monster Art Organization

Preferred folder rule:

**Folder = monster species/type**  
**Filename = variation**

Example:

```text
Assets/Art/Sprites/Monsters/
└─ Slime/
   ├─ slime_green_idle_01.png
   └─ slime_blue_idle_01.png
```

Future examples:

```text
Monsters/
├─ Slime/
│  ├─ slime_green_idle_01.png
│  ├─ slime_blue_idle_01.png
│  └─ slime_king_idle_01.png
├─ Goblin/
├─ Skeleton/
└─ Orc/
```

Avoid creating a separate folder for every color variant such as `SlimeBlue` unless there is a strong reason.

---

## 14. Current Hero Art Structure

Hero initially used a layered concept:

```text
PlayerPlaceholder
├─ Body
├─ Hair
├─ Face
├─ Armor
├─ Weapon
└─ Accessory
```

However, AI-generated armor overlays did not align pixel-perfectly with the base body.

### Final practical decision

Use:

- Full-character sprite for body/armor state
- Separate Weapon layer
- Separate Accessory layer where useful

Current hero assets:

```text
Assets/Art/Sprites/Hero/Body/
├─ hero_body_base.png
└─ hero_beginner_01.png
```

Current weapon:

```text
Assets/Art/Sprites/Hero/Weapon/
└─ weapon_wood_sword_01.png
```

The hero faces slightly toward the monster on the right, NOT straight toward the camera.

This pose is important and should remain consistent in future character art.

---

## 15. Hero Equipment Design Direction

The game should visually reward equipment purchases.

Planned progression:

```text
Base / beginner
→ leather gear
→ iron gear
→ knight gear
→ magic gear
→ legendary gear
```

Current approach:

- Armor/body state = full hero sprite swap
- Weapon = separate sprite layer
- Accessory = separate layer

Example future equipment logic:

```text
Character sprite:
hero_beginner_01
→ hero_iron_01
→ hero_knight_01

Weapon sprite:
wood sword
→ iron sword
→ steel sword
→ magic sword
→ legendary sword
```

Do not return to AI-generated body-part overlays unless there is a reliable pixel-perfect production pipeline.

---

## 16. Current Weapon Position

Current wooden sword visual alignment was manually tuned.

Known reference values:

```text
Pos X: 110
Pos Y: 5
Width: 150
Height: 150
Rotation: 0
```

Treat this as the current weapon placement baseline.

Future weapon assets should preferably be created to fit this placement rather than requiring different transforms for every weapon.

---

## 17. Combat Animation System

Existing combat animations should NOT be removed casually.

### Player attack

Current behavior:

1. Player moves toward monster
2. Weapon swings
3. Impact effect appears
4. Damage is applied
5. Monster knockback / shake
6. Player returns to original position
7. If monster survives, monster turn begins

Player attack movement distance was tuned to:

```text
+110f X
```

This was visually tested and considered good.

### Weapon swing

Current approximate swing:

```text
20 degrees
→ -55 degrees
```

### Monster attack

Monster moves left toward player, attacks, then returns.

Current approximate rush:

```text
-85f X
```

### Hit knockback

- Monster is knocked to the right.
- Player is knocked to the left.

Direction is determined from target.

---

## 18. Monster Death Animation

Current monster death behavior:

1. Small upward jump
2. Rotate
3. Shrink
4. Drift slightly downward/right
5. Scale reaches zero
6. Victory handling begins

This effect was visually tested and liked.

Do not replace it unless asked.

---

## 19. Damage Text

Damage text already exists.

Behavior:

- Appears above target
- Shows negative damage such as `-20`, `-40`
- Floats upward
- Fades out

Do not create another duplicate damage-number system.

---

## 20. Normal Hit Effect

Current file:

```text
Assets/Art/Sprites/Effects/Combat/impact_slash_01.png
```

Current approximate UI size:

```text
300 x 220
```

The first effect generated was too flashy for a wooden sword, so the normal effect was intentionally simplified.

Normal hit effect should feel appropriate for a low-level wooden sword.

---

## 21. Critical Hit Effect

Current file:

```text
Assets/Art/Sprites/Effects/Combat/impact_slash_critical_01.png
```

The earlier “too flashy” hit artwork was repurposed as the critical effect.

Current behavior:

- Critical visual is separate from normal impact
- Bigger / brighter effect
- Critical effect is only used for one-shot-word critical attacks

Builder creates:

```text
CriticalImpactEffect
```

BattleManager selects:

```text
normal → ImpactEffect
critical → CriticalImpactEffect
```

Do not show both simultaneously unless intentionally redesigning the effect.

---

## 22. CRITICAL Text Effect

Current Builder creates:

```text
CriticalText
```

Value:

```text
CRITICAL!
```

Behavior:

1. Initially hidden
2. Appears on critical
3. Pops from smaller scale
4. Enlarges
5. Moves upward
6. Fades out
7. Returns to hidden state

Important workflow reminder:

> If `BattleSceneBuilder.cs` is changed to create a new UI object, the user must run:
>
> `WordTower → Build Battle Scene`
>
> before expecting the new object to exist in Hierarchy.

This has already caused confusion once. Always check this first when newly-added Builder UI does not appear.

---

## 23. BattleSceneBuilder Is the Scene Source of Truth

This is one of the most important project rules.

`BattleSceneBuilder.cs` automatically constructs much of the battle scene.

Do NOT manually add important persistent UI objects only in Hierarchy if they are expected to survive scene rebuilds.

Instead:

1. Add creation logic to `BattleSceneBuilder.cs`
2. Build via:
   ```text
   WordTower → Build Battle Scene
   ```
3. BattleManager finds and controls the generated object

Examples already managed by Builder:

- BattleCanvas
- PlayerPlaceholder
- Hero layers
- Monster
- HP UI
- Word battle UI
- Status panel
- Victory panel
- ImpactEffect
- CriticalImpactEffect
- CriticalText
- Hero sprite
- Wooden sword sprite

If something manually added disappears after Build, that is expected.

---

## 24. Important Builder Cleanup Note

Current `BattleSceneBuilder.cs` had duplicate early calls observed in a prior version:

```csharp
ClearScene();
CreateEventSystem();

ClearScene();
CreateEventSystem();
```

If this duplication still exists in the working branch, clean it carefully when convenient, but only after verifying it does not affect behavior.

Do not perform broad refactoring while implementing unrelated gameplay features.

---

## 25. Victory / Floor Progression

Victory system is already implemented.

When monster dies:

- battle ends
- EXP reward is added
- Gold reward is added
- Victory panel appears
- reward text appears
- next-floor button is enabled

Next Floor:

```text
currentFloor++
→ LoadFloorAndMonsterData()
→ ResetBattleForNextFloor()
```

On new floor:

- monster HP resets
- player HP currently fully restores for MVP
- current word resets to `"사과"`
- usedWords resets
- dead monster scale/rotation/position restores
- UI updates
- input re-enables

Do NOT suggest implementing these again unless checking or extending them.

---

## 26. Current Floor Data Testing

The prototype successfully tested:

```text
Floor 1 → Green Slime
Floor 2 → Green Slime
Floor 3 → Blue Slime
```

Blue Slime successfully loaded:

- different name
- different HP
- different attack
- different EXP / Gold
- different image
- different word difficulty range

This validated data-driven monster switching.

---

## 27. Monster Sprite Loading

Current prototype uses a JSON `spritePath` and loads assets in the Unity Editor.

This works for editor testing.

However, code using:

```csharp
UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>()
```

will NOT work in final Android runtime.

This is a known future task.

Possible later solutions:

- Resources
- Addressables
- serialized references / ScriptableObjects

Do not prematurely rewrite this unless the task concerns mobile build/runtime asset loading.

---

## 28. Sprite Import Automation

A Sprite importer automation script was created/planned:

```text
Assets/Scripts/Editor/SpriteImportProcessor.cs
```

Target folder:

```text
Assets/Art/Sprites/
```

Desired automatic import defaults:

- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Pixels Per Unit: 100
- Alpha Is Transparency: enabled
- Wrap Mode: Clamp
- Compression: None

Filter Mode was discussed:
- Point can be used for crisp sprites
- Bilinear may look better because the current art is smooth SD illustration rather than true pixel art

If visuals become jagged, prefer Bilinear.

---

## 29. Font Setup

Old font:

```text
NotoSansKR-VF SDF
```

This variable-font-based setup produced missing Korean glyph boxes for characters such as `슴`.

It was replaced.

### Current preferred font

Source:

```text
NotoSansKR-Regular.otf
```

TMP font asset:

```text
NotoSansKR-Regular SDF.asset
```

Recommended/current settings:

- Atlas Population Mode: Dynamic
- Multi Atlas Textures: ON
- Atlas: 2048 x 2048
- Source Font File: NotoSansKR-Regular
- Regular static font, not variable font

`BattleSceneBuilder.cs` should load:

```text
Assets/Fonts/NotoSansKR-Regular SDF.asset
```

Font issue was tested and considered resolved.

Do not revert to `NotoSansKR-VF SDF`.

---

## 30. SQLite Integration

Library source:

```text
Assets/Scripts/Database/SQLite.cs
```

Downloaded from official `praeclarum/sqlite-net`.

Native Windows DLL:

```text
Assets/Plugins/x86_64/sqlite3.dll
```

Unity Plugin settings were configured for:

- Editor
- Standalone
- Windows x64

This fixed:

```text
DllNotFoundException: sqlite3
```

Successful log:

```text
Word DB Connected : .../Assets/StreamingAssets/Data/Words/words.db
```

Current integration is confirmed working in the Windows Unity Editor.

---

## 31. Mobile SQLite Caveat

Current Windows Editor DB connection works directly from StreamingAssets.

Final Android behavior needs additional handling.

On Android:
- StreamingAssets may be inside APK/JAR
- Direct filesystem SQLite connection may not work the same way
- Database should likely be copied to `Application.persistentDataPath` before use
- Android native SQLite library setup must be verified

This is a future deployment task.

Do not assume the current Windows `sqlite3.dll` setup is Android-ready.

---

## 32. Current Project Structure

Approximate important structure:

```text
Assets/
├─ Art/
│  └─ Sprites/
│     ├─ Hero/
│     │  ├─ Body/
│     │  ├─ Hair/
│     │  ├─ Face/
│     │  ├─ Armor/
│     │  ├─ Weapon/
│     │  └─ Accessory/
│     ├─ Monsters/
│     │  └─ Slime/
│     └─ Effects/
│        └─ Combat/
├─ Animations/
├─ Data/
│  ├─ Floors/
│  └─ Monsters/
├─ Fonts/
├─ Plugins/
│  └─ x86_64/
├─ Prefabs/
├─ Scenes/
├─ Scripts/
│  ├─ Data/
│  ├─ Database/
│  └─ Editor/
└─ StreamingAssets/
   └─ Data/
      └─ Words/

Tools/
└─ create_word_db.py
```

Main scene:

```text
Assets/Scenes/BattleScene.unity
```

---

## 33. Key Code Files

### BattleManager.cs

Responsible for:

- input validation
- HP
- player attack
- monster turn
- word chaining
- DB validation
- used word tracking
- one-shot detection
- critical damage
- hit effects
- damage numbers
- monster death
- victory
- floor progression
- monster data application
- sprite switching

This file is already large (well over 1000 lines).

### BattleSceneBuilder.cs

Responsible for generating battle UI and battle scene objects.

### WordService.cs

Responsible for SQLite word queries.

### WordData.cs

Maps SQLite `words` table.

### MonsterData.cs

Monster JSON model.

### FloorData.cs

Floor JSON model.

---

## 34. Refactoring Policy

BattleManager is large, but do NOT aggressively refactor simply because it is large.

Current priority is getting the game playable.

Refactoring should happen incrementally and only when there is a clear benefit.

Possible future extraction candidates:

```text
BattleManager
├─ BattleFlowController
├─ WordBattleService
├─ CombatAnimationController
├─ RewardService
├─ FloorManager
└─ PlayerProgressionService
```

Do not split everything at once.

When refactoring:

- preserve current animation timings
- preserve JSON behavior
- preserve DB behavior
- preserve object names relied upon by `GameObject.Find`
- test before and after

---

## 35. Git / Multi-PC Workflow

The project is developed on both company and home PCs.

Repository:

```text
https://github.com/davidcho-ai-cody/word-tower.git
```

Typical local path:

```text
D:\Projects\WordTower
```

Normal workflow:

### Before starting on another PC

```powershell
cd D:\Projects\WordTower
git status
git pull
```

### Save work

```powershell
git status
git add .
git commit -m "Meaningful commit message"
git push
```

### Verify

```powershell
git status
```

Expected:

```text
On branch main
Your branch is up to date with 'origin/main'.

nothing to commit, working tree clean
```

Unity `.meta` files MUST be committed with assets.

Do not ignore `.meta`.

---

## 36. Known Git Issue: Font Asset Local Changes

Unity may modify TMP font `.asset` files locally.

This previously blocked pull with:

```text
Your local changes would be overwritten by merge:
Assets/Fonts/NotoSansKR-VF SDF.asset
```

If the local change is disposable:

```powershell
git restore "Assets/Fonts/<font asset>.asset"
git pull
```

Always inspect `git status` before discarding meaningful changes.

---

## 37. Unity Build Scene Workflow

When working with `BattleSceneBuilder.cs`:

1. Modify builder code
2. Save
3. Return to Unity
4. Wait for compile
5. Confirm zero compile errors
6. Run:
   ```text
   WordTower → Build Battle Scene
   ```
7. Test in Play Mode

If a newly created Builder object does not appear, FIRST ask:
> “Did we run Build Battle Scene after modifying the Builder?”

Do not immediately debug the animation code before checking this.

---

## 38. User Development Preferences

Important collaboration preferences:

- Focus on **building**, not teaching programming theory.
- The user can read C# and follow file/position instructions.
- Give:
  - exact file path
  - exact method/location
  - code block
  - short explanation
- Avoid explaining every C# syntax detail unless asked.
- Use PowerShell commands, not CMD.
- Prefer step-by-step checkpoints.
- Push to Git at meaningful milestones.
- The user likes to test visually after small changes.
- Collaborate on design decisions rather than blindly coding.
- The user calls the assistant “코디”.
- The development style is intentionally “바이브코딩”.

When unsure about current source, inspect the actual file before proposing code instead of guessing.

---

## 39. Do Not Re-Implement Existing Features

Before suggesting a “next feature”, verify whether it already exists.

Already implemented / tested:

- HP reduction
- Hero attack animation
- Weapon swing
- Normal hit VFX
- Critical VFX
- CRITICAL text
- Damage numbers
- Monster knockback
- Player knockback
- Monster attack motion
- Monster death animation
- Victory panel
- EXP reward
- Gold reward
- Next floor
- JSON floor loading
- JSON monster loading
- Green → Blue slime swapping
- SQLite connection
- Word DB validation
- Used word rejection
- Monster DB word selection
- Monster word difficulty filtering
- One-shot detection
- One-shot critical x2 damage
- Monster counterattack skip on one-shot
- Random restart word after one-shot
- Korean font fix

Currently NOT confirmed complete:

- player level-up logic
- EXP requirement progression
- persistent save/load
- shop system
- equipment purchase
- armor/body progression
- item data implementation
- full 100-floor content
- large production word DB
- Android SQLite deployment
- runtime-safe monster asset loading for Android
- boss mechanics
- title/menu screen
- audio/BGM/SFX
- production UI polish

---

## 40. Current Recommended Next Feature

The next logical feature is:

# Player Level-Up System

Suggested first implementation:

- `playerLevel`
- `currentExp`
- `requiredExp`
- first requirement = 100
- level-up when currentExp >= requiredExp
- carry overflow EXP
- Max HP +10
- Attack +2
- display `LEVEL UP!`
- update `LV.1` labels
- gradually increase required EXP

Potential formula should remain simple initially.

Example:

```text
Lv.1 → 2 = 100
Lv.2 → 3 = 140
Lv.3 → 4 = 190
Lv.4 → 5 = 250
```

Before implementing, inspect current `BattleManager.cs` and `BattleSceneBuilder.cs` because they may have changed since this file was written.

---

## 41. Future Systems Roadmap

Suggested broad progression:

### Phase 1 — Core Battle MVP
- [x] Word battle
- [x] HP combat
- [x] word DB
- [x] monster DB AI
- [x] battle animations
- [x] critical one-shot words
- [x] rewards
- [x] floor progression
- [ ] level up

### Phase 2 — RPG Growth
- [ ] player stat model
- [ ] level-up
- [ ] item JSON
- [ ] inventory
- [ ] shop
- [ ] weapon upgrades
- [ ] armor upgrades
- [ ] visible equipment changes

### Phase 3 — Tower Content
- [ ] floors 1–10
- [ ] more monster types
- [ ] floor 10 boss
- [ ] difficulty balancing
- [ ] boss word mechanics

### Phase 4 — Vocabulary Expansion
- [ ] large Korean word source
- [ ] meanings
- [ ] categories
- [ ] level scoring
- [ ] blocked/inappropriate words
- [ ] one-shot-word balance
- [ ] word encyclopedia

### Phase 5 — Productization
- [ ] persistent save
- [ ] Android build
- [ ] mobile SQLite bootstrap
- [ ] audio
- [ ] title scene
- [ ] tutorial
- [ ] UI polish
- [ ] app store readiness

---

## 42. Gameplay Balance Notes

Current early-game balance is intentionally lightweight.

Do not over-balance before the loop is fun.

Important observations:

- First level-up around Floor 4 feels acceptable for early reward.
- One-shot words should feel powerful but not bypass the whole game.
- Monster word levels should rise with floors.
- Player should be free to enter higher-level words; difficulty limits mainly affect monster vocabulary.
- Later, player word difficulty may provide bonuses:
  - Lv.1 word → normal damage
  - Lv.2 → +10%
  - Lv.3 → +20%
  - Lv.4 → critical chance
  - Lv.5 → special effect
- This is an idea, not currently implemented.

---

## 43. Word Content Design Notes

The final vocabulary DB must not simply contain “many words”.

It must also support good chain connectivity.

For each `first_char`, analyze:

- number of candidate words
- difficulty distribution
- whether the chain leads to dead ends
- possible abuse of rare one-shot characters
- appropriateness for children
- whether technical/scientific/element names are allowed

Potential problematic endings include rare Korean syllables such as:

```text
튬
늄
슘
릇
쁨
```

Do not automatically ban them. They are part of the one-shot-word mechanic, but balance may later require rules.

---

## 44. Coding Agent Working Rules

When Codex receives a task:

1. Read this `AGENTS.md`.
2. Inspect relevant existing files before editing.
3. Do not guess current method names.
4. Prefer minimal, targeted changes.
5. Preserve all working combat effects.
6. Keep comments readable in Korean where the project already uses Korean comments.
7. Do not rename public scene object names casually.
8. Do not remove Builder-generated objects without updating both Builder and BattleManager.
9. Do not introduce a framework/library unless clearly needed.
10. Do not rewrite SQLite architecture without discussing Android implications.
11. Do not use AI/API calls for game runtime unless explicitly requested.
12. After code changes, explain:
    - files changed
    - behavior changed
    - how to test
13. If Builder changed, explicitly remind:
    ```text
    WordTower → Build Battle Scene
    ```
14. If changing assets, preserve `.meta`.
15. Avoid destructive Git operations unless explicitly approved.

---

## 45. Definition of Done for Small Changes

A feature is not considered done just because it compiles.

For gameplay changes, verify:

- zero compile errors
- no runtime exceptions
- visual behavior works
- battle can continue afterward
- next turn works
- victory still works
- next floor still works
- DB still connects
- no missing Korean glyphs
- Builder rebuild does not remove required behavior

For data changes, verify:

- JSON parses
- correct monster/floor loads
- DB regenerates if needed
- IDs and sprite paths match
- no duplicate word problems

---

## 46. Final Reminder

This project is being built iteratively with a strong emphasis on **fun, visible progress, and low-friction development**.

Do not sacrifice working gameplay for “perfect architecture”.

Preferred order:

```text
Make it work
→ Test it
→ Make it fun
→ Commit it
→ Refactor only when useful
```

The immediate project state is already beyond a static prototype: it has working SQLite-driven word battles, data-driven monsters/floors, combat animations, critical one-shot words, rewards, and floor progression.

The next major milestone is **player level-up and RPG progression**.
