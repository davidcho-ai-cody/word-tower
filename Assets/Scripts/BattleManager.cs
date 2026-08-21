using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BattleManager : MonoBehaviour
{
    private const float CriticalTextStartDelay = 0.05f;
    private const float CriticalDamageTextDelay = 0.6f;
    private const float LevelUpRewardDelay = 0.65f;
    private const float HeroIdleCycleDuration = 1.8f;
    private const float HeroIdleYOffset = 2.5f;
    private const float HeroIdleScaleX = 0.997f;
    private const float HeroIdleScaleY = 1.01f;
    private const float NormalSlimeIdleCycle = 1.5f;
    private const float EliteSlimeIdleCycle = 1.8f;
    private const float KingSlimeIdleCycle = 2.2f;
    private const float MonsterHitFlashDuration = 0.12f;
    private const float MonsterHitFlashAlpha = 0.15f;

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
    private int playerBaseAttack => playerProgress.PlayerAttack;
    private int equippedWeaponAttackBonus
    {
        get
        {
            ItemData weapon = itemService?.GetItem(
                playerProgress.EquippedWeaponId
            );

            return weapon != null &&
                weapon.GetItemType() == ItemType.Weapon
                ? weapon.attackBonus
                : 0;
        }
    }
    private float equippedArmorDefenseRate
    {
        get
        {
            ItemData armor = itemService?.GetItem(
                playerProgress.EquippedArmorId
            );

            return armor != null &&
                armor.GetItemType() == ItemType.Armor
                ? Mathf.Max(0f, armor.defenseRate)
                : 0f;
        }
    }
    private int playerAttack =>
        playerBaseAttack + equippedWeaponAttackBonus;

    private TMP_Text playerHpText;
    private TMP_Text slimeHpText;
    private TMP_Text enemyWordText;
    private TMP_Text chainHintText;
    private TMP_Text levelText;
    private TMP_Text playerNameText;
    private TMP_Text expText;
    private TMP_Text goldText;
    private TMP_FontAsset koreanFont;

    private TMP_Text floorTitleText;
    private TMP_Text monsterNameText;

    private TMP_InputField wordInput;
    private Button attackButton;
    private Canvas battleCanvas;
    private RectTransform wordBattlePanel;
    private Vector2 wordBattlePanelOriginalPosition;
    private bool wordBattlePanelPositionInitialized;
    private readonly Vector3[] wordBattlePanelWorldCorners = new Vector3[4];

#if UNITY_ANDROID && !UNITY_EDITOR
    private const float MobileKeyboardPanelMargin = 24f;
#endif

    private Button shopButton;
    private GameObject shopPanel;
    private TMP_Text shopCurrentGoldText;
    private TMP_Text shopMessageText;
    private RectTransform shopItemListContent;
    private Button shopCloseButton;
    private Button shopWeaponTabButton;
    private Button shopArmorTabButton;
    private Button shopAccessoryTabButton;
    private Button shopEtcTabButton;
    private bool isShopOpen = false;
    private ItemType currentShopTab = ItemType.Weapon;

    private Image playerHpFill;
    private Image slimeHpFill;
    private float playerHpFullWidth;
    private float slimeHpFullWidth;

    private RectTransform playerVisual;
    private RectTransform slimeVisual;
    private Image playerBodyImage;
    private Image weaponImage;
    private RectTransform heroShadow;
    private RectTransform monsterShadow;
    private Image monsterShadowImage;
    private Vector2 heroShadowBasePosition;
    private Vector3 heroShadowBaseScale = Vector3.one;
    private Vector2 monsterShadowBasePosition;
    private Vector3 monsterShadowBaseScale = Vector3.one;
    private Color monsterShadowBaseColor = new Color(0f, 0f, 0f, 0.22f);
    private Vector2 heroIdleBasePosition;
    private Vector3 heroIdleBaseScale;
    private float heroIdleElapsed;
    private bool heroIdleInitialized;
    private bool heroIdlePaused = true;

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
    private FloorDataList floorDataList;
    private MonsterDataList monsterDataList;

    // =========================
    // 승리 UI
    // =========================
    private GameObject victoryPanel;
    private TMP_Text victoryMonsterText;
    private TMP_Text victoryRewardText;
    private Button nextFloorButton;
    private Vector2 slimeOriginalPosition;
    private Vector2 monsterIdleBasePosition;
    private Vector3 monsterIdleBaseScale;
    private float monsterIdleElapsed;
    private bool monsterIdleInitialized;
    private bool monsterIdlePaused = true;
    private Coroutine monsterHitFlashCoroutine;

    // =========================
    // 현재 몬스터 이미지
    // =========================
    private Image monsterImage;

    private WordService wordService;
    private ItemService itemService;
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

    IEnumerator Start()
    {
        FindUI();

        playerProgress = new PlayerProgressService();
        saveService = new SaveService();

        Debug.Log("Save Path: " + saveService.GetSavePath());
        yield return StartCoroutine(LoadFloorAndMonsterDataLists());
        LoadGame();

        // 단어 DB 연결
        wordService = new WordService();
        yield return StartCoroutine(wordService.Initialize());

        itemService = new ItemService();
        yield return StartCoroutine(itemService.Initialize());
        ValidateStartingItems();
        ApplyEquipmentVisuals();

        // 현재 층 데이터와 몬스터 데이터 로드
        LoadFloorAndMonsterData();

        // 로드한 데이터 기준으로 전투 시작
        SetupBattle();
        ResumeHeroIdle();
        ResumeMonsterIdle();
    }

    void Update()
    {
        UpdateHeroIdle();
        UpdateMonsterIdle();
        UpdateGroundShadows();
        UpdateMobileKeyboardLayout();
    }

    void UpdateMobileKeyboardLayout()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!wordBattlePanelPositionInitialized ||
            wordBattlePanel == null ||
            battleCanvas == null)
        {
            return;
        }

        if (wordInput == null ||
            !wordInput.isFocused ||
            !TouchScreenKeyboard.visible ||
            TouchScreenKeyboard.area.height <= 0f)
        {
            RestoreWordBattlePanelPosition();
            return;
        }

        if (TryCalculateMobileKeyboardPanelLayout(
            TouchScreenKeyboard.area,
            out _,
            out _,
            out _,
            out _,
            out float clampedPanelY
        ))
        {
            wordBattlePanel.anchoredPosition = new Vector2(
                wordBattlePanelOriginalPosition.x,
                clampedPanelY
            );
        }
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    bool TryCalculateMobileKeyboardPanelLayout(
        Rect keyboardArea,
        out float panelBottomScreenY,
        out float keyboardTopScreenY,
        out float targetPanelBottom,
        out float requiredPanelShift,
        out float clampedPanelY
    )
    {
        panelBottomScreenY = float.NaN;
        keyboardTopScreenY = float.NaN;
        targetPanelBottom = float.NaN;
        requiredPanelShift = float.NaN;
        clampedPanelY = float.NaN;

        if (wordBattlePanel == null || battleCanvas == null)
            return false;

        RectTransform panelParent =
            wordBattlePanel.parent as RectTransform;

        if (panelParent == null)
            return false;

        Camera uiCamera =
            battleCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : battleCanvas.worldCamera;

        wordBattlePanel.GetWorldCorners(wordBattlePanelWorldCorners);
        panelBottomScreenY = RectTransformUtility.WorldToScreenPoint(
            uiCamera,
            wordBattlePanelWorldCorners[0]
        ).y;

        // Android keyboard area uses a top-left screen origin. Convert its
        // top edge to Unity's bottom-left screen coordinate system.
        keyboardTopScreenY = keyboardArea.height > 0f
            ? Mathf.Clamp(
                Screen.height - keyboardArea.y,
                0f,
                Screen.height
            )
            : 0f;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelParent,
            new Vector2(Screen.width * 0.5f, keyboardTopScreenY),
            uiCamera,
            out Vector2 keyboardTopLocalPoint
        ))
        {
            return false;
        }

        Vector3 currentBottomLocal = panelParent.InverseTransformPoint(
            wordBattlePanelWorldCorners[0]
        );
        Vector3 currentTopLocal = panelParent.InverseTransformPoint(
            wordBattlePanelWorldCorners[1]
        );
        float currentAnchoredShift =
            wordBattlePanel.anchoredPosition.y -
            wordBattlePanelOriginalPosition.y;
        float originalBottomLocalY =
            currentBottomLocal.y - currentAnchoredShift;
        float originalTopLocalY =
            currentTopLocal.y - currentAnchoredShift;

        targetPanelBottom =
            keyboardTopLocalPoint.y + MobileKeyboardPanelMargin;
        requiredPanelShift = Mathf.Max(
            0f,
            targetPanelBottom - originalBottomLocalY
        );

        float maximumPanelShift = Mathf.Max(
            0f,
            panelParent.rect.yMax - originalTopLocalY
        );
        float clampedPanelShift = Mathf.Min(
            requiredPanelShift,
            maximumPanelShift
        );
        clampedPanelY =
            wordBattlePanelOriginalPosition.y + clampedPanelShift;

        return true;
    }
#endif

    void RestoreWordBattlePanelPosition()
    {
        if (wordBattlePanelPositionInitialized && wordBattlePanel != null)
            wordBattlePanel.anchoredPosition = wordBattlePanelOriginalPosition;
    }

    void RequestWordInputFocus(bool keepMobileKeyboardOpen = false)
    {
        if (wordInput == null || !wordInput.interactable)
            return;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!keepMobileKeyboardOpen)
            return;

        StartCoroutine(RefocusWordInputNextFrame());
#else
        wordInput.ActivateInputField();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator RefocusWordInputNextFrame()
    {
        yield return null;

        if (!battleEnded &&
            !isShopOpen &&
            wordInput != null &&
            wordInput.interactable)
        {
            wordInput.ActivateInputField();
        }
    }
#endif

    void CloseMobileKeyboardAndRestorePanel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (wordInput != null)
            wordInput.DeactivateInputField();
#endif

        RestoreWordBattlePanelPosition();
    }

    void OnWordInputDeselected(string _)
    {
        RestoreWordBattlePanelPosition();
    }

    void PlaySfx(SfxId id)
    {
        AudioManager.Instance?.PlaySfx(id);
    }

    void UpdateHeroIdle()
    {
        if (!heroIdleInitialized || heroIdlePaused || playerVisual == null)
            return;

        heroIdleElapsed =
            (heroIdleElapsed + Time.deltaTime) % HeroIdleCycleDuration;

        float phase = heroIdleElapsed / HeroIdleCycleDuration;
        float breath = (1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 0.5f;

        playerVisual.anchoredPosition =
            heroIdleBasePosition + new Vector2(0f, HeroIdleYOffset * breath);
        playerVisual.localScale = Vector3.Scale(
            heroIdleBaseScale,
            new Vector3(
                Mathf.Lerp(1f, HeroIdleScaleX, breath),
                Mathf.Lerp(1f, HeroIdleScaleY, breath),
                1f
            )
        );
    }

    void UpdateMonsterIdle()
    {
        if (!monsterIdleInitialized || monsterIdlePaused || slimeVisual == null)
            return;

        float cycleDuration = currentFloor >= 10
            ? KingSlimeIdleCycle
            : currentFloor == 9
                ? EliteSlimeIdleCycle
                : NormalSlimeIdleCycle;
        float scaleXAmount = currentFloor >= 10
            ? 0.01f
            : currentFloor == 9
                ? 0.012f
                : 0.018f;
        float scaleYAmount = currentFloor >= 10
            ? 0.012f
            : currentFloor == 9
                ? 0.015f
                : 0.02f;
        float yAmount = currentFloor >= 10
            ? 1f
            : currentFloor == 9
                ? 1.25f
                : 1.5f;

        monsterIdleElapsed =
            (monsterIdleElapsed + Time.deltaTime) % cycleDuration;

        float phase = monsterIdleElapsed / cycleDuration;
        float wave = Mathf.Sin(phase * Mathf.PI * 2f);

        slimeVisual.anchoredPosition =
            monsterIdleBasePosition + new Vector2(0f, -wave * yAmount);
        slimeVisual.localScale = Vector3.Scale(
            monsterIdleBaseScale,
            new Vector3(
                1f + (wave * scaleXAmount),
                1f - (wave * scaleYAmount),
                1f
            )
        );
    }

    void UpdateGroundShadows()
    {
        if (heroShadow != null && playerVisual != null && heroIdleInitialized)
        {
            float heroDeltaX =
                playerVisual.anchoredPosition.x - heroIdleBasePosition.x;
            float heroLift = Mathf.Clamp01(
                (playerVisual.anchoredPosition.y - heroIdleBasePosition.y) /
                HeroIdleYOffset
            );
            float heroShadowScale = Mathf.Lerp(1f, 0.97f, heroLift);

            heroShadow.anchoredPosition =
                heroShadowBasePosition + new Vector2(heroDeltaX, 0f);
            heroShadow.localScale =
                heroShadowBaseScale * heroShadowScale;
        }

        if (monsterShadow == null ||
            slimeVisual == null ||
            !monsterIdleInitialized)
        {
            return;
        }

        float monsterDeltaX =
            slimeVisual.anchoredPosition.x - monsterIdleBasePosition.x;
        monsterShadow.anchoredPosition =
            monsterShadowBasePosition + new Vector2(monsterDeltaX, 0f);

        float baseScaleX = Mathf.Max(monsterIdleBaseScale.x, 0.0001f);
        float currentScaleRatio = slimeVisual.localScale.x / baseScaleX;

        if (slimeHp <= 0)
        {
            float deathScale = Mathf.Clamp01(currentScaleRatio);
            monsterShadow.localScale =
                monsterShadowBaseScale * deathScale;
            return;
        }

        float shadowScaleX = Mathf.Clamp(
            1f + ((currentScaleRatio - 1f) * 0.6f),
            0.9f,
            1.1f
        );
        float shadowScaleY = Mathf.Clamp(
            1f - ((currentScaleRatio - 1f) * 0.25f),
            0.95f,
            1.05f
        );

        monsterShadow.localScale = Vector3.Scale(
            monsterShadowBaseScale,
            new Vector3(shadowScaleX, shadowScaleY, 1f)
        );
    }

    void FindUI()
    {
        playerHpText = GameObject.Find("PlayerHP/HPText")?.GetComponent<TMP_Text>();
        slimeHpText = GameObject.Find("MonsterHP/HPText")?.GetComponent<TMP_Text>();

        enemyWordText = GameObject.Find("EnemyWord")?.GetComponent<TMP_Text>();
        chainHintText = GameObject.Find("ChainHint")?.GetComponent<TMP_Text>();

        levelText = GameObject.Find("LevelText")?.GetComponent<TMP_Text>();
        playerNameText = GameObject.Find("PlayerName")?.GetComponent<TMP_Text>();
        expText = GameObject.Find("ExpText")?.GetComponent<TMP_Text>();
        goldText = GameObject.Find("GoldText")?.GetComponent<TMP_Text>();

        wordInput = GameObject.Find("WordInput")?.GetComponent<TMP_InputField>();
        attackButton = GameObject.Find("AttackButton")?.GetComponent<Button>();

        playerHpFill = GameObject.Find("PlayerHP/Fill")?.GetComponent<Image>();
        slimeHpFill = GameObject.Find("MonsterHP/Fill")?.GetComponent<Image>();

        playerVisual = GameObject.Find("PlayerPlaceholder")?.GetComponent<RectTransform>();
        slimeVisual = GameObject.Find("SlimePlaceholder")?.GetComponent<RectTransform>();
        heroShadow = GameObject.Find("HeroShadow")?.GetComponent<RectTransform>();
        monsterShadow = GameObject.Find("MonsterShadow")?.GetComponent<RectTransform>();

        if (heroShadow != null)
        {
            heroShadowBasePosition = heroShadow.anchoredPosition;
            heroShadowBaseScale = heroShadow.localScale;
        }

        if (monsterShadow != null)
        {
            monsterShadowBasePosition = monsterShadow.anchoredPosition;
            monsterShadowBaseScale = monsterShadow.localScale;
            monsterShadowImage = monsterShadow.GetComponent<Image>();

            if (monsterShadowImage != null)
                monsterShadowBaseColor = monsterShadowImage.color;
        }

        if (playerVisual != null)
        {
            playerBodyImage = playerVisual.Find("Body")?.GetComponent<Image>();
            weaponImage = playerVisual.Find("Weapon")?.GetComponent<Image>();
            heroIdleBasePosition = playerVisual.anchoredPosition;
            heroIdleBaseScale = playerVisual.localScale;
            heroIdleInitialized = true;
        }

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
        Transform battleCanvasTransform = GameObject.Find("BattleCanvas")?.transform;
        battleCanvas = battleCanvasTransform?.GetComponent<Canvas>();
        wordBattlePanel = battleCanvasTransform
            ?.Find("WordBattlePanel")
            ?.GetComponent<RectTransform>();

        if (wordBattlePanel != null)
        {
            wordBattlePanelOriginalPosition = wordBattlePanel.anchoredPosition;
            wordBattlePanelPositionInitialized = true;
        }

        if (battleCanvasTransform != null)
        {
            Transform impactTransform = battleCanvasTransform.Find("ImpactEffect");

            if (impactTransform != null)
            {
                impactEffect =
                    impactTransform.GetComponent<RectTransform>();
            }
        }

        if (battleCanvasTransform != null)
        {
            Transform criticalImpactTransform =
                battleCanvasTransform.Find("CriticalImpactEffect");

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

        if (battleCanvasTransform != null)
        {
            Transform criticalTextTransform = battleCanvasTransform.Find("CriticalText");

            if (criticalTextTransform != null)
            {
                criticalText = criticalTextTransform.GetComponent<TMP_Text>();
                criticalText.gameObject.SetActive(false);
            }
        }

        if (battleCanvasTransform != null)
        {
            Transform levelUpTextTransform = battleCanvasTransform.Find("LevelUpText");

            if (levelUpTextTransform != null)
            {
                levelUpText = levelUpTextTransform.GetComponent<TMP_Text>();
                levelUpText.gameObject.SetActive(false);
            }
        }

        if (battleCanvasTransform != null)
            FindShopUI(battleCanvasTransform);

        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButtonClicked);

        if (wordInput != null)
        {
            wordInput.onSubmit.AddListener(_ => OnAttackButtonClicked());
            wordInput.onDeselect.AddListener(OnWordInputDeselected);
        }

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

    void FindShopUI(Transform battleCanvas)
    {
        shopButton = battleCanvas.Find("ShopButton")?.GetComponent<Button>();
        shopPanel = battleCanvas.Find("ShopPanel")?.gameObject;

        Transform shopPanelTransform = shopPanel != null
            ? shopPanel.transform
            : null;

        if (shopPanelTransform != null)
        {
            shopCurrentGoldText = shopPanelTransform
                .Find("ShopCurrentGold")?.GetComponent<TMP_Text>();
            shopMessageText = shopPanelTransform
                .Find("ShopMessage")?.GetComponent<TMP_Text>();
            shopItemListContent = shopPanelTransform
                .Find("ShopItemListContent")?.GetComponent<RectTransform>();
            shopCloseButton = shopPanelTransform
                .Find("ShopCloseButton")?.GetComponent<Button>();
            shopWeaponTabButton = shopPanelTransform
                .Find("ShopTabWeapon")?.GetComponent<Button>();
            shopArmorTabButton = shopPanelTransform
                .Find("ShopTabArmor")?.GetComponent<Button>();
            shopAccessoryTabButton = shopPanelTransform
                .Find("ShopTabAccessory")?.GetComponent<Button>();
            shopEtcTabButton = shopPanelTransform
                .Find("ShopTabEtc")?.GetComponent<Button>();
        }

        if (shopButton != null)
            shopButton.onClick.AddListener(OpenShop);

        if (shopCloseButton != null)
            shopCloseButton.onClick.AddListener(CloseShop);

        if (shopWeaponTabButton != null)
            shopWeaponTabButton.onClick.AddListener(
                () => SelectShopTab(ItemType.Weapon)
            );

        if (shopArmorTabButton != null)
            shopArmorTabButton.onClick.AddListener(
                () => SelectShopTab(ItemType.Armor)
            );

        if (shopAccessoryTabButton != null)
            shopAccessoryTabButton.interactable = false;

        if (shopEtcTabButton != null)
            shopEtcTabButton.interactable = false;
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
            RequestWordInputFocus();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RefreshFloorDebugUI();
#endif
    }

    IEnumerator LoadFloorAndMonsterDataLists()
    {
        string floorJson = null;
        string monsterJson = null;
        string floorLoadError = null;
        string monsterLoadError = null;

        yield return RuntimeDataLoader.LoadDataText(
            "Data/Floors/floors.json",
            text => floorJson = text,
            error => floorLoadError = error
        );

        yield return RuntimeDataLoader.LoadDataText(
            "Data/Monsters/monsters.json",
            text => monsterJson = text,
            error => monsterLoadError = error
        );

        if (!string.IsNullOrEmpty(floorLoadError))
        {
            Debug.LogError("floors.json load failed: " + floorLoadError);
            floorDataList = null;
        }
        else
        {
            floorDataList = JsonUtility.FromJson<FloorDataList>(floorJson);
        }

        if (!string.IsNullOrEmpty(monsterLoadError))
        {
            Debug.LogError("monsters.json load failed: " + monsterLoadError);
            monsterDataList = null;
        }
        else
        {
            monsterDataList =
                JsonUtility.FromJson<MonsterDataList>(monsterJson);
        }
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
        PauseHeroIdle();
        PauseMonsterIdle();
        ResetMonsterHitFlash();
        saveService?.DeleteSave();

        playerProgress.Reset();
        currentFloor = 1;
        highestFloor = 1;
        ValidateStartingItems();

        isShopOpen = false;
        if (shopPanel != null)
            shopPanel.SetActive(false);

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
        ApplyEquipmentVisuals();

        if (shopPanel != null)
            RefreshShopUI("");

        Debug.Log("Save 데이터 초기화 완료");
    }

    void ValidateStartingItems()
    {
        if (itemService == null)
            return;

        bool correctedWeapon = ValidateEquippedItem(
            playerProgress.EquippedWeaponId,
            PlayerProgressData.DefaultWeaponId,
            ItemType.Weapon
        );
        bool correctedArmor = ValidateEquippedItem(
            playerProgress.EquippedArmorId,
            PlayerProgressData.DefaultArmorId,
            ItemType.Armor
        );

        if (correctedWeapon || correctedArmor)
            SaveGame();
    }

    bool ValidateEquippedItem(
        string equippedItemId,
        string fallbackItemId,
        ItemType expectedType
    )
    {
        ItemData equippedItem = itemService.GetItem(equippedItemId);

        if (equippedItem != null &&
            equippedItem.GetItemType() == expectedType &&
            playerProgress.OwnsItem(equippedItemId))
        {
            return false;
        }

        ItemData fallbackItem = itemService.GetItem(fallbackItemId);

        if (fallbackItem == null ||
            fallbackItem.GetItemType() != expectedType)
        {
            Debug.LogError(
                $"기본 장비 데이터를 찾을 수 없습니다: {fallbackItemId}"
            );
            return false;
        }

        playerProgress.EnsureOwnedItem(fallbackItemId);
        playerProgress.TryEquipItem(fallbackItem);

        Debug.LogWarning(
            $"잘못된 장착 ID를 기본 장비로 복구했습니다: " +
            $"{equippedItemId} -> {fallbackItemId}"
        );
        return true;
    }

    bool CanOpenShop()
    {
        return !battleEnded &&
            !isShopOpen &&
            wordInput != null &&
            attackButton != null &&
            wordInput.interactable &&
            attackButton.interactable &&
            (victoryPanel == null || !victoryPanel.activeSelf);
    }

    void OpenShop()
    {
        if (!CanOpenShop())
            return;

        CloseMobileKeyboardAndRestorePanel();
        isShopOpen = true;

        if (shopPanel != null)
            shopPanel.SetActive(true);

        if (wordInput != null)
            wordInput.interactable = false;

        if (attackButton != null)
            attackButton.interactable = false;

        UpdateShopButtonState();
        RefreshShopUI("");
    }

    void CloseShop()
    {
        isShopOpen = false;

        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (!battleEnded)
        {
            if (wordInput != null)
            {
                wordInput.interactable = true;
                RequestWordInputFocus();
            }

            if (attackButton != null)
                attackButton.interactable = true;
        }

        UpdateShopButtonState();
    }

    void SelectShopTab(ItemType itemType)
    {
        currentShopTab = itemType;
        RefreshShopUI("");
    }

    void RefreshShopUI(string message)
    {
        if (shopCurrentGoldText != null)
            shopCurrentGoldText.text = $"GOLD {gold}";

        if (shopMessageText != null)
            shopMessageText.text = message;

        ClearShopItemList();

        if (shopItemListContent == null || itemService == null)
            return;

        List<ItemData> items = itemService.GetItemsByType(currentShopTab);

        if (items.Count == 0)
        {
            CreateShopText(
                shopItemListContent,
                "ShopEmptyText",
                "표시할 아이템이 없습니다.",
                28,
                FontStyles.Bold,
                new Vector2(0.5f, 0.5f),
                new Vector2(700f, 80f)
            );

            return;
        }

        for (int i = 0; i < items.Count; i++)
            CreateShopItemRow(items[i], i);
    }

    void ClearShopItemList()
    {
        if (shopItemListContent == null)
            return;

        for (int i = shopItemListContent.childCount - 1; i >= 0; i--)
            Destroy(shopItemListContent.GetChild(i).gameObject);
    }

    void CreateShopItemRow(ItemData item, int index)
    {
        float y = 0.86f - (index * 0.22f);

        GameObject row = CreateShopPanel(
            shopItemListContent,
            $"ShopItem_{item.id}",
            new Color(0.16f, 0.18f, 0.25f, 0.95f),
            new Vector2(0.5f, y),
            new Vector2(760f, 125f)
        );

        string statText = GetItemStatText(item);
        CreateShopText(
            row.transform,
            "Name",
            item.name,
            28,
            FontStyles.Bold,
            new Vector2(0.18f, 0.66f),
            new Vector2(230f, 45f)
        );

        CreateShopText(
            row.transform,
            "Description",
            item.description,
            20,
            FontStyles.Normal,
            new Vector2(0.33f, 0.29f),
            new Vector2(430f, 50f)
        );

        CreateShopText(
            row.transform,
            "Stat",
            statText,
            22,
            FontStyles.Bold,
            new Vector2(0.51f, 0.66f),
            new Vector2(180f, 45f)
        );

        bool ownsItem = playerProgress.OwnsItem(item.id);

        if (ownsItem)
        {
            bool isEquipped = IsItemEquipped(item);
            Button equipButton = CreateShopButton(
                row.transform,
                "EquipButton",
                isEquipped ? "장착 중" : "장착",
                new Vector2(0.82f, 0.50f),
                new Vector2(210f, 70f)
            );

            equipButton.interactable = !isEquipped;

            if (!isEquipped)
                equipButton.onClick.AddListener(
                    () => TryEquipItem(item.id)
                );

            return;
        }

        Button buyButton = CreateShopButton(
            row.transform,
            "BuyButton",
            $"구매 {item.price}G",
            new Vector2(0.82f, 0.50f),
            new Vector2(210f, 70f)
        );

        buyButton.onClick.AddListener(() => TryBuyItem(item.id));
    }

    string GetItemStatText(ItemData item)
    {
        ItemType itemType = item.GetItemType();

        if (itemType == ItemType.Weapon)
            return $"공격력 +{item.attackBonus}";

        if (itemType == ItemType.Armor)
            return $"받는 피해 {Mathf.RoundToInt(item.defenseRate * 100f)}% 감소";

        return "";
    }

    bool IsItemEquipped(ItemData item)
    {
        if (item == null)
            return false;

        if (item.GetItemType() == ItemType.Weapon)
            return item.id == playerProgress.EquippedWeaponId;

        if (item.GetItemType() == ItemType.Armor)
            return item.id == playerProgress.EquippedArmorId;

        return false;
    }

    void TryEquipItem(string itemId)
    {
        ItemData item = itemService.GetItem(itemId);

        if (item == null)
        {
            RefreshShopUI("아이템을 찾을 수 없습니다.");
            return;
        }

        if (!playerProgress.OwnsItem(item.id))
        {
            RefreshShopUI("보유하지 않은 아이템은 장착할 수 없습니다.");
            return;
        }

        if (!playerProgress.TryEquipItem(item))
        {
            RefreshShopUI("장착할 수 없는 아이템입니다.");
            return;
        }

        SaveGame();
        ApplyEquipmentVisuals();
        RefreshShopUI($"{item.name} 장착 완료!");
    }

    void ApplyEquipmentVisuals()
    {
        ApplyEquipmentSprite(
            playerProgress.EquippedWeaponId,
            ItemType.Weapon,
            playerBodyImage: null,
            equipmentImage: weaponImage
        );
        ApplyEquipmentSprite(
            playerProgress.EquippedArmorId,
            ItemType.Armor,
            playerBodyImage: playerBodyImage,
            equipmentImage: null
        );
    }

    void ApplyEquipmentSprite(
        string itemId,
        ItemType expectedType,
        Image playerBodyImage,
        Image equipmentImage
    )
    {
        ItemData item = itemService?.GetItem(itemId);
        Image targetImage = expectedType == ItemType.Weapon
            ? equipmentImage
            : playerBodyImage;

        if (item == null ||
            item.GetItemType() != expectedType ||
            targetImage == null)
        {
            return;
        }

        string spritePath = expectedType == ItemType.Weapon
            ? item.spritePath
            : item.characterSpritePath;

        if (string.IsNullOrEmpty(spritePath))
        {
            Debug.LogWarning($"장비 이미지 경로가 비어 있습니다: {item.id}");
            return;
        }

        Sprite equipmentSprite = LoadRuntimeSprite(spritePath);
        if (equipmentSprite == null)
        {
            Debug.LogWarning(
                $"장비 이미지를 찾을 수 없습니다: {spritePath}"
            );
            return;
        }

        targetImage.sprite = equipmentSprite;
        targetImage.color = Color.white;
        targetImage.preserveAspect = true;
    }

    void PauseHeroIdle()
    {
        heroIdlePaused = true;
        ResetHeroIdleTransform();
    }

    void ResumeHeroIdle()
    {
        if (!heroIdleInitialized || battleEnded)
            return;

        ResetHeroIdleTransform();
        heroIdleElapsed = 0f;
        heroIdlePaused = false;
    }

    void ResetHeroIdleTransform()
    {
        if (!heroIdleInitialized || playerVisual == null)
            return;

        playerVisual.anchoredPosition = heroIdleBasePosition;
        playerVisual.localScale = heroIdleBaseScale;
    }

    void PauseMonsterIdle()
    {
        monsterIdlePaused = true;
        ResetMonsterIdleTransform();
    }

    void ResumeMonsterIdle()
    {
        if (!monsterIdleInitialized || battleEnded || slimeHp <= 0)
            return;

        ResetMonsterIdleTransform();
        monsterIdleElapsed = 0f;
        monsterIdlePaused = false;
    }

    void ResetMonsterIdleTransform()
    {
        if (!monsterIdleInitialized || slimeVisual == null)
            return;

        slimeVisual.anchoredPosition = monsterIdleBasePosition;
        slimeVisual.localScale = monsterIdleBaseScale;
        slimeVisual.localRotation = Quaternion.identity;
    }

    void ResetGroundShadows()
    {
        if (heroShadow != null)
        {
            heroShadow.anchoredPosition = heroShadowBasePosition;
            heroShadow.localScale = heroShadowBaseScale;
        }

        if (monsterShadow != null)
        {
            monsterShadow.anchoredPosition = monsterShadowBasePosition;
            monsterShadow.localScale = monsterShadowBaseScale;
        }

        if (monsterShadowImage != null)
            monsterShadowImage.color = monsterShadowBaseColor;
    }

    void TryBuyItem(string itemId)
    {
        ItemData item = itemService.GetItem(itemId);

        if (item == null)
        {
            RefreshShopUI("아이템을 찾을 수 없습니다.");
            return;
        }

        if (playerProgress.OwnsItem(item.id))
        {
            RefreshShopUI("이미 보유한 아이템입니다.");
            return;
        }

        if (!playerProgress.TrySpendGold(item.price))
        {
            RefreshShopUI("Gold가 부족합니다.");
            return;
        }

        playerProgress.AddOwnedItem(item.id);
        SaveGame();
        UpdateUI();
        RefreshShopUI($"{item.name} 구매 완료!");
    }

    void UpdateShopButtonState()
    {
        if (shopButton != null)
            shopButton.interactable = CanOpenShop();
    }

    GameObject CreateShopPanel(
        Transform parent,
        string name,
        Color color,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject obj = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );

        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        obj.GetComponent<Image>().color = color;

        return obj;
    }

    TMP_Text CreateShopText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        FontStyles style,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject obj = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.font = koreanFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        return text;
    }

    Button CreateShopButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject obj = CreateShopPanel(
            parent,
            name,
            new Color(0.90f, 0.32f, 0.18f),
            anchor,
            size
        );

        Button button = obj.AddComponent<Button>();

        CreateShopText(
            obj.transform,
            "Label",
            label,
            22,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            size
        );

        return button;
    }

    public void OnAttackButtonClicked()
    {
        if (battleEnded || isShopOpen || wordInput == null)
            return;

        string inputWord = wordInput.text.Trim();

        if (string.IsNullOrEmpty(inputWord))
        {
            RequestWordInputFocus(true);
            return;
        }


        // ========================================
        // 1. 끝말잇기 규칙 검사
        // ========================================

        char requiredChar = currentWord[currentWord.Length - 1];

        if (inputWord[0] != requiredChar)
        {
            chainHintText.text =
                $"'{requiredChar}'로 시작해야 합니다!";

            wordInput.text = "";
            RequestWordInputFocus(true);

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
            RequestWordInputFocus(true);

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
            RequestWordInputFocus(true);

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
        PauseHeroIdle();
        CloseMobileKeyboardAndRestorePanel();

        // 공격 중에는 입력 방지
        wordInput.text = "";
        wordInput.interactable = false;
        attackButton.interactable = false;
        UpdateShopButtonState();

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
            UpdateShopButtonState();

            wordInput.text = "";
            RequestWordInputFocus();

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
        UpdateShopButtonState();

        RequestWordInputFocus();
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

        PauseMonsterIdle();
        PauseHeroIdle();

        Vector2 originalSlimePos = slimeVisual.anchoredPosition;
        Vector3 attackBaseScale = monsterIdleBaseScale;

        yield return StartCoroutine(MonsterAttackAnticipation());

        Vector2 anticipationPosition = slimeVisual.anchoredPosition;
        Vector3 anticipationScale = slimeVisual.localScale;

        // -------------------------
        // 1. 용사 쪽으로 돌진
        // 슬라임은 오른쪽에 있으므로 왼쪽(-X)으로 이동
        // -------------------------
        Vector2 attackPosition =
            originalSlimePos + new Vector2(-85f, 0f);

        float rushDuration = 0.13f;
        float scaleRecoveryDuration = 0.04f;
        float elapsed = 0f;

        while (elapsed < rushDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / rushDuration;

            slimeVisual.anchoredPosition =
                Vector2.Lerp(
                    anticipationPosition,
                    attackPosition,
                    t
                );
            slimeVisual.localScale = Vector3.Lerp(
                anticipationScale,
                attackBaseScale,
                Mathf.Clamp01(elapsed / scaleRecoveryDuration)
            );

            yield return null;
        }

        slimeVisual.anchoredPosition = attackPosition;
        slimeVisual.localScale = attackBaseScale;

        PlaySfx(SfxId.MonsterAttack);

        // -------------------------
        // 2. 실제 데미지 적용
        // -------------------------
        int finalDamage = CalculateIncomingDamage(attackDamage);

        playerHp -= finalDamage;
        playerHp = Mathf.Max(playerHp, 0);

        UpdateUI();

        if (isMonsterCritical)
        {
            chainHintText.text =
                $"몬스터 크리티컬 공격! {finalDamage} 데미지!";
        }
        else
        {
            chainHintText.text =
                $"슬라임의 공격! {finalDamage} 데미지\n" +
                $"'{currentWord[currentWord.Length - 1]}'로 시작하는 단어를 입력하세요!";
        }

        // Critical은 CRITICAL! 연출이 거의 끝난 뒤 데미지 숫자 표시
        if (isMonsterCritical)
        {
            PlaySfx(SfxId.Critical);
            StartCoroutine(CriticalImpactEffect());
            StartCoroutine(CriticalTextEffect());
            StartCoroutine(
                ShowDamageTextAfterDelay(
                    playerVisual,
                    finalDamage,
                    CriticalDamageTextDelay
                )
            );
        }
        else
        {
            ShowDamageText(playerVisual, finalDamage);
        }

        // 플레이어는 왼쪽으로 밀리며 피격
        if (playerVisual != null)
            StartCoroutine(PlayerHitEffect());

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
        ResumeMonsterIdle();
    }

    IEnumerator MonsterAttackAnticipation()
    {
        PlaySfx(SfxId.MonsterSquash);

        float duration = currentFloor >= 10
            ? 0.18f
            : currentFloor == 9
                ? 0.14f
                : 0.12f;
        float scaleX = currentFloor >= 10
            ? 1.03f
            : currentFloor == 9
                ? 1.04f
                : 1.05f;
        float scaleY = currentFloor >= 10
            ? 0.95f
            : currentFloor == 9
                ? 0.94f
                : 0.92f;
        float yOffset = currentFloor >= 10
            ? -2f
            : currentFloor == 9
                ? -2f
                : -2.5f;

        Vector2 squashPosition =
            monsterIdleBasePosition + new Vector2(0f, yOffset);
        Vector3 squashScale = Vector3.Scale(
            monsterIdleBaseScale,
            new Vector3(scaleX, scaleY, 1f)
        );
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            slimeVisual.anchoredPosition = Vector2.Lerp(
                monsterIdleBasePosition,
                squashPosition,
                t
            );
            slimeVisual.localScale = Vector3.Lerp(
                monsterIdleBaseScale,
                squashScale,
                t
            );

            yield return null;
        }

        slimeVisual.anchoredPosition = squashPosition;
        slimeVisual.localScale = squashScale;
    }

    int CalculateIncomingDamage(int rawDamage)
    {
        int calculatedDamage = Mathf.RoundToInt(
            rawDamage * (1f - equippedArmorDefenseRate)
        );

        return Mathf.Max(1, calculatedDamage);
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

    IEnumerator PlayerHitEffect()
    {
        PauseHeroIdle();
        yield return StartCoroutine(HitEffect(playerVisual));

        if (!battleEnded)
            ResumeHeroIdle();
    }

    IEnumerator MonsterHitEffect()
    {
        PauseMonsterIdle();
        StartMonsterHitFlash();
        yield return StartCoroutine(HitEffect(slimeVisual));

        if (!battleEnded && slimeHp > 0)
            ResumeMonsterIdle();
    }

    void StartMonsterHitFlash()
    {
        if (monsterImage == null)
            return;

        if (monsterHitFlashCoroutine != null)
            StopCoroutine(monsterHitFlashCoroutine);

        monsterHitFlashCoroutine = StartCoroutine(MonsterHitFlash());
    }

    IEnumerator MonsterHitFlash()
    {
        Color originalColor = monsterImage.color;
        monsterImage.color = new Color(
            originalColor.r,
            originalColor.g,
            originalColor.b,
            MonsterHitFlashAlpha
        );

        yield return new WaitForSeconds(MonsterHitFlashDuration);

        if (monsterImage != null)
            monsterImage.color = originalColor;

        monsterHitFlashCoroutine = null;
    }

    void ResetMonsterHitFlash()
    {
        if (monsterHitFlashCoroutine != null)
        {
            StopCoroutine(monsterHitFlashCoroutine);
            monsterHitFlashCoroutine = null;
        }

        if (monsterImage != null)
            monsterImage.color = Color.white;
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
        {
            ResumeHeroIdle();
            yield break;
        }

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

        PlaySfx(SfxId.HeroAttack);

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
        PlaySfx(SfxId.MonsterHit);

        if (isCriticalAttack)
        {
            PlaySfx(SfxId.Critical);
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

        // 일반 공격은 즉시, Critical은 CRITICAL! 종료 직전에 표시
        if (isCriticalAttack)
        {
            StartCoroutine(
                ShowDamageTextAfterDelay(
                    slimeVisual,
                    currentAttackDamage,
                    CriticalDamageTextDelay
                )
            );
        }
        else
        {
            ShowDamageText(slimeVisual, currentAttackDamage);
        }

        // 슬라임 피격 흔들림
        if (slimeVisual != null)
            StartCoroutine(MonsterHitEffect());


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

        ResumeHeroIdle();
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

        PauseMonsterIdle();
        ResetMonsterHitFlash();
        PlaySfx(SfxId.MonsterDeath);

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

            if (monsterShadowImage != null)
            {
                monsterShadowImage.color = new Color(
                    monsterShadowBaseColor.r,
                    monsterShadowBaseColor.g,
                    monsterShadowBaseColor.b,
                    monsterShadowBaseColor.a * (1f - t)
                );
            }

            yield return null;
        }


        // ========================================
        // 3. 완전히 숨김
        // ========================================

        slimeVisual.localScale = Vector3.zero;

        if (monsterShadowImage != null)
        {
            monsterShadowImage.color = new Color(
                monsterShadowBaseColor.r,
                monsterShadowBaseColor.g,
                monsterShadowBaseColor.b,
                0f
            );
        }
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

        yield return new WaitForSeconds(CriticalTextStartDelay);

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

    IEnumerator LevelUpRewardEffectAfterDelay(int rewardFloor)
    {
        yield return new WaitForSeconds(LevelUpRewardDelay);

        if (!battleEnded || currentFloor != rewardFloor)
            yield break;

        PlaySfx(SfxId.LevelUp);
        yield return StartCoroutine(LevelUpTextEffect());
    }

    // ========================================
    // 전투 승리 처리
    // ========================================
    void WinBattle()
    {
        PauseHeroIdle();
        monsterIdlePaused = true;
        battleEnded = true;

        // JSON 데이터 기준 보상 지급
        bool didLevelUp =
            playerProgress.AddExp(currentMonsterData.expReward);
        playerProgress.AddGold(currentMonsterData.goldReward);

        SaveGame();

        // 기존 하단 상태 UI 갱신
        UpdateUI();

        // 입력 잠금
        wordInput.interactable = false;
        attackButton.interactable = false;
        UpdateShopButtonState();

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

        PlaySfx(SfxId.Victory);

        if (didLevelUp)
        {
            StartCoroutine(
                LevelUpRewardEffectAfterDelay(currentFloor)
            );
        }
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

        PauseHeroIdle();
        PauseMonsterIdle();
        ResetMonsterHitFlash();
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
        if (floorDataList == null || floorDataList.floors == null)
            return false;

        return floorDataList.floors.Exists(f => f.floor == targetFloor);
    }

    // ========================================
    // 다음 층 전투 초기화
    // ========================================
    void ResetBattleForNextFloor()
    {
        CloseMobileKeyboardAndRestorePanel();
        PauseHeroIdle();
        PauseMonsterIdle();
        ResetMonsterHitFlash();
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

        ResetGroundShadows();

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
            RequestWordInputFocus();

        ResumeHeroIdle();
        ResumeMonsterIdle();
    }

    void LoseBattle()
    {
        PauseHeroIdle();
        PauseMonsterIdle();
        CloseMobileKeyboardAndRestorePanel();
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

        if (playerNameText != null)
            playerNameText.text = $"LV.{playerLevel} 용사";

        if (expText != null)
            expText.text = $"EXP {exp} / {requiredExp}";

        if (goldText != null)
            goldText.text = $"GOLD {gold}";

        UpdateShopButtonState();
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

    IEnumerator ShowDamageTextAfterDelay(
        RectTransform target,
        int damage,
        float delay
    )
    {
        yield return new WaitForSeconds(delay);
        ShowDamageText(target, damage);
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
        if (floorDataList == null ||
            floorDataList.floors == null ||
            monsterDataList == null ||
            monsterDataList.monsters == null)
        {
            Debug.LogError("Floor/Monster data is not loaded.");
            return;
        }


        // =========================
        // 현재 층 찾기
        // =========================

        currentFloorData =
            floorDataList.floors.Find(f => f.floor == currentFloor);

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
            monsterDataList.monsters.Find(
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

        Sprite monsterSprite = LoadRuntimeSprite(currentMonsterData.spritePath);
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
    }

    Sprite LoadRuntimeSprite(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

#if UNITY_EDITOR
        Sprite editorSprite =
            UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);

        if (editorSprite != null)
            return editorSprite;
#endif

        string resourcesPath = ToResourcesSpritePath(assetPath);

        if (string.IsNullOrEmpty(resourcesPath))
            return null;

        return Resources.Load<Sprite>(resourcesPath);
    }

    string ToResourcesSpritePath(string assetPath)
    {
        string normalizedPath = assetPath.Replace("\\", "/");

        if (normalizedPath.StartsWith("Assets/Resources/"))
        {
            normalizedPath = normalizedPath.Substring(
                "Assets/Resources/".Length
            );
        }
        else if (normalizedPath.StartsWith("Assets/Art/"))
        {
            normalizedPath = "Art/" + normalizedPath.Substring(
                "Assets/Art/".Length
            );
        }
        else
        {
            return null;
        }

        string extension = Path.GetExtension(normalizedPath);

        if (!string.IsNullOrEmpty(extension))
            normalizedPath = normalizedPath.Substring(
                0,
                normalizedPath.Length - extension.Length
            );

        return normalizedPath;
    }

    void ApplyMonsterVisualScale()
    {
        if (slimeVisual == null || currentMonsterData == null)
            return;

        float scale = currentMonsterData.visualScale > 0f
            ? currentMonsterData.visualScale
            : 1f;

        slimeVisual.localScale = Vector3.one * scale;
        monsterIdleBasePosition = slimeOriginalPosition;
        monsterIdleBaseScale = slimeVisual.localScale;
        monsterIdleElapsed = 0f;
        monsterIdleInitialized = true;

        if (monsterShadow != null)
        {
            float shadowScale = currentFloor >= 10
                ? 1.55f
                : currentFloor == 9
                    ? 1.20f
                    : 1f;

            monsterShadowBaseScale = Vector3.one * shadowScale;
            monsterShadow.localScale = monsterShadowBaseScale;
        }

        if (monsterShadowImage != null)
            monsterShadowImage.color = monsterShadowBaseColor;
    }
}
