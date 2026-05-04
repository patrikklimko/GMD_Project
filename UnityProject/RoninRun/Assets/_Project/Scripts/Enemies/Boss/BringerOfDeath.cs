using System.Collections;
using UnityEngine;

/// <summary>
/// Final boss. Two-phase fight using a plain-enum FSM. Phase 1
/// (HP > 50%) is a melee stalker that walks toward the player and
/// alternates Slash and Charge attacks. Phase 2 (HP <= 50%) is a
/// teleporting caster that vanishes, reappears at one of four
/// scene-placed anchors, and fires a 3-projectile fan. A Slash
/// fallback fires if the player crowds the boss in melee.
///
/// Reuses every system shipped in earlier milestones:
///   - EnemyBase: physics, sprite-flip, Health.
///   - Health.OnHealthChanged: drives phase transition + HP bar UI.
///   - Projectile prefab: fan-cast in Phase 2.
///   - SceneLoader.LoadVictory: end-of-fight transition.
///   - AudioManager: stinger SFX on transition + death.
/// </summary>
public class BringerOfDeath : EnemyBase
{
    private enum BossState
    {
        Intro,
        Walk,
        WindUpSlash,
        Slash,
        WindUpCharge,
        Charge,
        PhaseTransition,
        Teleport,
        WindUpCast,
        Cast,
        Recover,
        Dead
    }

    [Header("Optional config")]
    [SerializeField] private EnemyConfigSO config;

    [Header("Phase split")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseTwoThreshold = 0.5f;

    [Header("Slash (both phases)")]
    [SerializeField] private float slashRange = 2.0f;
    [SerializeField] private int   slashDamage = 3;
    [SerializeField] private float slashWindUp = 0.4f;
    [SerializeField] private float slashHitMoment = 0.2f;
    [SerializeField] private float slashCooldown = 1.6f;
    [SerializeField] private Vector2 slashHitboxSize = new Vector2(2.4f, 1.6f);
    [SerializeField] private LayerMask playerMask;

    [Header("Charge (Phase 1)")]
    [SerializeField] private float chargeMinDistance = 4f;
    [SerializeField] private float chargeMaxDistance = 6f;
    [SerializeField] private int   chargeDamage = 3;
    [SerializeField] private float chargeWindUp = 0.6f;
    [SerializeField] private float chargeDuration = 0.6f;
    [SerializeField] private float chargeSpeedMultiplier = 2f;
    [SerializeField] private float chargeCooldown = 4.5f;

    [Header("Phase 2 — Teleport + Cast")]
    [SerializeField] private Transform[] teleportAnchors;
    [SerializeField] private float teleportFadeOut = 0.2f;
    [SerializeField] private float teleportFadeIn = 0.2f;
    [SerializeField] private float teleportCooldown = 2.0f;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform castPoint;
    [SerializeField] private int projectileDamage = 2;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private float castWindUp = 0.5f;
    [SerializeField] private float castCooldown = 2.5f;
    [SerializeField] [Range(5f, 60f)] private float fanAngleDegrees = 25f;

    [Header("Phase transition")]
    [SerializeField] private float phaseTransitionDuration = 1.5f;
    [SerializeField] private float phaseTransitionInvulnTime = 1.0f;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string slashParam = "Slash";
    [SerializeField] private string castParam  = "Cast";
    [SerializeField] private string deathParam = "Death";

    [Header("SFX (optional, played via AudioManager)")]
    [SerializeField] private SfxId phaseTransitionSfx = SfxId.None;
    [SerializeField] private SfxId slashSfx = SfxId.None;
    [SerializeField] private SfxId castSfx  = SfxId.WizardCast;
    [SerializeField] private SfxId deathSfx = SfxId.None;

    [Header("Death sequence")]
    [SerializeField] private BossDeathSequence deathSequence;

    private BossState _state = BossState.Intro;
    private float _slashTimer;
    private float _chargeTimer;
    private float _castTimer;
    private float _teleportTimer;
    private bool _phaseTwoEntered;
    private bool _isInvulnerable;
    private float _invulnUntil;
    private float _originalScaleX;
    private SpriteRenderer _spriteRenderer;

    public bool IsInPhaseTwo => _phaseTwoEntered;

    protected override void Awake()
    {
        base.Awake();
        _spriteRenderer = spriteRenderer;
        _originalScaleX = Mathf.Abs(transform.localScale.x);
        if (animator == null) animator = GetComponent<Animator>();
        ApplyConfig();
    }

    private void ApplyConfig()
    {
        if (config == null) return;
        moveSpeed       = config.moveSpeed;
        detectionRange  = config.detectionRange;
        attackRange     = config.attackRange;
        chargeDamage    = Mathf.Max(chargeDamage, config.contactDamage);
        projectileDamage = Mathf.Max(projectileDamage, config.rangedDamage);
        slashCooldown   = config.attackCooldown;
        slashWindUp     = config.attackWindUp;
    }

    protected override void Start()
    {
        base.Start();
        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDied += HandleDied;
        }
        // Begin in Intro until external code (BossArenaTrigger) calls
        // BeginFight(); fall back to Walk after a short delay if it
        // isn't wired so designer-direct testing still works.
        StartCoroutine(IntroFallback());
    }

    private IEnumerator IntroFallback()
    {
        yield return new WaitForSeconds(0.25f);
        if (_state == BossState.Intro)
        {
            BeginFight();
        }
    }

    /// <summary>External hook — BossArenaTrigger calls this after the intro pan.</summary>
    public void BeginFight()
    {
        if (_state == BossState.Intro)
        {
            _state = BossState.Walk;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDied -= HandleDied;
        }
    }

    // ---- behaviour driver -------------------------------------------------

    protected override void TickBehaviour()
    {
        if (_state == BossState.Dead || _state == BossState.Intro) return;

        // Phase transition is the only state that can fire mid-anything.
        if (!_phaseTwoEntered &&
            health != null &&
            (float)health.CurrentHp / Mathf.Max(1, health.MaxHp) <= phaseTwoThreshold)
        {
            EnterPhaseTwo();
            return;
        }

        TickTimers();
        UpdateAnimatorSpeed();

        switch (_state)
        {
            case BossState.Walk:
                ChooseNextAttack();
                break;
            case BossState.PhaseTransition:
            case BossState.WindUpSlash:
            case BossState.Slash:
            case BossState.WindUpCharge:
            case BossState.Charge:
            case BossState.Teleport:
            case BossState.WindUpCast:
            case BossState.Cast:
            case BossState.Recover:
                // Driven by coroutines; nothing to do here.
                break;
        }
    }

    protected override void TickMovement()
    {
        if (_state == BossState.Dead || _state == BossState.Intro) return;

        if (_state == BossState.Walk)
        {
            MoveTowardsPlayer();
        }
        else
        {
            StopMoving();
        }
    }

    private void TickTimers()
    {
        float dt = Time.deltaTime;
        if (_slashTimer    > 0f) _slashTimer    -= dt;
        if (_chargeTimer   > 0f) _chargeTimer   -= dt;
        if (_castTimer     > 0f) _castTimer     -= dt;
        if (_teleportTimer > 0f) _teleportTimer -= dt;
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null) return;
        float speedValue = (_state == BossState.Walk && Mathf.Abs(rb.linearVelocity.x) > 0.05f) ? 1f : 0f;
        animator.SetFloat(speedParam, speedValue);
    }

    // ---- attack selection -------------------------------------------------

    private void ChooseNextAttack()
    {
        float distance = DistanceToPlayer();

        if (_phaseTwoEntered)
        {
            // Phase 2: prefer teleport+cast, slash interrupt at melee.
            if (distance <= slashRange && _slashTimer <= 0f)
            {
                StartCoroutine(SlashRoutine());
                return;
            }
            if (_teleportTimer <= 0f)
            {
                StartCoroutine(TeleportThenCastRoutine());
                return;
            }
            return;
        }

        // Phase 1: Slash if close, Charge if mid-range, else walk.
        if (distance <= slashRange && _slashTimer <= 0f)
        {
            StartCoroutine(SlashRoutine());
            return;
        }
        if (distance >= chargeMinDistance && distance <= chargeMaxDistance && _chargeTimer <= 0f)
        {
            StartCoroutine(ChargeRoutine());
            return;
        }
    }

    // ---- attacks ----------------------------------------------------------

    private IEnumerator SlashRoutine()
    {
        _state = BossState.WindUpSlash;
        _slashTimer = slashCooldown;
        FacePlayer();
        if (animator != null) animator.SetTrigger(slashParam);
        if (slashSfx != SfxId.None && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(slashSfx);

        yield return new WaitForSeconds(slashWindUp);
        if (_state == BossState.Dead) yield break;

        _state = BossState.Slash;
        DoSlashHit();

        yield return new WaitForSeconds(slashHitMoment);
        _state = BossState.Walk;
    }

    private void DoSlashHit()
    {
        Vector2 origin = (Vector2)transform.position +
                         new Vector2(FacingSign() * slashRange * 0.5f, 0f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(origin, slashHitboxSize, 0f, playerMask);
        foreach (Collider2D c in hits)
        {
            IDamageable target = c.GetComponentInParent<IDamageable>();
            target?.TakeDamage(slashDamage);
        }
    }

    private IEnumerator ChargeRoutine()
    {
        _state = BossState.WindUpCharge;
        _chargeTimer = chargeCooldown;
        FacePlayer();
        if (animator != null) animator.SetTrigger(slashParam); // reuse slash anim if no charge anim

        yield return new WaitForSeconds(chargeWindUp);
        if (_state == BossState.Dead) yield break;

        _state = BossState.Charge;
        float dir = FacingSign();
        float t = 0f;
        while (t < chargeDuration && _state == BossState.Charge)
        {
            t += Time.deltaTime;
            Vector2 step = new Vector2(dir * moveSpeed * chargeSpeedMultiplier * Time.deltaTime, 0f);
            rb.MovePosition(rb.position + step);

            // Damage player on contact during the charge.
            Collider2D[] hits = Physics2D.OverlapBoxAll(
                (Vector2)transform.position + new Vector2(dir * 0.6f, 0f),
                slashHitboxSize, 0f, playerMask);
            foreach (Collider2D c in hits)
            {
                IDamageable target = c.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(chargeDamage);
                    t = chargeDuration; // end the charge on connect
                    break;
                }
            }
            yield return null;
        }

        _state = BossState.Walk;
    }

    private IEnumerator TeleportThenCastRoutine()
    {
        _state = BossState.Teleport;
        _teleportTimer = teleportCooldown;

        // Fade out.
        yield return Fade(1f, 0f, teleportFadeOut);
        if (_state == BossState.Dead) yield break;

        // Reposition.
        Transform anchor = PickTeleportAnchor();
        if (anchor != null)
        {
            transform.position = anchor.position;
        }
        FacePlayer();

        // Fade in.
        yield return Fade(0f, 1f, teleportFadeIn);

        // Cast.
        _state = BossState.WindUpCast;
        _castTimer = castCooldown;
        if (animator != null) animator.SetTrigger(castParam);
        if (castSfx != SfxId.None && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(castSfx);

        yield return new WaitForSeconds(castWindUp);
        if (_state == BossState.Dead) yield break;

        _state = BossState.Cast;
        SpawnFanProjectiles();

        yield return new WaitForSeconds(0.25f);
        _state = BossState.Walk;
    }

    private Transform PickTeleportAnchor()
    {
        if (teleportAnchors == null || teleportAnchors.Length == 0) return null;

        // Coin flip: behind player, or random anchor.
        if (Random.value < 0.5f && player != null)
        {
            // Find anchor furthest from player to give "behind" feel.
            Transform best = teleportAnchors[0];
            float bestDist = Vector2.Distance(best.position, player.position);
            for (int i = 1; i < teleportAnchors.Length; i++)
            {
                float d = Vector2.Distance(teleportAnchors[i].position, player.position);
                if (d > bestDist) { best = teleportAnchors[i]; bestDist = d; }
            }
            return best;
        }
        return teleportAnchors[Random.Range(0, teleportAnchors.Length)];
    }

    private void SpawnFanProjectiles()
    {
        if (projectilePrefab == null || player == null) return;

        Vector2 origin = castPoint != null ? (Vector2)castPoint.position : (Vector2)transform.position;
        Vector2 toPlayer = ((Vector2)player.position - origin).normalized;

        float[] offsets = { -fanAngleDegrees, 0f, +fanAngleDegrees };
        foreach (float deg in offsets)
        {
            float rad = deg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(
                toPlayer.x * Mathf.Cos(rad) - toPlayer.y * Mathf.Sin(rad),
                toPlayer.x * Mathf.Sin(rad) + toPlayer.y * Mathf.Cos(rad));

            Projectile shot = Instantiate(projectilePrefab, origin, Quaternion.identity);
            shot.Launch(dir, projectileDamage, projectileSpeed);
        }
    }

    // ---- phase transition + death ----------------------------------------

    private void EnterPhaseTwo()
    {
        if (_phaseTwoEntered) return;
        _phaseTwoEntered = true;
        StartCoroutine(PhaseTransitionRoutine());
    }

    private IEnumerator PhaseTransitionRoutine()
    {
        _state = BossState.PhaseTransition;
        _isInvulnerable = true;
        _invulnUntil = Time.time + phaseTransitionInvulnTime;

        if (phaseTransitionSfx != SfxId.None && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(phaseTransitionSfx, 1.2f);

        // Brief flash.
        yield return Fade(1f, 0.4f, 0.15f);
        yield return Fade(0.4f, 1f, 0.15f);
        yield return new WaitForSeconds(phaseTransitionDuration - 0.3f);

        _isInvulnerable = false;
        _state = BossState.Walk;
    }

    public override void Die()
    {
        if (_state == BossState.Dead) return;
        _state = BossState.Dead;
        StopAllCoroutines();

        if (animator != null) animator.SetTrigger(deathParam);
        if (deathSfx != SfxId.None && AudioManager.Instance != null)
            AudioManager.Instance.PlaySfx(deathSfx);

        if (deathSequence != null)
        {
            deathSequence.Begin();
        }
        else
        {
            // Fallback if the death sequence component isn't wired.
            SceneLoader.LoadVictory();
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (_isInvulnerable) { /* eat the hit, but flash */ }
    }

    private void HandleDied()
    {
        Die();
    }

    // ---- helpers ----------------------------------------------------------

    private float FacingSign()
    {
        if (player == null) return spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
        return Mathf.Sign(player.position.x - transform.position.x);
    }

    private void FacePlayer()
    {
        if (player == null || spriteRenderer == null) return;
        spriteRenderer.flipX = player.position.x < transform.position.x;
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        if (_spriteRenderer == null || duration <= 0f)
        {
            if (_spriteRenderer != null)
            {
                Color c = _spriteRenderer.color;
                c.a = toAlpha;
                _spriteRenderer.color = c;
            }
            yield break;
        }
        float t = 0f;
        Color color = _spriteRenderer.color;
        while (t < duration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t / duration);
            _spriteRenderer.color = color;
            yield return null;
        }
        color.a = toAlpha;
        _spriteRenderer.color = color;
    }

    /// <summary>
    /// While invulnerable (phase transition), short-circuit damage by
    /// calling this from the EnemyHealth wrapper. Currently the boss
    /// uses the standard Health component, so invuln is "soft": damage
    /// still registers but design-time it's brief enough not to matter.
    /// Hook this up in a future polish pass if needed.
    /// </summary>
    public bool ConsumeIfInvulnerable() => _isInvulnerable && Time.time < _invulnUntil;

    // ---- gizmos -----------------------------------------------------------

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Vector2 origin = (Vector2)transform.position +
                         new Vector2(FacingSign() * slashRange * 0.5f, 0f);
        Gizmos.DrawWireCube(origin, slashHitboxSize);

        Gizmos.color = Color.cyan;
        if (teleportAnchors != null)
        {
            for (int i = 0; i < teleportAnchors.Length; i++)
            {
                if (teleportAnchors[i] != null)
                {
                    Gizmos.DrawWireSphere(teleportAnchors[i].position, 0.4f);
                }
            }
        }

        if (castPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(castPoint.position, 0.15f);
        }
    }
}
