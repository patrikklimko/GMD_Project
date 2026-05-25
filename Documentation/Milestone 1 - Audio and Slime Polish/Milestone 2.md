# Milestone 2 — Audio Architecture and Slime Combat Polish

This is blog post #4, reporting against the Milestone 2 contract from [blog post #2](../GDD%20and%20Milestones/GDD%20and%20Milestones.md). The original brief was "audio + game-feel polish + pause menu". The pause menu landed in M1, so this milestone focused on **building the audio system from scratch** and **polishing the Slime enemy into a real combat encounter**.

## What shipped

- **AudioManager singleton** — `DontDestroyOnLoad` source-of-truth for all sound. Owns two pooled BGM `AudioSource`s with crossfade, plus a six-channel SFX pool that recycles round-robin so overlapping hits never starve.
- **SfxLibrarySO + SfxId enum** — a `ScriptableObject` mapping a strongly-typed `SfxId` enum to `AudioClip` references. Gameplay code calls `AudioManager.Instance.PlaySfx(SfxId.SlimeAttack)` — typos become compile errors, the available sounds auto-complete in IntelliJ, and the library is fully Inspector-editable. Currently registers 10 clips: sword swings, sword impact, jump, slime attack/death, player hurt/death, boss attack/death, level-end sting.
- **SceneMusicBinder** — drops on any scene to declare its BGM track. On scene load it asks the `AudioManager` to crossfade into the new clip. Each level scene gets a different mood without any per-scene code.
- **Slime combat overhaul** — the placeholder patrol-and-touch slime became a real enemy: it patrols between waypoints, chases on detection, spins-and-thrusts on attack (one fluid animation triggered by `IsAttacking`), deals damage + knockback via an `OverlapBox` at the attack point, takes damage from the player's sword, and plays its tornado SFX every time it lunges.
- **Three populated levels** — Level 1 (plains), Level 2 (woods), Level 3 (forest dusk) are now actually populated with slimes, hazards, and a `LevelEndTrigger` that chains them together. The level-end trigger plays the `LevelEnd` sting and loads the next scene via `SceneLoader`.

Commits on `main`: `feat(audio): AudioManager + SfxLibrarySO + SceneMusicBinder`, `feat(enemies): slime polish - spin+thrust, sfx, gravity, sorting`, `feat(level): LevelEndTrigger chains scenes via SfxId.LevelEnd`.

## Design choices worth defending

### Why a ScriptableObject library instead of `Resources.Load` per-clip

The naive way is to store each clip as a path string and call `Resources.Load<AudioClip>("Audio/Sfx/" + name)` at the call site. That works, but it scatters magic strings, hides which clips actually exist, and makes "rename a clip" a search-and-replace bug hunt.

The `SfxLibrarySO` flips that. The **enum** is the contract — gameplay code only sees `SfxId.SlimeAttack`. The **library asset** is the implementation — the artist can swap which file plays that ID without touching code. When the slime got its tornado sound late in M2, it was three changes: add an enum entry, drop the mp3 in `SfxLibrary.asset`, and pass the ID to `PlaySfx` in one line of `SlimeEnemy`. No refactor, no missed references.

```csharp
[SerializeField] private SfxId attackSfx = SfxId.SlimeAttack;
[Range(0f, 1f)] [SerializeField] private float attackSfxVolume = 1f;

// inside AttackRoutine()
if (attackSfx != SfxId.None && AudioManager.Instance != null)
{
    AudioManager.Instance.PlaySfx(attackSfx, attackSfxVolume);
}
```

The `SfxId.None` sentinel and the null check on `AudioManager.Instance` mean the slime never throws even if audio is somehow not set up — silence is a recoverable failure.

### Why the slime attack uses an `OverlapBox`, not a child trigger collider

The first prototype gave the slime a child `AttackHitbox` with a `BoxCollider2D` that flipped on for the active animation frame. It worked in Level 1 and broke in every other scene because the inspector reference to the child collider was inconsistent and easy to forget. The replacement is a one-line `Physics2D.OverlapBoxAll` call inside `SlimeAttack.DoAttackHit()`:

```csharp
Vector2 origin = (Vector2)transform.position +
                 new Vector2(_slimeEnemy.FacingDirection * (attackBoxSize.x * 0.5f), 0f);
Collider2D[] hits = Physics2D.OverlapBoxAll(origin, attackBoxSize, 0f, playerLayer);
```

The hit origin is computed from the slime's transform + facing direction, so there is **no inspector wiring to forget**. The same call works in every scene the slime is dropped into. The lesson is that "self-contained per-Awake" beats "designer wires it up" whenever the wiring isn't actually a design decision.

### Velocity-based movement instead of `MovePosition`

The placeholder slime drove movement with `rb.MovePosition(new Vector2(targetX, rb.position.y))`. That pins the Y coordinate every fixed step, which cancels gravity — slimes that walked off ledges **levitated horizontally**. The fix is to write `rb.linearVelocity.x` directly and leave `linearVelocity.y` untouched so gravity owns the vertical axis:

```csharp
rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
```

Same trick now used everywhere a 2D enemy chases the player.

## What I learned

The biggest M2 lesson was **bake the contract into the type system**. The `SfxId` enum prevents an entire class of "I renamed the file in Audio/ and now half the project is silent" bugs. The same pattern will show up in M3 when the Bringer of Death exposes serialized `slashSfx`, `chargeSfx`, `castSfx`, `deathSfx` fields — none of those are strings, all of them auto-complete, all typos are caught at compile time.

The other lesson is **boring polish wins**. The slime didn't get smarter — it patrols, chases, lunges, dies. Same as in M1. But adding the spin-and-thrust sync, the tornado SFX, and the gravity fix made it feel like a real enemy instead of a moving sprite. Two days of polish on one enemy paid off more than two days of building a half-broken new one would have.

Next post is Milestone 3 — the Bringer of Death boss, the bootstrapper pattern that saved me when I corrupted three scenes, and the player-feel finals (coyote time, jump buffer, unified ground detection).
