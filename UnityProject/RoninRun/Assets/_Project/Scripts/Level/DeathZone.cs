using UnityEngine;

public class DeathZone : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag(playerTag))
            return;

        hasTriggered = true;

        PlayerDeathController deathController = other.GetComponent<PlayerDeathController>();

        if (deathController == null)
        {
            deathController = other.GetComponentInParent<PlayerDeathController>();
        }

        if (deathController != null)
        {
            deathController.Die();
        }
        else
        {
            Debug.LogError("DeathZone: PlayerDeathController not found on Player.");
        }
    }
}