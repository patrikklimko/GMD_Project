using System.Collections;
using UnityEngine;

/// <summary>
/// Sits at the entrance to the boss arena. Once the player enters
/// the trigger zone, plays a short intro (camera nudge optional,
/// title fade, BGM crossfade) and then signals the boss to begin
/// fighting. Disables the trigger after firing once so re-entering
/// the area doesn't re-trigger.
///
/// Camera locking is implemented as "stop following the player and
/// pan to the boss" if the camera follower is assigned; otherwise
/// the trigger just runs the timing and audio.
/// </summary>
public class BossArenaTrigger : MonoBehaviour
{
    [Header("Boss")]
    [SerializeField] private BringerOfDeath boss;

    [Header("Intro timing")]
    [SerializeField] private float introDuration = 1.5f;

    [Header("Camera (optional)")]
    [SerializeField] private CameraFollow2D cameraFollower;
    [SerializeField] private Transform cameraIntroTarget;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip bossBgm;
    [SerializeField] private SfxId introSfx = SfxId.None;

    [Header("UI (optional)")]
    [Tooltip("CanvasGroup faded in for the intro title (e.g. 'BRINGER OF DEATH').")]
    [SerializeField] private CanvasGroup introTitleGroup;

    private bool _triggered;

    private void Reset()
    {
        // Default: trigger collider should be a 2D trigger.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
{
    if (_triggered) return;
    if (other.GetComponent<PlayerMovement2D>() == null) return;

    Debug.Log("BOSS ARENA TRIGGERED");

    _triggered = true;
    StartCoroutine(IntroRoutine());
}

    private IEnumerator IntroRoutine()
    {
        // Camera handover.
        Transform originalTarget = null;
        if (cameraFollower != null && cameraIntroTarget != null)
        {
            originalTarget = cameraFollower.GetTarget();
            cameraFollower.SetTarget(cameraIntroTarget);
        }

        // Music + stinger.
        if (AudioManager.Instance != null)
        {
            if (bossBgm != null) AudioManager.Instance.PlayBgm(bossBgm);
            if (introSfx != SfxId.None) AudioManager.Instance.PlaySfx(introSfx);
        }

        // Title fade.
        if (introTitleGroup != null)
        {
            yield return FadeCanvas(introTitleGroup, 0f, 1f, introDuration * 0.4f);
            yield return new WaitForSeconds(introDuration * 0.3f);
            yield return FadeCanvas(introTitleGroup, 1f, 0f, introDuration * 0.3f);
        }
        else
        {
            yield return new WaitForSeconds(introDuration);
        }

        // Hand camera back to player.
        if (cameraFollower != null && originalTarget != null)
        {
            cameraFollower.SetTarget(originalTarget);
        }

        // Fight on.
        if (boss != null) boss.BeginFight();

        // Disable so re-entering the trigger doesn't re-fire the intro.
        gameObject.SetActive(false);
    }

    private IEnumerator FadeCanvas(CanvasGroup g, float from, float to, float duration)
    {
        if (g == null || duration <= 0f) yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        g.alpha = to;
    }
}
