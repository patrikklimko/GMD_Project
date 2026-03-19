using UnityEngine;

public class EnemyDamageOnTouch : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        var player = collision.gameObject.GetComponent<PlayerHealth>();
        if (player != null)
        {
            Vector2 dir = (collision.transform.position - transform.position).normalized;
            player.TakeDamage(damage, dir);
        }
    }
}