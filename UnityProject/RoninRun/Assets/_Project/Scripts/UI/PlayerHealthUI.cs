using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the player's on-screen health bar.
///
/// The visual bar (frame + fill + label) is built procedurally at runtime
/// the first time the script runs, so scene authors don't have to drag
/// references around or hand-build the bar in every level. They just need
/// a PlayerHealthUI component somewhere in the scene (or one auto-spawned
/// by HudBootstrapper) plus a Canvas. Everything else is auto-generated.
///
/// If the inspector fields for fillImage/frameImage are already wired,
/// the script uses those references as-is and skips the procedural build.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private PlayerHealth playerHealth;
    [Tooltip("Existing label, if any. If null, one is created inside the bar.")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Bar visuals (auto-built if left empty)")]
    [SerializeField] private RectTransform barContainer;
    [SerializeField] private Image frameImage;
    [SerializeField] private Image fillImage;

    [Header("Auto-build layout")]
    [Tooltip("Where to place the bar on the screen, relative to the top-left of the canvas.")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(40f, -40f);
    [SerializeField] private Vector2 size = new Vector2(360f, 32f);
    [SerializeField] private float frameInset = 4f;

    [Header("Style")]
    [SerializeField] private Color frameColor = new Color(0.05f, 0.05f, 0.05f, 0.85f);
    [Tooltip("Fill color across hp percentage. Defaults to green->yellow->red if left unset.")]
    [SerializeField] private Gradient fillGradient;
    [SerializeField] private float fillLerpSpeed = 10f;
    [SerializeField] private bool showText = true;
    [SerializeField] private int textFontSize = 18;

    private float _displayedFill = 1f;
    private float _targetFill = 1f;

    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();
        EnsureDefaultGradient();
    }

    private void Start()
    {
        if (fillImage == null)
            BuildBarHierarchy();
        SyncTargetFromHealth();
        _displayedFill = _targetFill;
        ApplyVisuals();
        RefreshLabel();
    }

    private void Update()
    {
        // PlayerHealth can be spawned AFTER our Awake (PlayerBootstrapper).
        // Re-find every frame until we have one.
        if (playerHealth == null)
        {
            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                SyncTargetFromHealth();
                _displayedFill = _targetFill;
            }
        }

        // If the bar visuals were not built (e.g. no Canvas was present at
        // Start) retry — a HudBootstrapper-built canvas can show up later.
        if (fillImage == null)
            BuildBarHierarchy();

        if (playerHealth == null || fillImage == null)
            return;

        SyncTargetFromHealth();
        _displayedFill = Mathf.MoveTowards(
            _displayedFill, _targetFill, fillLerpSpeed * Time.unscaledDeltaTime);
        ApplyVisuals();
        RefreshLabel();
    }

    private void SyncTargetFromHealth()
    {
        if (playerHealth == null) return;
        int max = Mathf.Max(1, playerHealth.GetMaxHp());
        _targetFill = Mathf.Clamp01((float)playerHealth.GetHp() / max);
    }

    private void ApplyVisuals()
    {
        if (fillImage == null) return;
        fillImage.fillAmount = _displayedFill;
        if (fillGradient != null)
            fillImage.color = fillGradient.Evaluate(_displayedFill);
    }

    private void RefreshLabel()
    {
        if (!showText || healthText == null || playerHealth == null) return;
        healthText.text = playerHealth.GetHp() + " / " + playerHealth.GetMaxHp();
    }

    private void BuildBarHierarchy()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null) return; // try again next Update

        // Container
        GameObject containerGo = new GameObject("PlayerHealthBar", typeof(RectTransform));
        containerGo.transform.SetParent(canvas.transform, false);
        barContainer = (RectTransform)containerGo.transform;
        barContainer.anchorMin = new Vector2(0f, 1f);
        barContainer.anchorMax = new Vector2(0f, 1f);
        barContainer.pivot     = new Vector2(0f, 1f);
        barContainer.anchoredPosition = anchoredPosition;
        barContainer.sizeDelta = size;
        containerGo.transform.SetAsLastSibling();

        // Frame
        GameObject frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
        frameGo.transform.SetParent(barContainer, false);
        StretchToParent((RectTransform)frameGo.transform);
        frameImage = frameGo.GetComponent<Image>();
        frameImage.color = frameColor;
        frameImage.raycastTarget = false;

        // Fill
        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(barContainer, false);
        RectTransform fillRt = (RectTransform)fillGo.transform;
        StretchToParent(fillRt);
        fillRt.offsetMin = new Vector2(frameInset, frameInset);
        fillRt.offsetMax = new Vector2(-frameInset, -frameInset);
        fillImage = fillGo.GetComponent<Image>();
        fillImage.color = fillGradient != null ? fillGradient.Evaluate(1f) : Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 1f;
        fillImage.raycastTarget = false;

        // Label
        if (showText)
        {
            if (healthText != null)
            {
                healthText.transform.SetParent(barContainer, false);
                StretchToParent((RectTransform)healthText.transform);
                healthText.alignment = TextAlignmentOptions.Center;
                healthText.color = Color.white;
                healthText.fontStyle = FontStyles.Bold;
                healthText.fontSize = textFontSize;
                healthText.raycastTarget = false;
            }
            else
            {
                GameObject labelGo = new GameObject("HpLabel", typeof(RectTransform));
                labelGo.transform.SetParent(barContainer, false);
                StretchToParent((RectTransform)labelGo.transform);
                healthText = labelGo.AddComponent<TextMeshProUGUI>();
                healthText.alignment = TextAlignmentOptions.Center;
                healthText.color = Color.white;
                healthText.fontStyle = FontStyles.Bold;
                healthText.fontSize = textFontSize;
                healthText.raycastTarget = false;
            }
        }
    }

    private Canvas FindCanvas()
    {
        // Best case: we ARE the Canvas (HudBootstrapper attaches us to one).
        Canvas own = GetComponent<Canvas>();
        if (own != null) return own;

        // Manually-wired label's canvas, if present.
        if (healthText != null)
        {
            Canvas owning = healthText.GetComponentInParent<Canvas>();
            if (owning != null) return owning;
        }

        // Otherwise the first ScreenSpaceOverlay canvas (avoid world-space
        // enemy health bar canvases).
        Canvas[] all = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            if (all[i] != null && all[i].renderMode == RenderMode.ScreenSpaceOverlay)
                return all[i];

        return FindFirstObjectByType<Canvas>();
    }

    private static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private void EnsureDefaultGradient()
    {
        if (fillGradient != null && fillGradient.colorKeys != null && fillGradient.colorKeys.Length > 0)
            return;

        fillGradient = new Gradient();
        fillGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.85f, 0.10f, 0.10f), 0.00f),
                new GradientColorKey(new Color(0.95f, 0.75f, 0.10f), 0.45f),
                new GradientColorKey(new Color(0.20f, 0.80f, 0.25f), 1.00f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f),
            });
    }
}
