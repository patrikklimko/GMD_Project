using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-of-screen boss HP bar. Subscribes to the boss's Health.
/// OnHealthChanged event so it doesn't have to poll. The bar is
/// hidden by default (via CanvasGroup alpha) and fades in the first
/// time the boss takes damage, which lets us drop it in any boss
/// scene without it appearing during the intro.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Health component on the boss. Subscribes on Start.")]
    [SerializeField] private Health bossHealth;

    [Header("UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Behaviour")]
    [SerializeField] private string title = "BRINGER OF DEATH";
    [SerializeField] private float fadeInDuration = 0.4f;
    [SerializeField] private float fillLerpSpeed = 6f;

    private float _targetFill = 1f;
    private bool _hasShown;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (titleLabel != null) titleLabel.text = title;
    }

    private void OnEnable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void Update()
    {
        if (fillImage == null) return;
        // Smoothly lerp the fill toward the target so HP-loss feels weighted.
        fillImage.fillAmount = Mathf.MoveTowards(
            fillImage.fillAmount, _targetFill, fillLerpSpeed * Time.deltaTime);
    }

    private void HandleHealthChanged(int current, int max)
    {
        _targetFill = max > 0 ? (float)current / max : 0f;

        if (!_hasShown)
        {
            _hasShown = true;
            if (canvasGroup != null) StartCoroutine(FadeIn());
        }
    }

    private System.Collections.IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float t = 0f;
        float start = canvasGroup.alpha;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, 1f, t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
