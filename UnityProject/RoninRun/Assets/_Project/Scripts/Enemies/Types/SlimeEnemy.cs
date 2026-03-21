using UnityEngine;

public class SlimeEnemy : EnemyBase
{
    [Header("Patrol")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private bool startMovingRight = true;
    

    [Header("Attack")]
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private Animator animator;

    private int _patrolDirection = 1;
    private bool _isChasingPlayer;
    private bool _isAttacking;
    private float _attackTimer;
    public int FacingDirection { get; private set; } = 1;

    private float _originalScaleX;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponent<Animator>();

        _originalScaleX = Mathf.Abs(transform.localScale.x);
    }

    protected override void Start()
    {
        base.Start();
        _patrolDirection = startMovingRight ? 1 : -1;
    }

    protected override void TickBehaviour()
    {
        _attackTimer -= Time.deltaTime;

        _isChasingPlayer = IsPlayerInDetectionRange();
        _isAttacking = IsPlayerInAttackRange();

        if (animator != null)
        {
            float speedValue = (!_isAttacking && Mathf.Abs(rb.linearVelocity.x) > 0.05f) ? 1f : 0f;
            animator.SetFloat("Speed", speedValue);
            animator.SetBool("IsAttacking", _isAttacking);
        }
    }

    protected override void TickMovement()
    {
        if (_isAttacking)
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

    // sprite faces LEFT by default
    scale.x = directionX > 0 ? -_originalScaleX : _originalScaleX;

    transform.localScale = scale;
}

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!_isAttacking) return;
        if (_attackTimer > 0f) return;

        PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
        if (playerHealth == null) return;

        Vector2 hitDirection = (collision.transform.position - transform.position).normalized;
        playerHealth.TakeDamage(contactDamage, hitDirection);
        _attackTimer = attackCooldown;
    }

    protected override void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (leftPoint != null && rightPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(leftPoint.position, rightPoint.position);
            Gizmos.DrawWireSphere(leftPoint.position, 0.12f);
            Gizmos.DrawWireSphere(rightPoint.position, 0.12f);
        }
    }
}