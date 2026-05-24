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

    private bool _attack1HitAlreadyProcessed;
    private bool _attack2HitAlreadyProcessed;
    private bool _attack1SlashSoundPlayed;
    private bool _attack2SlashSoundPlayed;

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
            ResetAttackFlags();

            StartCoroutine(AttackComboRoutine());
            return;
        }

        if (_canQueueCombo)
        {
            _comboQueued = true;
            _anim.SetBool("ComboQueued", true);

            _attack2HitAlreadyProcessed = false;
            _attack2SlashSoundPlayed = false;
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
        {
            yield return new WaitForSeconds(attack1EndBuffer + attack2EndBuffer);
        }
        else
        {
            yield return new WaitForSeconds(attack1EndBuffer);
        }

        _comboQueued = false;
        _anim.SetBool("ComboQueued", false);

        yield return new WaitForSeconds(cooldownAfterCombo);
        _isAttacking = false;
    }

    private void ResetAttackFlags()
    {
        _attack1HitAlreadyProcessed = false;
        _attack2HitAlreadyProcessed = false;
        _attack1SlashSoundPlayed = false;
        _attack2SlashSoundPlayed = false;
    }

    // -----------------------------------------------------------------------
    // Animation Events
    // -----------------------------------------------------------------------

    // OLD event name. Do not use this anymore in animations.
    public void DoAttackHit()
    {
        Debug.LogWarning(
            "Old animation event DoAttackHit was called. " +
            "Replace it with DoAttack1Hit or DoAttack2Hit in the animation clip."
        );
    }

    // Put this early in Attack 1 animation, when the sword starts moving.
    public void PlayAttack1SlashSound()
    {
        if (_attack1SlashSoundPlayed)
            return;

        _attack1SlashSoundPlayed = true;
        AudioManager.Instance?.PlaySfx(SfxId.SwordSlash1);
    }

    // Put this early in Attack 2 animation, when the sword starts moving.
    public void PlayAttack2SlashSound()
    {
        if (_attack2SlashSoundPlayed)
            return;

        _attack2SlashSoundPlayed = true;
        AudioManager.Instance?.PlaySfx(SfxId.SwordSlash2);
    }

    // Put this on the actual hit/contact frame of Attack 1.
    public void DoAttack1Hit()
    {
        if (_attack1HitAlreadyProcessed)
            return;

        _attack1HitAlreadyProcessed = true;
        DoAttackHitInternal();
    }

    // Put this on the actual hit/contact frame of Attack 2.
    public void DoAttack2Hit()
    {
        if (_attack2HitAlreadyProcessed)
            return;

        _attack2HitAlreadyProcessed = true;
        DoAttackHitInternal();
    }

    private void DoAttackHitInternal()
    {
        if (attackPoint == null)
            return;

        Vector3 localPos = _attackPointStartLocalPos;
        localPos.x = Mathf.Abs(localPos.x) * _move.FacingDir;
        attackPoint.localPosition = localPos;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            attackPoint.position,
            attackBoxSize,
            0f,
            enemyLayer
        );

        bool didHitEnemy = false;

        foreach (Collider2D hit in hits)
        {
            Health health = hit.GetComponent<Health>();

            if (health == null)
            {
                health = hit.GetComponentInParent<Health>();
            }

            if (health != null)
            {
                health.TakeDamage(damage);
                didHitEnemy = true;
            }

            Rigidbody2D rb = hit.attachedRigidbody;

            if (rb == null)
            {
                rb = hit.GetComponentInParent<Rigidbody2D>();
            }

            if (rb != null)
            {
                Vector2 dir = (hit.transform.position - transform.position).normalized;
                rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
            }
        }

        if (didHitEnemy)
        {
            AudioManager.Instance?.PlaySfx(SfxId.SwordImpact);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackBoxSize);
    }
}