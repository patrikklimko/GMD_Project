using UnityEngine;

/// <summary>
/// Drop one of these into a scene with an AudioClip assigned and
/// the AudioManager will crossfade to that track on Start. The
/// AudioManager itself is normally bootstrapped from the main menu
/// scene and persists; this binder is the per-scene declaration of
/// "what BGM should be playing right now".
///
/// Why this is its own component and not a field on each scene's
/// menu/HUD: scenes are level-specific, but the audio persistence
/// is global. Keeping the per-scene declaration in a single,
/// copy-pasteable component means you can change a level's music
/// without touching any other script.
/// </summary>
public class SceneMusicBinder : MonoBehaviour
{
    [Tooltip("BGM track to crossfade to when this scene loads.")]
    [SerializeField] private AudioClip bgmClip;

    [Tooltip("If true, stops BGM entirely instead of switching tracks.")]
    [SerializeField] private bool silenceScene = false;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            // No manager in the scene -- this is fine in scenes that
            // were entered directly from the editor without going
            // through the main menu. Bail silently.
            return;
        }

        if (silenceScene)
        {
            AudioManager.Instance.StopBgm();
            return;
        }

        if (bgmClip != null)
        {
            AudioManager.Instance.PlayBgm(bgmClip);
        }
    }
}
