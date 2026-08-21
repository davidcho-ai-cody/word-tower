using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleManager : MonoBehaviour
{
    private const string BattleSceneName = "BattleScene";

    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text startButtonText;
    [SerializeField] private Button quitButton;
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private TMP_FontAsset titleFont;

    private SaveService saveService;
    private bool isLoadingBattleScene;

    void Awake()
    {
        CreateRuntimeUIIfNeeded();
        saveService = new SaveService();

        if (startButton != null)
            startButton.onClick.AddListener(StartOrContinueGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        RefreshUI();
    }

    void CreateRuntimeUIIfNeeded()
    {
        if (startButton != null && quitButton != null && versionText != null)
            return;

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule)
            );
        }

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

        Transform parent = canvas.transform;
        CreateImage(parent, "Background", new Color(0.08f, 0.12f, 0.20f),
            new Vector2(0.5f, 0.5f), new Vector2(1080f, 1920f), false);
        CreateImage(parent, "TitlePanel", new Color(0.12f, 0.18f, 0.28f, 0.92f),
            new Vector2(0.5f, 0.53f), new Vector2(860f, 1080f), false);

        CreateText(parent, "GameTitle", "WORD TOWER", 96f, FontStyles.Bold,
            new Vector2(0.5f, 0.70f), new Vector2(900f, 150f),
            new Color(1f, 0.86f, 0.35f));
        CreateText(parent, "Subtitle", "끝말잇기 용사의 모험", 38f, FontStyles.Bold,
            new Vector2(0.5f, 0.62f), new Vector2(760f, 80f), Color.white);

        startButton = CreateButton(parent, "StartButton", "게임 시작",
            new Color(0.25f, 0.72f, 0.42f), new Vector2(0.5f, 0.42f),
            new Vector2(600f, 120f));
        startButtonText = startButton.transform.Find("Label").GetComponent<TMP_Text>();

        quitButton = CreateButton(parent, "QuitButton", "종료",
            new Color(0.34f, 0.39f, 0.48f), new Vector2(0.5f, 0.33f),
            new Vector2(600f, 105f));

        versionText = CreateText(parent, "VersionText", "", 26f, FontStyles.Normal,
            new Vector2(0.5f, 0.055f), new Vector2(500f, 60f),
            new Color(0.75f, 0.80f, 0.88f));
    }

    GameObject CreateImage(
        Transform parent,
        string objectName,
        Color color,
        Vector2 anchor,
        Vector2 size,
        bool raycastTarget
    )
    {
        GameObject imageObject = new GameObject(
            objectName,
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
        image.raycastTarget = raycastTarget;
        return imageObject;
    }

    TMP_Text CreateText(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Vector2 anchor,
        Vector2 size,
        Color color
    )
    {
        GameObject textObject = new GameObject(
            objectName,
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
        if (titleFont != null)
            text.font = titleFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    Button CreateButton(
        Transform parent,
        string objectName,
        string label,
        Color color,
        Vector2 anchor,
        Vector2 size
    )
    {
        GameObject buttonObject = CreateImage(
            parent,
            objectName,
            color,
            anchor,
            size,
            true
        );
        Button button = buttonObject.AddComponent<Button>();
        CreateText(buttonObject.transform, "Label", label, 40f, FontStyles.Bold,
            new Vector2(0.5f, 0.5f), size, Color.white);
        return button;
    }

    void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            QuitGame();
    }

    void RefreshUI()
    {
        if (startButtonText != null)
        {
            startButtonText.text = saveService.HasSave()
                ? "이어하기"
                : "게임 시작";
        }

        if (versionText != null)
            versionText.text = $"Ver. {Application.version}";
    }

    void StartOrContinueGame()
    {
        if (isLoadingBattleScene)
            return;

        isLoadingBattleScene = true;

        if (startButton != null)
            startButton.interactable = false;

        SceneManager.LoadScene(BattleSceneName);
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
