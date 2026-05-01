using UnityEngine;

/// <summary>
/// Generic 2D projectile. Travels in <see cref="initialDirection"/>
/// at constant speed, deals damage to anything implementing
/// <see cref="IDamageable"/> on the configured layer mask, and
/// despawns on hit, on terrain contact, or after a lifetime expires.
///
/// Designed to be reusable: the Wizard fires it today, the boss
/// fan-cast in Milestone 3 will reuse the same prefab with a
/// different direction.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 8f;
    [Tooltip("Set by Launch(); the inspector value is only a default for testing.")]
    [SerializeField] private Vector2 initialDirection = Vector2.right;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [Tooltip("Layers we are allowed to damage (typically 'Player').")]
    [SerializeField] private LayerMask damageMask;
    [Tooltip("Layers that destroy us on contact without taking damage (typically 'Ground').")]
    [SerializeField] private LayerMask blockMask;

    [Header("Lifetime")]
    [SerializeField] private float lifetime = 4f;

    [Header("Visuals (optional)")]
    [Tooltip("Particle prefab spawned at the impact location. Optional.")]
    [SerializeField] private GameObject impactVfxPrefab;

    private Rigidbody2D _rb;
    private float _spawnTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // Trigger collider so projectile passes through the shooter.
        // Wizard's hitbox should be on a non-overlapping layer.
        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void Start()
    {
        _spawnTime = Time.time;
        _rb.linearVelocity = initialDirection.normalized * speed;
    }

    private void Update()
    {
        if (Time.time - _spawnTime >= lifetime)
        {
            Despawn(transform.position);
        }
    }

    /// <summary>
    /// Configure direction (and optionally damage/speed) at spawn time.
    /// Called by WizardEnemy after Instantiate.
    /// </summary>
    public void Launch(Vector2 direction, int damageOverride = -1, float speedOverride = -1f)
    {
        initialDirection = direction.normalized;

        if (damageOverride >= 0) damage = damageOverride;
        if (speedOverride > 0f) speed = speedOverride;

        // Face the direction of travel.
        if (initialDirection.x < 0f)
        {
            Vector3 s = transform.localScale;
            s.x = -Mathf.Abs(s.x);
            transform.localScale = s;
        }

        if (_rb != null)
        {
            _rb.linearVelocity = initialDirection * speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Block on terrain.
        if (((1 << other.gameObject.layer) & blockMask) != 0)
        {
            Despawn(transform.position);
            return;
        }

        // Damage on player (or anything in damageMask).
        if (((1 << other.gameObject.layer) & damageMask) == 0)
        {
            return;
        }

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        Despawn(other.bounds.ClosestPoint(transform.position));
    }

    private void Despawn(Vector3 atPosition)
    {
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, atPosition, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawRay(transform.position, initialDirection.normalized * 1.5f);
    }
}
