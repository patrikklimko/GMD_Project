using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-of-screen boss HP bar. Subscribes to the boss's Health.
/// Hidden by default and fades in only after the boss actually takes damage.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Health component on the boss.")]
    [SerializeField] private Health bossHealth;

    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Behaviour")]
    [SerializeField] private string title = "BRINGER OF DEATH";
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float fillLerpSpeed = 6f;
    [SerializeField] private bool showOnlyAfterDamage = true;

    private float _targetFill = 1f;
    private bool _hasShown;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = showOnlyAfterDamage ? 0f : 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (fillImage != null)
        {
            fillImage.fillAmount = 1f;
        }

        if (titleLabel != null)
        {
            titleLabel.text = title;
        }
    }

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += HandleHealthChanged;
            bossHealth.OnDied += HandleBossDied;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleHealthChanged;
            bossHealth.OnDied -= HandleBossDied;
        }
    }

    private void Update()
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount = Mathf.MoveTowards(
            fillImage.fillAmount,
            _targetFill,
            fillLerpSpeed * Time.deltaTime
        );
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (max <= 0)
            return;

        _targetFill = Mathf.Clamp01((float)current / max);

        bool bossHasTakenDamage = current < max;

        if (!_hasShown && (!showOnlyAfterDamage || bossHasTakenDamage))
        {
            _hasShown = true;
            FadeTo(1f, fadeInDuration);
        }
    }

    private void HandleBossDied()
    {
        _targetFill = 0f;
        FadeTo(0f, fadeOutDuration);
    }

    private void FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
            return;

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha, duration));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float t = 0f;

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(t / duration));
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        _fadeRoutine = null;
    }
}