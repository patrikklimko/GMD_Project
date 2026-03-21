using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(1.2f, 1f);
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int damage = 1;

    [Header("Lunge")]
    [SerializeField] private float lungeForce = 3f;
    [SerializeField] private float lungeDuration = 0.12f;

    private SlimeEnemy _slimeEnemy;
    private Rigidbody2D _rb;
    private float _originalAttackPointX;

    private void Awake()
    {
        _slimeEnemy = GetComponent<SlimeEnemy>();
        _rb = GetComponent<Rigidbody2D>();

        if (attackPoint != null)
        {
            _originalAttackPointX = Mathf.Abs(attackPoint.localPosition.x);
        }
    }

    public void StartLunge()
    {
        StartCoroutine(LungeRoutine());
    }

    public void DoAttackHit()
    {
        if (attackPoint == null) return;

        UpdateAttackPointFacing();

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            attackPoint.position,
            attackBoxSize,
            0f,
            playerLayer
        );

        foreach (Collider2D hit in hits)
        {
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth == null) continue;

            Vector2 hitDirection = (hit.transform.position - transform.position).normalized;
            playerHealth.TakeDamage(damage, hitDirection);
        }
    }

    private IEnumerator LungeRoutine()
    {
        if (_rb == null || _slimeEnemy == null) yield break;

        float dir = _slimeEnemy.FacingDirection;
        float timer = 0f;

        while (timer < lungeDuration)
        {
            _rb.MovePosition(_rb.position + new Vector2(dir * lungeForce * Time.fixedDeltaTime, 0f));
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private void UpdateAttackPointFacing()
    {
        if (_slimeEnemy == null || attackPoint == null) return;

        Vector3 localPos = attackPoint.localPosition;
        localPos.x = _slimeEnemy.FacingDirection > 0 ? _originalAttackPointX : -_originalAttackPointX;
        attackPoint.localPosition = localPos;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(attackPoint.position, attackBoxSize);
    }
}