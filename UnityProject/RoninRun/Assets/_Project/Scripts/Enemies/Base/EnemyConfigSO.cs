using UnityEngine;

/// <summary>
/// Designer-friendly tuning data for an enemy. Each enemy variant
/// (slime, wizard, boss) can have its own EnemyConfig asset so HP,
/// damage, speed, ranges and cooldowns live in version-controllable
/// .asset files instead of being hard-coded in scripts.
///
/// Pulled in deliberately as a SOLID demo: EnemyBase / WizardEnemy
/// stay closed for modification but open for extension via new
/// config assets. Adding a new enemy variant is "duplicate the
/// asset and tweak fields" rather than "duplicate the script and
/// tweak constants".
/// </summary>
[CreateAssetMenu(
    fileName = "EnemyConfig",
    menuName = "RoninRun/Enemy Config",
    order = 0)]
public class EnemyConfigSO : ScriptableObject
{
    [Header("Health")]
    [Min(1)] public int maxHp = 3;

    [Header("Damage")]
    [Min(0)] public int contactDamage = 1;
    [Min(0)] public int rangedDamage = 1;

    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 2f;

    [Header("Detection")]
    [Min(0f)] public float detectionRange = 6f;
    [Min(0f)] public float attackRange = 1.2f;

    [Header("Attack timing")]
    [Min(0f)] public float attackCooldown = 2f;
    [Tooltip("Wind-up time before the actual attack lands (sec).")]
    [Min(0f)] public float attackWindUp = 0.5f;
}
