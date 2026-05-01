using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Single point of contact for all music and sound effects in
/// RoninRun. Persists across scene loads (DontDestroyOnLoad), runs
/// BGM through one of two AudioSources so it can crossfade between
/// scenes, and provides a pooled set of SFX AudioSources so concurrent
/// one-shot sounds don't compete for a single channel.
///
/// Volume routing goes through an AudioMixer with three exposed
/// parameters (MasterVolume, MusicVolume, SfxVolume in dB). If no
/// mixer is assigned the manager still works but won't be able to
/// drive volume sliders.
///
/// Designed to be used either via direct API
/// (AudioManager.Instance.PlayBgm(clip)) or via SceneMusicBinder
/// components dropped into each scene.
/// </summary>
[DefaultExecutionOrder(-100)]
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
    [Range(1, 16)]
    [SerializeField] private int sfxChannels = 6;
    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    [Header("Optional shared SFX library")]
    [SerializeField] private SfxLibrarySO sfxLibrary;

    // Two BGM sources so we can crossfade between them.
    private AudioSource _bgmA;
    private AudioSource _bgmB;
    private AudioSource _activeBgm;

    private AudioSource[] _sfxPool;
    private int _sfxCursor;

    private Coroutine _crossfadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _bgmA = CreateSource("BGM_A", musicGroup, loop: true, volume: 0f);
        _bgmB = CreateSource("BGM_B", musicGroup, loop: true, volume: 0f);
        _activeBgm = _bgmA;

        _sfxPool = new AudioSource[sfxChannels];
        for (int i = 0; i < sfxChannels; i++)
        {
            _sfxPool[i] = CreateSource($"SFX_{i}", sfxGroup, loop: false, volume: sfxVolume);
        }
    }

    private AudioSource CreateSource(
        string name, AudioMixerGroup group, bool loop, float volume)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.volume = volume;
        source.outputAudioMixerGroup = group;
        return source;
    }

    // -------- BGM --------

    public void PlayBgm(AudioClip clip)
    {
        if (clip == null)
        {
            StopBgm();
            return;
        }

        // If we're already playing this clip, do nothing.
        if (_activeBgm != null && _activeBgm.clip == clip && _activeBgm.isPlaying)
        {
            return;
        }

        if (_crossfadeRoutine != null)
        {
            StopCoroutine(_crossfadeRoutine);
        }
        _crossfadeRoutine = StartCoroutine(CrossfadeTo(clip));
    }

    public void StopBgm()
    {
        if (_crossfadeRoutine != null) StopCoroutine(_crossfadeRoutine);
        if (_bgmA != null) _bgmA.Stop();
        if (_bgmB != null) _bgmB.Stop();
    }

    private IEnumerator CrossfadeTo(AudioClip clip)
    {
        AudioSource fadeOut = _activeBgm;
        AudioSource fadeIn  = (_activeBgm == _bgmA) ? _bgmB : _bgmA;

        fadeIn.clip = clip;
        fadeIn.volume = 0f;
        fadeIn.Play();

        float t = 0f;
        float start = fadeOut != null ? fadeOut.volume : 0f;
        while (t < bgmCrossfadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / bgmCrossfadeSeconds);
            if (fadeOut != null) fadeOut.volume = Mathf.Lerp(start, 0f, k);
            fadeIn.volume = Mathf.Lerp(0f, bgmVolume, k);
            yield return null;
        }

        if (fadeOut != null && fadeOut != fadeIn)
        {
            fadeOut.Stop();
        }

        _activeBgm = fadeIn;
        _crossfadeRoutine = null;
    }

    // -------- SFX --------

    /// <summary>Play a one-shot SFX clip directly.</summary>
    public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource source = NextSfxChannel();
        source.pitch = pitch;
        source.PlayOneShot(clip, sfxVolume * Mathf.Clamp01(volumeScale));
    }

    /// <summary>Play a named SFX from the assigned SfxLibrarySO.</summary>
    public void PlaySfx(SfxId id, float volumeScale = 1f, float pitch = 1f)
    {
        if (sfxLibrary == null)
        {
            Debug.LogWarning($"[AudioManager] No SfxLibrary assigned; cannot play '{id}'.", this);
            return;
        }
        AudioClip clip = sfxLibrary.GetClip(id);
        if (clip != null) PlaySfx(clip, volumeScale, pitch);
    }

    private AudioSource NextSfxChannel()
    {
        // Round-robin across the pool. Skips channels that are still
        // playing a long sound -- prevents one heavy sfx from
        // monopolising the output.
        for (int i = 0; i < _sfxPool.Length; i++)
        {
            int idx = (_sfxCursor + i) % _sfxPool.Length;
            if (!_sfxPool[idx].isPlaying)
            {
                _sfxCursor = (idx + 1) % _sfxPool.Length;
                return _sfxPool[idx];
            }
        }
        // All busy: overwrite the next round-robin slot.
        AudioSource fallback = _sfxPool[_sfxCursor];
        _sfxCursor = (_sfxCursor + 1) % _sfxPool.Length;
        return fallback;
    }

    // -------- Volume API (drive from sliders) --------

    public void SetMasterVolume01(float linear) => SetMixerDb("MasterVolume", linear);
    public void SetMusicVolume01(float linear)  => SetMixerDb("MusicVolume",  linear);
    public void SetSfxVolume01(float linear)    => SetMixerDb("SfxVolume",    linear);

    private void SetMixerDb(string param, float linear)
    {
        if (mixer == null) return;
        // Linear 0..1 -> dB. -80 dB at 0 (silence), 0 dB at 1 (full).
        float db = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
        mixer.SetFloat(param, db);
    }
}
