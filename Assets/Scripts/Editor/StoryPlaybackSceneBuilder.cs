using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StoryPlaybackSceneBuilder
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

    private static readonly string[] Floor10ImagePaths =
    {
        "Assets/Art/Story/Floor10/floor10_01_battle_end.png",
        "Assets/Art/Story/Floor10/floor10_02_mysterious_light.png",
        "Assets/Art/Story/Floor10/floor10_03_love_crystal.png",
        "Assets/Art/Story/Floor10/floor10_04_hero_holds_love.png",
        "Assets/Art/Story/Floor10/floor10_05_slimes_reunite.png",
        "Assets/Art/Story/Floor10/floor10_06_slime_king_remembers.png",
        "Assets/Art/Story/Floor10/floor10_07_hero_next_journey.png",
        "Assets/Art/Story/Floor10/floor10_08_demon_king.png"
    };

    private static readonly string[] Speakers =
    {
        "",
        "용사",
        "용사",
        "용사",
        "슬라임킹",
        "슬라임킹",
        "용사",
        "마왕"
    };

    private static readonly string[] Dialogues =
    {
        "마침내...\n슬라임킹을 쓰러뜨렸다.",
        "...이건 뭐지?\n\n슬라임킹이 쓰러진 자리에서\n작은 빛이 피어올랐다.",
        "이 글자는...\n\n사랑",
        "사랑...?\n\n마왕에게 빼앗겼던 단어가...\n이 안에 있었던 건가?",
        "이... 따뜻한 느낌은...\n\n왜 잊고 있었던 거지...?",
        "기억났다...\n\n우리가 서로를 소중하게 생각했던 마음.\n\n그게... 사랑이었어.",
        "마왕이 빼앗아 간 건...\n\n단순한 단어가 아니었어.\n\n사람들의 마음까지 함께 빼앗아 간 거야.",
        "사랑이라...\n\n겨우 하나를 되찾았을 뿐이다.\n\n아직 아홉 개가 남아 있다."
    };

    private static readonly float[] CutStartTimes =
    {
        0f, 3f, 6.2f, 9.4f, 12.9f, 16.4f, 20.4f, 24.6f
    };

    private static readonly float[] CutEndTimes =
    {
        3f, 6.2f, 9.4f, 12.9f, 16.4f, 20.4f, 24.6f, 28.6f
    };

    private static readonly Vector2[] PanFrom =
    {
        new Vector2(0f, 0f),
        new Vector2(-10f, 8f),
        new Vector2(0f, 0f),
        new Vector2(0f, -10f),
        new Vector2(0f, 0f),
        new Vector2(0f, 8f),
        new Vector2(-16f, 0f),
        new Vector2(0f, 0f)
    };

    private static readonly Vector2[] PanTo =
    {
        new Vector2(0f, 16f),
        new Vector2(12f, -16f),
        new Vector2(0f, 10f),
        new Vector2(0f, 14f),
        new Vector2(0f, 10f),
        new Vector2(0f, -12f),
        new Vector2(18f, 0f),
        new Vector2(0f, 12f)
    };

    private static readonly float[] ZoomFrom =
    {
        1f, 1f, 1.04f, 1f, 1f, 1.02f, 1f, 1.02f
    };

    private static readonly float[] ZoomTo =
    {
        1.045f, 1.04f, 1.095f, 1.04f, 1.035f, 1.075f, 1.045f, 1.075f
    };

    private static TMP_FontAsset koreanFont;

    [MenuItem("WordTower/Build Story Playback Scene")]
    public static void BuildStoryPlaybackScene()
    {
        ImportFloor10ImagesAsSprites();

        koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            KoreanFontPath
        );
        Sprite[] floor10Sprites = LoadFloor10Sprites();

        if (koreanFont == null)
        {
            Debug.LogError(
                $"StoryPlaybackScene TMP Font load failed: {KoreanFontPath}"
            );
            return;
        }

        if (HasMissingSprite(floor10Sprites))
        {
            Debug.LogError("Floor10 Story image load failed.");
            return;
        }

        Scene playbackScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        CreateCamera();
        CreateEventSystem();

        GameObject managerObject = new GameObject("StoryPlaybackManager");
        StoryPlaybackManager manager =
            managerObject.AddComponent<StoryPlaybackManager>();

        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform);

        Image storyImageA = CreateStoryImage(canvas.transform, "StoryImageA");
        Image storyImageB = CreateStoryImage(canvas.transform, "StoryImageB");
        CreateDialoguePanel(
            canvas.transform,
            out TMP_Text speakerNameText,
            out TMP_Text dialogueText
        );
        Button skipButton = CreateSkipButton(
            canvas.transform,
            out TMP_Text skipButtonText
        );
        CanvasGroup acquisitionOverlay = CreateAcquisitionOverlay(
            canvas.transform,
            out TMP_Text acquisitionTitleText,
            out TMP_Text acquisitionKeywordText,
            out TMP_Text acquisitionProgressText
        );
        Image fadeOverlay = CreateFadeOverlay(canvas.transform);

        AssignManagerReferences(
            manager,
            canvas,
            storyImageA,
            storyImageB,
            fadeOverlay,
            acquisitionOverlay,
            skipButton,
            skipButtonText,
            speakerNameText,
            dialogueText,
            acquisitionTitleText,
            acquisitionKeywordText,
            acquisitionProgressText,
            floor10Sprites
        );

        EditorSceneManager.MarkSceneDirty(playbackScene);

        if (!EditorSceneManager.SaveScene(
            playbackScene,
            StoryPlaybackScenePath
        ))
        {
            Debug.LogError(
                $"StoryPlaybackScene save failed: {StoryPlaybackScenePath}"
            );
            return;
        }

        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"WordTower Story Playback Scene built: {StoryPlaybackScenePath}"
        );
    }

    public static void BuildStoryPlaybackSceneBatch()
    {
        BuildStoryPlaybackScene();
    }

    private static void ImportFloor10ImagesAsSprites()
    {
        foreach (string path in Floor10ImagePaths)
        {
            AssetDatabase.ImportAsset(path);
            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }

    private static Sprite[] LoadFloor10Sprites()
    {
        Sprite[] sprites = new Sprite[Floor10ImagePaths.Length];

        for (int i = 0; i < Floor10ImagePaths.Length; i++)
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                Floor10ImagePaths[i]
            );

        return sprites;
    }

    private static bool HasMissingSprite(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length != Floor10ImagePaths.Length)
            return true;

        foreach (Sprite sprite in sprites)
        {
            if (sprite == null)
                return true;
        }

        return false;
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
        camera.backgroundColor = Color.black;
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
            "StoryPlaybackCanvas",
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

    private static void CreateBackground(Transform parent)
    {
        GameObject backgroundObject = new GameObject(
            "Background",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        backgroundObject.transform.SetParent(parent, false);
        StretchToParent(backgroundObject.GetComponent<RectTransform>());
        backgroundObject.GetComponent<Image>().color = Color.black;
    }

    private static Image CreateStoryImage(Transform parent, string name)
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        imageObject.transform.SetParent(parent, false);
        StretchToParent(imageObject.GetComponent<RectTransform>());

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        imageObject.GetComponent<CanvasGroup>().alpha = 0f;
        return image;
    }

    private static void CreateDialoguePanel(
        Transform parent,
        out TMP_Text speakerNameText,
        out TMP_Text dialogueText
    )
    {
        GameObject panelObject = new GameObject(
            "DialoguePanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.06f, 0.055f);
        panelRect.anchorMax = new Vector2(0.94f, 0.285f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.68f);
        panelImage.raycastTarget = false;

        speakerNameText = CreateText(
            panelObject.transform,
            "SpeakerName",
            "",
            31f,
            FontStyles.Bold,
            new Vector2(0.06f, 0.66f),
            new Vector2(0.34f, 0.91f),
            Vector2.zero,
            Vector2.zero,
            new Color(1f, 0.8f, 0.34f, 1f),
            TextAlignmentOptions.Left
        );

        dialogueText = CreateText(
            panelObject.transform,
            "DialogueText",
            "",
            32f,
            FontStyles.Normal,
            new Vector2(0.06f, 0.12f),
            new Vector2(0.94f, 0.74f),
            Vector2.zero,
            Vector2.zero,
            new Color(1f, 0.96f, 0.88f, 1f),
            TextAlignmentOptions.Left
        );
    }

    private static Button CreateSkipButton(
        Transform parent,
        out TMP_Text label
    )
    {
        GameObject buttonObject = new GameObject(
            "SkipButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-32f, -32f);
        rect.sizeDelta = new Vector2(176f, 68f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.46f);

        Button button = buttonObject.GetComponent<Button>();

        label = CreateText(
            buttonObject.transform,
            "Label",
            "SKIP",
            30f,
            FontStyles.Bold,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            Color.white,
            TextAlignmentOptions.Center
        );

        buttonObject.transform.SetAsLastSibling();
        return button;
    }

    private static CanvasGroup CreateAcquisitionOverlay(
        Transform parent,
        out TMP_Text titleText,
        out TMP_Text keywordText,
        out TMP_Text progressText
    )
    {
        GameObject overlayObject = new GameObject(
            "AcquisitionOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        overlayObject.transform.SetParent(parent, false);
        StretchToParent(overlayObject.GetComponent<RectTransform>());

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.92f);
        overlayImage.raycastTarget = false;

        titleText = CreateText(
            overlayObject.transform,
            "AcquisitionTitle",
            "빼앗긴 단어를 되찾았습니다",
            36f,
            FontStyles.Bold,
            new Vector2(0.12f, 0.58f),
            new Vector2(0.88f, 0.66f),
            Vector2.zero,
            Vector2.zero,
            new Color(1f, 0.92f, 0.72f, 1f),
            TextAlignmentOptions.Center
        );

        keywordText = CreateText(
            overlayObject.transform,
            "AcquisitionKeyword",
            StoryCatalog.Floor10KeywordName,
            96f,
            FontStyles.Bold,
            new Vector2(0.08f, 0.43f),
            new Vector2(0.92f, 0.58f),
            Vector2.zero,
            Vector2.zero,
            new Color(1f, 0.78f, 0.26f, 1f),
            TextAlignmentOptions.Center
        );

        progressText = CreateText(
            overlayObject.transform,
            "AcquisitionProgress",
            "1 / 10",
            34f,
            FontStyles.Bold,
            new Vector2(0.2f, 0.35f),
            new Vector2(0.8f, 0.42f),
            Vector2.zero,
            Vector2.zero,
            new Color(1f, 0.92f, 0.72f, 1f),
            TextAlignmentOptions.Center
        );

        CanvasGroup group = overlayObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        overlayObject.SetActive(false);
        overlayObject.transform.SetAsLastSibling();
        return group;
    }

    private static Image CreateFadeOverlay(Transform parent)
    {
        GameObject overlayObject = new GameObject(
            "FadeOverlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        overlayObject.transform.SetParent(parent, false);
        StretchToParent(overlayObject.GetComponent<RectTransform>());

        Image image = overlayObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        overlayObject.GetComponent<CanvasGroup>().alpha = 0f;
        overlayObject.transform.SetAsLastSibling();
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
        Color color,
        TextAlignmentOptions alignment
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
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
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
        StoryPlaybackManager manager,
        Canvas canvas,
        Image storyImageA,
        Image storyImageB,
        Image fadeOverlay,
        CanvasGroup acquisitionOverlay,
        Button skipButton,
        TMP_Text skipButtonText,
        TMP_Text speakerNameText,
        TMP_Text dialogueText,
        TMP_Text acquisitionTitleText,
        TMP_Text acquisitionKeywordText,
        TMP_Text acquisitionProgressText,
        Sprite[] floor10Sprites
    )
    {
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("storyId").stringValue =
            StoryCatalog.Floor10ClearStoryId;
        serializedManager.FindProperty("storyCanvas").objectReferenceValue =
            canvas;
        serializedManager.FindProperty("storyImageA").objectReferenceValue =
            storyImageA;
        serializedManager.FindProperty("storyImageB").objectReferenceValue =
            storyImageB;
        serializedManager.FindProperty("storyImageGroupA").objectReferenceValue =
            storyImageA.GetComponent<CanvasGroup>();
        serializedManager.FindProperty("storyImageGroupB").objectReferenceValue =
            storyImageB.GetComponent<CanvasGroup>();
        serializedManager.FindProperty("fadeOverlayGroup").objectReferenceValue =
            fadeOverlay.GetComponent<CanvasGroup>();
        serializedManager.FindProperty("acquisitionOverlayGroup")
            .objectReferenceValue = acquisitionOverlay;
        serializedManager.FindProperty("skipButton").objectReferenceValue =
            skipButton;
        serializedManager.FindProperty("skipButtonText").objectReferenceValue =
            skipButtonText;
        serializedManager.FindProperty("speakerNameText").objectReferenceValue =
            speakerNameText;
        serializedManager.FindProperty("dialogueText").objectReferenceValue =
            dialogueText;
        serializedManager.FindProperty("acquisitionTitleText")
            .objectReferenceValue = acquisitionTitleText;
        serializedManager.FindProperty("acquisitionKeywordText")
            .objectReferenceValue = acquisitionKeywordText;
        serializedManager.FindProperty("acquisitionProgressText")
            .objectReferenceValue = acquisitionProgressText;
        serializedManager.FindProperty("crossFadeDuration").floatValue = 0.3f;
        serializedManager.FindProperty("sceneFadeOutDuration").floatValue = 0.5f;
        serializedManager.FindProperty("acquisitionOverlayDuration").floatValue =
            2.8f;

        SerializedProperty cutsProperty =
            serializedManager.FindProperty("storyCuts");
        cutsProperty.arraySize = floor10Sprites.Length;

        for (int i = 0; i < floor10Sprites.Length; i++)
        {
            SerializedProperty cutProperty =
                cutsProperty.GetArrayElementAtIndex(i);
            cutProperty.FindPropertyRelative("sprite").objectReferenceValue =
                floor10Sprites[i];
            cutProperty.FindPropertyRelative("speaker").stringValue =
                Speakers[i];
            cutProperty.FindPropertyRelative("dialogue").stringValue =
                Dialogues[i];
            cutProperty.FindPropertyRelative("startTime").floatValue =
                CutStartTimes[i];
            cutProperty.FindPropertyRelative("endTime").floatValue =
                CutEndTimes[i];
            cutProperty.FindPropertyRelative("panFrom").vector2Value =
                PanFrom[i];
            cutProperty.FindPropertyRelative("panTo").vector2Value =
                PanTo[i];
            cutProperty.FindPropertyRelative("zoomFrom").floatValue =
                ZoomFrom[i];
            cutProperty.FindPropertyRelative("zoomTo").floatValue =
                ZoomTo[i];
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
