using System.Collections;
using UnityEngine;

/// <summary>
/// Plays the boss death anim, fades the screen to black, and loads
/// the Victory scene. Sits as a sibling component on the boss
/// GameObject (or on a dedicated "BossDeathSequence" GameObject the
/// boss references). Begin() is called by BringerOfDeath.Die().
///
/// The fade overlay is a full-screen CanvasGroup; if no overlay is
/// assigned the sequence still works -- it just skips the fade and
/// loads Victory after the configured delay.
/// </summary>
public class BossDeathSequence : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float deathHoldSeconds = 2.0f;
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("Optional fade overlay")]
    [Tooltip("CanvasGroup of a black, full-screen panel. Assigned a=0 in the editor; we'll lerp to a=1.")]
    [SerializeField] private CanvasGroup fadeOverlay;

    private bool _begun;

    public void Begin()
    {
        if (_begun) return;
        _begun = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(deathHoldSeconds);

        if (fadeOverlay != null)
        {
            yield return FadeTo(1f, fadeDuration);
        }

        SceneLoader.LoadVictory();
    }

    private IEnumerator FadeTo(float toAlpha, float duration)
    {
        if (fadeOverlay == null || duration <= 0f) yield break;

        float fromAlpha = fadeOverlay.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(t / duration));
            yield return null;
        }
        fadeOverlay.alpha = toAlpha;
    }
}
