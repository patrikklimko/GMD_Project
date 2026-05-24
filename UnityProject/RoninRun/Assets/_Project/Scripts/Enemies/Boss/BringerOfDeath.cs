using System.Collections;
using UnityEngine;

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

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool debugAnimatorLogs = true;

    [Header("Optional config")]
    [SerializeField] private EnemyConfigSO config;

    [Header("Phase split")]
    [Range(0f, 1f)]
    [SerializeField] private float phaseTwoThreshold = 0.5f;

    [Header("Slash")]
    [SerializeField] private float slashRange = 2.2f;
    [SerializeField] private int slashDamage = 3;
    [SerializeField] private float slashWindUp = 0.45f;
    [SerializeField] private float slashHitMoment = 0.45f;
    [SerializeField] private float slashCooldown = 4f;
    [SerializeField] private Vector2 slashHitboxOffset = new Vector2(1.4f, 0.9f);
    [SerializeField] private Vector2 slashHitboxSize = new Vector2(3.5f, 2.5f);
    [SerializeField] private LayerMask playerMask;

    [Header("Charge - Phase 1")]
    [SerializeField] private float chargeMinDistance = 4f;
    [SerializeField] private float chargeMaxDistance = 6f;
    [SerializeField] private int chargeDamage = 3;
    [SerializeField] private float chargeWindUp = 0.6f;
    [SerializeField] private float chargeDuration = 0.6f;
    [SerializeField] private float chargeSpeedMultiplier = 2f;
    [SerializeField] private float chargeCooldown = 4.5f;

    [Header("Phase 2 - Teleport + Cast")]
    [SerializeField] private Transform[] teleportAnchors;
    [SerializeField] private float teleportFadeOut = 0.2f;
    [SerializeField] private float teleportFadeIn = 0.2f;
    [SerializeField] private float teleportCooldown = 4f;
    [SerializeField] private float stayAfterTeleportSeconds = 4f;

    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform castPoint;
    [SerializeField] private int projectileDamage = 2;
    [SerializeField] private float projectileSpeed = 7f;
    [SerializeField] private float castWindUp = 0.5f;
    [SerializeField] private float castCooldown = 2.5f;
    [SerializeField] [Range(5f, 60f)] private float fanAngleDegrees = 25f;

    [Header("Phase transition")]
    [SerializeField] private float phaseTransitionDuration = 1.5f;
    [SerializeField] private float phaseTransitionInvulnTime = 1f;

    [Header("Animation")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string slashParam = "Slash";
    [SerializeField] private string castParam = "Cast";
    [SerializeField] private string deathParam = "Death";
    [SerializeField] private string spellParam = "Spell";


    [Header("Facing")]
    [Tooltip("Toggle this if the boss looks away from the player.")]
    [SerializeField] private bool spriteFacesRightByDefault = false;

    [Header("SFX")]
    [SerializeField] private SfxId phaseTransitionSfx = SfxId.None;
    [SerializeField] private SfxId slashSfx = SfxId.SwordSlash1;
    [SerializeField] private SfxId castSfx = SfxId.WizardCast;
    [SerializeField] private SfxId deathSfx = SfxId.None;

    [Header("Death sequence")]
    [SerializeField] private BossDeathSequence deathSequence;

    private BossState _state = BossState.Intro;

    private float _chargeTimer;
    private float _castTimer;
    private float _teleportTimer;

    // HARD slash cooldown.
    // This is the important anti-spam fix.
    private float _nextSlashAllowedTime;
    private bool _slashRoutineRunning;
    private float _meleeLockedUntil;

    private bool _phaseTwoEntered;
    private bool _isInvulnerable;
    private float _invulnUntil;

    private SpriteRenderer _spriteRenderer;

    public bool IsInPhaseTwo => _phaseTwoEntered;

    protected override void Awake()
    {
        base.Awake();

        ForceChildVisualReferences();

        if (health == null)
        {
            health = GetComponent<Health>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (deathSequence == null)
        {
            deathSequence = GetComponent<BossDeathSequence>();
        }

        ApplyConfig();
        DebugValidateReferences("Awake");
    }

    protected override void Start()
    {
        base.Start();

        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDied += HandleDied;
        }

        DebugValidateReferences("Start");
        StartCoroutine(IntroFallback());
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDied -= HandleDied;
        }
    }

    private void ForceChildVisualReferences()
{
    Animator childAnimator = null;
    SpriteRenderer childRenderer = null;

    foreach (Animator candidate in GetComponentsInChildren<Animator>(true))
    {
        if (candidate.gameObject != gameObject)
        {
            childAnimator = candidate;
            break;
        }
    }

    foreach (SpriteRenderer candidate in GetComponentsInChildren<SpriteRenderer>(true))
    {
        if (candidate.gameObject != gameObject)
        {
            childRenderer = candidate;
            break;
        }
    }

    if (animator == null || animator.gameObject == gameObject)
    {
        animator = childAnimator;

        if (animator != null)
        {
            Log("Auto-assigned child Animator: " + animator.gameObject.name);
        }
        else
        {
            LogWarning("No child Animator found. Boss animations will not play.");
        }
    }

    if (spriteRenderer == null || spriteRenderer.gameObject == gameObject)
    {
        spriteRenderer = childRenderer;

        if (spriteRenderer != null)
        {
            Log("Auto-assigned child SpriteRenderer: " + spriteRenderer.gameObject.name);
        }
        else
        {
            LogWarning("No child SpriteRenderer found. Boss facing/fade will not work.");
        }
    }

    _spriteRenderer = spriteRenderer;
}

    private void ApplyConfig()
    {
        if (config == null)
        {
            return;
        }

        moveSpeed = config.moveSpeed;
        detectionRange = config.detectionRange;
        attackRange = config.attackRange;

        chargeDamage = Mathf.Max(chargeDamage, config.contactDamage);
        projectileDamage = Mathf.Max(projectileDamage, config.rangedDamage);

        // Keep boss slash cooldown at minimum 4 seconds.
        slashCooldown = Mathf.Max(4f, config.attackCooldown);
        slashWindUp = config.attackWindUp;
    }

    private IEnumerator IntroFallback()
    {
        yield return new WaitForSeconds(0.25f);

        if (_state == BossState.Intro)
        {
            Log("IntroFallback started boss automatically.");
            BeginFight();
        }
    }

    public void BeginFight()
    {
        if (_state != BossState.Intro)
        {
            Log("BeginFight ignored because state is already: " + _state);
            return;
        }

        DebugValidateReferences("BeginFight");

        ChangeState(BossState.Walk, "BeginFight");

        _chargeTimer = 0f;
        _castTimer = 0f;
        _teleportTimer = 0f;

        // Allow first slash immediately.
        _nextSlashAllowedTime = 0f;
        _slashRoutineRunning = false;

        FacePlayer();
        Log("BOSS BEGIN FIGHT CALLED");
    }

    protected override void TickBehaviour()
    {
        if (_state == BossState.Dead || _state == BossState.Intro)
        {
            return;
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
                Log("Player was missing, found by tag: " + player.name);
            }
            else
            {
                LogWarning("Player is NULL and no GameObject with Tag Player was found.");
                return;
            }
        }

        if (!_phaseTwoEntered &&
            health != null &&
            (float)health.CurrentHp / Mathf.Max(1, health.MaxHp) <= phaseTwoThreshold)
        {
            EnterPhaseTwo();
            return;
        }

        TickTimers();
        FacePlayer();
        UpdateAnimatorSpeed();

        if (_state == BossState.Walk)
        {
            ChooseNextAttack();
        }
    }

    protected override void TickMovement()
    {
        if (_state == BossState.Dead || _state == BossState.Intro)
        {
            return;
        }

        if (_state == BossState.Walk)
        {
            MoveTowardsPlayer();
        }
        else
        {
            StopMoving();
        }
    }

    protected override void MoveTowardsPlayer()
    {
        if (player == null || rb == null)
        {
            return;
        }

        Vector2 targetPosition = new Vector2(player.position.x, rb.position.y);

        Vector2 newPosition = Vector2.MoveTowards(
            rb.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPosition);
    }

    private void TickTimers()
    {
        float dt = Time.deltaTime;

        if (_chargeTimer > 0f)
        {
            _chargeTimer -= dt;
        }

        if (_castTimer > 0f)
        {
            _castTimer -= dt;
        }

        if (_teleportTimer > 0f)
        {
            _teleportTimer -= dt;
        }
    }

    private void UpdateAnimatorSpeed()
    {
        if (animator == null)
        {
            LogWarning("Cannot update Speed because Animator is NULL.");
            return;
        }

        float speedValue = _state == BossState.Walk ? 1f : 0f;
        animator.SetFloat(speedParam, speedValue);
    }

    private void ChooseNextAttack()
{
    if (player == null)
        return;

    // Hard action lock. If the boss recently attacked, he does nothing.
    if (Time.time < _meleeLockedUntil)
    {
        StopMoving();
        UpdateAnimatorSpeed();
        return;
    }

    float distance = DistanceToPlayer();

    LogVerbose(
        $"State={_state}, Distance={distance:F2}, CanSlash={CanSlash()}, NextSlashAllowed={_nextSlashAllowedTime:F2}, MeleeLockedUntil={_meleeLockedUntil:F2}, Time={Time.time:F2}, Phase2={_phaseTwoEntered}"
    );

    if (_phaseTwoEntered)
    {
        if (distance <= slashRange && CanSlash())
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

    if (distance <= slashRange && CanSlash())
    {
        StartCoroutine(SlashRoutine());
        return;
    }

    if (distance >= chargeMinDistance &&
        distance <= chargeMaxDistance &&
        _chargeTimer <= 0f)
    {
        StartCoroutine(ChargeRoutine());
        return;
    }
}

private bool CanSlash()
{
    if (_slashRoutineRunning)
        return false;

    if (Time.time < _nextSlashAllowedTime)
        return false;

    if (Time.time < _meleeLockedUntil)
        return false;

    return true;
}

    private IEnumerator SlashRoutine()
{
    if (_slashRoutineRunning)
        yield break;

    _slashRoutineRunning = true;

    // Lock boss attacks immediately.
    _nextSlashAllowedTime = Time.time + slashCooldown;
    _meleeLockedUntil = Time.time + slashCooldown;

    Log("BOSS SLASH STARTED. Next slash allowed at: " + _nextSlashAllowedTime);

    ChangeState(BossState.WindUpSlash, "SlashRoutine");

    FacePlayer();
    StopMoving();
    UpdateAnimatorSpeed();

    SendTriggerToAnimator(slashParam, "SLASH");

    if (slashSfx != SfxId.None && AudioManager.Instance != null)
    {
        AudioManager.Instance.PlaySfx(slashSfx);
    }

    yield return new WaitForSeconds(slashWindUp);

    if (_state == BossState.Dead)
    {
        _slashRoutineRunning = false;
        yield break;
    }

    ChangeState(BossState.Slash, "Slash hit moment");

    DoSlashHit();

    yield return new WaitForSeconds(slashHitMoment);

    if (_state != BossState.Dead)
    {
        ChangeState(BossState.Recover, "Slash recovery / waiting for cooldown");
    }

    // Stay in recovery until the full 4-second cooldown is done.
    while (_state != BossState.Dead && Time.time < _meleeLockedUntil)
    {
        StopMoving();
        UpdateAnimatorSpeed();
        yield return null;
    }

    if (_state != BossState.Dead)
    {
        ChangeState(BossState.Walk, "Slash cooldown finished");
    }

    _slashRoutineRunning = false;
}

    private void DoSlashHit()
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(
            FacingSign() * slashHitboxOffset.x,
            slashHitboxOffset.y
        );

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            origin,
            slashHitboxSize,
            0f,
            playerMask
        );

        Log($"BOSS SLASH HITBOX origin={origin}, size={slashHitboxSize}, hits={hits.Length}, playerMask={playerMask.value}");

        bool damagedPlayer = false;

        foreach (Collider2D hit in hits)
        {
            Log("Slash overlapped: " + hit.name + " / root: " + hit.transform.root.name);

            if (damagedPlayer)
            {
                break;
            }

            PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();

            if (playerHealth != null)
            {
                Vector2 knockback = new Vector2(FacingSign() * 6f, 3f);
                playerHealth.TakeDamage(slashDamage, knockback);
                damagedPlayer = true;
                continue;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(slashDamage);
                damagedPlayer = true;
            }
        }

        if (damagedPlayer)
        {
            Log("BOSS SLASH HIT PLAYER");
        }
        else
        {
            LogWarning("BOSS SLASH MISSED. Increase Slash Hitbox Offset/Size or check Player Layer/Mask.");
        }
    }

    private IEnumerator ChargeRoutine()
    {
        Log("BOSS CHARGE STARTED");

        ChangeState(BossState.WindUpCharge, "ChargeRoutine");
        _chargeTimer = chargeCooldown;

        FacePlayer();
        SendTriggerToAnimator(slashParam, "CHARGE using SLASH animation");

        yield return new WaitForSeconds(chargeWindUp);

        if (_state == BossState.Dead)
        {
            yield break;
        }

        ChangeState(BossState.Charge, "Charge movement");

        float dir = FacingSign();
        float t = 0f;
        bool damagedPlayer = false;

        while (t < chargeDuration && _state == BossState.Charge)
        {
            t += Time.deltaTime;

            Vector2 step = new Vector2(
                dir * moveSpeed * chargeSpeedMultiplier * Time.deltaTime,
                0f
            );

            rb.MovePosition(rb.position + step);

            if (!damagedPlayer)
            {
                Collider2D[] hits = Physics2D.OverlapBoxAll(
                    (Vector2)transform.position + new Vector2(dir * slashHitboxOffset.x, slashHitboxOffset.y),
                    slashHitboxSize,
                    0f,
                    playerMask
                );

                foreach (Collider2D hit in hits)
                {
                    PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();

                    if (playerHealth != null)
                    {
                        Vector2 knockback = new Vector2(dir * 8f, 4f);
                        playerHealth.TakeDamage(chargeDamage, knockback);
                        damagedPlayer = true;
                        Log("BOSS CHARGE HIT PLAYER");
                        break;
                    }
                }
            }

            yield return null;
        }

        if (_state != BossState.Dead)
        {
            ChangeState(BossState.Walk, "Charge finished");
        }
    }

    private IEnumerator TeleportThenCastRoutine()
{
    Log("BOSS TELEPORT STARTED");

    ChangeState(BossState.Teleport, "TeleportThenCastRoutine");

    // Prevent another teleport routine from starting while this one is running.
    _teleportTimer = 999f;

    FacePlayer();
    StopMoving();
    UpdateAnimatorSpeed();

    // Play teleport/spell animation.
    SendTriggerToAnimator(spellParam, "SPELL / TELEPORT");

    if (castSfx != SfxId.None && AudioManager.Instance != null)
    {
        AudioManager.Instance.PlaySfx(castSfx);
    }

    // Let the Spell animation show before disappearing.
    yield return new WaitForSeconds(castWindUp);

    if (_state == BossState.Dead)
        yield break;

    yield return Fade(1f, 0f, teleportFadeOut);

    if (_state == BossState.Dead)
        yield break;

    Transform anchor = PickTeleportAnchor();

    if (anchor != null)
    {
        Log("Teleporting to anchor: " + anchor.name + " at " + anchor.position);
        transform.position = anchor.position;
    }
    else
    {
        LogWarning("No teleport anchor found. Boss will not move.");
    }

    FacePlayer();

    yield return Fade(0f, 1f, teleportFadeIn);

    // Force visible after teleport.
    if (_spriteRenderer != null)
    {
        Color c = _spriteRenderer.color;
        c.a = 1f;
        _spriteRenderer.color = c;
    }

    ChangeState(BossState.Cast, "After teleport / spawn projectiles");

    SpawnFanProjectiles();

    ChangeState(BossState.Recover, "Stay after teleport/cast");

    Log("BOSS STAYS AFTER TELEPORT FOR " + stayAfterTeleportSeconds + " SECONDS");

    yield return new WaitForSeconds(stayAfterTeleportSeconds);

    if (_state != BossState.Dead)
    {
        // IMPORTANT:
        // Cooldown starts AFTER the 4 second stay, not before.
        _teleportTimer = teleportCooldown;

        ChangeState(BossState.Walk, "Recover finished, teleport cooldown started");
    }
}

    private Transform PickTeleportAnchor()
    {
        if (teleportAnchors == null || teleportAnchors.Length == 0)
        {
            LogWarning("Teleport anchors array is empty.");
            return null;
        }

        Transform best = null;
        float bestDist = float.MinValue;

        foreach (Transform anchor in teleportAnchors)
        {
            if (anchor == null)
            {
                continue;
            }

            if (player == null)
            {
                best = anchor;
                break;
            }

            float dist = Vector2.Distance(anchor.position, player.position);

            if (dist > bestDist)
            {
                best = anchor;
                bestDist = dist;
            }
        }

        return best;
    }

    private void SpawnFanProjectiles()
    {
        if (projectilePrefab == null)
        {
            LogWarning("Projectile Prefab is NULL. Cast animation can play, but no projectile will spawn.");
            return;
        }

        if (player == null)
        {
            LogWarning("Player is NULL. Cannot aim projectiles.");
            return;
        }

        Vector2 origin = castPoint != null
            ? (Vector2)castPoint.position
            : (Vector2)transform.position;

        Vector2 toPlayer = ((Vector2)player.position - origin).normalized;

        float[] offsets = { -fanAngleDegrees, 0f, fanAngleDegrees };

        foreach (float deg in offsets)
        {
            float rad = deg * Mathf.Deg2Rad;

            Vector2 dir = new Vector2(
                toPlayer.x * Mathf.Cos(rad) - toPlayer.y * Mathf.Sin(rad),
                toPlayer.x * Mathf.Sin(rad) + toPlayer.y * Mathf.Cos(rad)
            );

            Projectile shot = Instantiate(projectilePrefab, origin, Quaternion.identity);
            shot.Launch(dir, projectileDamage, projectileSpeed);
        }

        Log("BOSS CAST PROJECTILES");
    }

    private void EnterPhaseTwo()
    {
        if (_phaseTwoEntered)
        {
            return;
        }

        _phaseTwoEntered = true;
        StartCoroutine(PhaseTransitionRoutine());
    }

    private IEnumerator PhaseTransitionRoutine()
    {
        Log("BOSS ENTERED PHASE 2");

        ChangeState(BossState.PhaseTransition, "PhaseTransitionRoutine");

        _isInvulnerable = true;
        _invulnUntil = Time.time + phaseTransitionInvulnTime;

        if (phaseTransitionSfx != SfxId.None && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(phaseTransitionSfx, 1.2f);
        }

        yield return Fade(1f, 0.4f, 0.15f);
        yield return Fade(0.4f, 1f, 0.15f);

        float remaining = Mathf.Max(0f, phaseTransitionDuration - 0.3f);
        yield return new WaitForSeconds(remaining);

        _isInvulnerable = false;

        ChangeState(BossState.Walk, "Phase transition finished");
    }

    public override void Die()
    {
        if (_state == BossState.Dead)
        {
            return;
        }

        Log("BOSS DEATH STARTED");

        ChangeState(BossState.Dead, "Die");

        StopAllCoroutines();
        StopMoving();

        SendTriggerToAnimator(deathParam, "DEATH");

        if (deathSfx != SfxId.None && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(deathSfx);
        }

        if (deathSequence != null)
        {
            deathSequence.Begin();
        }
        else
        {
            LogWarning("DeathSequence is NULL. Loading Victory directly.");
            SceneLoader.LoadVictory();
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        Log($"Boss HP changed: {current}/{max}");

        if (_isInvulnerable && Time.time < _invulnUntil)
        {
            Log("Boss is currently marked invulnerable.");
        }
    }

    private void HandleDied()
    {
        Log("Health.OnDied fired.");
        Die();
    }

    private float FacingSign()
    {
        if (player == null)
        {
            return spriteRenderer != null && spriteRenderer.flipX ? -1f : 1f;
        }

        float sign = Mathf.Sign(player.position.x - transform.position.x);

        if (Mathf.Approximately(sign, 0f))
        {
            return 1f;
        }

        return sign;
    }

    private void FacePlayer()
    {
        if (player == null || spriteRenderer == null)
        {
            return;
        }

        bool playerIsRight = player.position.x > transform.position.x;

        // If original sprite faces right, flip when player is left.
        // If original sprite faces left, flip when player is right.
        spriteRenderer.flipX = spriteFacesRightByDefault ? !playerIsRight : playerIsRight;
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        if (_spriteRenderer == null)
        {
            LogWarning("Cannot fade because SpriteRenderer is NULL.");
            yield break;
        }

        Color color = _spriteRenderer.color;

        if (duration <= 0f)
        {
            color.a = toAlpha;
            _spriteRenderer.color = color;
            yield break;
        }

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(t / duration));
            _spriteRenderer.color = color;
            yield return null;
        }

        color.a = toAlpha;
        _spriteRenderer.color = color;
    }

    public bool ConsumeIfInvulnerable()
    {
        return _isInvulnerable && Time.time < _invulnUntil;
    }

    private void SendTriggerToAnimator(string triggerName, string readableName)
    {
        if (animator == null)
        {
            LogError("Cannot send " + readableName + " trigger because Animator is NULL.");
            return;
        }

        if (!HasAnimatorParameter(animator, triggerName, AnimatorControllerParameterType.Trigger))
        {
            LogError($"Animator '{animator.gameObject.name}' does NOT have Trigger parameter '{triggerName}'.");
            return;
        }

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);

        if (debugAnimatorLogs)
        {
            Log($"BOSS {readableName} TRIGGER SENT TO ANIMATOR: {animator.gameObject.name}, parameter={triggerName}");
        }
    }

    private bool HasAnimatorParameter(
        Animator targetAnimator,
        string parameterName,
        AnimatorControllerParameterType type)
    {
        if (targetAnimator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == type)
            {
                return true;
            }
        }

        return false;
    }

    private void ChangeState(BossState newState, string reason)
    {
        BossState oldState = _state;
        _state = newState;

        if (oldState != newState)
        {
            Log($"STATE CHANGE: {oldState} → {newState}. Reason: {reason}");
        }
    }

    private void DebugValidateReferences(string context)
    {
        if (!debugLogs)
        {
            return;
        }

        Log($"--- Boss reference check: {context} ---");
        Log("Player: " + (player != null ? player.name : "NULL"));
        Log("SpriteRenderer: " + (spriteRenderer != null ? spriteRenderer.gameObject.name : "NULL"));
        Log("Animator: " + (animator != null ? animator.gameObject.name : "NULL"));
        Log("Rigidbody2D: " + (rb != null ? rb.gameObject.name : "NULL"));
        Log("Health: " + (health != null ? health.gameObject.name : "NULL"));
        Log("DeathSequence: " + (deathSequence != null ? deathSequence.gameObject.name : "NULL"));
        Log("PlayerMask value: " + playerMask.value);
        Log("TeleportAnchors count: " + (teleportAnchors != null ? teleportAnchors.Length : 0));

        if (animator != null)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                Log($"Animator param found: {parameter.name} / {parameter.type}");
            }
        }
    }

    private void Log(string message)
    {
        if (debugLogs)
        {
            Debug.Log("[BringerOfDeath] " + message, this);
        }
    }

    private void LogWarning(string message)
    {
        if (debugLogs)
        {
            Debug.LogWarning("[BringerOfDeath] " + message, this);
        }
    }

    private void LogError(string message)
    {
        Debug.LogError("[BringerOfDeath] " + message, this);
    }

    private void LogVerbose(string message)
    {
        if (debugLogs && false)
        {
            Debug.Log("[BringerOfDeath][Verbose] " + message, this);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.9f);

        float facing = Application.isPlaying ? FacingSign() : 1f;

        Vector2 origin = (Vector2)transform.position + new Vector2(
            facing * slashHitboxOffset.x,
            slashHitboxOffset.y
        );

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