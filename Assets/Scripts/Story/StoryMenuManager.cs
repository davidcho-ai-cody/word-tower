using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryMenuManager : MonoBehaviour
{
    private const string OpeningSceneName = "OpeningScene";
    private const string TitleSceneName = "TitleScene";
    private const string StoryPlaybackSceneName = "StoryPlaybackScene";

    private static readonly Color UnlockedCardColor =
        new Color(0.13f, 0.09f, 0.29f, 0.97f);
    private static readonly Color LoveUnlockedCardColor =
        new Color(0.15f, 0.08f, 0.28f, 0.98f);
    private static readonly Color GoldAccentColor =
        new Color(0.94f, 0.69f, 0.28f, 0.92f);
    private static readonly Color WarmTitleColor =
        new Color(0.98f, 0.94f, 0.84f, 1f);
    private static readonly Color SoftDescriptionColor =
        new Color(0.9f, 0.86f, 0.78f, 0.88f);
    private static readonly Color RoseDescriptionColor =
        new Color(0.95f, 0.78f, 0.82f, 0.9f);
    private static readonly Color LockedCardColor =
        new Color(0.08f, 0.1f, 0.16f, 0.9f);
    private static readonly Color LockedBorderColor =
        new Color(0.24f, 0.2f, 0.36f, 0.55f);
    private static readonly Color LockedHeaderColor =
        new Color(0.64f, 0.68f, 0.78f, 1f);
    private static readonly Color LockedDescriptionColor =
        new Color(0.82f, 0.85f, 0.92f, 1f);

    [SerializeField] private Button backButton;
    [SerializeField] private Button prologueButton;
    [SerializeField] private Button[] lockedChapterButtons;
    [SerializeField] private TMP_Text progressValueText;

    private StoryProgressService storyProgressService;
    private bool isSceneTransitioning;

    void Awake()
    {
        storyProgressService = new StoryProgressService();

        if (backButton != null)
            backButton.onClick.AddListener(ReturnToTitle);

        if (prologueButton != null)
            prologueButton.onClick.AddListener(PlayPrologue);

        UpdateProgress();
        UpdateChapterCards();
    }

    void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            ReturnToTitle();
    }

    void UpdateProgress()
    {
        if (progressValueText != null)
        {
            int progress =
                storyProgressService.GetUnlockedFloorStoryCount();
            progressValueText.text =
                $"{progress} / {StoryCatalog.TotalFloorStoryCount}";
        }
    }

    void UpdateChapterCards()
    {
        if (lockedChapterButtons == null)
            return;

        bool isFloor10Unlocked = storyProgressService.IsStoryUnlocked(
            StoryCatalog.Floor10ClearStoryId
        );

        for (int i = 0; i < lockedChapterButtons.Length; i++)
        {
            Button button = lockedChapterButtons[i];
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();

            if (i == 0 && isFloor10Unlocked)
            {
                button.interactable = true;
                button.onClick.AddListener(PlayFloor10Story);
                SetCardText(
                    button.transform,
                    StoryCatalog.Floor10KeywordName,
                    StoryCatalog.Floor10ChapterDescription,
                    "PLAY"
                );
                ApplyUnlockedCardStyle(button.transform, true);
            }
            else
            {
                button.interactable = false;
                SetCardText(
                    button.transform,
                    "???",
                    "아직 되찾지 못한 단어입니다.",
                    "LOCK"
                );
                ApplyLockedCardStyle(button.transform);
            }
        }
    }

    void SetCardText(
        Transform card,
        string title,
        string description,
        string status
    )
    {
        TMP_Text titleText =
            card.Find("Title")?.GetComponent<TMP_Text>();
        TMP_Text descriptionText =
            card.Find("Description")?.GetComponent<TMP_Text>();
        TMP_Text statusText =
            card.Find("LockLabel")?.GetComponent<TMP_Text>();

        if (titleText != null)
            titleText.text = title;

        if (descriptionText != null)
            descriptionText.text = description;

        if (statusText != null)
            statusText.text = status;
    }

    void ApplyUnlockedCardStyle(Transform card, bool useRoseAccent)
    {
        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null)
        {
            cardImage.color = useRoseAccent
                ? LoveUnlockedCardColor
                : UnlockedCardColor;
        }

        Outline outline = card.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = GoldAccentColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        SetTextColor(card, "Eyebrow", GoldAccentColor);
        SetTextColor(card, "Title", WarmTitleColor);
        SetTextColor(
            card,
            "Description",
            useRoseAccent
                ? RoseDescriptionColor
                : SoftDescriptionColor
        );
        SetTextColor(card, "LockLabel", GoldAccentColor);
    }

    void ApplyLockedCardStyle(Transform card)
    {
        Image cardImage = card.GetComponent<Image>();
        if (cardImage != null)
            cardImage.color = LockedCardColor;

        Outline outline = card.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = LockedBorderColor;
            outline.effectDistance = new Vector2(2f, -2f);
        }

        SetTextColor(card, "Eyebrow", LockedHeaderColor);
        SetTextColor(card, "Title", Color.white);
        SetTextColor(card, "Description", LockedDescriptionColor);
        SetTextColor(card, "LockLabel", GoldAccentColor);
    }

    void SetTextColor(Transform card, string childName, Color color)
    {
        TMP_Text text = card.Find(childName)?.GetComponent<TMP_Text>();
        if (text != null)
            text.color = color;
    }

    void PlayPrologue()
    {
        if (isSceneTransitioning)
            return;

        isSceneTransitioning = true;

        if (prologueButton != null)
            prologueButton.interactable = false;

        OpeningStoryManager.RequestReplay();
        SceneManager.LoadScene(OpeningSceneName);
    }

    void PlayFloor10Story()
    {
        if (isSceneTransitioning)
            return;

        isSceneTransitioning = true;

        if (lockedChapterButtons != null &&
            lockedChapterButtons.Length > 0 &&
            lockedChapterButtons[0] != null)
        {
            lockedChapterButtons[0].interactable = false;
        }

        StoryPlaybackManager.RequestReplay(
            StoryCatalog.Floor10ClearStoryId,
            "StoryScene"
        );
        SceneManager.LoadScene(StoryPlaybackSceneName);
    }

    void ReturnToTitle()
    {
        if (isSceneTransitioning)
            return;

        isSceneTransitioning = true;

        if (backButton != null)
            backButton.interactable = false;

        SceneManager.LoadScene(TitleSceneName);
    }
}
