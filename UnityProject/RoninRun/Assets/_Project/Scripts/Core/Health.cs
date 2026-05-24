using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHp = 3;

    [Header("Death")]
    [SerializeField] private bool playDeathSfx = true;
    [SerializeField] private SfxId deathSfx = SfxId.EnemyDeath;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 0.6f;

    private int _hp;
    private bool _isDead;

    public int CurrentHp => _hp;
    public int MaxHp => maxHp;
    public bool IsDead => _isDead;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDied;

    private void Awake()
    {
        _hp = maxHp;
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(_hp, maxHp);
    }

    public void TakeDamage(int amount)
    {
        if (_isDead || amount <= 0)
            return;

        _hp = Mathf.Max(0, _hp - amount);
        OnHealthChanged?.Invoke(_hp, maxHp);

        if (_hp <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (_isDead || amount <= 0)
            return;

        _hp = Mathf.Min(maxHp, _hp + amount);
        OnHealthChanged?.Invoke(_hp, maxHp);
    }

    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;

        if (playDeathSfx)
        {
            AudioManager.Instance?.PlaySfx(deathSfx);
        }

        OnDied?.Invoke();

        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
}