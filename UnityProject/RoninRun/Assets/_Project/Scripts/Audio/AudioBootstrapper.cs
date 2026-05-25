using UnityEngine;

/// <summary>
/// Ensures an AudioManager exists no matter which scene is the entry point.
///
/// Why this exists:
///   The AudioManager was only placed in 00_MainMenu and relied on
///   DontDestroyOnLoad to persist into subsequent scenes. That works in
///   a normal play-through, but if you press Play directly from any
///   other scene (e.g. 01_Level1, 04_BossLevel) there is no
///   AudioManager in the scene, AudioManager.Instance stays null, and
///   every PlaySfx / PlayBgm call silently no-ops — no music, no SFX.
///
/// What this does:
///   Before any scene is loaded, instantiate the AudioManager prefab
///   located at Assets/_Project/Resources/AudioManager.prefab.
///   If the singleton is already present (e.g. because you started from
///   00_MainMenu and that scene has its own AudioManager), this is a no-op.
/// </summary>
public static class AudioBootstrapper
{
    private const string AudioManagerResourcePath = "AudioManager";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureAudioManager()
    {
        // Already created (either by us last domain reload or by the scene).
        if (AudioManager.Instance != null)
            return;

        AudioManager prefab = Resources.Load<AudioManager>(AudioManagerResourcePath);

        if (prefab == null)
        {
            Debug.LogWarning(
                "[AudioBootstrapper] No AudioManager prefab found at " +
                "Resources/" + AudioManagerResourcePath +
                ". SFX and BGM will be silent. " +
                "Re-create the prefab under Assets/_Project/Resources/AudioManager.prefab.");
            return;
        }

        AudioManager instance = Object.Instantiate(prefab);
        instance.name = "AudioManager (Bootstrapped)";
        // AudioManager.Awake() handles DontDestroyOnLoad + singleton assignment.
    }
}
