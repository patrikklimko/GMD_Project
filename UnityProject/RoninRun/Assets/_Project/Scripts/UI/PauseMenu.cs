using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pause menu controller. Toggles a UI panel on Esc, freezes the
/// world via Time.timeScale, and exposes Resume / Restart / Quit
/// callbacks for the menu buttons.
///
/// Wires through Unity's new Input System: an InputAction
/// reference is exposed so the binding lives in the existing
/// InputSystem_Actions asset alongside player controls. If the
/// reference is left empty we fall back to polling the keyboard
/// directly so the script still works in scenes that don't have
/// the input action wired.
///
/// The pause menu is intentionally per-scene (not a singleton) so
/// scene reloads cleanly destroy the canvas and Time.timeScale is
/// always restored on enable/disable. Subpanel handling (controls
/// screen) is done via SetActive on a child panel.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Root GameObject of the pause panel — toggled active/inactive.")]
    [SerializeField] private GameObject pausePanel;

    [Tooltip("Optional subpanel shown when the player clicks Controls.")]
    [SerializeField] private GameObject controlsPanel;

    [Header("Input")]
    [Tooltip("InputAction that toggles pause. Leave empty to fall back to Esc on the keyboard.")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("Behaviour")]
    [SerializeField] private bool startPaused = false;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(startPaused);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        IsPaused = startPaused;
        Time.timeScale = startPaused ? 0f : 1f;
    }

    private void OnEnable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed += OnPauseInput;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null && pauseAction.action != null)
        {
            pauseAction.action.performed -= OnPauseInput;
        }

        // Defensive: never leave a destroyed pause menu with a frozen world.
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // Fallback keyboard polling for scenes where the action ref isn't wired.
        if (pauseAction == null || pauseAction.action == null)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }
    }

    private void OnPauseInput(InputAction.CallbackContext _)
    {
        Toggle();
    }

    public void Toggle()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    // Wired to UI buttons.

    public void OnResumeButton()
    {
        Resume();
    }

    public void OnRestartButton()
    {
        Resume();
        SceneLoader.RestartCurrentLevel();
    }

    public void OnMainMenuButton()
    {
        Resume();
        SceneLoader.LoadMainMenu();
    }

    public void OnQuitButton()
    {
        Resume();
        SceneLoader.QuitGame();
    }

    public void OnControlsButton()
    {
        if (controlsPanel != null) controlsPanel.SetActive(true);
    }

    public void OnControlsBackButton()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }
}
