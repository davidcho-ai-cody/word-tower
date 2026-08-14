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
            "Assets/Fonts/NotoSansKR-VF SDF.asset"
        );

        if (KoreanFont == null)
        {
            Debug.LogError("한글 TMP 폰트를 찾을 수 없습니다.");
            return;
        }

        ClearScene();

        CreateEventSystem();

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
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystem = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );
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

        CreateText(
            player.transform,
            "PlayerLabel",
            "용사\n이미지 자리",
            32,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(220, 120)
        );
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

        CreateText(
            slime.transform,
            "SlimeLabel",
            "슬라임\n이미지 자리",
            32,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(220, 120)
        );
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