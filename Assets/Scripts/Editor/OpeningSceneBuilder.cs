using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OpeningSceneBuilder
{
    private const string OpeningScenePath = "Assets/Scenes/OpeningScene.unity";
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
    private const string KoreanFontPath =
        "Assets/Fonts/NotoSansKR-Regular SDF.asset";
    private const string OpeningAudioPath =
        "Assets/Audio/BGM/Opening/wordtower_opening_theme.mp3";

    private static readonly string[] OpeningImagePaths =
    {
        "Assets/Art/Opening/opening_01_peaceful_world.png",
        "Assets/Art/Opening/opening_02_words_disappear.png",
        "Assets/Art/Opening/opening_03_demon_king_steals_words.png",
        "Assets/Art/Opening/opening_04_world_without_words.png",
        "Assets/Art/Opening/opening_05_hero_awakens.png",
        "Assets/Art/Opening/opening_06_toward_word_tower.png",
        "Assets/Art/Opening/opening_07_demon_kings_plan.png",
        "Assets/Art/Opening/opening_08_adventure_begins.png"
    };

    private static readonly float[] CutStartTimes =
    {
        0f,
        4f,
        6.8f,
        10.4f,
        13.7f,
        17f,
        20.4f,
        24.6f
    };

    private static readonly float[] CutEndTimes =
    {
        4f,
        6.8f,
        10.4f,
        13.7f,
        17f,
        20.4f,
        24.6f,
        30.8f
    };

    private static readonly Vector2[] PanFrom =
    {
        new Vector2(0f, 0f),
        new Vector2(0f, 10f),
        new Vector2(0f, 0f),
        new Vector2(0f, 0f),
        new Vector2(0f, -24f),
        new Vector2(-16f, 0f),
        new Vector2(0f, 0f),
        new Vector2(0f, 0f)
    };

    private static readonly Vector2[] PanTo =
    {
        new Vector2(0f, 0f),
        new Vector2(0f, -20f),
        new Vector2(0f, 0f),
        new Vector2(0f, 0f),
        new Vector2(0f, 24f),
        new Vector2(18f, 0f),
        new Vector2(0f, 0f),
        new Vector2(0f, 0f)
    };

    private static readonly float[] ZoomFrom =
    {
        1f,
        1f,
        1f,
        1f,
        1.01f,
        1f,
        1f,
        1f
    };

    private static readonly float[] ZoomTo =
    {
        1.05f,
        1.04f,
        1.055f,
        1.025f,
        1.045f,
        1.055f,
        1.05f,
        1.045f
    };

    [MenuItem("WordTower/Build Opening Scene")]
    public static void BuildOpeningScene()
    {
        ImportOpeningImagesAsSprites();
        AssetDatabase.ImportAsset(OpeningAudioPath);

        TMP_FontAsset koreanFont =
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        AudioClip openingClip =
            AssetDatabase.LoadAssetAtPath<AudioClip>(OpeningAudioPath);
        Sprite[] openingSprites = LoadOpeningSprites();

        if (koreanFont == null)
        {
            Debug.LogError($"오프닝 한글 TMP 폰트를 찾을 수 없습니다: {KoreanFontPath}");
            return;
        }

        if (openingClip == null || HasMissingSprite(openingSprites))
        {
            Debug.LogError("오프닝 이미지 또는 음원 Asset 로드에 실패했습니다.");
            return;
        }

        Scene openingScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        CreateCamera();
        CreateEventSystem();

        GameObject managerObject = new GameObject("OpeningStoryManager");
        OpeningStoryManager manager =
            managerObject.AddComponent<OpeningStoryManager>();

        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform);

        Image storyImageA = CreateStoryImage(canvas.transform, "StoryImageA");
        Image storyImageB = CreateStoryImage(canvas.transform, "StoryImageB");
        Image fadeOverlay = CreateFadeOverlay(canvas.transform);
        Button skipButton = CreateSkipButton(
            canvas.transform,
            koreanFont,
            out TMP_Text skipButtonText
        );
        AudioSource audioSource = managerObject.AddComponent<AudioSource>();
        audioSource.clip = openingClip;
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        AssignManagerReferences(
            manager,
            canvas,
            storyImageA,
            storyImageB,
            fadeOverlay,
            skipButton,
            skipButtonText,
            audioSource,
            openingSprites
        );

        EditorSceneManager.MarkSceneDirty(openingScene);

        if (!EditorSceneManager.SaveScene(openingScene, OpeningScenePath))
        {
            Debug.LogError($"OpeningScene 저장 실패: {OpeningScenePath}");
            return;
        }

        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"WordTower Opening Scene 생성 완료: {OpeningScenePath}");
    }

    public static void BuildOpeningSceneBatch()
    {
        BuildOpeningScene();
    }

    private static void ImportOpeningImagesAsSprites()
    {
        foreach (string path in OpeningImagePaths)
        {
            AssetDatabase.ImportAsset(path);
            TextureImporter importer =
                AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
    }

    private static Sprite[] LoadOpeningSprites()
    {
        Sprite[] sprites = new Sprite[OpeningImagePaths.Length];

        for (int i = 0; i < OpeningImagePaths.Length; i++)
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(
                OpeningImagePaths[i]
            );

        return sprites;
    }

    private static bool HasMissingSprite(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length != OpeningImagePaths.Length)
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
            "OpeningCanvas",
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

    private static Button CreateSkipButton(
        Transform parent,
        TMP_FontAsset koreanFont,
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
        rect.sizeDelta = new Vector2(210f, 72f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.42f);

        Button button = buttonObject.GetComponent<Button>();

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        StretchToParent(labelRect);

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.font = koreanFont;
        text.text = "SKIP ▶";
        text.fontSize = 32f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        label = text;

        buttonObject.transform.SetAsLastSibling();
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
        OpeningStoryManager manager,
        Canvas canvas,
        Image storyImageA,
        Image storyImageB,
        Image fadeOverlay,
        Button skipButton,
        TMP_Text skipButtonText,
        AudioSource audioSource,
        Sprite[] openingSprites
    )
    {
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("openingCanvas").objectReferenceValue =
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
        serializedManager.FindProperty("skipButton").objectReferenceValue =
            skipButton;
        serializedManager.FindProperty("skipButtonText").objectReferenceValue =
            skipButtonText;
        serializedManager.FindProperty("audioSource").objectReferenceValue =
            audioSource;
        serializedManager.FindProperty("crossFadeDuration").floatValue = 0.3f;
        serializedManager.FindProperty("titleFadeOutDuration").floatValue = 0.5f;

        SerializedProperty cutsProperty =
            serializedManager.FindProperty("storyCuts");
        cutsProperty.arraySize = openingSprites.Length;

        for (int i = 0; i < openingSprites.Length; i++)
        {
            SerializedProperty cutProperty =
                cutsProperty.GetArrayElementAtIndex(i);
            cutProperty.FindPropertyRelative("sprite").objectReferenceValue =
                openingSprites[i];
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
            new EditorBuildSettingsScene(OpeningScenePath, true),
            new EditorBuildSettingsScene(TitleScenePath, true),
            new EditorBuildSettingsScene(BattleScenePath, true)
        };
    }
}
