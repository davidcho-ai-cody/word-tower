using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

public static class BattleSceneBuilder
{
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
    private const string BattleHudFramePath =
        "Assets/Art/UI/Battle/battle_hud_frame.png";
    private const string BattleScreenFramePath =
        "Assets/Art/UI/Battle/battle_screen_frame.png";
    private const string LevelUpMagicCirclePath =
        "Assets/Art/UI/Battle/levelup_magic_circle.png";

    private static readonly Color BackgroundColor = new Color(0.08f, 0.10f, 0.16f);
    private static readonly Color PanelColor = new Color(0.13f, 0.16f, 0.23f);
    private static readonly Color PlayerColor = new Color(0.25f, 0.55f, 1.00f);
    private static readonly Color SlimeColor = new Color(0.35f, 0.85f, 0.40f);
    private static readonly Color HpColor = new Color(0.20f, 0.85f, 0.35f);
    private static readonly Color ButtonColor = new Color(0.90f, 0.32f, 0.18f);
    private static readonly Color HudBackgroundColor =
        new Color(0.07f, 0.06f, 0.16f, 0.94f);
    private static readonly Color HudGoldColor =
        new Color(1.00f, 0.76f, 0.28f, 1f);
    private static readonly Color HudIvoryColor =
        new Color(0.98f, 0.94f, 0.84f, 1f);
    private static readonly Color ExpFillColor =
        new Color(0.28f, 0.86f, 0.38f, 1f);
    private static TMP_FontAsset KoreanFont;
    private static Sprite battleHudFrameSprite;
    private static Sprite battleScreenFrameSprite;
    private static Sprite levelUpMagicCircleSprite;

    [MenuItem("WordTower/Build Battle Scene")]
    public static void BuildBattleScene()
    {
        Scene battleScene = EditorSceneManager.OpenScene(
            BattleScenePath,
            OpenSceneMode.Single
        );

        KoreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/NotoSansKR-Regular SDF.asset"
        );

        ImportBattleUiSprite(BattleHudFramePath);
        ImportBattleUiSprite(BattleScreenFramePath);
        ImportBattleUiSprite(LevelUpMagicCirclePath);

        battleHudFrameSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(BattleHudFramePath);
        battleScreenFrameSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(BattleScreenFramePath);
        levelUpMagicCircleSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(LevelUpMagicCirclePath);

        if (KoreanFont == null)
        {
            Debug.LogError("한글 TMP 폰트를 찾을 수 없습니다.");
            return;
        }

        if (battleHudFrameSprite == null ||
            battleScreenFrameSprite == null ||
            levelUpMagicCircleSprite == null)
        {
            Debug.LogError(
                "Battle UI PNG Sprite 로드 실패: " +
                $"{BattleHudFramePath}, {BattleScreenFramePath}, " +
                $"{LevelUpMagicCirclePath}"
            );
            return;
        }

        ClearScene();

        CreateEventSystem();

        ClearScene();

        CreateEventSystem();

        // 전투 로직 관리자 자동 생성
        CreateBattleManager();
        CreateAudioManager();

        Canvas canvas = CreateCanvas();

        CreateFullScreenImage(
            canvas.transform,
            "Background",
            BackgroundColor
        );

        CreateText(
            canvas.transform,
            "FloorTitle",
            "마왕성 1층",
            52,
            FontStyles.Bold,
            new Vector2(0.5f, 0.94f),
            new Vector2(800, 90)
        );

        CreatePlayerArea(canvas.transform);
        CreateMonsterArea(canvas.transform);

        CreateWordBattlePanel(canvas.transform);

        CreateStatusPanel(canvas.transform);
        CreateVictoryPanel(canvas.transform); // 승리패널
        CreateShopUI(canvas.transform);
        CreateHomeButton(canvas.transform);
        CreateBattleScreenDecoration(canvas.transform);
        CreateFloorDebugPanel(canvas.transform);
        CreateLevelUpOverlay(canvas.transform);

        Selection.activeGameObject = canvas.gameObject;

        EditorUtility.SetDirty(canvas.gameObject);
        EditorSceneManager.MarkSceneDirty(battleScene);

        if (!EditorSceneManager.SaveScene(battleScene, BattleScenePath))
        {
            Debug.LogError($"BattleScene 저장 실패: {BattleScenePath}");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"WordTower Battle Scene 생성 완료: {BattleScenePath}");
    }

    private static void ClearScene()
    {
        GameObject[] roots = UnityEngine.SceneManagement
            .SceneManager
            .GetActiveScene()
            .GetRootGameObjects();

        foreach (GameObject obj in roots)
        {
            if (obj.name == "Main Camera" ||
                obj.name == "Global Light 2D" ||
                obj.name == "BattleManager" ||
                obj.name == "AudioManager")
            {
                continue;
            }

            Object.DestroyImmediate(obj);
        }
    }

    private static void CreateEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );
    }

    // ========================================
    // BattleManager 자동 생성
    // ========================================
    private static void CreateBattleManager()
    {
        // 이미 있으면 중복 생성하지 않음
        if (GameObject.Find("BattleManager") != null)
            return;

        GameObject managerObject = new GameObject("BattleManager");

        // 실제 전투 로직 스크립트 연결
        managerObject.AddComponent<BattleManager>();
    }

    private static void CreateAudioManager()
    {
        GameObject managerObject = GameObject.Find("AudioManager");

        if (managerObject == null)
            managerObject = new GameObject("AudioManager");

        AudioManager audioManager =
            managerObject.GetComponent<AudioManager>();

        if (audioManager == null)
            audioManager = managerObject.AddComponent<AudioManager>();

        Transform sfxTransform = managerObject.transform.Find(
            "SFX AudioSource"
        );
        GameObject sfxObject;

        if (sfxTransform == null)
        {
            sfxObject = new GameObject(
                "SFX AudioSource",
                typeof(AudioSource)
            );
            sfxObject.transform.SetParent(managerObject.transform, false);
        }
        else
        {
            sfxObject = sfxTransform.gameObject;
        }

        AudioSource sfxSource = sfxObject.GetComponent<AudioSource>();

        if (sfxSource == null)
            sfxSource = sfxObject.AddComponent<AudioSource>();

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        Transform bgmTransform = managerObject.transform.Find(
            "BGM AudioSource"
        );
        GameObject bgmObject;

        if (bgmTransform == null)
        {
            bgmObject = new GameObject(
                "BGM AudioSource",
                typeof(AudioSource)
            );
            bgmObject.transform.SetParent(managerObject.transform, false);
        }
        else
        {
            bgmObject = bgmTransform.gameObject;
        }

        AudioSource bgmSource = bgmObject.GetComponent<AudioSource>();

        if (bgmSource == null)
            bgmSource = bgmObject.AddComponent<AudioSource>();

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        audioManager.ConfigureSources(sfxSource, bgmSource);

        AudioClip heroAttackClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/SFX/Combat/hero_attack_01.flac"
        );
        AudioClip monsterHitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/SFX/Combat/monster_hit_01.wav"
        );
        AudioClip criticalClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/SFX/Combat/critical_01.mp3"
        );
        AudioClip monsterSquashClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/SFX/Combat/monster_squash_01.wav"
        );
        AudioClip monsterAttackClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/SFX/Combat/monster_attack_01.mp3"
        );
        AudioClip monsterDeathClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/SFX/Combat/monster_death_01.wav"
        );
        AudioClip levelUpClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/SFX/Reward/level_up_01.wav"
        );
        AudioClip victoryClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/Audio/SFX/Reward/victory_01.wav"
        );

        audioManager.ConfigureDefaultSfxClips(
            heroAttackClip,
            monsterHitClip,
            criticalClip,
            monsterSquashClip,
            monsterAttackClip,
            monsterDeathClip,
            levelUpClip,
            victoryClip
        );
        EditorUtility.SetDirty(audioManager);
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "BattleCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void CreatePlayerArea(Transform parent)
    {
        CreateText(
            parent,
            "PlayerName",
            "LV.1 용사",
            36,
            FontStyles.Bold,
            new Vector2(0.23f, 0.82f),
            new Vector2(350, 70)
        );

        CreateHpBar(
            parent,
            "PlayerHP",
            new Vector2(0.23f, 0.77f),
            "HP 100 / 100"
        );

        CreateGroundShadow(
            parent,
            "HeroShadow",
            new Vector2(0.25f, 0.505f),
            new Vector2(110f, 24f)
        );

        GameObject player = CreatePanel(
            parent,
            "PlayerPlaceholder",
            PlayerColor,
            new Vector2(0.25f, 0.59f),
            new Vector2(260, 330)
        );

        // ========================================
        // 용사 캐릭터 레이어 구성
        // ========================================

        // 기존 PlayerPlaceholder의 파란색 배경 제거
        Image playerBackground = player.GetComponent<Image>();
        playerBackground.color = Color.clear;

        // ========================================
        // 용사 캐릭터 레이어 구성
        // ========================================

        // 기본 캐릭터 외형
        GameObject bodyLayer = CreateHeroLayer(player.transform, "Body");

        // 현재는 사용하지 않지만 추후 확장용으로 유지
        CreateHeroLayer(player.transform, "Hair");
        CreateHeroLayer(player.transform, "Face");
        CreateHeroLayer(player.transform, "Armor");

        // 무기 레이어
        GameObject weaponLayer = CreateHeroLayer(player.transform, "Weapon");

        // 액세서리 레이어
        CreateHeroLayer(player.transform, "Accessory");


        // ========================================
        // 기본 용사 이미지 자동 적용
        // ========================================

        Sprite heroSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Sprites/Hero/Body/hero_beginner_01.png"
        );

        Image bodyImage = bodyLayer.GetComponent<Image>();

        if (heroSprite != null)
        {
            bodyImage.sprite = heroSprite;
            bodyImage.color = Color.white;
            bodyImage.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning("기본 용사 이미지를 찾을 수 없습니다.");
        }


        // ========================================
        // 기본 나무검 자동 적용
        // ========================================

        Sprite weaponSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Sprites/Hero/Weapon/weapon_wood_sword_01.png"
        );

        Image weaponImage = weaponLayer.GetComponent<Image>();

        if (weaponSprite != null)
        {
            weaponImage.sprite = weaponSprite;
            weaponImage.color = Color.white;
            weaponImage.preserveAspect = true;

            // 현재 화면에서 맞춘 나무검 기준 위치
            RectTransform weaponRect = weaponLayer.GetComponent<RectTransform>();

            weaponRect.anchoredPosition = new Vector2(110f, 5f);
            weaponRect.sizeDelta = new Vector2(150f, 150f);
            weaponRect.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            Debug.LogWarning("나무검 이미지를 찾을 수 없습니다.");
        }
    }

    // ========================================
    // 용사 장비/외형 레이어 생성
    // ========================================
    private static GameObject CreateHeroLayer(
        Transform parent,
        string layerName
    )
    {
        GameObject layer = new GameObject(
            layerName,
            typeof(RectTransform),
            typeof(Image)
        );

        layer.transform.SetParent(parent, false);

        RectTransform rect = layer.GetComponent<RectTransform>();

        // 모든 장비가 정확히 같은 위치에서 겹치도록 설정
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(260, 260);

        Image image = layer.GetComponent<Image>();

        // 원본 이미지 비율 유지
        image.preserveAspect = true;

        // Sprite가 없는 상태에서는 화면에 표시하지 않음
        image.color = Color.clear;

        return layer;
    }

    private static void CreateMonsterArea(Transform parent)
    {
        CreateText(
            parent,
            "MonsterName",
            "SLIME",
            36,
            FontStyles.Bold,
            new Vector2(0.77f, 0.82f),
            new Vector2(350, 70)
        );

        CreateHpBar(
            parent,
            "MonsterHP",
            new Vector2(0.77f, 0.77f),
            "HP 100 / 100"
        );

        CreateGroundShadow(
            parent,
            "MonsterShadow",
            new Vector2(0.75f, 0.52f),
            new Vector2(135f, 26f)
        );

        GameObject slime = CreatePanel(
            parent,
            "SlimePlaceholder",
            SlimeColor,
            new Vector2(0.75f, 0.59f),
            new Vector2(260, 260)
        );

        // ========================================
        // 타격 이펙트 생성
        // 평상시에는 숨겨두고 공격 순간에만 표시
        // ========================================

        GameObject impact = CreatePanel(
            parent,
            "ImpactEffect",

            // 실제 PNG를 사용하므로 배경색은 투명으로 생성
            Color.clear,

            new Vector2(0.72f, 0.59f),
            new Vector2(300f, 220f)
        );


        // ========================================
        // 기본 검격 이펙트 이미지 자동 적용
        // ========================================

        Image impactImage = impact.GetComponent<Image>();

        Sprite impactSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Sprites/Effects/Combat/impact_slash_01.png"
        );

        if (impactSprite != null)
        {
            // 실제 검격 이미지 적용
            impactImage.sprite = impactSprite;

            // 원본 색상 그대로 사용
            impactImage.color = Color.white;

            // PNG 원본 비율 유지
            impactImage.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning(
                "타격 이펙트 이미지를 찾을 수 없습니다: impact_slash_01.png"
            );
        }


        // 평상시에는 숨김
        // BattleManager가 공격 순간에 활성화한다.
        impact.SetActive(false);

        // ========================================
        // 크리티컬 타격 이펙트 생성
        // 한방단어 크리티컬 공격 시에만 표시
        // ========================================

        GameObject criticalImpact = CreatePanel(
            parent,
            "CriticalImpactEffect",

            // 실제 PNG를 사용하므로 배경색은 투명
            Color.clear,

            new Vector2(0.72f, 0.59f),
            new Vector2(380f, 300f)
        );


        // ========================================
        // 크리티컬 검격 이미지 자동 적용
        // ========================================

        Image criticalImpactImage =
            criticalImpact.GetComponent<Image>();

        Sprite criticalImpactSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/Sprites/Effects/Combat/impact_slash_critical_01.png"
            );

        if (criticalImpactSprite != null)
        {
            criticalImpactImage.sprite = criticalImpactSprite;
            criticalImpactImage.color = Color.white;
            criticalImpactImage.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning(
                "크리티컬 타격 이펙트 이미지를 찾을 수 없습니다: " +
                "impact_slash_critical_01.png"
            );
        }


        // 평상시에는 숨김
        // BattleManager가 크리티컬 공격 순간에 활성화한다.
        criticalImpact.SetActive(false);

        // ========================================
        // 크리티컬 텍스트 생성
        // 한방단어 공격 순간에만 표시
        // ========================================

        TextMeshProUGUI criticalText = CreateText(
            parent,
            "CriticalText",
            "CRITICAL!",
            72,
            FontStyles.Bold,
            new Vector2(0.72f, 0.70f),
            new Vector2(500f, 120f)
        );

        // 처음에는 숨김
        criticalText.gameObject.SetActive(false);

        // =========================
        // 슬라임 이미지 설정
        // =========================
        Image slimeImage = slime.GetComponent<Image>();

        Sprite slimeSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Sprites/Monsters/Slime/slime_green_idle_01.png"
        );

        if (slimeSprite != null)
        {
            // 실제 슬라임 이미지 적용
            slimeImage.sprite = slimeSprite;

            // 원본 이미지 색상을 그대로 표시
            slimeImage.color = Color.white;

            // 가로/세로 비율 유지
            slimeImage.preserveAspect = true;
        }
        else
        {
            Debug.LogWarning("슬라임 이미지를 찾을 수 없습니다.");
        }
    }

    private static void CreateWordBattlePanel(Transform parent)
    {
        GameObject panel = CreatePanel(
            parent,
            "WordBattlePanel",
            PanelColor,
            new Vector2(0.5f, 0.31f),
            new Vector2(950, 500)
        );

        CreateText(
            panel.transform,
            "EnemyWordLabel",
            "슬라임의 단어",
            28,
            FontStyles.Normal,
            new Vector2(0.5f, 0.82f),
            new Vector2(500, 50)
        );

        CreateText(
            panel.transform,
            "EnemyWord",
            "사과",
            60,
            FontStyles.Bold,
            new Vector2(0.5f, 0.67f),
            new Vector2(600, 100)
        );

        CreateText(
            panel.transform,
            "ChainHint",
            "『 과 』로 시작하는 단어를 입력하세요!",
            30,
            FontStyles.Bold,
            new Vector2(0.5f, 0.51f),
            new Vector2(800, 70)
        );

        TMP_InputField input = CreateInputField(
            panel.transform,
            new Vector2(0.38f, 0.27f),
            new Vector2(600, 100)
        );

        Button attackButton = CreateButton(
            panel.transform,
            "AttackButton",
            "공격!",
            new Vector2(0.82f, 0.27f),
            new Vector2(220, 100)
        );

        attackButton.interactable = true;
    }

    private static void CreateStatusPanel(Transform parent)
    {
        GameObject status = CreatePanel(
            parent,
            "StatusPanel",
            Color.clear,
            new Vector2(0.5f, 0.078f),
            new Vector2(970f, 486f)
        );
        status.GetComponent<Image>().raycastTarget = false;

        Image hudFrame = CreateSpriteImage(
            status.transform,
            "HudFrameImage",
            battleHudFrameSprite,
            new Vector2(0.5f, 0.5f),
            new Vector2(970f, 486f),
            true
        );
        hudFrame.raycastTarget = false;

        CreateText(
            status.transform,
            "LevelLabel",
            "LV.",
            30,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(110f, 38f)
        ).color = HudGoldColor;
        status.transform.Find("LevelLabel")
            .GetComponent<RectTransform>().anchoredPosition =
            new Vector2(-382f, 40f);

        CreateText(
            status.transform,
            "LevelText",
            "1",
            66,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(130f, 74f)
        ).color = HudIvoryColor;
        status.transform.Find("LevelText")
            .GetComponent<RectTransform>().anchoredPosition =
            new Vector2(-382f, -18f);

        CreateText(
            status.transform,
            "ExpLabel",
            "EXP",
            28,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(120f, 36f)
        ).color = HudGoldColor;
        status.transform.Find("ExpLabel")
            .GetComponent<RectTransform>().anchoredPosition =
            new Vector2(-130f, 48f);

        GameObject expBarBackground = CreatePanel(
            status.transform,
            "ExpBarBackground",
            new Color(0.02f, 0.015f, 0.045f, 0.68f),
            new Vector2(0.5f, 0.5f),
            new Vector2(448f, 34f)
        );
        expBarBackground.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(-54f, -2f);
        expBarBackground.GetComponent<Image>().raycastTarget = false;

        GameObject expBarFill = CreatePanel(
            expBarBackground.transform,
            "ExpBarFill",
            new Color(1f, 0.78f, 0.22f, 0.96f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0f)
        );
        RectTransform expFillRect = expBarFill.GetComponent<RectTransform>();
        expFillRect.anchorMin = new Vector2(0f, 0f);
        expFillRect.anchorMax = new Vector2(0f, 1f);
        expFillRect.pivot = new Vector2(0f, 0.5f);
        expFillRect.offsetMin = new Vector2(4f, 6f);
        expFillRect.offsetMax = new Vector2(-4f, -6f);
        Image expFillImage = expBarFill.GetComponent<Image>();
        expFillImage.type = Image.Type.Simple;
        expFillImage.fillAmount = 1f;
        expFillImage.raycastTarget = false;

        CreateText(
            status.transform,
            "ExpText",
            "0 / 100",
            25,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(380f, 36f)
        ).color = HudIvoryColor;
        status.transform.Find("ExpText")
            .GetComponent<RectTransform>().anchoredPosition =
            new Vector2(-54f, -46f);

        CreateText(
            status.transform,
            "GoldLabel",
            "GOLD",
            24,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(150f, 34f)
        ).color = HudIvoryColor;
        status.transform.Find("GoldLabel")
            .GetComponent<RectTransform>().anchoredPosition =
            new Vector2(356f, 36f);

        CreateText(
            status.transform,
            "GoldText",
            "0",
            44,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(180f, 58f)
        ).color = HudGoldColor;
        status.transform.Find("GoldText")
            .GetComponent<RectTransform>().anchoredPosition =
            new Vector2(356f, -16f);
    }

    // ========================================
    // 승리 패널 생성
    // 평상시에는 숨겨두고 승리 시 BattleManager가 표시
    // ========================================
    private static void CreateVictoryPanel(Transform parent)
    {
        GameObject panel = CreatePanel(
            parent,
            "VictoryPanel",
            new Color(0.08f, 0.10f, 0.16f, 0.96f),
            new Vector2(0.5f, 0.5f),
            new Vector2(850f, 700f)
        );

        CreateText(
            panel.transform,
            "VictoryTitle",
            "VICTORY!",
            72,
            FontStyles.Bold,
            new Vector2(0.5f, 0.82f),
            new Vector2(700f, 120f)
        );

        CreateText(
            panel.transform,
            "VictoryMonsterText",
            "초록 슬라임 처치!",
            36,
            FontStyles.Bold,
            new Vector2(0.5f, 0.64f),
            new Vector2(700f, 80f)
        );

        CreateText(
            panel.transform,
            "VictoryRewardText",
            "EXP +20\nGOLD +10",
            38,
            FontStyles.Bold,
            new Vector2(0.5f, 0.44f),
            new Vector2(600f, 150f)
        );

        Button nextFloorButton = CreateButton(
            panel.transform,
            "NextFloorButton",
            "다음 층",
            new Vector2(0.5f, 0.18f),
            new Vector2(360f, 110f)
        );

        nextFloorButton.interactable = true;

        // 평상시에는 숨김
        panel.SetActive(false);
    }

    private static void CreateShopUI(Transform parent)
    {
        Button shopButton = CreateButton(
            parent,
            "ShopButton",
            "상점",
            new Vector2(0.10f, 0.91f),
            new Vector2(160f, 70f)
        );

        shopButton.transform.Find("Label")
            .GetComponent<TMP_Text>().fontSize = 28;
    }

    private static void CreateHomeButton(Transform parent)
    {
        Button homeButton = CreateButton(
            parent,
            "HomeButton",
            "HOME",
            new Vector2(0.28f, 0.91f),
            new Vector2(160f, 70f)
        );

        homeButton.transform.Find("Label")
            .GetComponent<TMP_Text>().fontSize = 28;
    }

    private static void CreateLevelUpOverlay(Transform parent)
    {
        GameObject overlay = CreatePanel(
            parent,
            "LevelUpOverlay",
            Color.clear,
            new Vector2(0.5f, 0.5f),
            new Vector2(1080f, 1920f)
        );
        CanvasGroup overlayGroup = overlay.AddComponent<CanvasGroup>();
        overlayGroup.alpha = 0f;

        CreatePanel(
            overlay.transform,
            "DimBackground",
            new Color(0.01f, 0.005f, 0.03f, 0.62f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1080f, 1920f)
        );

        GameObject content = CreatePanel(
            overlay.transform,
            "LevelUpContent",
            Color.clear,
            new Vector2(0.5f, 0.5f),
            new Vector2(740f, 740f)
        );
        content.GetComponent<Image>().raycastTarget = false;

        Image magicCircle = CreateSpriteImage(
            content.transform,
            "MagicCircleImage",
            levelUpMagicCircleSprite,
            new Vector2(0.5f, 0.5f),
            new Vector2(740f, 740f),
            true
        );
        magicCircle.color = new Color(1f, 1f, 1f, 0.84f);
        magicCircle.raycastTarget = false;

        TextMeshProUGUI levelUpText = CreateText(
            content.transform,
            "LevelUpText",
            "LEVEL UP!",
            72,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(560f, 90f)
        );
        ConfigureLevelUpText(levelUpText, new Vector2(0f, 135f));
        levelUpText.color = new Color(1f, 0.8352941f, 0.3098039f, 1f);
        Shadow levelUpShadow = levelUpText.gameObject.AddComponent<Shadow>();
        levelUpShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        levelUpShadow.effectDistance = new Vector2(4f, -4f);

        TextMeshProUGUI newLevelText = CreateText(
            content.transform,
            "NewLevelText",
            "LV. 2",
            88,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(460f, 110f)
        );
        ConfigureLevelUpText(newLevelText, new Vector2(0f, 15f));
        newLevelText.color = HudIvoryColor;
        Shadow newLevelShadow = newLevelText.gameObject.AddComponent<Shadow>();
        newLevelShadow.effectColor = new Color(0f, 0f, 0f, 0.66f);
        newLevelShadow.effectDistance = new Vector2(3f, -3f);

        TextMeshProUGUI hpIncreaseText = CreateText(
            content.transform,
            "HpIncreaseText",
            "HP +10",
            32,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(360f, 52f)
        );
        ConfigureLevelUpText(hpIncreaseText, new Vector2(0f, -80f));
        hpIncreaseText.color = new Color(0.88f, 1f, 0.72f, 1f);
        Shadow hpShadow = hpIncreaseText.gameObject.AddComponent<Shadow>();
        hpShadow.effectColor = new Color(0f, 0f, 0f, 0.64f);
        hpShadow.effectDistance = new Vector2(2f, -2f);

        TextMeshProUGUI atkIncreaseText = CreateText(
            content.transform,
            "AtkIncreaseText",
            "ATK +2",
            32,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(360f, 52f)
        );
        ConfigureLevelUpText(atkIncreaseText, new Vector2(0f, -125f));
        atkIncreaseText.color = new Color(0.88f, 1f, 0.72f, 1f);
        Shadow atkShadow = atkIncreaseText.gameObject.AddComponent<Shadow>();
        atkShadow.effectColor = new Color(0f, 0f, 0f, 0.64f);
        atkShadow.effectDistance = new Vector2(2f, -2f);

        overlay.SetActive(false);
    }

    private static void ConfigureLevelUpText(
        TextMeshProUGUI text,
        Vector2 anchoredPosition
    )
    {
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = Vector4.zero;
    }

    private static void CreateBattleScreenDecoration(Transform parent)
    {
        Image decoration = CreateSpriteImage(
            parent,
            "BattleScreenDecoration",
            battleScreenFrameSprite,
            new Vector2(0.5f, 0.5f),
            new Vector2(1080f, 1920f),
            false
        );

        RectTransform rect = decoration.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(14f, 210f);
        rect.offsetMax = new Vector2(-14f, -14f);
        decoration.raycastTarget = false;
    }

    private static void CreateFloorDebugPanel(Transform parent)
    {
        GameObject panel = CreatePanel(
            parent,
            "FloorDebugPanel",
            new Color(0.05f, 0.06f, 0.09f, 0.88f),
            new Vector2(0.84f, 0.90f),
            new Vector2(300f, 185f)
        );

        CreateText(
            panel.transform,
            "DebugFloorText",
            "DEBUG FLOOR 1",
            22,
            FontStyles.Bold,
            new Vector2(0.5f, 0.78f),
            new Vector2(270f, 45f)
        );

        Button previousButton = CreateButton(
            panel.transform,
            "DebugPreviousFloorButton",
            "이전",
            new Vector2(0.17f, 0.45f),
            new Vector2(82f, 48f)
        );

        Button nextButton = CreateButton(
            panel.transform,
            "DebugNextFloorButton",
            "다음",
            new Vector2(0.50f, 0.45f),
            new Vector2(82f, 48f)
        );

        Button floorTenButton = CreateButton(
            panel.transform,
            "DebugFloorTenButton",
            "10층",
            new Vector2(0.83f, 0.45f),
            new Vector2(82f, 48f)
        );

        Button saveResetButton = CreateButton(
            panel.transform,
            "DebugSaveResetButton",
            "Save Reset",
            new Vector2(0.5f, 0.16f),
            new Vector2(260f, 44f)
        );

        previousButton.transform.Find("Label")
            .GetComponent<TMP_Text>().fontSize = 20;
        nextButton.transform.Find("Label")
            .GetComponent<TMP_Text>().fontSize = 20;
        floorTenButton.transform.Find("Label")
            .GetComponent<TMP_Text>().fontSize = 20;
        saveResetButton.transform.Find("Label")
            .GetComponent<TMP_Text>().fontSize = 20;
    }

    private static void CreateHpBar(
        Transform parent,
        string name,
        Vector2 anchor,
        string text
    )
    {
        GameObject background = CreatePanel(
            parent,
            name,
            new Color(0.22f, 0.22f, 0.25f),
            anchor,
            new Vector2(380, 55)
        );

        GameObject fill = CreatePanel(
            background.transform,
            "Fill",
            HpColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(360, 35)
        );

        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchorMin = new Vector2(0f, 0.5f);
        fillRect.anchorMax = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = new Vector2(10f, 0f);

        CreateText(
            background.transform,
            "HPText",
            text,
            22,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(350, 50)
        );
    }

    private static GameObject CreatePanel(
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

        Image image = obj.GetComponent<Image>();
        image.color = color;

        return obj;
    }

    private static void AddOutline(
        GameObject target,
        Color color,
        Vector2 distance
    )
    {
        Outline outline = target.GetComponent<Outline>();

        if (outline == null)
            outline = target.AddComponent<Outline>();

        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void ImportBattleUiSprite(string path)
    {
        AssetDatabase.ImportAsset(path);

        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"Battle UI PNG Importer를 찾을 수 없습니다: {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static Image CreateSpriteImage(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 anchor,
        Vector2 size,
        bool preserveAspect
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

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;

        return image;
    }

    private static void CreateGroundShadow(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject shadow = CreatePanel(
            parent,
            name,
            new Color(0f, 0f, 0f, 0.22f),
            anchor,
            size
        );

        Image shadowImage = shadow.GetComponent<Image>();
        shadowImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/Knob.psd"
        );
        shadowImage.preserveAspect = false;
        shadowImage.raycastTarget = false;
    }

    private static void CreateFullScreenImage(
        Transform parent,
        string name,
        Color color
    )
    {
        GameObject obj = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );

        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        obj.GetComponent<Image>().color = color;

        obj.transform.SetAsFirstSibling();
    }

    private static TextMeshProUGUI CreateText(
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

        text.font = KoreanFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        return text;
    }

    private static TMP_InputField CreateInputField(
        Transform parent,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject root = CreatePanel(
            parent,
            "WordInput",
            Color.white,
            anchor,
            size
        );

        TMP_InputField input = root.AddComponent<TMP_InputField>();

        TextMeshProUGUI text = CreateText(
            root.transform,
            "Text",
            "",
            38,
            FontStyles.Normal,
            new Vector2(0.5f, 0.5f),
            new Vector2(size.x - 50, size.y - 20)
        );

        text.color = Color.black;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        TextMeshProUGUI placeholder = CreateText(
            root.transform,
            "Placeholder",
            "단어 입력",
            34,
            FontStyles.Normal,
            new Vector2(0.5f, 0.5f),
            new Vector2(size.x - 50, size.y - 20)
        );

        placeholder.color = new Color(0.45f, 0.45f, 0.45f);

        input.textComponent = text;
        input.placeholder = placeholder;

        return input;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject root = CreatePanel(
            parent,
            name,
            ButtonColor,
            anchor,
            size
        );

        Button button = root.AddComponent<Button>();

        CreateText(
            root.transform,
            "Label",
            label,
            36,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            size
        );

        return button;
    }
}
