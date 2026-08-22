using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StoryPlaybackManager : MonoBehaviour
{
    private const string StorySceneName = "StoryScene";
    private const string BattleSceneName = "BattleScene";
    private const float InitialMotionEaseDuration = 0.45f;

    private static string requestedStoryId;
    private static string requestedReturnSceneName = StorySceneName;
    private static bool requestedUnlockOnComplete;
    private static bool requestedQueueBattleVictory;
    private static string pendingBattleVictoryStoryId;
    private static int pendingBattleReturnFloor;

    [SerializeField] private string storyId =
        StoryCatalog.Floor10ClearStoryId;
    [SerializeField] private Canvas storyCanvas;
    [SerializeField] private Image storyImageA;
    [SerializeField] private Image storyImageB;
    [SerializeField] private CanvasGroup storyImageGroupA;
    [SerializeField] private CanvasGroup storyImageGroupB;
    [SerializeField] private CanvasGroup fadeOverlayGroup;
    [SerializeField] private CanvasGroup acquisitionOverlayGroup;
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text skipButtonText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text acquisitionTitleText;
    [SerializeField] private TMP_Text acquisitionKeywordText;
    [SerializeField] private TMP_Text acquisitionProgressText;
    [SerializeField] private float crossFadeDuration = 0.3f;
    [SerializeField] private float sceneFadeOutDuration = 0.5f;
    [SerializeField] private float acquisitionOverlayDuration = 2.8f;
    [SerializeField] private StoryCut[] storyCuts;

    private StoryProgressService storyProgressService;
    private bool isCompleting;
    private bool shouldUnlockOnComplete;
    private bool shouldQueueBattleVictory;
    private string returnSceneName;
    private int currentCutIndex = -1;
    private int incomingCutIndex = -1;
    private float playbackStartUnscaledTime;
    private Image activeImage;
    private Image inactiveImage;
    private CanvasGroup activeGroup;
    private CanvasGroup inactiveGroup;
    private Coroutine crossFadeCoroutine;

    public static void RequestFirstClearStory(string storyId)
    {
        requestedStoryId = storyId;
        requestedReturnSceneName = BattleSceneName;
        requestedUnlockOnComplete = true;
        requestedQueueBattleVictory = true;
    }

    public static void RequestReplay(
        string storyId,
        string returnSceneName = StorySceneName
    )
    {
        requestedStoryId = storyId;
        requestedReturnSceneName = returnSceneName;
        requestedUnlockOnComplete = false;
        requestedQueueBattleVictory = false;
    }

    public static bool ConsumePendingBattleVictory(
        string storyId,
        out int returnFloor
    )
    {
        returnFloor = pendingBattleReturnFloor;

        if (string.IsNullOrEmpty(pendingBattleVictoryStoryId) ||
            pendingBattleVictoryStoryId != storyId)
        {
            return false;
        }

        pendingBattleVictoryStoryId = null;
        pendingBattleReturnFloor = 0;
        return true;
    }

    void Awake()
    {
        if (storyCanvas != null)
            storyCanvas.enabled = false;

        storyProgressService = new StoryProgressService();
    }

    IEnumerator Start()
    {
        ConsumeRequest();

        if (!ValidateReferences())
        {
            CompleteStory();
            yield break;
        }

        InitializeVisuals();

        if (skipButton != null)
            skipButton.onClick.AddListener(CompleteStory);

        if (storyCanvas != null)
            storyCanvas.enabled = true;

        playbackStartUnscaledTime = Time.unscaledTime;
        yield return PlayStoryTimeline();
    }

    void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            CompleteStory();
    }

    void ConsumeRequest()
    {
        if (!string.IsNullOrEmpty(requestedStoryId))
            storyId = requestedStoryId;

        returnSceneName = string.IsNullOrEmpty(requestedReturnSceneName)
            ? StorySceneName
            : requestedReturnSceneName;
        shouldUnlockOnComplete = requestedUnlockOnComplete;
        shouldQueueBattleVictory = requestedQueueBattleVictory;

        requestedStoryId = null;
        requestedReturnSceneName = StorySceneName;
        requestedUnlockOnComplete = false;
        requestedQueueBattleVictory = false;
    }

    IEnumerator PlayStoryTimeline()
    {
        float finalEndTime = GetFinalEndTime();

        while (!isCompleting)
        {
            float playbackTime = GetPlaybackTime();
            int cutIndex = GetFadeAwareCutIndex(playbackTime);

            if (currentCutIndex < 0)
                ShowFirstCut(cutIndex, playbackTime);
            else if (cutIndex != currentCutIndex &&
                cutIndex != incomingCutIndex)
                StartCutTransition(cutIndex, playbackTime);

            UpdateCutMotion(activeImage, currentCutIndex, playbackTime);
            UpdateCutMotion(inactiveImage, incomingCutIndex, playbackTime);

            if (playbackTime >= finalEndTime)
                break;

            yield return null;
        }

        CompleteStory();
    }

    void ShowFirstCut(int cutIndex, float playbackTime)
    {
        if (!IsValidCut(cutIndex))
            return;

        currentCutIndex = cutIndex;
        activeImage.sprite = storyCuts[cutIndex].sprite;
        activeGroup.alpha = 1f;
        inactiveGroup.alpha = 0f;
        ApplyDialogue(cutIndex);
        UpdateCutMotion(activeImage, currentCutIndex, playbackTime);
    }

    void StartCutTransition(int cutIndex, float playbackTime)
    {
        if (!IsValidCut(cutIndex))
            return;

        if (crossFadeCoroutine != null)
            StopCoroutine(crossFadeCoroutine);

        incomingCutIndex = cutIndex;
        inactiveImage.sprite = storyCuts[cutIndex].sprite;
        ApplyDialogue(cutIndex);
        UpdateCutMotion(inactiveImage, incomingCutIndex, playbackTime);
        crossFadeCoroutine = StartCoroutine(CrossFadeToInactiveLayer());
    }

    IEnumerator CrossFadeToInactiveLayer()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, crossFadeDuration);
        CanvasGroup fromGroup = activeGroup;
        CanvasGroup toGroup = inactiveGroup;
        Image fromImage = activeImage;
        Image toImage = inactiveImage;

        toGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fromGroup.alpha = 1f - t;
            toGroup.alpha = t;
            UpdateCutMotion(fromImage, currentCutIndex, GetPlaybackTime());
            UpdateCutMotion(toImage, incomingCutIndex, GetPlaybackTime());
            yield return null;
        }

        fromGroup.alpha = 0f;
        toGroup.alpha = 1f;
        currentCutIndex = incomingCutIndex;
        incomingCutIndex = -1;
        activeGroup = toGroup;
        inactiveGroup = fromGroup;
        activeImage = toImage;
        inactiveImage = fromImage;
        crossFadeCoroutine = null;
    }

    void UpdateCutMotion(Image image, int cutIndex, float playbackTime)
    {
        if (image == null || !IsValidCut(cutIndex))
            return;

        StoryCut cut = storyCuts[cutIndex];
        RectTransform rect = image.rectTransform;
        float motionStartTime = GetTransitionStartTime(cutIndex);
        float duration = Mathf.Max(0.01f, cut.endTime - motionStartTime);
        float normalizedTime = Mathf.Clamp01(
            (playbackTime - motionStartTime) / duration
        );
        float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
        float entryTime = Mathf.Clamp01(
            (playbackTime - motionStartTime) / InitialMotionEaseDuration
        );
        float entryMotionWeight = Mathf.Lerp(
            0.35f,
            1f,
            Mathf.SmoothStep(0f, 1f, entryTime)
        );
        easedTime *= entryMotionWeight;

        float zoom = Mathf.Lerp(cut.zoomFrom, cut.zoomTo, easedTime);
        rect.localScale = new Vector3(zoom, zoom, 1f);
        rect.anchoredPosition = Vector2.Lerp(
            cut.panFrom,
            cut.panTo,
            easedTime
        );
    }

    void ApplyDialogue(int cutIndex)
    {
        if (!IsValidCut(cutIndex))
            return;

        StoryCut cut = storyCuts[cutIndex];
        bool hasSpeaker = !string.IsNullOrWhiteSpace(cut.speaker);

        if (speakerNameText != null)
        {
            speakerNameText.gameObject.SetActive(hasSpeaker);
            speakerNameText.text = cut.speaker;
        }

        if (dialogueText != null)
            dialogueText.text = cut.dialogue;
    }

    void CompleteStory()
    {
        if (isCompleting)
            return;

        isCompleting = true;

        if (skipButton != null)
            skipButton.interactable = false;

        if (shouldUnlockOnComplete)
            storyProgressService.UnlockStoryAndSave(storyId);

        StartCoroutine(CompleteStorySequence());
    }

    IEnumerator CompleteStorySequence()
    {
        if (shouldUnlockOnComplete)
            yield return ShowAcquisitionOverlay();

        if (shouldQueueBattleVictory)
        {
            pendingBattleVictoryStoryId = storyId;
            pendingBattleReturnFloor = 10;
        }

        yield return FadeOutAndLoadReturnScene();
    }

    IEnumerator ShowAcquisitionOverlay()
    {
        if (acquisitionOverlayGroup == null)
            yield break;

        if (acquisitionTitleText != null)
            acquisitionTitleText.text = "빼앗긴 단어를 되찾았습니다";

        if (acquisitionKeywordText != null)
            acquisitionKeywordText.text = StoryCatalog.Floor10KeywordName;

        if (acquisitionProgressText != null)
        {
            int progress = storyProgressService.GetUnlockedFloorStoryCount();
            acquisitionProgressText.text =
                $"{progress} / {StoryCatalog.TotalFloorStoryCount}";
        }

        acquisitionOverlayGroup.gameObject.SetActive(true);

        float fadeDuration = 0.35f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            acquisitionOverlayGroup.alpha =
                Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        acquisitionOverlayGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(acquisitionOverlayDuration);
    }

    IEnumerator FadeOutAndLoadReturnScene()
    {
        if (fadeOverlayGroup != null)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, sceneFadeOutDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlayGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
        }

        SceneManager.LoadScene(returnSceneName);
    }

    void InitializeVisuals()
    {
        activeImage = storyImageA;
        inactiveImage = storyImageB;
        activeGroup = storyImageGroupA;
        inactiveGroup = storyImageGroupB;
        currentCutIndex = -1;
        incomingCutIndex = -1;

        activeGroup.alpha = 0f;
        inactiveGroup.alpha = 0f;

        if (fadeOverlayGroup != null)
            fadeOverlayGroup.alpha = 0f;

        if (acquisitionOverlayGroup != null)
        {
            acquisitionOverlayGroup.alpha = 0f;
            acquisitionOverlayGroup.gameObject.SetActive(false);
        }

        if (skipButtonText != null)
            skipButtonText.text = "SKIP";
    }

    bool ValidateReferences()
    {
        bool valid = true;

        if (storyId != StoryCatalog.Floor10ClearStoryId)
        {
            Debug.LogError($"Unknown story id: {storyId}");
            valid = false;
        }

        if (storyImageA == null ||
            storyImageB == null ||
            storyImageGroupA == null ||
            storyImageGroupB == null ||
            storyCuts == null ||
            storyCuts.Length == 0)
        {
            Debug.LogError("Story Playback UI 또는 Timeline 설정이 누락됐습니다.");
            valid = false;
        }

        return valid;
    }

    bool IsValidCut(int cutIndex)
    {
        return storyCuts != null &&
            cutIndex >= 0 &&
            cutIndex < storyCuts.Length &&
            storyCuts[cutIndex].sprite != null;
    }

    float GetPlaybackTime()
    {
        return Time.unscaledTime - playbackStartUnscaledTime;
    }

    int GetFadeAwareCutIndex(float playbackTime)
    {
        if (storyCuts == null || storyCuts.Length == 0)
            return -1;

        for (int i = storyCuts.Length - 1; i >= 0; i--)
        {
            if (playbackTime >= GetTransitionStartTime(i))
                return i;
        }

        return 0;
    }

    float GetTransitionStartTime(int cutIndex)
    {
        if (storyCuts == null ||
            cutIndex <= 0 ||
            cutIndex >= storyCuts.Length)
        {
            return 0f;
        }

        return Mathf.Max(
            0f,
            storyCuts[cutIndex].startTime - Mathf.Max(0f, crossFadeDuration)
        );
    }

    float GetFinalEndTime()
    {
        if (storyCuts == null || storyCuts.Length == 0)
            return 0f;

        return storyCuts[storyCuts.Length - 1].endTime;
    }

    [Serializable]
    public class StoryCut
    {
        public Sprite sprite;
        public string speaker;
        [TextArea(2, 5)] public string dialogue;
        public float startTime;
        public float endTime;
        public Vector2 panFrom;
        public Vector2 panTo;
        public float zoomFrom = 1f;
        public float zoomTo = 1.03f;
    }
}
