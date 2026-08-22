using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OpeningStoryManager : MonoBehaviour
{
    private const string TitleSceneName = "TitleScene";
    private const float AudioFadeInDuration = 1.3f;
    private const float AudioFadeOutDuration = 0.9f;
    private const float InitialMotionEaseDuration = 0.45f;
    private const float FinalCutStableDuration = 1.8f;

    private static readonly float[] TunedCutStartTimes =
    {
        0f, 3.8f, 6.2f, 9.7f, 14f, 17.4f, 21.2f, 26f
    };

    private static readonly float[] TunedCutEndTimes =
    {
        3.8f, 6.2f, 9.7f, 14f, 17.4f, 21.2f, 26f, 30.8f
    };

    private static bool replayRequested;

    [SerializeField] private bool forceReplay = false;
    [SerializeField] private Canvas openingCanvas;
    [SerializeField] private Image storyImageA;
    [SerializeField] private Image storyImageB;
    [SerializeField] private CanvasGroup storyImageGroupA;
    [SerializeField] private CanvasGroup storyImageGroupB;
    [SerializeField] private CanvasGroup fadeOverlayGroup;
    [SerializeField] private Button skipButton;
    [SerializeField] private TMP_Text skipButtonText;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float crossFadeDuration = 0.3f;
    [SerializeField] private float titleFadeOutDuration = 0.5f;
    [SerializeField] private StoryCut[] storyCuts;

    private StoryProgressService storyProgressService;
    private bool isCompleting;
    private int currentCutIndex = -1;
    private float playbackStartUnscaledTime;
    private Image activeImage;
    private Image inactiveImage;
    private CanvasGroup activeGroup;
    private CanvasGroup inactiveGroup;
    private Coroutine crossFadeCoroutine;
    private int incomingCutIndex = -1;
    private float targetAudioVolume = 1f;

    public static void RequestReplay()
    {
        replayRequested = true;
    }

    void Awake()
    {
        if (openingCanvas != null)
            openingCanvas.enabled = false;

        storyProgressService = new StoryProgressService();
    }

    IEnumerator Start()
    {
        StoryProgressData storyProgress =
            storyProgressService.LoadOrCreate();
        bool shouldForceReplay = forceReplay || ConsumeReplayRequest();

        if (!shouldForceReplay && storyProgress.hasSeenOpeningStory)
        {
            LoadTitleScene();
            yield break;
        }

        if (!ValidateReferences())
        {
            CompleteOpeningStory();
            yield break;
        }

        InitializeVisuals();

        if (skipButton != null)
            skipButton.onClick.AddListener(CompleteOpeningStory);

        if (openingCanvas != null)
            openingCanvas.enabled = true;

        playbackStartUnscaledTime = Time.unscaledTime;

        if (audioSource != null && audioSource.clip != null)
            audioSource.Play();

        yield return PlayStoryTimeline();
    }

    static bool ConsumeReplayRequest()
    {
        if (!replayRequested)
            return false;

        replayRequested = false;
        return true;
    }

    void Update()
    {
        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            CompleteOpeningStory();
    }

    IEnumerator PlayStoryTimeline()
    {
        float finalEndTime = GetFinalEndTime();

        while (!isCompleting)
        {
            float playbackTime = GetPlaybackTime();
            UpdateOpeningAudioVolume(playbackTime, finalEndTime);

            int cutIndex = GetFadeAwareCutIndex(playbackTime);

            if (currentCutIndex < 0)
            {
                ShowFirstCut(cutIndex, playbackTime);
            }
            else if (cutIndex != currentCutIndex &&
                cutIndex != incomingCutIndex)
            {
                StartCutTransition(cutIndex, playbackTime);
            }

            UpdateCutMotion(activeImage, currentCutIndex, playbackTime);
            UpdateCutMotion(inactiveImage, incomingCutIndex, playbackTime);

            if (playbackTime >= finalEndTime)
                break;

            yield return null;
        }

        CompleteOpeningStory();
    }

    void ShowFirstCut(int cutIndex, float playbackTime)
    {
        if (cutIndex < 0 ||
            storyCuts == null ||
            cutIndex >= storyCuts.Length ||
            storyCuts[cutIndex].sprite == null)
        {
            return;
        }

        currentCutIndex = cutIndex;
        activeImage.sprite = storyCuts[cutIndex].sprite;
        activeGroup.alpha = 1f;
        inactiveGroup.alpha = 0f;
        UpdateCutMotion(activeImage, currentCutIndex, playbackTime);
    }

    void StartCutTransition(int cutIndex, float playbackTime)
    {
        if (cutIndex < 0 ||
            storyCuts == null ||
            cutIndex >= storyCuts.Length ||
            storyCuts[cutIndex].sprite == null)
        {
            return;
        }

        if (crossFadeCoroutine != null)
            StopCoroutine(crossFadeCoroutine);

        incomingCutIndex = cutIndex;
        inactiveImage.sprite = storyCuts[cutIndex].sprite;
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
        if (image == null ||
            storyCuts == null ||
            cutIndex < 0 ||
            cutIndex >= storyCuts.Length)
        {
            return;
        }

        StoryCut cut = storyCuts[cutIndex];
        RectTransform rect = image.rectTransform;
        float motionStartTime = GetMotionStartTime(cutIndex);
        float motionEndTime = GetMotionEndTime(cutIndex);
        float duration = Mathf.Max(0.01f, motionEndTime - motionStartTime);
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

    void CompleteOpeningStory()
    {
        if (isCompleting)
            return;

        isCompleting = true;

        if (skipButton != null)
            skipButton.interactable = false;

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = targetAudioVolume;
        }

        storyProgressService.MarkOpeningStorySeen();
        StartCoroutine(FadeOutAndLoadTitle());
    }

    IEnumerator FadeOutAndLoadTitle()
    {
        if (fadeOverlayGroup != null)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, titleFadeOutDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlayGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
        }

        LoadTitleScene();
    }

    void LoadTitleScene()
    {
        SceneManager.LoadScene(TitleSceneName);
    }

    bool ValidateReferences()
    {
        bool valid = true;

        if (storyImageA == null ||
            storyImageB == null ||
            storyImageGroupA == null ||
            storyImageGroupB == null ||
            storyCuts == null ||
            storyCuts.Length == 0)
        {
            Debug.LogError("Opening Story UI 또는 Timeline 설정이 누락됐습니다.");
            valid = false;
        }

        return valid;
    }

    void InitializeVisuals()
    {
        ApplyTunedTimeline();

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

        if (skipButtonText != null)
            skipButtonText.text = "SKIP ▶";

        if (audioSource != null)
        {
            targetAudioVolume = audioSource.volume;
            audioSource.volume = 0f;
        }
    }

    void ApplyTunedTimeline()
    {
        if (storyCuts == null)
            return;

        int count = Math.Min(
            storyCuts.Length,
            Math.Min(TunedCutStartTimes.Length, TunedCutEndTimes.Length)
        );

        for (int i = 0; i < count; i++)
        {
            storyCuts[i].startTime = TunedCutStartTimes[i];
            storyCuts[i].endTime = TunedCutEndTimes[i];
        }
    }

    float GetPlaybackTime()
    {
        if (audioSource != null &&
            audioSource.clip != null &&
            audioSource.isPlaying)
        {
            return audioSource.time;
        }

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

    float GetMotionStartTime(int cutIndex)
    {
        if (storyCuts == null ||
            cutIndex < 0 ||
            cutIndex >= storyCuts.Length)
        {
            return 0f;
        }

        return GetTransitionStartTime(cutIndex);
    }

    float GetMotionEndTime(int cutIndex)
    {
        if (storyCuts == null ||
            cutIndex < 0 ||
            cutIndex >= storyCuts.Length)
        {
            return 0f;
        }

        float endTime = storyCuts[cutIndex].endTime;
        if (cutIndex == storyCuts.Length - 1)
            endTime -= FinalCutStableDuration;

        return Mathf.Max(GetMotionStartTime(cutIndex) + 0.01f, endTime);
    }

    void UpdateOpeningAudioVolume(float playbackTime, float finalEndTime)
    {
        if (audioSource == null)
            return;

        float fadeInWeight = Mathf.Clamp01(playbackTime / AudioFadeInDuration);
        float fadeOutStartTime = finalEndTime - AudioFadeOutDuration;
        float fadeOutWeight = playbackTime >= fadeOutStartTime
            ? Mathf.Clamp01((finalEndTime - playbackTime) / AudioFadeOutDuration)
            : 1f;

        audioSource.volume = targetAudioVolume *
            Mathf.Min(fadeInWeight, fadeOutWeight);
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
        public float startTime;
        public float endTime;
        public Vector2 panFrom;
        public Vector2 panTo;
        public float zoomFrom = 1f;
        public float zoomTo = 1.03f;
    }
}
