using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central place for every scene transition in the game. Existing
/// scripts (MainMenuUI, VictoryUI, LevelEndTrigger) hard-code scene
/// names or use buildIndex+1 directly; new code (PauseMenu, future
/// boss death sequence) goes through this helper instead so the
/// scene names live in one file. The legacy scripts can be migrated
/// to call SceneLoader during the Milestone 1 refactor.
///
/// Always restores Time.timeScale = 1 before transitioning so a
/// scene loaded while paused doesn't stay paused.
/// </summary>
public static class SceneLoader
{
    public const string MainMenu = "00_MainMenu";
    public const string Level1   = "01_Level1";
    public const string Level2   = "02_Level2";
    public const string Level3   = "03_Level3";
    public const string Boss     = "04_BossLevel";
    public const string Victory  = "05_Victory";

    public static void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenu);
    }

    public static void RestartCurrentLevel()
    {
        Time.timeScale = 1f;
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    public static void LoadByName(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public static void LoadNextInBuildOrder()
    {
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(next);
        }
        else
        {
            Debug.Log("[SceneLoader] No further scene in build settings — game complete.");
        }
    }

    public static void LoadVictory()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(Victory);
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
