using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer routing (optional)")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private AudioMixerGroup musicGroup;
    [SerializeField] private AudioMixerGroup sfxGroup;

    [Header("BGM")]
    [SerializeField] private float bgmCrossfadeSeconds = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.7f;

    [Header("SFX pool")]
    [SerializeField] private int sfxChannels = 6;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("Optional shared SFX library")]
    [SerializeField] private SfxLibrarySO sfxLibrary;

    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private AudioSource activeBgmSource;
    private AudioSource inactiveBgmSource;

    private AudioSource[] sfxSources;
    private int nextSfxIndex;

    private Coroutine bgmFadeRoutine;
    private AudioClip currentBgmClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        CreateBgmSources();
        CreateSfxPool();
    }

    private void CreateBgmSources()
    {
        bgmSourceA = CreateAudioSource("BGM Source A", musicGroup, true);
        bgmSourceB = CreateAudioSource("BGM Source B", musicGroup, true);

        activeBgmSource = bgmSourceA;
        inactiveBgmSource = bgmSourceB;

        activeBgmSource.volume = bgmVolume;
        inactiveBgmSource.volume = 0f;
    }

    private void CreateSfxPool()
    {
        sfxChannels = Mathf.Max(1, sfxChannels);
        sfxSources = new AudioSource[sfxChannels];

        for (int i = 0; i < sfxChannels; i++)
        {
            sfxSources[i] = CreateAudioSource("SFX Source " + i, sfxGroup, false);
            sfxSources[i].volume = sfxVolume;
        }
    }

    private AudioSource CreateAudioSource(string sourceName, AudioMixerGroup outputGroup, bool loop)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);

        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = loop ? bgmVolume : sfxVolume;

        if (outputGroup != null)
        {
            source.outputAudioMixerGroup = outputGroup;
        }

        return source;
    }

    public void PlayBgm(AudioClip clip)
    {
        if (clip == null)
            return;

        if (currentBgmClip == clip && activeBgmSource.isPlaying)
            return;

        currentBgmClip = clip;

        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
        }

        bgmFadeRoutine = StartCoroutine(CrossfadeBgm(clip));
    }

    public void StopBgm()
    {
        currentBgmClip = null;

        if (bgmFadeRoutine != null)
        {
            StopCoroutine(bgmFadeRoutine);
        }

        bgmFadeRoutine = StartCoroutine(FadeOutBgm());
    }

    private IEnumerator CrossfadeBgm(AudioClip newClip)
    {
        inactiveBgmSource.clip = newClip;
        inactiveBgmSource.volume = 0f;
        inactiveBgmSource.loop = true;
        inactiveBgmSource.Play();

        float duration = Mathf.Max(0.01f, bgmCrossfadeSeconds);
        float time = 0f;

        float activeStartVolume = activeBgmSource.volume;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            activeBgmSource.volume = Mathf.Lerp(activeStartVolume, 0f, t);
            inactiveBgmSource.volume = Mathf.Lerp(0f, bgmVolume, t);

            yield return null;
        }

        activeBgmSource.Stop();
        activeBgmSource.volume = 0f;

        inactiveBgmSource.volume = bgmVolume;

        AudioSource temp = activeBgmSource;
        activeBgmSource = inactiveBgmSource;
        inactiveBgmSource = temp;

        bgmFadeRoutine = null;
    }

    private IEnumerator FadeOutBgm()
    {
        float duration = Mathf.Max(0.01f, bgmCrossfadeSeconds);
        float time = 0f;

        float startVolume = activeBgmSource.volume;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            activeBgmSource.volume = Mathf.Lerp(startVolume, 0f, t);

            yield return null;
        }

        activeBgmSource.Stop();
        activeBgmSource.clip = null;
        activeBgmSource.volume = bgmVolume;

        bgmFadeRoutine = null;
    }

    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
            return;

        if (sfxSources == null || sfxSources.Length == 0)
            return;

        AudioSource source = sfxSources[nextSfxIndex];
        nextSfxIndex = (nextSfxIndex + 1) % sfxSources.Length;

        source.pitch = 1f;
        source.volume = sfxVolume;
        source.PlayOneShot(clip, sfxVolume);
    }

    public void PlaySfx(SfxId sfxId)
{
    if (sfxLibrary == null)
    {
        Debug.LogWarning("AudioManager: SFX Library is not assigned.");
        return;
    }

    AudioClip clip = sfxLibrary.GetClip(sfxId);

    if (clip == null)
    {
        Debug.LogWarning("AudioManager: No clip assigned for SFX ID: " + sfxId);
        return;
    }

    PlaySfx(clip);
}

public void PlaySfx(SfxId sfxId, float volumeMultiplier)
{
    if (sfxLibrary == null)
    {
        Debug.LogWarning("AudioManager: SFX Library is not assigned.");
        return;
    }

    AudioClip clip = sfxLibrary.GetClip(sfxId);

    if (clip == null)
    {
        Debug.LogWarning("AudioManager: No clip assigned for SFX ID: " + sfxId);
        return;
    }

    PlaySfx(clip, volumeMultiplier);
}

    public void PlaySfx(AudioClip clip, float volumeMultiplier)
    {
        if (clip == null)
            return;

        if (sfxSources == null || sfxSources.Length == 0)
            return;

        AudioSource source = sfxSources[nextSfxIndex];
        nextSfxIndex = (nextSfxIndex + 1) % sfxSources.Length;

        source.pitch = 1f;
        source.volume = sfxVolume;
        source.PlayOneShot(clip, Mathf.Clamp01(volumeMultiplier) * sfxVolume);
    }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (activeBgmSource != null)
        {
            activeBgmSource.volume = bgmVolume;
        }
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }
}