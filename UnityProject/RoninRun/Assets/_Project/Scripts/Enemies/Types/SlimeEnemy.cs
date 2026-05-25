using System.Collections;
using UnityEngine;

public class SlimeEnemy : EnemyBase
{
    [Header("Patrol")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private bool startMovingRight = true;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float attackTriggerRange = 4f;
    [SerializeField] private Animator animator;
    [SerializeField] private SlimeAttack slimeAttack;

    [Header("SFX")]
    [Tooltip("Sound played at the start of each attack (the tornado).")]
    [SerializeField] private SfxId attackSfx = SfxId.SlimeAttack;
    [Range(0f, 1f)]
    [SerializeField] private float attackSfxVolume = 1f;

    private int _patrolDirection = 1;
    private bool _isChasingPlayer;
    private bool _isPerformingAttack;
    private float _attackTimer;
    private float _originalScaleX;

    public int FacingDirection { get; private set; } = 1;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (slimeAttack == null)
            slimeAttack = GetComponent<SlimeAttack>();

        _originalScaleX = Mathf.Abs(transform.localScale.x);
    }

    protected override void Start()
    {
        base.Start();
        _patrolDirection = startMovingRight ? 1 : -1;
    }

    protected override void TickBehaviour()
    {
        if (_attackTimer > 0f)
            _attackTimer -= Time.deltaTime;

        float distance = DistanceToPlayer();

        _isChasingPlayer = distance <= detectionRange;

        // Always face the player when they're in attack range, even during
        // the cooldown between dashes. Without this the slime can end up
        // looking the wrong way while waiting to attack again.
        if (!_isPerformingAttack && player != null && distance <= attackTriggerRange)
        {
            float faceDir = Mathf.Sign(player.position.x - transform.position.x);
            UpdateFacingFromDirection(faceDir);
        }

        if (!_isPerformingAttack && distance <= attackTriggerRange && _attackTimer <= 0f)
        {
            StartCoroutine(AttackRoutine());
            return;
        }

        if (animator != null)
        {
            float speedValue = (!_isPerformingAttack && Mathf.Abs(rb.linearVelocity.x) > 0.05f) ? 1f : 0f;
            animator.SetFloat("Speed", speedValue);
            animator.SetBool("IsAttacking", _isPerformingAttack);
        }
    }

    // SlimeEnemy flips using transform.localScale.x (because the slime sprite's
    // default-facing is LEFT). The base class also tries to flip via
    // spriteRenderer.flipX, which would *double-flip* on top of our scale flip
    // and make the slime look the wrong way. Suppress it here.
    protected override void UpdateFacing()
    {
    }

    protected override void TickMovement()
    {
        if (_isPerformingAttack)
        {
            StopMoving();
            return;
        }

        if (_isChasingPlayer)
        {
            MoveTowardsPlayer();
        }
        else
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        if (leftPoint == null || rightPoint == null)
        {
            StopMoving();
            return;
        }

        // Flip patrol direction when hitting a patrol bound.
        if (_patrolDirection > 0 && transform.position.x >= rightPoint.position.x - 0.05f)
            _patrolDirection = -1;
        else if (_patrolDirection < 0 && transform.position.x <= leftPoint.position.x + 0.05f)
            _patrolDirection = 1;

        // Velocity-based movement: drive X, let physics handle Y (gravity).
        // Using MovePosition here would re-pin Y every fixed step and make
        // the slime levitate whenever it walks off a ledge.
        rb.linearVelocity = new Vector2(_patrolDirection * moveSpeed, rb.linearVelocity.y);

        UpdateFacingFromDirection(_patrolDirection);
    }

    protected override void MoveTowardsPlayer()
    {
        if (player == null) return;

        float directionX = Mathf.Sign(player.position.x - transform.position.x);

        // Velocity-based chase. Preserve current Y velocity so gravity
        // applies normally when the slime is mid-air.
        rb.linearVelocity = new Vector2(directionX * moveSpeed, rb.linearVelocity.y);

        UpdateFacingFromDirection(directionX);
    }

    private void UpdateFacingFromDirection(float directionX)
    {
        if (!canFlipSprite) return;
        if (Mathf.Abs(directionX) < 0.01f) return;

        FacingDirection = directionX > 0 ? 1 : -1;

        Vector3 scale = transform.localScale;

        // Slime sprite faces RIGHT at its natural (positive) scale.
        // Moving right  -> keep positive scale.
        // Moving left   -> negate scale to mirror the sprite.
        scale.x = directionX > 0 ? _originalScaleX : -_originalScaleX;

        transform.localScale = scale;
    }

    private IEnumerator AttackRoutine()
    {
        _isPerformingAttack = true;
        _attackTimer = attackCooldown;

        if (player != null)
        {
            float directionX = Mathf.Sign(player.position.x - transform.position.x);
            UpdateFacingFromDirection(directionX);
        }

        if (animator != null)
            animator.SetBool("IsAttacking", true);

        if (attackSfx != SfxId.None && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(attackSfx, attackSfxVolume);
        }

        yield return slimeAttack.PerformDashAttack();

        if (animator != null)
            animator.SetBool("IsAttacking", false);

        _isPerformingAttack = false;
    }

    protected override void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }
}