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
    [SerializeField] private Button storyButton;
    [SerializeField] private Button collectionButton;
    [SerializeField] private Button settingsButton;
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

        if (storyButton != null)
            storyButton.onClick.AddListener(OnStoryClicked);

        if (collectionButton != null)
            collectionButton.onClick.AddListener(OnCollectionClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

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
        CreateImage(parent, "Background", new Color(0.04f, 0.05f, 0.09f),
            new Vector2(0.5f, 0.5f), new Vector2(1080f, 1920f), false);
        CreateImage(parent, "ReadabilityOverlay", new Color(0f, 0f, 0f, 0.32f),
            new Vector2(0.5f, 0.5f), new Vector2(1080f, 1920f), false);

        CreateText(parent, "WordTowerTitle", "WORD TOWER", 104f, FontStyles.Bold,
            new Vector2(0.5f, 0.79f), new Vector2(900f, 150f),
            new Color(1f, 0.83f, 0.32f));
        CreateText(parent, "Subtitle", "빼앗긴 단어를 되찾는 모험", 34f, FontStyles.Bold,
            new Vector2(0.5f, 0.725f), new Vector2(820f, 80f), Color.white);

        startButton = CreateButton(parent, "StartButton", "게임 시작",
            new Color(0.78f, 0.48f, 0.18f), new Vector2(0.5f, 0.255f),
            new Vector2(660f, 118f));
        startButtonText = startButton.transform.Find("Label").GetComponent<TMP_Text>();

        storyButton = CreateButton(parent, "StoryButton", "STORY",
            new Color(0.16f, 0.18f, 0.24f, 0.86f), new Vector2(0.275f, 0.17f),
            new Vector2(250f, 82f));
        collectionButton = CreateButton(parent, "CollectionButton", "도감",
            new Color(0.16f, 0.18f, 0.24f, 0.86f), new Vector2(0.5f, 0.17f),
            new Vector2(250f, 82f));
        settingsButton = CreateButton(parent, "SettingsButton", "설정",
            new Color(0.16f, 0.18f, 0.24f, 0.86f), new Vector2(0.725f, 0.17f),
            new Vector2(250f, 82f));

        quitButton = CreateButton(parent, "QuitButton", "종료",
            new Color(0.02f, 0.025f, 0.035f, 0.46f), new Vector2(0.89f, 0.94f),
            new Vector2(150f, 62f));

        versionText = CreateText(parent, "VersionText", "", 26f, FontStyles.Normal,
            new Vector2(0.5f, 0.055f), new Vector2(500f, 60f),
            new Color(0.78f, 0.80f, 0.86f, 0.82f));
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

    void OnStoryClicked()
    {
        Debug.Log("[WordTower] STORY menu will be implemented next.");
    }

    void OnCollectionClicked()
    {
        Debug.Log("[WordTower] Collection menu will be implemented later.");
    }

    void OnSettingsClicked()
    {
        Debug.Log("[WordTower] Settings menu will be implemented later.");
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
