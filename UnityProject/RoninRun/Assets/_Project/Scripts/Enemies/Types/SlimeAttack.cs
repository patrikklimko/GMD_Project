using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SlimeAttack : MonoBehaviour
{
    [Header("Attack")]
    [Tooltip("Optional. If null, the hit origin is computed from the slime's " +
             "transform + facing direction so the scene doesn't need a child " +
             "AttackPoint object wired up.")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(3.5f, 2f);
    [Tooltip("How far in front of the slime to center the hit box when no " +
             "attackPoint is assigned. Defaults to half the box width.")]
    [SerializeField] private float fallbackReach = -1f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 14f;

    [Header("Dash")]
    [Tooltip("Kept for backwards compat. Set to 0 so the spin and the forward " +
             "thrust start at the same instant. A value > 0 will spin in place first.")]
    [SerializeField] private float windupDelay = 0f;
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

        // Optional pre-spin wait. Default is 0 so the spin and thrust happen
        // simultaneously (the slime lunges while spinning).
        if (windupDelay > 0f)
            yield return new WaitForSeconds(windupDelay);

        // Drive the dash via X-velocity only. We deliberately don't touch
        // linearVelocity.y so gravity still applies — otherwise the slime
        // would levitate horizontally if a dash starts while it's mid-air.
        float dirX = _slimeEnemy.FacingDirection;
        float dashSpeed = dashDuration > 0f ? dashDistance / dashDuration : 0f;

        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            _rb.linearVelocity = new Vector2(dirX * dashSpeed, _rb.linearVelocity.y);

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);

            if (!_hasHitDuringThisAttack && t >= hitMoment)
            {
                DoAttackHit();
                _hasHitDuringThisAttack = true;
            }

            yield return new WaitForFixedUpdate();
        }

        // Kill horizontal momentum at the end of the dash so the slime
        // doesn't slide; gravity (Y) is preserved.
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    public void DoAttackHit()
    {
        Vector2 hitOrigin = GetHitOrigin();

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            hitOrigin,
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

    /// <summary>
    /// World-space center of the attack hitbox.
    /// Uses the assigned AttackPoint child if available, otherwise computes a
    /// position in front of the slime based on its facing direction.
    /// </summary>
    private Vector2 GetHitOrigin()
    {
        if (attackPoint != null)
            return attackPoint.position;

        float facing = _slimeEnemy != null ? _slimeEnemy.FacingDirection : 1f;
        float reach = fallbackReach > 0f ? fallbackReach : attackBoxSize.x * 0.5f;
        return (Vector2)transform.position + new Vector2(facing * reach, 0f);
    }

    private void UpdateAttackPointFacing()
    {
        // No-op: AttackPoint is a child of the slime root, which flips via
        // transform.localScale.x. The child's local position is automatically
        // mirrored by the parent scale, so we don't (and shouldn't) move it
        // manually — doing so would double-flip the hitbox to the wrong side.
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = attackPoint != null
            ? attackPoint.position
            : transform.position + new Vector3(attackBoxSize.x * 0.5f, 0f, 0f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(origin, attackBoxSize);
    }
}