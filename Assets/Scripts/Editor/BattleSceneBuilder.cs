using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class BattleSceneBuilder
{
    private static readonly Color BackgroundColor = new Color(0.08f, 0.10f, 0.16f);
    private static readonly Color PanelColor = new Color(0.13f, 0.16f, 0.23f);
    private static readonly Color PlayerColor = new Color(0.25f, 0.55f, 1.00f);
    private static readonly Color SlimeColor = new Color(0.35f, 0.85f, 0.40f);
    private static readonly Color HpColor = new Color(0.20f, 0.85f, 0.35f);
    private static readonly Color ButtonColor = new Color(0.90f, 0.32f, 0.18f);
    private static TMP_FontAsset KoreanFont;

    [MenuItem("WordTower/Build Battle Scene")]
    public static void BuildBattleScene()
    {

        KoreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/NotoSansKR-Regular SDF.asset"
        );

        if (KoreanFont == null)
        {
            Debug.LogError("한글 TMP 폰트를 찾을 수 없습니다.");
            return;
        }

        ClearScene();

        CreateEventSystem();

        ClearScene();

        CreateEventSystem();

        // 전투 로직 관리자 자동 생성
        CreateBattleManager();

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
        CreateLevelUpText(canvas.transform);
        CreateFloorDebugPanel(canvas.transform);

        Selection.activeGameObject = canvas.gameObject;

        EditorUtility.SetDirty(canvas.gameObject);

        Debug.Log("WordTower Battle Scene 생성 완료!");
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
                obj.name == "BattleManager")
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
            new Color(0.10f, 0.12f, 0.18f),
            new Vector2(0.5f, 0.075f),
            new Vector2(950, 150)
        );

        CreateText(
            status.transform,
            "LevelText",
            "LV.1",
            32,
            FontStyles.Bold,
            new Vector2(0.12f, 0.65f),
            new Vector2(180, 50)
        );

        CreateText(
            status.transform,
            "ExpText",
            "EXP 0 / 100",
            28,
            FontStyles.Normal,
            new Vector2(0.44f, 0.65f),
            new Vector2(350, 50)
        );

        CreateText(
            status.transform,
            "GoldText",
            "GOLD 0",
            28,
            FontStyles.Bold,
            new Vector2(0.80f, 0.65f),
            new Vector2(250, 50)
        );
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

        GameObject panel = CreatePanel(
            parent,
            "ShopPanel",
            new Color(0.06f, 0.07f, 0.10f, 0.96f),
            new Vector2(0.5f, 0.5f),
            new Vector2(900f, 1120f)
        );

        CreateText(
            panel.transform,
            "ShopTitle",
            "SHOP",
            58,
            FontStyles.Bold,
            new Vector2(0.5f, 0.92f),
            new Vector2(500f, 90f)
        );

        Button closeButton = CreateButton(
            panel.transform,
            "ShopCloseButton",
            "닫기",
            new Vector2(0.87f, 0.92f),
            new Vector2(150f, 65f)
        );

        closeButton.transform.Find("Label")
            .GetComponent<TMP_Text>().fontSize = 24;

        CreateText(
            panel.transform,
            "ShopCurrentGold",
            "GOLD 0",
            32,
            FontStyles.Bold,
            new Vector2(0.18f, 0.84f),
            new Vector2(260f, 60f)
        );

        CreateText(
            panel.transform,
            "ShopMessage",
            "",
            26,
            FontStyles.Bold,
            new Vector2(0.62f, 0.84f),
            new Vector2(460f, 60f)
        );

        CreateShopTabButton(panel.transform, "ShopTabWeapon", "Weapon", 0.16f);
        CreateShopTabButton(panel.transform, "ShopTabArmor", "Armor", 0.38f);
        CreateShopTabButton(panel.transform, "ShopTabAccessory", "Accessory", 0.62f);
        CreateShopTabButton(panel.transform, "ShopTabEtc", "Etc", 0.84f);

        CreatePanel(
            panel.transform,
            "ShopItemListContent",
            new Color(0.10f, 0.12f, 0.18f, 0.80f),
            new Vector2(0.5f, 0.42f),
            new Vector2(820f, 680f)
        );

        panel.SetActive(false);
    }

    private static void CreateShopTabButton(
        Transform parent,
        string name,
        string label,
        float anchorX
    )
    {
        Button button = CreateButton(
            parent,
            name,
            label,
            new Vector2(anchorX, 0.75f),
            new Vector2(180f, 60f)
        );

        button.transform.Find("Label")
            .GetComponent<TMP_Text>().fontSize = 22;
    }

    private static void CreateLevelUpText(Transform parent)
    {
        TextMeshProUGUI levelUpText = CreateText(
            parent,
            "LevelUpText",
            "LEVEL UP!",
            76,
            FontStyles.Bold,
            new Vector2(0.5f, 0.58f),
            new Vector2(700f, 180f)
        );

        levelUpText.color = new Color(1f, 0.82f, 0.20f, 1f);
        levelUpText.gameObject.SetActive(false);
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
