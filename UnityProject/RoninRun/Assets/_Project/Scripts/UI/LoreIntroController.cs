using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Drives the lore intro scene. Fades a sequence of paragraphs
/// in, holds them for a configurable duration, then either fades
/// to the next paragraph or finishes by loading the first level.
///
/// Skippable: pressing any key (or clicking the Continue button)
/// advances immediately. If the player ignores the screen, the
/// sequence auto-advances on its own timer.
///
/// All references are optional so the scene can be built up
/// incrementally without breaking the controller.
/// </summary>
public class LoreIntroController : MonoBehaviour
{
    [Header("Text")]
    [Tooltip("Single TMP text component the controller writes paragraphs into.")]
    [SerializeField] private TextMeshProUGUI bodyText;

    [Tooltip("Lines of lore shown one after the other. Empty lines are still timed.")]
    [TextArea(2, 4)]
    [SerializeField] private string[] paragraphs = new string[]
    {
        "A wandering Ronin, his master slain, his lands burnt to ash.",
        "The Bringer of Death walks free — a shadow that swallowed everything he loved.",
        "Now, sword drawn and oath sworn, the Ronin walks the path of revenge.",
        "Through cursed forests and broken kingdoms, he hunts the one who must fall.",
        "The night is darker than memory. The blade is hungry."
    };

    [Header("Timing")]
    [Tooltip("Fade-in duration per paragraph.")]
    [SerializeField] private float fadeInSeconds = 0.8f;
    [Tooltip("Hold time after fade-in before the next paragraph starts fading in.")]
    [SerializeField] private float holdSeconds = 2.5f;
    [Tooltip("Fade-out duration when advancing between paragraphs.")]
    [SerializeField] private float fadeOutSeconds = 0.6f;
    [Tooltip("Final delay after the last paragraph before the level loads.")]
    [SerializeField] private float endHoldSeconds = 1.5f;

    [Header("Continue prompt (optional)")]
    [Tooltip("CanvasGroup of the 'Press SPACE to begin' prompt. Fades in after the last paragraph.")]
    [SerializeField] private CanvasGroup continuePromptGroup;
    [SerializeField] private float continuePromptFadeIn = 0.5f;

    [Header("Scene flow")]
    [Tooltip("Scene to load after the intro finishes.")]
    [SerializeField] private string nextSceneName = "01_Level1";

    [Header("Skip input (optional)")]
    [Tooltip("Any-key skip uses Keyboard.current directly so we don't need an InputAction wired.")]
    [SerializeField] private bool skipOnAnyKey = true;

    private bool _finished;
    private bool _skipRequested;
    private bool _onLastParagraph;

    private void Start()
    {
        if (continuePromptGroup != null) continuePromptGroup.alpha = 0f;
        if (bodyText != null) SetTextAlpha(0f);

        StartCoroutine(RunIntro());
    }

    private void Update()
    {
        if (_finished) return;

        if (skipOnAnyKey && Keyboard.current != null)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                _skipRequested = true;
            }
        }
    }

    /// <summary>Called by the Continue UI button.</summary>
    public void OnContinueButton()
    {
        _skipRequested = true;
    }

    private IEnumerator RunIntro()
    {
        if (paragraphs == null || paragraphs.Length == 0)
        {
            FinishAndLoadNext();
            yield break;
        }

        for (int i = 0; i < paragraphs.Length; i++)
        {
            _onLastParagraph = (i == paragraphs.Length - 1);

            if (bodyText != null)
            {
                bodyText.text = paragraphs[i];
                yield return Fade(0f, 1f, fadeInSeconds);
            }

            // Hold (skippable). Don't fade out on the last paragraph.
            float held = 0f;
            while (held < holdSeconds && !_skipRequested)
            {
                held += Time.deltaTime;
                yield return null;
            }
            _skipRequested = false;

            if (!_onLastParagraph && bodyText != null)
            {
                yield return Fade(1f, 0f, fadeOutSeconds);
            }
        }

        // Show the continue prompt and wait for input on the final paragraph.
        if (continuePromptGroup != null)
        {
            yield return FadeCanvas(continuePromptGroup, 0f, 1f, continuePromptFadeIn);
        }

        float endTimer = 0f;
        while (endTimer < endHoldSeconds && !_skipRequested)
        {
            endTimer += Time.deltaTime;
            yield return null;
        }

        FinishAndLoadNext();
    }

    private void FinishAndLoadNext()
    {
        if (_finished) return;
        _finished = true;
        SceneLoader.LoadByName(nextSceneName);
    }

    // ---- helpers ----------------------------------------------------------

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        if (bodyText == null || duration <= 0f)
        {
            SetTextAlpha(toAlpha);
            yield break;
        }

        float t = 0f;
        while (t < duration && !_skipRequested)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            SetTextAlpha(Mathf.Lerp(fromAlpha, toAlpha, k));
            yield return null;
        }
        SetTextAlpha(toAlpha);
    }

    private IEnumerator FadeCanvas(CanvasGroup g, float fromAlpha, float toAlpha, float duration)
    {
        if (g == null || duration <= 0f)
        {
            if (g != null) g.alpha = toAlpha;
            yield break;
        }
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            g.alpha = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(t / duration));
            yield return null;
        }
        g.alpha = toAlpha;
    }

    private void SetTextAlpha(float a)
    {
        if (bodyText == null) return;
        Color c = bodyText.color;
        c.a = Mathf.Clamp01(a);
        bodyText.color = c;
    }
}
