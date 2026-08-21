using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StudioSplashSceneBuilder
{
    private const string StudioSplashScenePath =
        "Assets/Scenes/StudioSplashScene.unity";
    private const string OpeningScenePath = "Assets/Scenes/OpeningScene.unity";
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string BattleScenePath = "Assets/Scenes/BattleScene.unity";
    private const string LogoPath =
        "Assets/Art/Brand/Studio/play_your_next_world_logo.png";
    private const string VoicePath =
        "Assets/Audio/Brand/Studio/play_your_next_world_voice.mp3";

    [MenuItem("WordTower/Build Studio Splash Scene")]
    public static void BuildStudioSplashScene()
    {
        ImportLogoAsSprite();
        AssetDatabase.ImportAsset(VoicePath);

        Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        AudioClip voiceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(VoicePath);

        if (logoSprite == null || voiceClip == null)
        {
            Debug.LogError(
                "Studio Splash 로고 또는 음성 Asset 로드에 실패했습니다."
            );
            return;
        }

        Scene studioScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        CreateCamera();
        CreateEventSystem();

        GameObject managerObject = new GameObject("StudioSplashManager");
        StudioSplashManager manager =
            managerObject.AddComponent<StudioSplashManager>();

        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform);
        Image glow = CreateGlow(canvas.transform);
        Image logo = CreateLogo(canvas.transform, logoSprite);
        Image lightSweep = CreateLightSweep(canvas.transform);
        Image fadeOverlay = CreateFadeOverlay(canvas.transform);

        AudioSource voiceAudioSource = managerObject.AddComponent<AudioSource>();
        voiceAudioSource.clip = voiceClip;
        voiceAudioSource.playOnAwake = false;
        voiceAudioSource.loop = false;
        voiceAudioSource.spatialBlend = 0f;

        AssignManagerReferences(
            manager,
            canvas,
            logo,
            glow,
            lightSweep,
            fadeOverlay,
            voiceAudioSource
        );

        EditorSceneManager.MarkSceneDirty(studioScene);

        if (!EditorSceneManager.SaveScene(studioScene, StudioSplashScenePath))
        {
            Debug.LogError($"StudioSplashScene 저장 실패: {StudioSplashScenePath}");
            return;
        }

        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"WordTower Studio Splash Scene 생성 완료: {StudioSplashScenePath}");
    }

    public static void BuildStudioSplashSceneBatch()
    {
        BuildStudioSplashScene();
    }

    private static void ImportLogoAsSprite()
    {
        AssetDatabase.ImportAsset(LogoPath);
        TextureImporter importer =
            AssetImporter.GetAtPath(LogoPath) as TextureImporter;

        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
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
        camera.backgroundColor = new Color(0.006f, 0.012f, 0.027f, 1f);
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
            "SplashCanvas",
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

        Image image = backgroundObject.GetComponent<Image>();
        image.color = new Color(0.006f, 0.012f, 0.027f, 1f);
        image.raycastTarget = false;
    }

    private static Image CreateGlow(Transform parent)
    {
        GameObject glowObject = new GameObject(
            "Glow",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        glowObject.transform.SetParent(parent, false);

        RectTransform rect = glowObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(880f, 360f);

        Image image = glowObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.72f, 1f, 0.22f);
        image.raycastTarget = false;
        glowObject.GetComponent<CanvasGroup>().alpha = 0.12f;
        return image;
    }

    private static Image CreateLogo(Transform parent, Sprite logoSprite)
    {
        GameObject logoObject = new GameObject(
            "Logo",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        logoObject.transform.SetParent(parent, false);

        RectTransform rect = logoObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(780f, 300f);

        Image image = logoObject.GetComponent<Image>();
        image.sprite = logoSprite;
        image.preserveAspect = true;
        image.color = Color.white;
        image.raycastTarget = false;
        logoObject.GetComponent<CanvasGroup>().alpha = 0f;
        return image;
    }

    private static Image CreateLightSweep(Transform parent)
    {
        GameObject sweepObject = new GameObject(
            "LightSweep",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup)
        );
        sweepObject.transform.SetParent(parent, false);

        RectTransform rect = sweepObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-620f, 0f);
        rect.sizeDelta = new Vector2(72f, 420f);
        rect.localEulerAngles = new Vector3(0f, 0f, -14f);

        Image image = sweepObject.GetComponent<Image>();
        image.color = new Color(0.38f, 0.86f, 1f, 0.42f);
        image.raycastTarget = false;
        sweepObject.GetComponent<CanvasGroup>().alpha = 0f;
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
        StudioSplashManager manager,
        Canvas canvas,
        Image logo,
        Image glow,
        Image lightSweep,
        Image fadeOverlay,
        AudioSource voiceAudioSource
    )
    {
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("splashCanvas").objectReferenceValue =
            canvas;
        serializedManager.FindProperty("logoGroup").objectReferenceValue =
            logo.GetComponent<CanvasGroup>();
        serializedManager.FindProperty("logoRect").objectReferenceValue =
            logo.rectTransform;
        serializedManager.FindProperty("glowGroup").objectReferenceValue =
            glow.GetComponent<CanvasGroup>();
        serializedManager.FindProperty("glowRect").objectReferenceValue =
            glow.rectTransform;
        serializedManager.FindProperty("lightSweepGroup").objectReferenceValue =
            lightSweep.GetComponent<CanvasGroup>();
        serializedManager.FindProperty("lightSweepRect").objectReferenceValue =
            lightSweep.rectTransform;
        serializedManager.FindProperty("fadeOverlayGroup").objectReferenceValue =
            fadeOverlay.GetComponent<CanvasGroup>();
        serializedManager.FindProperty("voiceAudioSource").objectReferenceValue =
            voiceAudioSource;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(StudioSplashScenePath, true),
            new EditorBuildSettingsScene(OpeningScenePath, true),
            new EditorBuildSettingsScene(TitleScenePath, true),
            new EditorBuildSettingsScene(BattleScenePath, true)
        };
    }
}
