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

        float targetX = _patrolDirection > 0 ? rightPoint.position.x : leftPoint.position.x;

        Vector2 target = new Vector2(targetX, rb.position.y);
        Vector2 newPosition = Vector2.MoveTowards(rb.position, target, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

        if (_patrolDirection > 0 && transform.position.x >= rightPoint.position.x - 0.05f)
            _patrolDirection = -1;
        else if (_patrolDirection < 0 && transform.position.x <= leftPoint.position.x + 0.05f)
            _patrolDirection = 1;

        UpdateFacingFromDirection(_patrolDirection);
    }

    protected override void MoveTowardsPlayer()
    {
        if (player == null) return;

        float directionX = Mathf.Sign(player.position.x - transform.position.x);
        Vector2 target = new Vector2(transform.position.x + directionX, rb.position.y);
        Vector2 newPosition = Vector2.MoveTowards(rb.position, target, moveSpeed * Time.fixedDeltaTime);

        rb.MovePosition(newPosition);

        UpdateFacingFromDirection(directionX);
    }

    private void UpdateFacingFromDirection(float directionX)
    {
        if (!canFlipSprite) return;
        if (Mathf.Abs(directionX) < 0.01f) return;

        FacingDirection = directionX > 0 ? 1 : -1;

        Vector3 scale = transform.localScale;

        // slime faces LEFT by default
        scale.x = directionX > 0 ? -_originalScaleX : _originalScaleX;

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