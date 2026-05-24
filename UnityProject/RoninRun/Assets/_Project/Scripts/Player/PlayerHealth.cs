using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHp = 5;

    [Header("Damage")]
    [SerializeField] private float invincibleTime = 1f;
    [SerializeField] private float movementLockTime = 0.15f;

    [Header("Death")]
    [SerializeField] private PlayerDeathController deathController;

    private int _hp;
    private bool _isInvincible;
    private bool _isDead;

    private Rigidbody2D _rb;
    private PlayerMovement2D _movement;

    public int CurrentHp => _hp;
    public int MaxHp => maxHp;
    public bool IsDead => _isDead;

    private void Awake()
    {
        _hp = maxHp;

        _rb = GetComponent<Rigidbody2D>();
        _movement = GetComponent<PlayerMovement2D>();

        if (deathController == null)
        {
            deathController = GetComponent<PlayerDeathController>();
        }
    }

    public void TakeDamage(int amount, Vector2 knockbackForce)
    {
        if (_isInvincible || _isDead)
            return;

        _hp -= amount;
        Debug.Log("Player HP: " + _hp);

        if (_hp <= 0)
        {
            _hp = 0;
            Die();
            return;
        }

        AudioManager.Instance?.PlaySfx(SfxId.PlayerHurt);

        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        if (_movement != null)
        {
            _movement.LockMovement(movementLockTime);
        }

        StartCoroutine(Invincibility());
    }

    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;
        Debug.Log("PLAYER DEAD");

        if (deathController != null)
        {
            deathController.Die();
        }
        else
        {
            Debug.LogError("PlayerHealth: PlayerDeathController is missing on Player.");
        }
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