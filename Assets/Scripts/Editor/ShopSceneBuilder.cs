using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ShopSceneBuilder
{
    private const string StudioSplashScenePath =
        "Assets/Scenes/StudioSplashScene.unity";
    private const string OpeningScenePath = "Assets/Scenes/OpeningScene.unity";
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string StoryScenePath = "Assets/Scenes/StoryScene.unity";
    private const string StoryPlaybackScenePath =
        "Assets/Scenes/StoryPlaybackScene.unity";
    private const string ShopScenePath = "Assets/Scenes/ShopScene.unity";
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
    private const string KoreanFontPath =
        "Assets/Fonts/NotoSansKR-Regular SDF.asset";
    private const string ShopBackgroundPath =
        "Assets/Art/UI/Shop/shop_background.png";
    private const string ShopTabActivePath =
        "Assets/Art/UI/Shop/shop_tab_active.png";
    private const string ShopTabInactivePath =
        "Assets/Art/UI/Shop/shop_tab_inactive.png";
    private const string ShopItemCardPath =
        "Assets/Art/UI/Shop/shop_item_card.png";
    private const string ShopItemCardEquippedPath =
        "Assets/Art/UI/Shop/shop_item_card_equipped.png";
    private const string ShopBuyButtonPath =
        "Assets/Art/UI/Shop/shop_buy_button.png";

    private static readonly Color CameraBackgroundColor =
        new Color(0.025f, 0.02f, 0.04f, 1f);
    private static readonly Color GoldColor =
        new Color(1f, 0.78f, 0.32f, 1f);
    private static readonly Color IvoryColor =
        new Color(0.98f, 0.94f, 0.84f, 1f);
    private static readonly Color BrightLabelColor =
        new Color(0.97f, 0.91f, 0.75f, 1f);
    private static readonly Color SoftTextColor =
        new Color(0.86f, 0.82f, 0.74f, 1f);

    private static TMP_FontAsset koreanFont;
    private static Sprite backgroundSprite;
    private static Sprite tabActiveSprite;
    private static Sprite tabInactiveSprite;
    private static Sprite itemCardSprite;
    private static Sprite itemCardEquippedSprite;
    private static Sprite buyButtonSprite;

    [MenuItem("WordTower/Build Shop Scene")]
    public static void BuildShopScene()
    {
        ImportShopSprites();
        LoadAssets();

        if (!ValidateAssets())
            return;

        Scene shopScene = EditorSceneManager.NewScene(
            NewSceneSetup.DefaultGameObjects,
            NewSceneMode.Single
        );

        ConfigureCamera();
        CreateEventSystem();

        GameObject managerObject = new GameObject("ShopSceneManager");
        ShopSceneManager manager =
            managerObject.AddComponent<ShopSceneManager>();
        ConfigureManagerSprites(manager);

        Canvas canvas = CreateCanvas();
        GameObject root = CreateRoot(canvas.transform);

        CreateBackground(root.transform);
        CreateHeader(root.transform);
        CreateTabs(root.transform);
        CreateItemScrollView(root.transform);
        CreateBackButton(root.transform);

        Selection.activeGameObject = canvas.gameObject;
        EditorSceneManager.MarkSceneDirty(shopScene);

        if (!EditorSceneManager.SaveScene(shopScene, ShopScenePath))
        {
            Debug.LogError($"ShopScene 저장 실패: {ShopScenePath}");
            return;
        }

        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"WordTower Shop Scene 생성 완료: {ShopScenePath}");
    }

    public static void BuildShopSceneBatch()
    {
        BuildShopScene();
    }

    private static void ImportShopSprites()
    {
        ImportShopSprite(ShopBackgroundPath);
        ImportShopSprite(ShopTabActivePath);
        ImportShopSprite(ShopTabInactivePath);
        ImportShopSprite(ShopItemCardPath);
        ImportShopSprite(ShopItemCardEquippedPath);
        ImportShopSprite(ShopBuyButtonPath);
    }

    private static void ImportShopSprite(string path)
    {
        AssetDatabase.ImportAsset(path);

        TextureImporter importer =
            AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"Shop PNG Importer를 찾을 수 없습니다: {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void LoadAssets()
    {
        koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            KoreanFontPath
        );
        backgroundSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(ShopBackgroundPath);
        tabActiveSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(ShopTabActivePath);
        tabInactiveSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(ShopTabInactivePath);
        itemCardSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(ShopItemCardPath);
        itemCardEquippedSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(ShopItemCardEquippedPath);
        buyButtonSprite =
            AssetDatabase.LoadAssetAtPath<Sprite>(ShopBuyButtonPath);
    }

    private static bool ValidateAssets()
    {
        if (koreanFont == null)
        {
            Debug.LogError($"한글 TMP 폰트를 찾을 수 없습니다: {KoreanFontPath}");
            return false;
        }

        if (backgroundSprite == null ||
            tabActiveSprite == null ||
            tabInactiveSprite == null ||
            itemCardSprite == null ||
            itemCardEquippedSprite == null ||
            buyButtonSprite == null)
        {
            Debug.LogError("Shop PNG Sprite 로드 실패");
            return false;
        }

        return true;
    }

    private static void ConfigureCamera()
    {
        Camera camera = Object.FindAnyObjectByType<Camera>();

        if (camera == null)
            return;

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = CameraBackgroundColor;
        camera.orthographic = true;
    }

    private static void CreateEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "ShopCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static GameObject CreateRoot(Transform parent)
    {
        GameObject root = new GameObject("ShopRoot", typeof(RectTransform));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return root;
    }

    private static void CreateBackground(Transform parent)
    {
        Image background = CreateSpriteImage(
            parent,
            "Background",
            backgroundSprite,
            new Vector2(0.5f, 0.5f),
            new Vector2(1080f, 1920f),
            true
        );

        RectTransform rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        background.raycastTarget = false;
    }

    private static void CreateHeader(Transform parent)
    {
        GameObject header = CreateRect(parent, "Header");
        RectTransform headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 0.78f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        CreateText(
            header.transform,
            "GoldText",
            "GOLD 0",
            42,
            FontStyles.Bold,
            new Vector2(0.50f, 0.39f),
            new Vector2(620f, 70f),
            GoldColor
        );

        CreateText(
            header.transform,
            "MessageText",
            "",
            26,
            FontStyles.Bold,
            new Vector2(0.50f, 0.12f),
            new Vector2(760f, 58f),
            SoftTextColor
        );
    }

    private static void CreateTabs(Transform parent)
    {
        GameObject tabArea = CreateRect(parent, "TabArea");
        RectTransform rect = tabArea.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.69f);
        rect.anchorMax = new Vector2(1f, 0.78f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CreateTabButton(tabArea.transform, "WeaponTab", "무기", 0.25f, true);
        CreateTabButton(tabArea.transform, "ArmorTab", "방어구", 0.50f, false);
        CreateTabButton(
            tabArea.transform,
            "AccessoryTab",
            "악세서리",
            0.75f,
            false
        );
    }

    private static void CreateTabButton(
        Transform parent,
        string name,
        string label,
        float anchorX,
        bool isActive
    )
    {
        Button button = CreateButton(
            parent,
            name,
            label,
            new Vector2(anchorX, 0.5f),
            new Vector2(260f, 84f),
            isActive ? tabActiveSprite : tabInactiveSprite
        );

        TMP_Text text = button.transform.Find("Label")
            .GetComponent<TMP_Text>();
        text.fontSize = 34;
        text.color = BrightLabelColor;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.raycastTarget = false;
        text.alpha = 1f;
        text.rectTransform.SetAsLastSibling();
    }

    private static void CreateItemScrollView(Transform parent)
    {
        GameObject scrollView = CreatePanel(
            parent,
            "ItemScrollView",
            new Color(0f, 0f, 0f, 0f),
            new Vector2(0.5f, 0.39f),
            new Vector2(960f, 980f)
        );
        scrollView.GetComponent<Image>().raycastTarget = true;

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 38f;

        GameObject viewport = CreateRect(scrollView.transform, "Viewport");
        viewport.AddComponent<RectMask2D>();

        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-10f, -10f);

        GameObject content = CreateRect(viewport.transform, "Content");
        content.name = "ShopItemListContent";
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 1f);
        contentRect.anchorMax = new Vector2(0.5f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(940f, 780f);

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
    }

    private static void CreateBackButton(Transform parent)
    {
        Button button = CreateButton(
            parent,
            "BackButton",
            "닫기",
            new Vector2(0.50f, 0.065f),
            new Vector2(320f, 92f),
            buyButtonSprite
        );

        TMP_Text label = button.transform.Find("Label")
            .GetComponent<TMP_Text>();
        label.fontSize = 40;
        label.color = BrightLabelColor;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = false;
        label.raycastTarget = false;
        label.alpha = 1f;
        label.rectTransform.SetAsLastSibling();
    }

    private static GameObject CreateRect(Transform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
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

    private static Image CreateSpriteImage(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 anchor,
        Vector2 size,
        bool preserveAspect
    )
    {
        GameObject obj = CreatePanel(parent, name, Color.white, anchor, size);
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Vector2 anchor,
        Vector2 size,
        Color color
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
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        text.alpha = 1f;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchor,
        Vector2 size,
        Sprite sprite
    )
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Button button = obj.AddComponent<Button>();

        Image background = CreateSpriteImage(
            obj.transform,
            "Background",
            sprite,
            new Vector2(0.5f, 0.5f),
            size,
            true
        );
        StretchToParent(background.GetComponent<RectTransform>());
        background.raycastTarget = true;
        button.targetGraphic = background;

        TextMeshProUGUI labelText = CreateText(
            obj.transform,
            "Label",
            label,
            30,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            size,
            IvoryColor
        );
        StretchToParent(labelText.GetComponent<RectTransform>());
        labelText.raycastTarget = false;
        labelText.alpha = 1f;
        labelText.enableAutoSizing = false;
        labelText.rectTransform.SetAsLastSibling();

        return button;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static void ConfigureBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(StudioSplashScenePath, true),
            new EditorBuildSettingsScene(OpeningScenePath, true),
            new EditorBuildSettingsScene(TitleScenePath, true),
            new EditorBuildSettingsScene(StoryScenePath, true),
            new EditorBuildSettingsScene(StoryPlaybackScenePath, true),
            new EditorBuildSettingsScene(ShopScenePath, true),
            new EditorBuildSettingsScene(BattleScenePath, true)
        };
    }

    private static void ConfigureManagerSprites(ShopSceneManager manager)
    {
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("serializedTabActiveSprite")
            .objectReferenceValue = tabActiveSprite;
        serializedManager.FindProperty("serializedTabInactiveSprite")
            .objectReferenceValue = tabInactiveSprite;
        serializedManager.FindProperty("serializedItemCardSprite")
            .objectReferenceValue = itemCardSprite;
        serializedManager.FindProperty("serializedItemCardEquippedSprite")
            .objectReferenceValue = itemCardEquippedSprite;
        serializedManager.FindProperty("serializedBuyButtonSprite")
            .objectReferenceValue = buyButtonSprite;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(manager);
    }
}
