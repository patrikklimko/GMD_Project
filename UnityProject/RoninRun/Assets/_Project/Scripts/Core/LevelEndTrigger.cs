using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelEndTrigger : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private float delayBeforeSceneLoad = 1.2f;

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered)
            return;

        if (other.GetComponent<PlayerMovement2D>() == null)
            return;

        _triggered = true;
        StartCoroutine(LoadNextLevelRoutine());
    }

    private IEnumerator LoadNextLevelRoutine()
    {
        AudioManager.Instance?.PlaySfx(SfxId.LevelEnd);

        yield return new WaitForSeconds(delayBeforeSceneLoad);

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("GAME COMPLETE");
        }
    }
}