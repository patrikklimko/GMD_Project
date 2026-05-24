/// <summary>
/// Named identifiers for one-shot SFX. Using an enum instead of
/// string keys means typos turn into compile errors and the
/// available sounds are auto-completed by the IDE.
///
/// Add a new entry here and a matching row in SfxLibrarySO when a
/// new sound effect is introduced.
/// </summary>
public enum SfxId
{
    None = 0,

    // Combat
    PlayerSwordSwing,
    PlayerSwordHit,
    SwordSlash1,
SwordSlash2,
 SwordImpact,
  Jump,
EnemyDeathSlime,
    PlayerHurt,
    PlayerDeath,
    EnemyHit,
    EnemyDeath,
    WizardCast,

    // Movement
    PlayerJump,
    PlayerLand,

    // UI
    UiButtonClick,
    UiButtonHover,
    UiPause,
    UiUnpause,
    LevelEnd,

    // Game flow
    LevelComplete,
    Victory
}
