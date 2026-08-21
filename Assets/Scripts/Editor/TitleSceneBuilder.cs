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
    private static readonly Color BackgroundColor = new Color(0.08f, 0.12f, 0.20f);
    private static readonly Color PanelColor = new Color(0.12f, 0.18f, 0.28f, 0.92f);
    private static readonly Color PrimaryButtonColor = new Color(0.25f, 0.72f, 0.42f);
    private static readonly Color SecondaryButtonColor = new Color(0.34f, 0.39f, 0.48f);
    private static TMP_FontAsset koreanFont;

    [MenuItem("WordTower/Build Title Scene")]
    public static void BuildTitleScene()
    {
        koreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/NotoSansKR-Regular SDF.asset"
        );

        if (koreanFont == null)
        {
            Debug.LogError("TitleScene 생성에 필요한 한글 TMP 폰트를 찾을 수 없습니다.");
            return;
        }

        Scene titleScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        CreateCamera();
        CreateEventSystem();

        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform);
        CreateTitleContent(canvas.transform);

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
        camera.backgroundColor = BackgroundColor;
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

    private static void CreateBackground(Transform parent)
    {
        GameObject background = CreateImage(
            parent,
            "Background",
            BackgroundColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(1080f, 1920f)
        );
        background.transform.SetAsFirstSibling();

        CreateImage(
            parent,
            "TitlePanel",
            PanelColor,
            new Vector2(0.5f, 0.53f),
            new Vector2(860f, 1080f)
        );
    }

    private static void CreateTitleContent(Transform parent)
    {
        GameObject managerObject = new GameObject("TitleManager");
        TitleManager titleManager = managerObject.AddComponent<TitleManager>();

        CreateText(
            parent,
            "GameTitle",
            "WORD TOWER",
            96f,
            FontStyles.Bold,
            new Vector2(0.5f, 0.70f),
            new Vector2(900f, 150f),
            new Color(1f, 0.86f, 0.35f)
        );

        CreateText(
            parent,
            "Subtitle",
            "끝말잇기 용사의 모험",
            38f,
            FontStyles.Bold,
            new Vector2(0.5f, 0.62f),
            new Vector2(760f, 80f),
            Color.white
        );

        Button startButton = CreateButton(
            parent,
            "StartButton",
            "게임 시작",
            PrimaryButtonColor,
            new Vector2(0.5f, 0.42f),
            new Vector2(600f, 120f)
        );

        Button quitButton = CreateButton(
            parent,
            "QuitButton",
            "종료",
            SecondaryButtonColor,
            new Vector2(0.5f, 0.33f),
            new Vector2(600f, 105f)
        );

        TMP_Text versionText = CreateText(
            parent,
            "VersionText",
            "Ver. 0.0.0",
            26f,
            FontStyles.Normal,
            new Vector2(0.5f, 0.055f),
            new Vector2(500f, 60f),
            new Color(0.75f, 0.80f, 0.88f)
        );

        SerializedObject serializedManager = new SerializedObject(titleManager);
        serializedManager.FindProperty("startButton").objectReferenceValue = startButton;
        serializedManager.FindProperty("startButtonText").objectReferenceValue =
            startButton.transform.Find("Label").GetComponent<TMP_Text>();
        serializedManager.FindProperty("quitButton").objectReferenceValue = quitButton;
        serializedManager.FindProperty("versionText").objectReferenceValue = versionText;
        serializedManager.FindProperty("titleFont").objectReferenceValue = koreanFont;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateImage(
        Transform parent,
        string name,
        Color color,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject imageObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return imageObject;
    }

    private static TMP_Text CreateText(
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
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = koreanFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;

        return text;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Color color,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject buttonObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;

        Button button = buttonObject.GetComponent<Button>();

        CreateText(
            buttonObject.transform,
            "Label",
            label,
            40f,
            FontStyles.Bold,
            new Vector2(0.5f, 0.5f),
            size,
            Color.white
        );

        return button;
    }
}
