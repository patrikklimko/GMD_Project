using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement2D))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHp = 5;
    [SerializeField] private float invincibleTime = 1f;
    [SerializeField] private float knockbackForce = 16f;
    [SerializeField] private float movementLockTime = 0.15f;
    [SerializeField] private float deathReloadDelay = 2f;

    private int _hp;
    private bool _isInvincible;
    private bool _isDead;
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
        if (_isInvincible || _isDead) return;

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
        if (_isDead) return;

        _isDead = true;
        Debug.Log("PLAYER DEAD");

        StartCoroutine(ReloadCurrentScene());
    }

    private IEnumerator ReloadCurrentScene()
    {
        yield return new WaitForSeconds(deathReloadDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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