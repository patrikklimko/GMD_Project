using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHp = 3;
    private int _hp;

    public int CurrentHp => _hp;
    public int MaxHp => maxHp;

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
        Destroy(gameObject);
    }
}