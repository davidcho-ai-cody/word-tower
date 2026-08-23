using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StorySceneBuilder
{
    private const float CardHeight = 200f;
    private const float CardSpacing = 27f;
    private const int ContentTopPadding = 22;
    private const int ContentBottomPadding = 88;

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
    private const string StoryBackgroundPath =
        "Assets/Art/UI/Story/wordtower_story_background.png";

    private static readonly Color CameraBackgroundColor =
        new Color(0.025f, 0.027f, 0.04f, 1f);
    private static readonly Color GoldColor =
        new Color(1f, 0.78f, 0.32f, 1f);
    private static readonly Color UnlockedCardColor =
        new Color(0.13f, 0.09f, 0.29f, 0.97f);
    private static readonly Color UnlockedBorderColor =
        new Color(0.94f, 0.69f, 0.28f, 0.92f);
    private static readonly Color UnlockedTitleColor =
        new Color(0.98f, 0.94f, 0.84f, 1f);
    private static readonly Color UnlockedDescriptionColor =
        new Color(0.9f, 0.86f, 0.78f, 0.88f);
    private static readonly Color LockedCardColor =
        new Color(0.08f, 0.1f, 0.16f, 0.9f);
    private static readonly Color LockedBorderColor =
        new Color(0.24f, 0.2f, 0.36f, 0.55f);

    private static TMP_FontAsset koreanFont;

    [MenuItem("WordTower/Build Story Scene")]
    public static void BuildStoryScene()
    {
        ImportStoryBackgroundAsSprite();

        koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            KoreanFontPath
        );
        Sprite storyBackground =
            AssetDatabase.LoadAssetAtPath<Sprite>(StoryBackgroundPath);

        if (koreanFont == null)
        {
            Debug.LogError("StoryScene 생성에 필요한 한글 TMP 폰트를 찾을 수 없습니다.");
            return;
        }

        if (storyBackground == null)
        {
            Debug.LogError($"Story 배경 Sprite를 찾을 수 없습니다: {StoryBackgroundPath}");
            return;
        }

        Scene storyScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        CreateCamera();
        CreateEventSystem();

        GameObject managerObject = new GameObject("StoryMenuManager");
        StoryMenuManager manager =
            managerObject.AddComponent<StoryMenuManager>();

        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform, storyBackground);
        CreateReadabilityOverlay(canvas.transform);
        Button backButton = CreateHeader(
            canvas.transform,
            out TMP_Text progressValueText
        );
        Transform content = CreateStoryScrollView(canvas.transform);
        Button prologueButton = CreatePrologueCard(content);
        Button[] lockedButtons = CreateLockedChapterCards(content);

        AssignManagerReferences(
            manager,
            backButton,
            prologueButton,
            lockedButtons,
            progressValueText
        );

        EditorSceneManager.MarkSceneDirty(storyScene);

        if (!EditorSceneManager.SaveScene(storyScene, StoryScenePath))
        {
            Debug.LogError($"StoryScene 저장 실패: {StoryScenePath}");
            return;
        }

        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"WordTower Story Scene 생성 완료: {StoryScenePath}");
    }

    public static void BuildStorySceneBatch()
    {
        BuildStoryScene();
    }

    private static void ImportStoryBackgroundAsSprite()
    {
        AssetDatabase.ImportAsset(StoryBackgroundPath);

        TextureImporter importer =
            AssetImporter.GetAtPath(StoryBackgroundPath) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = false;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject(
            "Main Camera",
            typeof(Camera),
            typeof(AudioListener)
        );
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = CameraBackgroundColor;
        camera.orthographic = true;
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static void CreateEventSystem()
    {
        new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(InputSystemUIInputModule)
        );
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "StoryCanvas",
            typeof(RectTransform),
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

    private static void CreateBackground(Transform parent, Sprite background)
    {
        Transform backgroundRoot = CreateContainer(
            parent,
            "Background",
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        CreateSolidImage(
            backgroundRoot,
            "BackgroundFill",
            CameraBackgroundColor,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        GameObject imageObject = new GameObject(
            "StoryBackgroundImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(AspectRatioFitter)
        );
        imageObject.transform.SetParent(backgroundRoot, false);
        StretchToParent(imageObject.GetComponent<RectTransform>());

        Image image = imageObject.GetComponent<Image>();
        image.sprite = background;
        image.preserveAspect = true;
        image.raycastTarget = false;

        AspectRatioFitter fitter =
            imageObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = background.rect.width / background.rect.height;
    }

    private static void CreateReadabilityOverlay(Transform parent)
    {
        Transform overlay = CreateContainer(
            parent,
            "ReadabilityOverlay",
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        CreateSolidImage(
            overlay,
            "Vignette",
            new Color(0f, 0f, 0f, 0.34f),
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );
        CreateSolidImage(
            overlay,
            "HeaderShade",
            new Color(0f, 0f, 0f, 0.48f),
            new Vector2(0f, 0.7f),
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );
    }

    private static Button CreateHeader(
        Transform parent,
        out TMP_Text progressValueText
    )
    {
        Transform header = CreateContainer(
            parent,
            "Header",
            new Vector2(0f, 0.72f),
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        Button backButton = CreateButton(
            header,
            "BackButton",
            "←",
            42f,
            new Color(0.02f, 0.025f, 0.035f, 0.62f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(74f, -66f),
            new Vector2(96f, 72f)
        );

        CreateText(
            header,
            "StoryTitle",
            "STORY",
            82f,
            FontStyles.Bold,
            new Vector2(0.5f, 0.74f),
            new Vector2(0.5f, 0.74f),
            Vector2.zero,
            new Vector2(640f, 98f),
            GoldColor
        );

        CreateText(
            header,
            "StorySubtitle",
            "되찾은 단어의 기록",
            32f,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(620f, 58f),
            new Color(1f, 0.96f, 0.84f, 1f)
        );

        Transform progressPanel = CreateContainer(
            header,
            "ProgressPanel",
            new Vector2(0.5f, 0.2f),
            new Vector2(0.5f, 0.2f),
            Vector2.zero,
            new Vector2(520f, 68f)
        );
        CreateSolidImage(
            progressPanel,
            "ProgressBackground",
            new Color(0.03f, 0.04f, 0.07f, 0.78f),
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );
        CreateText(
            progressPanel,
            "ProgressLabel",
            "되찾은 단어",
            26f,
            FontStyles.Bold,
            new Vector2(0.28f, 0.5f),
            new Vector2(0.28f, 0.5f),
            Vector2.zero,
            new Vector2(250f, 52f),
            new Color(0.9f, 0.9f, 0.94f, 1f)
        );
        progressValueText = CreateText(
            progressPanel,
            "ProgressValue",
            "0 / 10",
            35f,
            FontStyles.Bold,
            new Vector2(0.72f, 0.5f),
            new Vector2(0.72f, 0.5f),
            Vector2.zero,
            new Vector2(210f, 58f),
            GoldColor
        );

        return backButton;
    }

    private static Transform CreateStoryScrollView(Transform parent)
    {
        Transform storyList = CreateContainer(
            parent,
            "StoryList",
            new Vector2(0f, 0.04f),
            new Vector2(1f, 0.72f),
            Vector2.zero,
            Vector2.zero
        );

        GameObject scrollObject = new GameObject(
            "ScrollView",
            typeof(RectTransform),
            typeof(ScrollRect)
        );
        scrollObject.transform.SetParent(storyList, false);
        StretchToParent(scrollObject.GetComponent<RectTransform>());

        GameObject viewportObject = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Mask)
        );
        viewportObject.transform.SetParent(scrollObject.transform, false);
        RectTransform viewportRect =
            viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0.07f, 0f);
        viewportRect.anchorMax = new Vector2(0.93f, 1f);
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportImage.raycastTarget = true;
        viewportObject.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter)
        );
        contentObject.transform.SetParent(viewportObject.transform, false);

        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout =
            contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(
            0,
            0,
            ContentTopPadding,
            ContentBottomPadding
        );
        layout.spacing = CardSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter =
            contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        return contentObject.transform;
    }

    private static Button CreatePrologueCard(Transform parent)
    {
        Transform card = CreateCardRoot(
            parent,
            "PrologueCard",
            UnlockedCardColor,
            UnlockedBorderColor
        );

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();

        CreateText(
            card,
            "Eyebrow",
            "PROLOGUE",
            27f,
            FontStyles.Bold,
            new Vector2(0.07f, 0.72f),
            new Vector2(0.36f, 0.92f),
            Vector2.zero,
            Vector2.zero,
            UnlockedBorderColor
        );
        CreateText(
            card,
            "Title",
            "빼앗긴 단어",
            43f,
            FontStyles.Bold,
            new Vector2(0.07f, 0.43f),
            new Vector2(0.78f, 0.72f),
            Vector2.zero,
            Vector2.zero,
            UnlockedTitleColor
        );
        CreateText(
            card,
            "Description",
            "세상의 단어가 사라지기 시작했다...",
            30f,
            FontStyles.Normal,
            new Vector2(0.07f, 0.15f),
            new Vector2(0.78f, 0.42f),
            Vector2.zero,
            Vector2.zero,
            UnlockedDescriptionColor
        );
        CreateText(
            card,
            "PlayIcon",
            "▶",
            44f,
            FontStyles.Bold,
            new Vector2(0.82f, 0.2f),
            new Vector2(0.96f, 0.84f),
            Vector2.zero,
            Vector2.zero,
            GoldColor
        );

        return button;
    }

    private static Button[] CreateLockedChapterCards(Transform parent)
    {
        Button[] buttons = new Button[10];

        for (int i = 0; i < buttons.Length; i++)
        {
            int floor = (i + 1) * 10;
            string chapter = $"CHAPTER {i + 1:00}";
            Transform card = CreateCardRoot(
                parent,
                $"Chapter{floor}Card",
                LockedCardColor,
                LockedBorderColor
            );

            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.interactable = false;
            buttons[i] = button;

            CreateText(
                card,
                "Eyebrow",
                $"{chapter}        {floor}F",
                26f,
                FontStyles.Bold,
                new Vector2(0.07f, 0.7f),
                new Vector2(0.72f, 0.9f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.64f, 0.68f, 0.78f, 1f)
            );
            CreateText(
                card,
                "Title",
                "???",
                41f,
                FontStyles.Bold,
                new Vector2(0.07f, 0.42f),
                new Vector2(0.72f, 0.68f),
                Vector2.zero,
                Vector2.zero,
                Color.white
            );
            CreateText(
                card,
                "Description",
                "아직 되찾지 못한 단어입니다.",
                29f,
                FontStyles.Normal,
                new Vector2(0.07f, 0.16f),
                new Vector2(0.72f, 0.4f),
                Vector2.zero,
                Vector2.zero,
                new Color(0.82f, 0.85f, 0.92f, 1f)
            );
            CreateText(
                card,
                "LockLabel",
                "LOCK",
                30f,
                FontStyles.Bold,
                new Vector2(0.78f, 0.2f),
                new Vector2(0.95f, 0.82f),
                Vector2.zero,
                Vector2.zero,
                new Color(1f, 0.78f, 0.32f, 1f)
            );
        }

        return buttons;
    }

    private static Transform CreateCardRoot(
        Transform parent,
        string name,
        Color color,
        Color borderColor
    )
    {
        GameObject cardObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline),
            typeof(LayoutElement)
        );
        cardObject.transform.SetParent(parent, false);

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, CardHeight);

        Image image = cardObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Outline outline = cardObject.GetComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;

        LayoutElement layout = cardObject.GetComponent<LayoutElement>();
        layout.preferredHeight = CardHeight;
        layout.minHeight = CardHeight;

        return cardObject.transform;
    }

    private static Transform CreateContainer(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta
    )
    {
        GameObject container = new GameObject(name, typeof(RectTransform));
        container.transform.SetParent(parent, false);

        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        if (anchorMin != anchorMax)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        return container.transform;
    }

    private static Image CreateSolidImage(
        Transform parent,
        string name,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 sizeDelta
    )
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        if (anchorMin != anchorMax)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color
    )
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        if (anchorMin != anchorMax)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = koreanFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        float fontSize,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 anchoredPosition,
        Vector2 size
    )
    {
        GameObject buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        CreateText(
            buttonObject.transform,
            "Label",
            label,
            fontSize,
            FontStyles.Bold,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            Color.white
        );

        return button;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AssignManagerReferences(
        StoryMenuManager manager,
        Button backButton,
        Button prologueButton,
        Button[] lockedButtons,
        TMP_Text progressValueText
    )
    {
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("backButton").objectReferenceValue =
            backButton;
        serializedManager.FindProperty("prologueButton").objectReferenceValue =
            prologueButton;
        serializedManager.FindProperty("progressValueText").objectReferenceValue =
            progressValueText;

        SerializedProperty lockedButtonsProperty =
            serializedManager.FindProperty("lockedChapterButtons");
        lockedButtonsProperty.arraySize = lockedButtons.Length;

        for (int i = 0; i < lockedButtons.Length; i++)
        {
            lockedButtonsProperty.GetArrayElementAtIndex(i)
                .objectReferenceValue = lockedButtons[i];
        }

        serializedManager.ApplyModifiedPropertiesWithoutUndo();
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
}
