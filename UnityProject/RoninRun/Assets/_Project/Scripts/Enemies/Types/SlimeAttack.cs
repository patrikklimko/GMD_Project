using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(3.5f, 2f);
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 14f;

    [Header("Dash")]
    [SerializeField] private float windupDelay = 0.18f;
    [SerializeField] private float dashDistance = 4f;
    [SerializeField] private float dashDuration = 0.35f;
    [SerializeField] private float hitMoment = 0.5f;

    private SlimeEnemy _slimeEnemy;
    private Rigidbody2D _rb;
    private float _originalAttackPointX;
    private bool _hasHitDuringThisAttack;

    private void Awake()
    {
        _slimeEnemy = GetComponent<SlimeEnemy>();
        _rb = GetComponent<Rigidbody2D>();

        if (attackPoint != null)
            _originalAttackPointX = Mathf.Abs(attackPoint.localPosition.x);
    }

    public IEnumerator PerformDashAttack()
    {
        if (_slimeEnemy == null || _rb == null)
            yield break;

        _hasHitDuringThisAttack = false;

        UpdateAttackPointFacing();

        // Let the spin animation start first
        yield return new WaitForSeconds(windupDelay);

        Vector2 startPos = _rb.position;
        Vector2 endPos = startPos + new Vector2(_slimeEnemy.FacingDirection * dashDistance, 0f);

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);

            Vector2 newPos = Vector2.Lerp(startPos, endPos, t);
            _rb.MovePosition(newPos);

            if (!_hasHitDuringThisAttack && t >= hitMoment)
            {
                DoAttackHit();
                _hasHitDuringThisAttack = true;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    public void DoAttackHit()
    {
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
            Vector2 knockback = hitDirection * knockbackForce;

            playerHealth.TakeDamage(damage, knockback);
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