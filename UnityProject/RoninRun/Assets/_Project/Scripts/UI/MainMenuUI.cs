using UnityEngine;

/// <summary>
/// Main-menu controller. Loads the intro scene (which then chains
/// into Level 1) on Start, quits the application on Quit, and
/// toggles a Controls subpanel listing the keyboard mapping.
///
/// Field references are optional: if no Controls subpanel is wired
/// the button-callbacks no-op gracefully so the menu still works
/// on scenes that haven't been updated yet.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Scene flow")]
    [Tooltip("Scene loaded when 'Start Game' is pressed. Defaults to the intro/lore scene.")]
    [SerializeField] private string firstSceneName = "00b_Intro";

    [Header("Controls subpanel (optional)")]
    [Tooltip("Root of the main menu buttons (Start, Controls, Quit). Hidden while Controls is open.")]
    [SerializeField] private GameObject mainPanel;
    [Tooltip("Controls subpanel shown when Controls button is clicked.")]
    [SerializeField] private GameObject controlsPanel;

    private void Awake()
    {
        // Make sure controls panel starts hidden if it's wired.
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void StartGame()
    {
        SceneLoader.LoadByName(firstSceneName);
    }

    public void QuitGame()
    {
        SceneLoader.QuitGame();
    }

    public void OnControlsButton()
    {
        if (controlsPanel != null) controlsPanel.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    public void OnControlsBackButton()
    {
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }
}
