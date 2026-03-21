using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Transform player;
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Rigidbody2D rb;
    [SerializeField] protected Health health;

    [Header("Detection")]
    [SerializeField] protected float detectionRange = 6f;
    [SerializeField] protected float attackRange = 1.2f;

    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected bool canFlipSprite = true;

    protected bool isDead;

    protected virtual void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (health == null) health = GetComponent<Health>();
    }

    protected virtual void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return;

        TickBehaviour();
        UpdateFacing();
    }

    protected virtual void FixedUpdate()
    {
        if (isDead) return;
        TickMovement();
    }

    protected virtual void TickBehaviour()
    {
        // Override in child classes for custom AI decisions
    }

    protected virtual void TickMovement()
    {
        // Override in child classes for movement logic
    }

    protected float DistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, player.position);
    }

    protected bool IsPlayerInDetectionRange()
    {
        return DistanceToPlayer() <= detectionRange;
    }

    protected bool IsPlayerInAttackRange()
    {
        return DistanceToPlayer() <= attackRange;
    }

    protected virtual void MoveTowardsPlayer()
    {
        if (player == null) return;

        Vector2 targetPosition = new Vector2(player.position.x, transform.position.y);
        Vector2 newPosition = Vector2.MoveTowards(rb.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);
    }

    protected virtual void StopMoving()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    protected virtual void UpdateFacing()
    {
        if (!canFlipSprite || player == null || spriteRenderer == null) return;

        if (player.position.x < transform.position.x)
            spriteRenderer.flipX = true;
        else if (player.position.x > transform.position.x)
            spriteRenderer.flipX = false;
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        Destroy(gameObject);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}