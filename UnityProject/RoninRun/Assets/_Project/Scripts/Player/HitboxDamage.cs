using UnityEngine;

public class HitboxDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private LayerMask enemyLayer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only hit objects on the Enemy layer mask
        if (((1 << other.gameObject.layer) & enemyLayer) == 0) return;

        // Damage if the enemy has Health
        var health = other.GetComponent<Health>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }

        // Optional knockback
        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            Vector2 dir = (other.transform.position - transform.position).normalized;
            rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        }
    }
}