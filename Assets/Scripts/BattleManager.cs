using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BattleManager : MonoBehaviour
{
    [Header("Player")]
    public int playerHp = 100;

    private int currentAttackDamage;
    private bool isCriticalAttack;

    [Header("Slime")]
    public int slimeMaxHp = 100;
    public int slimeHp = 100;
    public int slimeAttack = 10;

    [Header("Reward")]
    public int slimeExpReward = 20;
    public int slimeGoldReward = 10;

    private PlayerProgressService playerProgress;
    private SaveService saveService;

    private int playerLevel => playerProgress.PlayerLevel;
    private int exp => playerProgress.Exp;
    private int requiredExp => playerProgress.RequiredExp;
    private int gold => playerProgress.Gold;
    private int playerMaxHp => playerProgress.PlayerMaxHp;
    private int playerAttack => playerProgress.PlayerAttack;

    private TMP_Text playerHpText;
    private TMP_Text slimeHpText;
    private TMP_Text enemyWordText;
    private TMP_Text chainHintText;
    private TMP_Text levelText;
    private TMP_Text expText;
    private TMP_Text goldText;
    private TMP_FontAsset koreanFont;

    private TMP_Text floorTitleText;
    private TMP_Text monsterNameText;

    private TMP_InputField wordInput;
    private Button attackButton;

    private Image playerHpFill;
    private Image slimeHpFill;
    private float playerHpFullWidth;
    private float slimeHpFullWidth;

    private RectTransform playerVisual;
    private RectTransform slimeVisual;

    // =========================
    // 공격 / 타격 연출
    // =========================
    private RectTransform weaponVisual;
    private TMP_Text criticalText;
    private TMP_Text levelUpText;

    // 슬라임이 맞는 순간 표시할 타격 이펙트
    private RectTransform impactEffect;
    private GameObject criticalImpactEffect;

    private string currentWord = "사과";
    private bool battleEnded = false;

    // =========================
    // 현재 층 / 몬스터 데이터
    // =========================
    private int currentFloor = 1;
    private int highestFloor = 1;

    private FloorData currentFloorData;
    private MonsterData currentMonsterData;

    // =========================
    // 승리 UI
    // =========================
    private GameObject victoryPanel;
    private TMP_Text victoryMonsterText;
    private TMP_Text victoryRewardText;
    private Button nextFloorButton;
    private Vector2 slimeOriginalPosition;

    // =========================
    // 현재 몬스터 이미지
    // =========================
    private Image monsterImage;

    private WordService wordService;
    // 이번 전투에서 이미 사용한 단어
    private HashSet<string> usedWords = new HashSet<string>();

    private GameObject floorDebugPanel;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private TMP_Text debugFloorText;
    private Button debugPreviousFloorButton;
    private Button debugNextFloorButton;
    private Button debugFloorTenButton;
    private Button debugSaveResetButton;
    private Vector2 debugPlayerOriginalPosition;
    private Quaternion debugWeaponOriginalRotation;
#endif

    void Start()
    {
        FindUI();

        playerProgress = new PlayerProgressService();
        saveService = new SaveService();

        Debug.Log("Save Path: " + saveService.GetSavePath());
        LoadGame();

        // 단어 DB 연결
        wordService = new WordService();
        wordService.Initialize();

        // 현재 층 데이터와 몬스터 데이터 로드
        LoadFloorAndMonsterData();

        // 로드한 데이터 기준으로 전투 시작
        SetupBattle();
    }

    void FindUI()
    {
        playerHpText = GameObject.Find("PlayerHP/HPText")?.GetComponent<TMP_Text>();
        slimeHpText = GameObject.Find("MonsterHP/HPText")?.GetComponent<TMP_Text>();

        enemyWordText = GameObject.Find("EnemyWord")?.GetComponent<TMP_Text>();
        chainHintText = GameObject.Find("ChainHint")?.GetComponent<TMP_Text>();

        levelText = GameObject.Find("LevelText")?.GetComponent<TMP_Text>();
        expText = GameObject.Find("ExpText")?.GetComponent<TMP_Text>();
        goldText = GameObject.Find("GoldText")?.GetComponent<TMP_Text>();

        wordInput = GameObject.Find("WordInput")?.GetComponent<TMP_InputField>();
        attackButton = GameObject.Find("AttackButton")?.GetComponent<Button>();

        playerHpFill = GameObject.Find("PlayerHP/Fill")?.GetComponent<Image>();
        slimeHpFill = GameObject.Find("MonsterHP/Fill")?.GetComponent<Image>();

        playerVisual = GameObject.Find("PlayerPlaceholder")?.GetComponent<RectTransform>();
        slimeVisual = GameObject.Find("SlimePlaceholder")?.GetComponent<RectTransform>();

        // 현재 몬스터를 표시하는 UI Image
        if (slimeVisual != null)
        {
            monsterImage = slimeVisual.GetComponent<Image>();
        }

        floorTitleText = GameObject.Find("FloorTitle")?.GetComponent<TMP_Text>();
        monsterNameText = GameObject.Find("MonsterName")?.GetComponent<TMP_Text>();

        // 용사의 무기 레이어
        weaponVisual = GameObject.Find("Weapon")?.GetComponent<RectTransform>();

        // ImpactEffect는 시작 시 비활성화 상태이므로
        // GameObject.Find() 대신 BattleCanvas 하위에서 직접 검색한다.
        Transform battleCanvas = GameObject.Find("BattleCanvas")?.transform;

        if (battleCanvas != null)
        {
            Transform impactTransform = battleCanvas.Find("ImpactEffect");

            if (impactTransform != null)
            {
                impactEffect =
                    impactTransform.GetComponent<RectTransform>();
            }
        }

        if (battleCanvas != null)
        {
            Transform criticalImpactTransform =
                battleCanvas.Find("CriticalImpactEffect");

            if (criticalImpactTransform != null)
            {
                criticalImpactEffect = criticalImpactTransform.gameObject;
                criticalImpactEffect.SetActive(false);
            }
            else
            {
                Debug.LogWarning("CriticalImpactEffect를 찾을 수 없습니다.");
            }
        }

        if (battleCanvas != null)
        {
            Transform criticalTextTransform = battleCanvas.Find("CriticalText");

            if (criticalTextTransform != null)
            {
                criticalText = criticalTextTransform.GetComponent<TMP_Text>();
                criticalText.gameObject.SetActive(false);
            }
        }

        if (battleCanvas != null)
        {
            Transform levelUpTextTransform = battleCanvas.Find("LevelUpText");

            if (levelUpTextTransform != null)
            {
                levelUpText = levelUpTextTransform.GetComponent<TMP_Text>();
                levelUpText.gameObject.SetActive(false);
            }
        }

        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonClicked);

        if (wordInput != null)
            wordInput.onSubmit.AddListener(_ => OnAttackButtonClicked());

        if (playerHpFill != null)
            playerHpFullWidth = playerHpFill.rectTransform.sizeDelta.x;

        if (slimeHpFill != null)
            slimeHpFullWidth = slimeHpFill.rectTransform.sizeDelta.x;

        if (slimeVisual != null)
            slimeOriginalPosition = slimeVisual.anchoredPosition;

        koreanFont = Resources.Load<TMP_FontAsset>("Fonts/NotoSansKR-VF SDF");

        if (chainHintText != null)
            koreanFont = chainHintText.font;

        // =========================
        // 승리 UI 찾기
        // =========================
        victoryPanel = GameObject.Find("VictoryPanel");

        if (victoryPanel == null)
        {
            Transform canvasTransform = GameObject.Find("BattleCanvas")?.transform;

            if (canvasTransform != null)
            {
                Transform victoryTransform = canvasTransform.Find("VictoryPanel");

                if (victoryTransform != null)
                    victoryPanel = victoryTransform.gameObject;
            }
        }

        if (victoryPanel != null)
        {
            victoryMonsterText =
                victoryPanel.transform.Find("VictoryMonsterText")
                ?.GetComponent<TMP_Text>();

            victoryRewardText =
                victoryPanel.transform.Find("VictoryRewardText")
                ?.GetComponent<TMP_Text>();

            nextFloorButton =
                victoryPanel.transform.Find("NextFloorButton")
                ?.GetComponent<Button>();

            if (nextFloorButton != null)
                nextFloorButton.onClick.AddListener(OnNextFloorClicked);
        }

        floorDebugPanel = GameObject.Find("FloorDebugPanel");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (floorDebugPanel != null)
        {
            debugFloorText = floorDebugPanel.transform
                .Find("DebugFloorText")?.GetComponent<TMP_Text>();
            debugPreviousFloorButton = floorDebugPanel.transform
                .Find("DebugPreviousFloorButton")?.GetComponent<Button>();
            debugNextFloorButton = floorDebugPanel.transform
                .Find("DebugNextFloorButton")?.GetComponent<Button>();
            debugFloorTenButton = floorDebugPanel.transform
                .Find("DebugFloorTenButton")?.GetComponent<Button>();
            debugSaveResetButton = floorDebugPanel.transform
                .Find("DebugSaveResetButton")?.GetComponent<Button>();

            if (debugPreviousFloorButton != null)
                debugPreviousFloorButton.onClick.AddListener(
                    () => DebugMoveToFloor(currentFloor - 1)
                );

            if (debugNextFloorButton != null)
                debugNextFloorButton.onClick.AddListener(
                    () => DebugMoveToFloor(currentFloor + 1)
                );

            if (debugFloorTenButton != null)
                debugFloorTenButton.onClick.AddListener(
                    () => DebugMoveToFloor(10)
                );

            if (debugSaveResetButton != null)
                debugSaveResetButton.onClick.AddListener(ResetSaveData);
        }

        if (playerVisual != null)
            debugPlayerOriginalPosition = playerVisual.anchoredPosition;

        if (weaponVisual != null)
            debugWeaponOriginalRotation = weaponVisual.localRotation;
#else
        if (floorDebugPanel != null)
            floorDebugPanel.SetActive(false);
#endif
    }

    void SetupBattle()
    {
        playerHp = playerMaxHp;
        slimeHp = slimeMaxHp;

        currentWord = "사과";
        battleEnded = false;

        usedWords.Clear();
        usedWords.Add(currentWord);

        UpdateUI();

        if (wordInput != null)
        {
            wordInput.text = "";
            wordInput.ActivateInputField();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RefreshFloorDebugUI();
#endif
    }

    SaveData CreateSaveData()
    {
        return new SaveData
        {
            currentFloor = currentFloor,
            highestFloor = highestFloor,
            playerProgress = playerProgress.Data
        };
    }

    void LoadGame()
    {
        if (saveService == null || !saveService.HasSave())
            return;

        if (!saveService.TryLoad(out SaveData saveData))
            return;

        playerProgress.SetData(saveData.playerProgress);

        int savedFloor = Mathf.Max(1, saveData.currentFloor);
        currentFloor = FloorDataExists(savedFloor)
            ? savedFloor
            : 1;

        highestFloor = Mathf.Max(
            Mathf.Max(1, saveData.highestFloor),
            currentFloor
        );
    }

    void SaveGame()
    {
        saveService?.Save(CreateSaveData());
    }

    void ResetSaveData()
    {
        saveService?.DeleteSave();

        playerProgress.Reset();
        currentFloor = 1;
        highestFloor = 1;

        StopAllCoroutines();

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (impactEffect != null)
        {
            impactEffect.gameObject.SetActive(false);
            impactEffect.localScale = Vector3.one;
        }

        if (criticalImpactEffect != null)
            criticalImpactEffect.SetActive(false);

        if (criticalText != null)
            criticalText.gameObject.SetActive(false);

        if (levelUpText != null)
            levelUpText.gameObject.SetActive(false);

        LoadFloorAndMonsterData();
        ResetBattleForNextFloor();

        Debug.Log("Save 데이터 초기화 완료");
    }

    public void OnAttackButtonClicked()
    {
        if (battleEnded || wordInput == null)
            return;

        string inputWord = wordInput.text.Trim();

        if (string.IsNullOrEmpty(inputWord))
            return;


        // ========================================
        // 1. 끝말잇기 규칙 검사
        // ========================================

        char requiredChar = currentWord[currentWord.Length - 1];

        if (inputWord[0] != requiredChar)
        {
            chainHintText.text =
                $"'{requiredChar}'로 시작해야 합니다!";

            wordInput.text = "";
            wordInput.ActivateInputField();

            return;
        }


        // ========================================
        // 2. DB에 실제 등록된 단어인지 검사
        // ========================================

        if (!wordService.IsValidWord(inputWord))
        {
            chainHintText.text =
                $"'{inputWord}'은(는) 등록되지 않은 단어입니다!";

            wordInput.text = "";
            wordInput.ActivateInputField();

            return;
        }


        // ========================================
        // 3. 이미 사용한 단어인지 검사
        // ========================================

        if (usedWords.Contains(inputWord))
        {
            chainHintText.text =
                $"'{inputWord}'은(는) 이미 사용한 단어입니다!";

            wordInput.text = "";
            wordInput.ActivateInputField();

            return;
        }

        // ========================================
        // 한방단어 판정
        // ========================================

        isCriticalAttack = wordService.IsOneShotWord(
            inputWord,
            currentMonsterData.wordLevelMin,
            currentMonsterData.wordLevelMax,
            usedWords
        );

        // 기본 데미지
        currentAttackDamage = playerAttack;

        // 한방단어라면 크리티컬 2배
        if (isCriticalAttack)
        {
            currentAttackDamage = playerAttack * 2;

            Debug.Log(
                $"한방단어 크리티컬! {inputWord} / " +
                $"Damage {currentAttackDamage}"
            );
        }

        // ========================================
        // 4. 정상 단어 → 사용 단어 등록
        // ========================================
        usedWords.Add(inputWord);


        // ========================================
        // 5. 기존 공격 실행
        // ========================================
        PlayerAttack(inputWord);
    }

    // ========================================
    // 플레이어 공격 시작
    // ========================================
    void PlayerAttack(string inputWord)
    {
        currentWord = inputWord;

        // 공격 중에는 입력 방지
        wordInput.text = "";
        wordInput.interactable = false;
        attackButton.interactable = false;

        // 돌진 + 검 공격 연출 시작
        StartCoroutine(PlayerAttackSequence());
    }

    IEnumerator SlimeTurn()
    {
        yield return new WaitForSeconds(1f);

        // ========================================
        // DB에서 몬스터가 사용할 단어 선택
        // ========================================

        // 플레이어가 말한 단어의 마지막 글자
        string requiredChar =
            currentWord[currentWord.Length - 1].ToString();

        // 현재 몬스터의 단어 난이도 범위에 맞춰
        // DB에서 사용 가능한 단어를 하나 선택
        WordData monsterWord = wordService.GetMonsterWord(
            requiredChar,
            currentMonsterData.wordLevelMin,
            currentMonsterData.wordLevelMax,
            usedWords
        );


        // ========================================
        // 몬스터가 이어갈 단어가 없음
        // = 플레이어가 한방단어 사용 성공
        // ========================================
        if (monsterWord == null)
        {
            Debug.Log(
                $"몬스터가 사용할 단어가 없습니다. 시작 글자: {requiredChar}"
            );

            // ----------------------------------------
            // 새로운 제시어를 DB에서 선택
            // ----------------------------------------
            WordData restartWord =
                wordService.GetRandomStartWordForPlayer(
                    usedWords
                );

            if (restartWord == null)
            {
                Debug.LogError("새로운 제시어를 찾을 수 없습니다.");
                yield break;
            }


            // 새로운 단어도 사용 단어에 등록
            usedWords.Add(restartWord.word);

            // 끝말잇기 기준 단어 변경
            currentWord = restartWord.word;

            // 화면에 새로운 제시어 표시
            enemyWordText.text = currentWord;

            chainHintText.text =
                $"크리티컬! 새로운 제시어: {currentWord}\n" +
                $"'{currentWord[currentWord.Length - 1]}'로 시작하세요!";


            // ----------------------------------------
            // 한방단어이므로 몬스터 공격은 없음
            // 바로 플레이어 턴으로 복귀
            // ----------------------------------------
            wordInput.interactable = true;
            attackButton.interactable = true;

            wordInput.text = "";
            wordInput.ActivateInputField();

            yield break;
        }


        // ========================================
        // 몬스터가 선택한 단어 적용
        // ========================================

        string slimeWord = monsterWord.word;

        // 몬스터가 사용한 단어도 중복 방지를 위해 등록
        usedWords.Add(slimeWord);

        bool isMonsterCritical =
            wordService.IsOneShotForPlayer(
                slimeWord,
                usedWords
            );

        int monsterAttackDamage =
            isMonsterCritical
                ? slimeAttack * 2
                : slimeAttack;

        // 화면 표시
        enemyWordText.text = slimeWord;

        // 현재 끝말잇기 기준 단어 변경
        currentWord = slimeWord;

        Debug.Log(
            $"몬스터 단어 선택: {slimeWord} " +
            $"(난이도 Lv.{monsterWord.level})"
        );

        // 슬라임 공격 연출 시작
        yield return StartCoroutine(
            SlimeAttackSequence(
                monsterAttackDamage,
                isMonsterCritical
            )
        );

        if (playerHp <= 0)
        {
            LoseBattle();
            yield break;
        }

        if (isMonsterCritical)
        {
            WordData restartWord =
                wordService.GetRandomStartWordForPlayer(
                    usedWords
                );

            if (restartWord == null)
            {
                Debug.LogError("몬스터 크리티컬 후 새로운 제시어를 찾을 수 없습니다.");
                yield break;
            }

            usedWords.Add(restartWord.word);
            currentWord = restartWord.word;
            enemyWordText.text = currentWord;

            chainHintText.text =
                $"몬스터 크리티컬! 새로운 제시어: {currentWord}\n" +
                $"'{currentWord[currentWord.Length - 1]}'로 시작하세요!";
        }

        wordInput.interactable = true;
        attackButton.interactable = true;

        wordInput.ActivateInputField();
    }

    // ========================================
    // 슬라임 공격 연출
    // 1. 플레이어 방향으로 돌진
    // 2. 데미지 적용
    // 3. 플레이어 피격
    // 4. 슬라임 원위치 복귀
    // ========================================
    IEnumerator SlimeAttackSequence(
        int attackDamage,
        bool isMonsterCritical
    )
    {
        if (slimeVisual == null)
            yield break;

        Vector2 originalSlimePos = slimeVisual.anchoredPosition;

        // -------------------------
        // 1. 용사 쪽으로 돌진
        // 슬라임은 오른쪽에 있으므로 왼쪽(-X)으로 이동
        // -------------------------
        Vector2 attackPosition =
            originalSlimePos + new Vector2(-85f, 0f);

        float rushDuration = 0.13f;
        float elapsed = 0f;

        while (elapsed < rushDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / rushDuration;

            slimeVisual.anchoredPosition =
                Vector2.Lerp(
                    originalSlimePos,
                    attackPosition,
                    t
                );

            yield return null;
        }

        slimeVisual.anchoredPosition = attackPosition;

        // -------------------------
        // 2. 실제 데미지 적용
        // -------------------------
        playerHp -= attackDamage;
        playerHp = Mathf.Max(playerHp, 0);

        UpdateUI();

        if (isMonsterCritical)
        {
            chainHintText.text =
                $"몬스터 크리티컬 공격! {attackDamage} 데미지!";
        }
        else
        {
            chainHintText.text =
                $"슬라임의 공격! {attackDamage} 데미지\n" +
                $"'{currentWord[currentWord.Length - 1]}'로 시작하는 단어를 입력하세요!";
        }

        // 플레이어 머리 위 데미지 숫자
        ShowDamageText(playerVisual, attackDamage);

        // 플레이어는 왼쪽으로 밀리며 피격
        if (playerVisual != null)
            StartCoroutine(HitEffect(playerVisual));

        yield return new WaitForSeconds(0.1f);

        // -------------------------
        // 3. 슬라임 원위치 복귀
        // -------------------------
        float returnDuration = 0.15f;
        elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / returnDuration;

            slimeVisual.anchoredPosition =
                Vector2.Lerp(
                    attackPosition,
                    originalSlimePos,
                    t
                );

            yield return null;
        }

        slimeVisual.anchoredPosition = originalSlimePos;
    }

    // ========================================
    // 피격 연출
    // 1. 맞는 순간 뒤로 밀림
    // 2. 짧게 흔들림
    // 3. 원래 위치로 복귀
    // ========================================
    IEnumerator HitEffect(RectTransform target)
    {
        if (target == null)
            yield break;

        // 피격 전 원래 위치 저장
        Vector2 originalPosition = target.anchoredPosition;

        // ========================================
        // 1. 뒤로 밀려나는 연출
        // ========================================

        // ========================================
        // 피격 방향 결정
        // 슬라임은 오른쪽으로, 용사는 왼쪽으로 밀림
        // ========================================

        float knockbackDirection = 1f;

        // 플레이어가 맞았으면 왼쪽 방향
        if (target == playerVisual)
        {
            knockbackDirection = -1f;
        }

        // 슬라임은 기본값 +1 → 오른쪽 방향

        Vector2 knockbackPosition =
            originalPosition + new Vector2(35f * knockbackDirection, 0f);

        float knockbackDuration = 0.08f;
        float elapsed = 0f;

        while (elapsed < knockbackDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / knockbackDuration;

            target.anchoredPosition =
                Vector2.Lerp(
                    originalPosition,
                    knockbackPosition,
                    t
                );

            yield return null;
        }

        target.anchoredPosition = knockbackPosition;


        // ========================================
        // 2. 피격 흔들림
        // ========================================

        for (int i = 0; i < 4; i++)
        {
            float offset =
                (i % 2 == 0)
                    ? 10f * knockbackDirection
                    : -10f * knockbackDirection;

            target.anchoredPosition =
                knockbackPosition + new Vector2(offset, 0f);

            yield return new WaitForSeconds(0.035f);
        }


        // ========================================
        // 3. 원래 위치로 복귀
        // ========================================

        float returnDuration = 0.12f;
        elapsed = 0f;

        Vector2 currentPosition = target.anchoredPosition;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / returnDuration;

            target.anchoredPosition =
                Vector2.Lerp(
                    currentPosition,
                    originalPosition,
                    t
                );

            yield return null;
        }

        // 오차 방지를 위해 정확한 원위치 지정
        target.anchoredPosition = originalPosition;
    }

    // ========================================
    // 플레이어 공격 연출
    // 1. 앞으로 돌진
    // 2. 검 휘두르기
    // 3. 데미지 적용
    // 4. 원위치 복귀
    // ========================================
    IEnumerator PlayerAttackSequence()
    {
        if (playerVisual == null)
            yield break;

        // 현재 용사의 원래 위치 저장
        Vector2 originalPlayerPos = playerVisual.anchoredPosition;

        // 현재 나무검 각도 저장
        Quaternion originalWeaponRotation =
            weaponVisual != null
                ? weaponVisual.localRotation
                : Quaternion.identity;


        // ========================================
        // 1. 슬라임 방향으로 빠르게 돌진
        // ========================================

        Vector2 attackPosition =
            originalPlayerPos + new Vector2(110f, 0f);

        float rushDuration = 0.12f;
        float elapsed = 0f;

        while (elapsed < rushDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / rushDuration;

            playerVisual.anchoredPosition =
                Vector2.Lerp(
                    originalPlayerPos,
                    attackPosition,
                    t
                );

            yield return null;
        }

        playerVisual.anchoredPosition = attackPosition;


        // ========================================
        // 2. 나무검 휘두르기
        // ========================================

        if (weaponVisual != null)
        {
            float swingDuration = 0.13f;
            elapsed = 0f;

            Quaternion startRotation =
                Quaternion.Euler(0f, 0f, 20f);

            Quaternion endRotation =
                Quaternion.Euler(0f, 0f, -55f);

            while (elapsed < swingDuration)
            {
                elapsed += Time.deltaTime;

                float t = elapsed / swingDuration;

                weaponVisual.localRotation =
                    Quaternion.Lerp(
                        startRotation,
                        endRotation,
                        t
                    );

                yield return null;
            }
        }


        // ========================================
        // 3. 검이 닿는 순간 실제 데미지 적용
        // ========================================

        // 검이 닿는 순간 타격 이펙트!
        if (isCriticalAttack)
        {
            StartCoroutine(CriticalImpactEffect());
            StartCoroutine(CriticalTextEffect());
        }
        else
        {
            StartCoroutine(ImpactEffect());
        }

        // 실제 데미지
        slimeHp -= currentAttackDamage;
        slimeHp = Mathf.Max(slimeHp, 0);

        UpdateUI();

        if (isCriticalAttack)
        {
            chainHintText.text =
                $"크리티컬! 한방단어 공격! {currentAttackDamage} 데미지!";
        }
        else
        {
            chainHintText.text =
                $"공격 성공! {currentAttackDamage} 데미지!";
        }

        // -20 데미지 숫자
        ShowDamageText(slimeVisual, currentAttackDamage);

        // 슬라임 피격 흔들림
        if (slimeVisual != null)
            StartCoroutine(HitEffect(slimeVisual));


        // ========================================
        // 4. 검 원래 각도로 복구
        // ========================================

        if (weaponVisual != null)
            weaponVisual.localRotation = originalWeaponRotation;

        yield return new WaitForSeconds(0.08f);


        // ========================================
        // 5. 용사 원위치 복귀
        // ========================================

        float returnDuration = 0.15f;
        elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / returnDuration;

            playerVisual.anchoredPosition =
                Vector2.Lerp(
                    attackPosition,
                    originalPlayerPos,
                    t
                );

            yield return null;
        }

        playerVisual.anchoredPosition = originalPlayerPos;


        // ========================================
        // 6. 슬라임 사망 체크
        // ========================================

        if (slimeHp <= 0)
        {
            // 슬라임 사망 연출이 끝난 후 승리 처리
            yield return StartCoroutine(SlimeDeathSequence());

            WinBattle();
            yield break;
        }


        // ========================================
        // 7. 슬라임 반격 시작
        // ========================================

        StartCoroutine(SlimeTurn());
    }

    // ========================================
    // 슬라임 사망 연출
    // 1. 살짝 위로 튀어오름
    // 2. 회전하면서 작아짐
    // 3. 완전히 사라짐
    // ========================================
    IEnumerator SlimeDeathSequence()
    {
        if (slimeVisual == null)
            yield break;

        Vector2 originalPosition = slimeVisual.anchoredPosition;
        Vector3 originalScale = slimeVisual.localScale;
        Quaternion originalRotation = slimeVisual.localRotation;


        // ========================================
        // 1. 죽기 직전 살짝 위로 튀어오름
        // ========================================

        Vector2 jumpPosition =
            originalPosition + new Vector2(0f, 35f);

        float jumpDuration = 0.12f;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / jumpDuration;

            slimeVisual.anchoredPosition =
                Vector2.Lerp(
                    originalPosition,
                    jumpPosition,
                    t
                );

            yield return null;
        }


        // ========================================
        // 2. 회전하면서 작아짐
        // ========================================

        float disappearDuration = 0.30f;
        elapsed = 0f;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / disappearDuration;

            // 점점 작아짐
            slimeVisual.localScale =
                Vector3.Lerp(
                    originalScale,
                    Vector3.zero,
                    t
                );

            // 오른쪽으로 빙글 회전
            slimeVisual.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(0f, -180f, t)
                );

            // 살짝 아래로 떨어지는 느낌
            slimeVisual.anchoredPosition =
                Vector2.Lerp(
                    jumpPosition,
                    jumpPosition + new Vector2(20f, -40f),
                    t
                );

            yield return null;
        }


        // ========================================
        // 3. 완전히 숨김
        // ========================================

        slimeVisual.localScale = Vector3.zero;
    }

    // ========================================
    // 기본 공격 타격 이펙트
    // 검이 맞는 순간 크게 나타났다 사라지는 연출
    // ========================================
    IEnumerator ImpactEffect()
    {
        if (impactEffect == null)
            yield break;

        // 공격 순간 활성화
        impactEffect.gameObject.SetActive(true);

        // 기존보다 크게 시작해서 눈에 잘 보이도록 설정
        Vector3 startScale = new Vector3(0.7f, 0.7f, 1f);
        Vector3 endScale   = new Vector3(1.5f, 1.5f, 1f);

        impactEffect.localScale = startScale;

        // 기존 0.10초 → 0.18초로 조금 길게
        float duration = 0.18f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            impactEffect.localScale =
                Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        // 타격 모양을 잠깐 유지
        yield return new WaitForSeconds(0.08f);

        // 다시 숨김
        impactEffect.gameObject.SetActive(false);

        // 다음 공격을 위해 초기화
        impactEffect.localScale = Vector3.one;
    }

    IEnumerator CriticalImpactEffect()
    {
        if (criticalImpactEffect == null)
            yield break;

        criticalImpactEffect.SetActive(true);

        RectTransform rect =
            criticalImpactEffect.GetComponent<RectTransform>();

        Vector3 originalScale = rect.localScale;

        // 처음에는 조금 작게
        rect.localScale = originalScale * 0.7f;

        // 빠르게 크게 터지는 느낌
        float duration = 0.10f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            rect.localScale = Vector3.Lerp(
                originalScale * 0.7f,
                originalScale * 1.25f,
                t
            );

            yield return null;
        }

        // 아주 잠깐 유지
        yield return new WaitForSeconds(0.10f);

        rect.localScale = originalScale;

        criticalImpactEffect.SetActive(false);
    }

    // ========================================
    // CRITICAL! 텍스트 연출
    // 1. 크게 등장
    // 2. 살짝 확대
    // 3. 위로 떠오르며 사라짐
    // ========================================
    IEnumerator CriticalTextEffect()
    {
        if (criticalText == null)
            yield break;

        RectTransform rect =
            criticalText.GetComponent<RectTransform>();

        Vector2 originalPosition = rect.anchoredPosition;
        Vector3 originalScale = Vector3.one;

        criticalText.gameObject.SetActive(true);

        // 시작 상태
        rect.anchoredPosition = originalPosition;
        rect.localScale = new Vector3(0.6f, 0.6f, 1f);

        Color startColor = Color.white;
        criticalText.color = startColor;

        // -------------------------
        // 1. 팡! 하고 크게 등장
        // -------------------------
        float popDuration = 0.12f;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / popDuration;

            rect.localScale =
                Vector3.Lerp(
                    new Vector3(0.6f, 0.6f, 1f),
                    new Vector3(1.25f, 1.25f, 1f),
                    t
                );

            yield return null;
        }

        // 잠깐 유지
        yield return new WaitForSeconds(0.12f);

        // -------------------------
        // 2. 위로 떠오르며 사라짐
        // -------------------------
        Vector2 endPosition =
            originalPosition + new Vector2(0f, 90f);

        float fadeDuration = 0.45f;
        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeDuration;

            rect.anchoredPosition =
                Vector2.Lerp(
                    originalPosition,
                    endPosition,
                    t
                );

            criticalText.color =
                new Color(
                    startColor.r,
                    startColor.g,
                    startColor.b,
                    1f - t
                );

            yield return null;
        }

        // 초기화
        rect.anchoredPosition = originalPosition;
        rect.localScale = originalScale;
        criticalText.color = startColor;

        criticalText.gameObject.SetActive(false);
    }

    IEnumerator LevelUpTextEffect()
    {
        if (levelUpText == null)
            yield break;

        RectTransform rect = levelUpText.GetComponent<RectTransform>();
        Vector2 originalPosition = rect.anchoredPosition;
        Vector3 originalScale = Vector3.one;
        Color originalColor = new Color(1f, 0.82f, 0.20f, 1f);

        levelUpText.text = $"LEVEL UP!\nLV.{playerLevel}";
        levelUpText.gameObject.SetActive(true);
        rect.anchoredPosition = originalPosition;
        rect.localScale = new Vector3(0.55f, 0.55f, 1f);
        levelUpText.color = originalColor;

        float popDuration = 0.18f;
        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            rect.localScale = Vector3.Lerp(
                new Vector3(0.55f, 0.55f, 1f),
                new Vector3(1.15f, 1.15f, 1f),
                t
            );

            yield return null;
        }

        yield return new WaitForSeconds(0.45f);

        Vector2 endPosition = originalPosition + new Vector2(0f, 100f);
        float fadeDuration = 0.55f;
        elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            rect.anchoredPosition = Vector2.Lerp(
                originalPosition,
                endPosition,
                t
            );

            levelUpText.color = new Color(
                originalColor.r,
                originalColor.g,
                originalColor.b,
                1f - t
            );

            yield return null;
        }

        rect.anchoredPosition = originalPosition;
        rect.localScale = originalScale;
        levelUpText.color = originalColor;
        levelUpText.gameObject.SetActive(false);
    }

    // ========================================
    // 전투 승리 처리
    // ========================================
    void WinBattle()
    {
        battleEnded = true;

        // JSON 데이터 기준 보상 지급
        bool didLevelUp =
            playerProgress.AddExp(currentMonsterData.expReward);
        playerProgress.AddGold(currentMonsterData.goldReward);

        if (didLevelUp)
            StartCoroutine(LevelUpTextEffect());

        SaveGame();

        // 기존 하단 상태 UI 갱신
        UpdateUI();

        // 입력 잠금
        wordInput.interactable = false;
        attackButton.interactable = false;

        // 승리 패널 내용 설정
        if (victoryMonsterText != null)
        {
            victoryMonsterText.text =
                $"{currentMonsterData.name} 처치!";
        }

        if (victoryRewardText != null)
        {
            victoryRewardText.text =
                $"EXP +{currentMonsterData.expReward}\n" +
                $"GOLD +{currentMonsterData.goldReward}";
        }

        // 승리 패널 표시
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
    }

    void OnNextFloorClicked()
    {
        int nextFloor = currentFloor + 1;

        if (!FloorDataExists(nextFloor))
        {
            Debug.LogWarning(
                $"다음 층 데이터가 없어 이동하지 않습니다. Floor: {nextFloor}"
            );
            return;
        }

        // 승리 패널 숨김
        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        // 다음 층으로 이동
        currentFloor = nextFloor;
        highestFloor = Mathf.Max(highestFloor, currentFloor);

        // 다음 층 데이터 로드
        LoadFloorAndMonsterData();

        // 새로운 전투 시작
        ResetBattleForNextFloor();

        SaveGame();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void DebugMoveToFloor(int targetFloor)
    {
        if (!FloorDataExists(targetFloor))
        {
            Debug.LogWarning(
                $"Debug Floor 이동 취소: {targetFloor}층 데이터가 없습니다."
            );
            return;
        }

        StopAllCoroutines();

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        if (impactEffect != null)
        {
            impactEffect.gameObject.SetActive(false);
            impactEffect.localScale = Vector3.one;
        }

        if (criticalImpactEffect != null)
            criticalImpactEffect.SetActive(false);

        if (criticalText != null)
            criticalText.gameObject.SetActive(false);

        if (levelUpText != null)
            levelUpText.gameObject.SetActive(false);

        if (playerVisual != null)
            playerVisual.anchoredPosition = debugPlayerOriginalPosition;

        if (weaponVisual != null)
            weaponVisual.localRotation = debugWeaponOriginalRotation;

        currentFloor = targetFloor;
        LoadFloorAndMonsterData();
        ResetBattleForNextFloor();

        Debug.Log($"Debug Floor 이동 완료: {currentFloor}층");
    }

    void RefreshFloorDebugUI()
    {
        if (debugFloorText != null)
            debugFloorText.text = $"DEBUG FLOOR {currentFloor}";

        if (debugPreviousFloorButton != null)
            debugPreviousFloorButton.interactable =
                FloorDataExists(currentFloor - 1);

        if (debugNextFloorButton != null)
            debugNextFloorButton.interactable =
                FloorDataExists(currentFloor + 1);

        if (debugFloorTenButton != null)
            debugFloorTenButton.interactable = FloorDataExists(10);
    }
#endif

    bool FloorDataExists(int targetFloor)
    {
        string floorPath =
            Path.Combine(Application.dataPath, "Data/Floors/floors.json");

        if (!File.Exists(floorPath))
            return false;

        string floorJson = File.ReadAllText(floorPath);
        FloorDataList floorList =
            JsonUtility.FromJson<FloorDataList>(floorJson);

        return floorList != null &&
            floorList.floors != null &&
            floorList.floors.Exists(f => f.floor == targetFloor);
    }

    // ========================================
    // 다음 층 전투 초기화
    // ========================================
    void ResetBattleForNextFloor()
    {
        battleEnded = false;

        // 몬스터 HP 초기화
        slimeHp = slimeMaxHp;

        // 플레이어 HP는 일단 MVP에서는 풀 회복
        playerHp = playerMaxHp;

        // 첫 단어 초기화
        currentWord = "사과";

        // 새로운 층이므로 사용 단어 기록 초기화
        usedWords.Clear();
        usedWords.Add(currentWord);

        // 죽어서 사라졌던 슬라임 원상복구
        if (slimeVisual != null)
        {
            ApplyMonsterVisualScale();
            slimeVisual.localRotation = Quaternion.identity;
            slimeVisual.anchoredPosition = slimeOriginalPosition;
        }

        // 입력창 복구
        if (wordInput != null)
        {
            wordInput.text = "";
            wordInput.interactable = true;
        }

        if (attackButton != null)
            attackButton.interactable = true;

        // UI 갱신
        UpdateUI();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RefreshFloorDebugUI();
#endif

        if (wordInput != null)
            wordInput.ActivateInputField();
    }

    void LoseBattle()
    {
        battleEnded = true;

        chainHintText.text = "패배했습니다.";

        wordInput.interactable = false;
        attackButton.interactable = false;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (playerHpText != null)
            playerHpText.text = $"HP {playerHp} / {playerMaxHp}";

        if (slimeHpText != null)
            slimeHpText.text = $"HP {slimeHp} / {slimeMaxHp}";

        if (playerHpFill != null)
        {
            float ratio = (float)playerHp / playerMaxHp;

            Vector2 size = playerHpFill.rectTransform.sizeDelta;
            size.x = playerHpFullWidth * ratio;
            playerHpFill.rectTransform.sizeDelta = size;
        }

        if (slimeHpFill != null)
        {
            float ratio = (float)slimeHp / slimeMaxHp;

            Vector2 size = slimeHpFill.rectTransform.sizeDelta;
            size.x = slimeHpFullWidth * ratio;
            slimeHpFill.rectTransform.sizeDelta = size;
        }

        if (enemyWordText != null)
            enemyWordText.text = currentWord;

        if (chainHintText != null && !battleEnded)
        {
            char requiredChar = currentWord[currentWord.Length - 1];
            chainHintText.text = $"『 {requiredChar} 』로 시작하는 단어를 입력하세요!";
        }

        if (levelText != null)
            levelText.text = $"LV.{playerLevel}";

        if (expText != null)
            expText.text = $"EXP {exp} / {requiredExp}";

        if (goldText != null)
            goldText.text = $"GOLD {gold}";
    }

    void ShowDamageText(RectTransform target, int damage)
    {
        if (target == null)
            return;

        GameObject obj = new GameObject(
            "DamageText",
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        obj.transform.SetParent(target.parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = target.anchorMin;
        rect.anchorMax = target.anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = target.anchoredPosition + new Vector2(0, 180);
        rect.sizeDelta = new Vector2(250, 100);

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();

        text.text = $"-{damage}";
        text.font = koreanFont;
        text.fontSize = 54;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        StartCoroutine(DamageTextAnimation(rect, text));
    }

    IEnumerator DamageTextAnimation(
        RectTransform rect,
        TextMeshProUGUI text
    )
    {
        float duration = 0.7f;
        float elapsed = 0f;

        Vector2 startPosition = rect.anchoredPosition;
        Vector2 endPosition = startPosition + new Vector2(0, 100);

        Color startColor = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            rect.anchoredPosition =
                Vector2.Lerp(startPosition, endPosition, t);

            text.color = new Color(
                startColor.r,
                startColor.g,
                startColor.b,
                1f - t
            );

            yield return null;
        }

        Destroy(rect.gameObject);
    }

    // ========================================
    // 현재 층 및 몬스터 데이터 로드
    // ========================================
    void LoadFloorAndMonsterData()
    {
        string floorPath =
            Path.Combine(Application.dataPath, "Data/Floors/floors.json");

        string monsterPath =
            Path.Combine(Application.dataPath, "Data/Monsters/monsters.json");


        // =========================
        // JSON 파일 존재 여부 확인
        // =========================

        if (!File.Exists(floorPath))
        {
            Debug.LogError("floors.json 파일을 찾을 수 없습니다.");
            return;
        }

        if (!File.Exists(monsterPath))
        {
            Debug.LogError("monsters.json 파일을 찾을 수 없습니다.");
            return;
        }


        // =========================
        // JSON 읽기
        // =========================

        string floorJson = File.ReadAllText(floorPath);
        string monsterJson = File.ReadAllText(monsterPath);

        FloorDataList floorList =
            JsonUtility.FromJson<FloorDataList>(floorJson);

        MonsterDataList monsterList =
            JsonUtility.FromJson<MonsterDataList>(monsterJson);


        // =========================
        // 현재 층 찾기
        // =========================

        currentFloorData =
            floorList.floors.Find(f => f.floor == currentFloor);

        if (currentFloorData == null)
        {
            Debug.LogError(
                $"현재 층 데이터를 찾을 수 없습니다. Floor: {currentFloor}"
            );

            return;
        }


        // =========================
        // 현재 층의 몬스터 찾기
        // =========================

        currentMonsterData =
            monsterList.monsters.Find(
                m => m.id == currentFloorData.monsterId
            );

        if (currentMonsterData == null)
        {
            Debug.LogError(
                $"몬스터 데이터를 찾을 수 없습니다. ID: {currentFloorData.monsterId}"
            );

            return;
        }


        // =========================
        // BattleManager 전투값 적용
        // =========================

        slimeMaxHp = currentMonsterData.maxHp;
        slimeAttack = currentMonsterData.attack;

        slimeExpReward = currentMonsterData.expReward;
        slimeGoldReward = currentMonsterData.goldReward;

        // =========================
        // 현재 층 / 몬스터 이름 UI 적용
        // =========================
        if (floorTitleText != null)
        {
            floorTitleText.text = currentFloorData.title;
        }

        if (monsterNameText != null)
        {
            monsterNameText.text = currentMonsterData.name;
        }

        // JSON에 설정된 몬스터 이미지 적용
        ApplyMonsterSprite();
        ApplyMonsterVisualScale();

        Debug.Log(
            $"Floor {currentFloor} Loaded / " +
            $"{currentMonsterData.name} / " +
            $"HP {slimeMaxHp} / " +
            $"ATK {slimeAttack}"
        );
    }

    // ========================================
    // 현재 몬스터 이미지 적용
    // JSON의 spritePath를 읽어 실제 Sprite 교체
    // ========================================
    void ApplyMonsterSprite()
    {
        if (monsterImage == null || currentMonsterData == null)
            return;

    #if UNITY_EDITOR

        Sprite monsterSprite =
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                currentMonsterData.spritePath
            );

        if (monsterSprite != null)
        {
            monsterImage.sprite = monsterSprite;
            monsterImage.color = Color.white;
            monsterImage.preserveAspect = true;

            Debug.Log(
                $"몬스터 이미지 변경: {currentMonsterData.spritePath}"
            );
        }
        else
        {
            Debug.LogWarning(
                $"몬스터 이미지를 찾을 수 없습니다: " +
                currentMonsterData.spritePath
            );
        }

    #endif
    }

    void ApplyMonsterVisualScale()
    {
        if (slimeVisual == null || currentMonsterData == null)
            return;

        float scale = currentMonsterData.visualScale > 0f
            ? currentMonsterData.visualScale
            : 1f;

        slimeVisual.localScale = Vector3.one * scale;
    }
}
