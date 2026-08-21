using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StudioSplashManager : MonoBehaviour
{
    private const string OpeningSceneName = "OpeningScene";
    private const string TitleSceneName = "TitleScene";
    private const float IntroDelay = 0.35f;
    private const float LogoRevealDuration = 0.5f;
    private const float LogoSettleDuration = 0.3f;
    private const float VoiceStartTime = 1.05f;
    private const float MinimumSplashDuration = 3f;
    private const float FadeOutDuration = 0.35f;

    [SerializeField] private Canvas splashCanvas;
    [SerializeField] private CanvasGroup logoGroup;
    [SerializeField] private RectTransform logoRect;
    [SerializeField] private CanvasGroup glowGroup;
    [SerializeField] private RectTransform glowRect;
    [SerializeField] private CanvasGroup lightSweepGroup;
    [SerializeField] private RectTransform lightSweepRect;
    [SerializeField] private CanvasGroup fadeOverlayGroup;
    [SerializeField] private AudioSource voiceAudioSource;

    private StoryProgressService storyProgressService;
    private bool isTransitioning;
    private float startUnscaledTime;

    void Awake()
    {
        storyProgressService = new StoryProgressService();
        InitializeVisuals();
    }

    IEnumerator Start()
    {
        if (!ValidateReferences())
        {
            LoadNextScene();
            yield break;
        }

        if (splashCanvas != null)
            splashCanvas.enabled = true;

        startUnscaledTime = Time.unscaledTime;
        yield return PlaySplash();
    }

    IEnumerator PlaySplash()
    {
        bool voiceStarted = false;
        float fadeOutStartTime = GetFadeOutStartTime();

        while (!isTransitioning)
        {
            float elapsed = Time.unscaledTime - startUnscaledTime;

            if (!voiceStarted && elapsed >= VoiceStartTime)
            {
                PlayVoice();
                voiceStarted = true;
            }

            UpdateIntroAnimation(elapsed);

            if (elapsed >= fadeOutStartTime)
                break;

            yield return null;
        }

        yield return FadeOutAndLoadNextScene();
    }

    void InitializeVisuals()
    {
        if (splashCanvas != null)
            splashCanvas.enabled = false;

        if (logoGroup != null)
            logoGroup.alpha = 0f;

        if (logoRect != null)
            logoRect.localScale = Vector3.one * 0.92f;

        if (glowGroup != null)
            glowGroup.alpha = 0.12f;

        if (glowRect != null)
            glowRect.localScale = Vector3.one * 0.92f;

        if (lightSweepGroup != null)
            lightSweepGroup.alpha = 0f;

        if (lightSweepRect != null)
            lightSweepRect.anchoredPosition = new Vector2(-620f, 0f);

        if (fadeOverlayGroup != null)
            fadeOverlayGroup.alpha = 0f;
    }

    void UpdateIntroAnimation(float elapsed)
    {
        if (elapsed < IntroDelay)
        {
            SetLogoAlpha(0f);
            SetLightSweepAlpha(0f);
            return;
        }

        float revealTime = Mathf.Clamp01(
            (elapsed - IntroDelay) / LogoRevealDuration
        );
        float revealEase = Mathf.SmoothStep(0f, 1f, revealTime);
        float settleTime = Mathf.Clamp01(
            (elapsed - IntroDelay - LogoRevealDuration) / LogoSettleDuration
        );
        float settleEase = Mathf.SmoothStep(0f, 1f, settleTime);

        SetLogoAlpha(revealEase);
        SetLogoScale(Mathf.Lerp(
            0.92f,
            Mathf.Lerp(1.03f, 1f, settleEase),
            revealEase
        ));
        SetGlowAlpha(Mathf.Lerp(0.14f, 0.28f, revealEase) *
            Mathf.Lerp(1f, 0.78f, settleEase));
        SetGlowScale(Mathf.Lerp(0.95f, 1.12f, revealEase));
        UpdateLightSweep(revealTime);
    }

    void UpdateLightSweep(float revealTime)
    {
        float sweepAlpha = Mathf.Sin(Mathf.Clamp01(revealTime) * Mathf.PI);
        SetLightSweepAlpha(sweepAlpha * 0.48f);

        if (lightSweepRect != null)
        {
            lightSweepRect.anchoredPosition = new Vector2(
                Mathf.Lerp(-620f, 620f, Mathf.SmoothStep(0f, 1f, revealTime)),
                0f
            );
        }
    }

    IEnumerator FadeOutAndLoadNextScene()
    {
        if (isTransitioning)
            yield break;

        isTransitioning = true;

        float elapsed = 0f;
        while (elapsed < FadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (fadeOverlayGroup != null)
                fadeOverlayGroup.alpha = Mathf.Clamp01(elapsed / FadeOutDuration);

            yield return null;
        }

        LoadNextScene();
    }

    void PlayVoice()
    {
        if (voiceAudioSource == null || voiceAudioSource.clip == null)
            return;

        voiceAudioSource.Play();
    }

    float GetFadeOutStartTime()
    {
        float voiceEndTime = VoiceStartTime;

        if (voiceAudioSource != null && voiceAudioSource.clip != null)
            voiceEndTime += voiceAudioSource.clip.length;

        return Mathf.Max(MinimumSplashDuration, voiceEndTime);
    }

    void LoadNextScene()
    {
        StoryProgressData storyProgress = storyProgressService.LoadOrCreate();
        string nextSceneName = storyProgress.hasSeenOpeningStory
            ? TitleSceneName
            : OpeningSceneName;

        SceneManager.LoadScene(nextSceneName);
    }

    bool ValidateReferences()
    {
        bool hasRequiredVisuals =
            logoGroup != null &&
            logoRect != null &&
            fadeOverlayGroup != null;

        if (!hasRequiredVisuals)
            Debug.LogError("Studio Splash UI 설정이 누락됐습니다.");

        return hasRequiredVisuals;
    }

    void SetLogoAlpha(float alpha)
    {
        if (logoGroup != null)
            logoGroup.alpha = alpha;
    }

    void SetLogoScale(float scale)
    {
        if (logoRect != null)
            logoRect.localScale = Vector3.one * scale;
    }

    void SetGlowAlpha(float alpha)
    {
        if (glowGroup != null)
            glowGroup.alpha = alpha;
    }

    void SetGlowScale(float scale)
    {
        if (glowRect != null)
            glowRect.localScale = Vector3.one * scale;
    }

    void SetLightSweepAlpha(float alpha)
    {
        if (lightSweepGroup != null)
            lightSweepGroup.alpha = alpha;
    }
}
