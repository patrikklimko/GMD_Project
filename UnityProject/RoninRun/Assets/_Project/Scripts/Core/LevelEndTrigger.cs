using UnityEngine;

public class LevelEndTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerMovement2D>() != null)
        {
            Debug.Log("LEVEL COMPLETE");
        }
    }
}