using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryUI : MonoBehaviour
{
    [SerializeField] private string mainMenuSceneName = "00_MainMenu";

    public void BackToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}