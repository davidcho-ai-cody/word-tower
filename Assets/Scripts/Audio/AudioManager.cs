using UnityEngine;

public enum SfxId
{
    HeroAttack,
    MonsterHit,
    Critical,
    MonsterAttack,
    MonsterDeath,
    LevelUp,
    Victory
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource bgmAudioSource;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 1f;

    [Header("Combat SFX")]
    [SerializeField] private AudioClip heroAttackClip;
    [SerializeField] private AudioClip monsterHitClip;
    [SerializeField] private AudioClip criticalClip;
    [SerializeField] private AudioClip monsterAttackClip;
    [SerializeField] private AudioClip monsterDeathClip;
    [SerializeField] private AudioClip levelUpClip;
    [SerializeField] private AudioClip victoryClip;

    public float SfxVolume
    {
        get => sfxVolume;
        set => sfxVolume = Mathf.Clamp01(value);
    }

    public float BgmVolume
    {
        get => bgmVolume;
        set
        {
            bgmVolume = Mathf.Clamp01(value);

            if (bgmAudioSource != null)
                bgmAudioSource.volume = bgmVolume;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindSourcesIfNeeded();
        ApplyVolumes();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ConfigureSources(AudioSource sfxSource, AudioSource bgmSource)
    {
        sfxAudioSource = sfxSource;
        bgmAudioSource = bgmSource;
        ApplyVolumes();
    }

    public bool ConfigureDefaultCombatClips(
        AudioClip defaultHeroAttackClip,
        AudioClip defaultMonsterHitClip
    )
    {
        bool changed = false;

        if (heroAttackClip == null && defaultHeroAttackClip != null)
        {
            heroAttackClip = defaultHeroAttackClip;
            changed = true;
        }

        if (monsterHitClip == null && defaultMonsterHitClip != null)
        {
            monsterHitClip = defaultMonsterHitClip;
            changed = true;
        }

        return changed;
    }

    public void PlaySfx(SfxId id)
    {
        if (sfxAudioSource == null)
            return;

        AudioClip clip = GetSfxClip(id);

        if (clip == null)
            return;

        sfxAudioSource.PlayOneShot(clip, sfxVolume);
    }

    private void FindSourcesIfNeeded()
    {
        if (sfxAudioSource == null)
        {
            Transform sfxTransform = transform.Find("SFX AudioSource");

            if (sfxTransform != null)
                sfxAudioSource = sfxTransform.GetComponent<AudioSource>();
        }

        if (bgmAudioSource == null)
        {
            Transform bgmTransform = transform.Find("BGM AudioSource");

            if (bgmTransform != null)
                bgmAudioSource = bgmTransform.GetComponent<AudioSource>();
        }
    }

    private void ApplyVolumes()
    {
        if (sfxAudioSource != null)
            sfxAudioSource.volume = 1f;

        if (bgmAudioSource != null)
            bgmAudioSource.volume = bgmVolume;
    }

    private AudioClip GetSfxClip(SfxId id)
    {
        switch (id)
        {
            case SfxId.HeroAttack:
                return heroAttackClip;
            case SfxId.MonsterHit:
                return monsterHitClip;
            case SfxId.Critical:
                return criticalClip;
            case SfxId.MonsterAttack:
                return monsterAttackClip;
            case SfxId.MonsterDeath:
                return monsterDeathClip;
            case SfxId.LevelUp:
                return levelUpClip;
            case SfxId.Victory:
                return victoryClip;
            default:
                return null;
        }
    }
}
