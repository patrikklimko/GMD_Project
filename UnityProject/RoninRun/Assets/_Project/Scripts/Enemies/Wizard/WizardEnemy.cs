using System.Collections;
using UnityEngine;

/// <summary>
/// Ranged spellcaster enemy. Patrols between two waypoints; on
/// detecting the player, stops, faces them, casts a projectile
/// after a wind-up, and goes on cooldown. Resumes patrol if the
/// player leaves detection range.
///
/// Reuses <see cref="EnemyBase"/> for movement/HP/facing scaffolding
/// and stays decoupled from the player by working through
/// <see cref="EnemyBase.player"/>. Combat tunables come from an
/// optional <see cref="EnemyConfigSO"/> asset so the same script
/// can power a balance variant by swapping a config asset.
/// </summary>
public class WizardEnemy : EnemyBase
{
    [Header("Optional config asset")]
    [Tooltip("If assigned, overrides serialized HP/speed/range/cooldown values on Awake.")]
    [SerializeField] private EnemyConfigSO config;

    [Header("Patrol")]
    [SerializeField] private Transform leftPoint;
    [SerializeField] private Transform rightPoint;
    [SerializeField] private bool startMovingRight = true;

    [Header("Casting")]
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("World transform from which projectiles spawn. Place at the wizard's hand.")]
    [SerializeField] private Transform castPoint;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private int projectileDamage = 1;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string castTriggerParam = "Cast";

    private int _patrolDirection = 1;
    private float _attackTimer;
    private bool _isCasting;
    private float _originalScaleX;

    public int FacingDirection { get; private set; } = 1;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null) animator = GetComponent<Animator>();
        _originalScaleX = Mathf.Abs(transform.localScale.x);

        ApplyConfig();
    }

    /// <summary>
    /// Pulls tunables from the config asset. Override the ones you
    /// want different on the prefab itself by leaving config null.
    /// </summary>
    private void ApplyConfig()
    {
        if (config == null) return;

        // Health is on a separate component; only set if it's still
        // at its default and the config disagrees.
        if (health != null)
        {
            // Health.MaxHp is read-only; designers tune Health directly.
            // The config exists primarily for AI tunables.
        }

        moveSpeed       = config.moveSpeed;
        detectionRange  = config.detectionRange;
        attackRange     = config.attackRange;
        projectileDamage = Mathf.Max(projectileDamage, config.rangedDamage);
    }

    protected override void Start()
    {
        base.Start();
        _patrolDirection = startMovingRight ? 1 : -1;
        if (health != null)
        {
            health.OnDied += HandleDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnDied -= HandleDied;
        }
    }

    private void HandleDied()
    {
        // EnemyBase.Die() destroys us; this hook exists so future
        // milestones (drops, score) have a single place to extend.
        isDead = true;
    }

    protected override void TickBehaviour()
    {
        if (_isCasting) return;

        if (_attackTimer > 0f)
        {
            _attackTimer -= Time.deltaTime;
        }

        if (IsPlayerInDetectionRange() && _attackTimer <= 0f)
        {
            StartCoroutine(CastRoutine());
        }

        if (animator != null)
        {
            float speedValue = Mathf.Abs(rb.linearVelocity.x) > 0.05f ? 1f : 0f;
            animator.SetFloat(speedParam, speedValue);
        }
    }

    protected override void TickMovement()
    {
        if (_isCasting)
        {
            StopMoving();
            return;
        }

        if (IsPlayerInDetectionRange())
        {
            // In detection range: hold ground, face the player, charge spells.
            StopMoving();
            FacePlayer();
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

    private void FacePlayer()
    {
        if (player == null) return;
        float dirX = Mathf.Sign(player.position.x - transform.position.x);
        UpdateFacingFromDirection(dirX);
    }

    private void UpdateFacingFromDirection(float directionX)
    {
        if (!canFlipSprite || Mathf.Abs(directionX) < 0.01f) return;

        FacingDirection = directionX > 0 ? 1 : -1;

        Vector3 scale = transform.localScale;
        scale.x = directionX > 0 ? _originalScaleX : -_originalScaleX;
        transform.localScale = scale;
    }

    private IEnumerator CastRoutine()
    {
        _isCasting = true;
        _attackTimer = config != null ? config.attackCooldown : 2f;

        FacePlayer();

        if (animator != null)
        {
            animator.SetTrigger(castTriggerParam);
        }

        float windUp = config != null ? config.attackWindUp : 0.5f;
        if (windUp > 0f)
        {
            yield return new WaitForSeconds(windUp);
        }

        SpawnProjectile();

        // Brief recovery so the wizard doesn't immediately move during the
        // cast end-frames.
        yield return new WaitForSeconds(0.25f);

        _isCasting = false;
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: projectilePrefab not assigned.", this);
            return;
        }

        Vector2 origin = castPoint != null ? (Vector2)castPoint.position : (Vector2)transform.position;
        Vector2 direction = new Vector2(FacingDirection, 0f);

        Projectile shot = Instantiate(projectilePrefab, origin, Quaternion.identity);
        shot.Launch(direction, projectileDamage, projectileSpeed);
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (castPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(castPoint.position, 0.15f);
        }
    }
}
