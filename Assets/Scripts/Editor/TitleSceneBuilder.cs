using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TitleSceneBuilder
{
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string KoreanFontPath =
        "Assets/Fonts/NotoSansKR-Regular SDF.asset";
    private const string TitleBackgroundPath =
        "Assets/Art/UI/Title/wordtower_title_main.png";

    private static readonly Color CameraBackgroundColor =
        new Color(0.025f, 0.027f, 0.04f, 1f);
    private static readonly Color TitleGold =
        new Color(1f, 0.82f, 0.32f, 1f);
    private static readonly Color PrimaryButtonColor =
        new Color(0.78f, 0.48f, 0.18f, 0.96f);
    private static readonly Color SecondaryButtonColor =
        new Color(0.13f, 0.15f, 0.21f, 0.88f);
    private static readonly Color QuietButtonColor =
        new Color(0.02f, 0.025f, 0.035f, 0.5f);

    private static TMP_FontAsset koreanFont;

    [MenuItem("WordTower/Build Title Scene")]
    public static void BuildTitleScene()
    {
        ImportTitleBackgroundAsSprite();

        koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            KoreanFontPath
        );
        Sprite titleBackground =
            AssetDatabase.LoadAssetAtPath<Sprite>(TitleBackgroundPath);

        if (koreanFont == null)
        {
            Debug.LogError("TitleScene 생성에 필요한 한글 TMP 폰트를 찾을 수 없습니다.");
            return;
        }

        if (titleBackground == null)
        {
            Debug.LogError($"Title 배경 Sprite를 찾을 수 없습니다: {TitleBackgroundPath}");
            return;
        }

        Scene titleScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        CreateCamera();
        CreateEventSystem();

        Canvas canvas = CreateCanvas();
        CreateTitleHierarchy(canvas.transform, titleBackground);

        EditorSceneManager.MarkSceneDirty(titleScene);

        if (!EditorSceneManager.SaveScene(titleScene, TitleScenePath))
        {
            Debug.LogError($"TitleScene을 저장하지 못했습니다: {TitleScenePath}");
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"WordTower Title Scene 생성 완료: {TitleScenePath}");
    }

    private static void ImportTitleBackgroundAsSprite()
    {
        AssetDatabase.ImportAsset(TitleBackgroundPath);

        TextureImporter importer =
            AssetImporter.GetAtPath(TitleBackgroundPath) as TextureImporter;

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
            "TitleCanvas",
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

    private static void CreateTitleHierarchy(
        Transform canvasTransform,
        Sprite titleBackground
    )
    {
        GameObject managerObject = new GameObject("TitleManager");
        TitleManager titleManager = managerObject.AddComponent<TitleManager>();

        Transform background = CreateContainer(
            canvasTransform,
            "Background",
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );
        CreateSolidImage(
            background,
            "BackgroundFill",
            CameraBackgroundColor,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );
        CreateBackgroundImage(background, titleBackground);

        CreateReadabilityOverlay(canvasTransform);
        CreateTitleHeader(canvasTransform);
        CreateMainMenu(canvasTransform, titleManager);

        TMP_Text versionText = CreateText(
            canvasTransform,
            "VersionText",
            "Ver. 0.0.0",
            24f,
            FontStyles.Normal,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 50f),
            new Vector2(520f, 54f),
            new Color(0.82f, 0.84f, 0.9f, 0.82f)
        );

        AssignManagerReferences(titleManager, versionText);
    }

    private static void CreateBackgroundImage(
        Transform parent,
        Sprite titleBackground
    )
    {
        GameObject imageObject = new GameObject(
            "TitleBackgroundImage",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(AspectRatioFitter)
        );
        imageObject.transform.SetParent(parent, false);
        StretchToParent(imageObject.GetComponent<RectTransform>());

        Image image = imageObject.GetComponent<Image>();
        image.sprite = titleBackground;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;

        AspectRatioFitter fitter =
            imageObject.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = titleBackground.rect.width /
            titleBackground.rect.height;
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
            new Color(0f, 0f, 0f, 0.24f),
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );
        CreateSolidImage(
            overlay,
            "HeaderShade",
            new Color(0f, 0f, 0f, 0.42f),
            new Vector2(0f, 0.68f),
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );
        CreateSolidImage(
            overlay,
            "MenuShade",
            new Color(0f, 0f, 0f, 0.46f),
            Vector2.zero,
            new Vector2(1f, 0.36f),
            Vector2.zero,
            Vector2.zero
        );
    }

    private static void CreateTitleHeader(Transform parent)
    {
        Transform header = CreateContainer(
            parent,
            "TitleHeader",
            new Vector2(0f, 0.68f),
            Vector2.one,
            Vector2.zero,
            Vector2.zero
        );

        TMP_Text title = CreateText(
            header,
            "WordTowerTitle",
            "WORD TOWER",
            104f,
            FontStyles.Bold,
            new Vector2(0.5f, 0.56f),
            new Vector2(0.5f, 0.56f),
            Vector2.zero,
            new Vector2(900f, 138f),
            TitleGold
        );
        AddShadow(title.gameObject, new Color(0.05f, 0.03f, 0f, 0.65f),
            new Vector2(0f, -4f));

        TMP_Text subtitle = CreateText(
            header,
            "Subtitle",
            "빼앗긴 단어를 되찾는 모험",
            34f,
            FontStyles.Bold,
            new Vector2(0.5f, 0.27f),
            new Vector2(0.5f, 0.27f),
            Vector2.zero,
            new Vector2(820f, 70f),
            new Color(1f, 0.96f, 0.84f, 1f)
        );
        AddShadow(subtitle.gameObject, new Color(0f, 0f, 0f, 0.6f),
            new Vector2(0f, -2f));
    }

    private static void CreateMainMenu(
        Transform parent,
        TitleManager titleManager
    )
    {
        Transform mainMenu = CreateContainer(
            parent,
            "MainMenu",
            Vector2.zero,
            new Vector2(1f, 0.38f),
            Vector2.zero,
            Vector2.zero
        );

        Button startButton = CreateButton(
            mainMenu,
            "StartButton",
            "게임 시작",
            42f,
            PrimaryButtonColor,
            new Vector2(0.5f, 0.66f),
            new Vector2(0.5f, 0.66f),
            Vector2.zero,
            new Vector2(680f, 118f)
        );

        Transform secondaryMenu = CreateContainer(
            mainMenu,
            "SecondaryMenu",
            new Vector2(0.1f, 0.32f),
            new Vector2(0.9f, 0.48f),
            Vector2.zero,
            Vector2.zero
        );

        Button storyButton = CreateButton(
            secondaryMenu,
            "StoryButton",
            "STORY",
            30f,
            SecondaryButtonColor,
            new Vector2(0.17f, 0.5f),
            new Vector2(0.17f, 0.5f),
            Vector2.zero,
            new Vector2(242f, 82f)
        );
        Button collectionButton = CreateButton(
            secondaryMenu,
            "CollectionButton",
            "도감",
            30f,
            SecondaryButtonColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(242f, 82f)
        );
        Button settingsButton = CreateButton(
            secondaryMenu,
            "SettingsButton",
            "설정",
            30f,
            SecondaryButtonColor,
            new Vector2(0.83f, 0.5f),
            new Vector2(0.83f, 0.5f),
            Vector2.zero,
            new Vector2(242f, 82f)
        );

        Button quitButton = CreateButton(
            parent,
            "QuitButton",
            "종료",
            24f,
            QuietButtonColor,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-96f, -56f),
            new Vector2(138f, 58f)
        );

        SerializedObject serializedManager = new SerializedObject(titleManager);
        serializedManager.FindProperty("startButton").objectReferenceValue =
            startButton;
        serializedManager.FindProperty("startButtonText").objectReferenceValue =
            startButton.transform.Find("Label").GetComponent<TMP_Text>();
        serializedManager.FindProperty("storyButton").objectReferenceValue =
            storyButton;
        serializedManager.FindProperty("collectionButton").objectReferenceValue =
            collectionButton;
        serializedManager.FindProperty("settingsButton").objectReferenceValue =
            settingsButton;
        serializedManager.FindProperty("quitButton").objectReferenceValue =
            quitButton;
        serializedManager.FindProperty("titleFont").objectReferenceValue =
            koreanFont;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignManagerReferences(
        TitleManager titleManager,
        TMP_Text versionText
    )
    {
        SerializedObject serializedManager = new SerializedObject(titleManager);
        serializedManager.FindProperty("versionText").objectReferenceValue =
            versionText;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
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
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

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
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

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

        TMP_Text text = CreateText(
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
        StretchToParent(text.GetComponent<RectTransform>());
        AddShadow(text.gameObject, new Color(0f, 0f, 0f, 0.45f),
            new Vector2(0f, -2f));

        return button;
    }

    private static void AddShadow(
        GameObject target,
        Color color,
        Vector2 distance
    )
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
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
}
