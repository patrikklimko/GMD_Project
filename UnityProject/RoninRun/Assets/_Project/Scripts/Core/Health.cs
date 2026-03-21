using System;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHp = 3;
    private int _hp;

    public int CurrentHp => _hp;
    public int MaxHp => maxHp;

    public event Action OnDied;

    private void Awake()
    {
        _hp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        _hp -= amount;
        if (_hp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        OnDied?.Invoke();
        Destroy(gameObject);
    }
}