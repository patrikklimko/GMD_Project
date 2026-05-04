using System;
using UnityEngine;

/// <summary>
/// Reusable HP component implementing IDamageable. Used by player,
/// every enemy, and the boss.
///
/// Exposes two events:
///   - OnHealthChanged(current, max) fires whenever HP changes,
///     including the initial value at Awake. UI (player HUD, enemy
///     bars, boss bar) subscribes to this so it never has to poll.
///   - OnDied fires once when HP hits zero, just before the host
///     GameObject is destroyed.
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHp = 3;

    private int _hp;
    private bool _isDead;

    public int CurrentHp => _hp;
    public int MaxHp => maxHp;
    public bool IsDead => _isDead;

    /// <summary>Fired with (current, max) any time HP changes.</summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>Fired exactly once when HP first hits zero.</summary>
    public event Action OnDied;

    private void Awake()
    {
        _hp = maxHp;
    }

    private void Start()
    {
        // Fire once after subscribers in other Awake/Start hooks have
        // had a chance to subscribe -- guarantees UI sees the initial
        // value without callers needing to remember to push it.
        OnHealthChanged?.Invoke(_hp, maxHp);
    }

    public void TakeDamage(int amount)
    {
        if (_isDead || amount <= 0) return;

        _hp = Mathf.Max(0, _hp - amount);
        OnHealthChanged?.Invoke(_hp, maxHp);

        if (_hp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Heal the host. Capped at maxHp. Used by future pickups; the
    /// existing slime/wizard/boss don't call this.
    /// </summary>
    public void Heal(int amount)
    {
        if (_isDead || amount <= 0) return;

        _hp = Mathf.Min(maxHp, _hp + amount);
        OnHealthChanged?.Invoke(_hp, maxHp);
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        OnDied?.Invoke();
        Destroy(gameObject);
    }
}
