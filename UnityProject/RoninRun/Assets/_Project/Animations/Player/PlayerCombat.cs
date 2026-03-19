using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement2D))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;

    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackBoxSize = new Vector2(3.2f, 1.2f);
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 6f;

    [Header("Combo Timing")]
    [SerializeField] private float comboQueueWindow = 0.4f;
    [SerializeField] private float attack1EndBuffer = 0.25f;
    [SerializeField] private float attack2EndBuffer = 0.35f;
    [SerializeField] private float cooldownAfterCombo = 0.1f;

    private Animator _anim;
    private PlayerMovement2D _move;

    private bool _isAttacking;
    private bool _canQueueCombo;
    private bool _comboQueued;

    private Vector3 _attackPointStartLocalPos;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        _move = GetComponent<PlayerMovement2D>();

        if (attackPoint != null)
        {
            _attackPointStartLocalPos = attackPoint.localPosition;
        }
    }

    private void OnEnable()
    {
        if (attackAction != null)
        {
            attackAction.action.Enable();
            attackAction.action.performed += OnAttack;
        }
    }

    private void OnDisable()
    {
        if (attackAction != null)
        {
            attackAction.action.performed -= OnAttack;
            attackAction.action.Disable();
        }
    }

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        if (!_isAttacking)
        {
            StartCoroutine(AttackComboRoutine());
            return;
        }

        if (_canQueueCombo)
        {
            _comboQueued = true;
            _anim.SetBool("ComboQueued", true);
        }
    }

    private IEnumerator AttackComboRoutine()
    {
        _isAttacking = true;
        _canQueueCombo = true;
        _comboQueued = false;

        _anim.SetBool("ComboQueued", false);
        _anim.ResetTrigger("Attack");
        _anim.SetTrigger("Attack");

        yield return new WaitForSeconds(comboQueueWindow);
        _canQueueCombo = false;

        if (_comboQueued)
            yield return new WaitForSeconds(attack1EndBuffer + attack2EndBuffer);
        else
            yield return new WaitForSeconds(attack1EndBuffer);

        _comboQueued = false;
        _anim.SetBool("ComboQueued", false);

        yield return new WaitForSeconds(cooldownAfterCombo);
        _isAttacking = false;
    }

    public void DoAttackHit()
    {
        if (attackPoint == null) return;

        // Move attack point to the side the player is facing
        Vector3 localPos = _attackPointStartLocalPos;
        localPos.x = Mathf.Abs(localPos.x) * _move.FacingDir;
        attackPoint.localPosition = localPos;

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackPoint.position, attackBoxSize, 0f, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            Health health = hit.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }

            Rigidbody2D rb = hit.attachedRigidbody;
            if (rb != null)
            {
                Vector2 dir = (hit.transform.position - transform.position).normalized;
                rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackBoxSize);
    }
}