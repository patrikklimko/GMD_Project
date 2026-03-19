using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement2D))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHp = 5;
    [SerializeField] private float invincibleTime = 1f;
    [SerializeField] private float knockbackForce = 16f;
    [SerializeField] private float movementLockTime = 0.15f;

    private int _hp;
    private bool _isInvincible;
    private Rigidbody2D _rb;
    private PlayerMovement2D _movement;

    public int CurrentHp => _hp;
    public int MaxHp => maxHp;

    private void Awake()
    {
        _hp = maxHp;
        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement2D>();
    }

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (_isInvincible) return;

        _hp -= amount;
        Debug.Log("Player HP: " + _hp);

        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(hitDirection.normalized * knockbackForce, ForceMode2D.Impulse);

        if (_movement != null)
        {
            _movement.LockMovement(movementLockTime);
        }

        if (_hp <= 0)
        {
            Die();
            return;
        }

        StartCoroutine(Invincibility());
    }

    private void Die()
    {
        Debug.Log("PLAYER DEAD");
        gameObject.SetActive(false);
    }

    private IEnumerator Invincibility()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(invincibleTime);
        _isInvincible = false;
    }

    public int GetHp()
    {
        return _hp;
    }

    public int GetMaxHp()
    {
        return maxHp;
    }
}