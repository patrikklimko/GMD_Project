# Milestone 3 — Bringer of Death, Bootstrappers, and Player-Feel Polish

This is blog post #5, the last development blog before the final showcase. The brief was "Bringer of Death boss + final integration". It landed, plus more — a **scene-resilience pattern born out of a real disaster**, and the **last-mile player-feel work** that turns a working prototype into something that feels good to hold.

## What shipped

- **Bringer of Death boss** — a `BringerOfDeath : EnemyBase` running a plain-enum FSM (`Intro`, `Walk`, `WindUpSlash`, `Slash`, `WindUpCharge`, `Charge`, `PhaseTransition`, `Teleport`, `Cast`, `Recover`, `Dead`). Phase 1 above 50% HP is walk-and-slash with the occasional charge across mid-range. Phase 2 below 50% HP is teleport-and-fan-cast — the boss vanishes, reappears at one of four randomly-picked anchor transforms, and releases a three-bolt projectile fan at the player.
- **Boss arena scaffolding** — `BossArenaTrigger` to start the fight when the player crosses a threshold, a top-of-screen `BossHealthBar` with the boss's name and a red fill, and a `BossDeathSequence` that fades the screen to black and loads the Victory scene.
- **Player HUD bar** — `PlayerHealthUI` self-builds a top-left health bar at runtime: dark frame, color-graded fill (red→amber→green via a `Gradient`), centered "X / Y" label, smooth lerp toward the target fill.
- **Bootstrapper pattern** — four `RuntimeInitializeOnLoadMethod` + `SceneManager.sceneLoaded` static classes (`AudioBootstrapper`, `PlayerBootstrapper`, `HudBootstrapper`, `LevelEndBootstrapper`) that auto-spawn their respective prefabs in every gameplay scene that doesn't already have them. **A scene can be opened directly in Play mode and the game just works.**
- **Player-feel finals** — coyote time (10 frames), jump buffer (10 frames), unified ground detection (`PlayerMovement2D` defers to `GroundDetector2D` so the movement script and animator can never disagree), HP scaling per scene (10 HP in L1–L3, 25 HP in the boss arena).

## The boss FSM, in one paragraph

`TickBehaviour()` ticks every Update. If the boss is `Walk`, it calls `ChooseNextAttack()` which is a flat decision tree: in Phase 1 it slashes when close, charges at 4–6 m, walks otherwise; in Phase 2 it slashes if the player crowds it, teleports if it's far and the teleport timer is ready. Routines are coroutines that change state, play the wind-up animation trigger (`Slash`, `Spell`, etc.), wait, do the hit / spawn the projectile, change state back. The reason it's an enum FSM and not an Animator-driven controller is **debuggability**: one `Log()` call inside `ChangeState` prints `[BossState] Walk -> WindUpSlash (reason: SlashRoutine)` and the entire fight history is in the console.

## The disaster, and the pattern that came out of it

Halfway through M3 a batch regex script I was running over the scene files **corrupted L2 and L3** — the Player GameObject was deleted from the middle of both scenes. No git rollback, no auto-save snapshots that big. The clean fix was to **stop relying on scene state for things that should always exist**.

The pattern: for every "always-needed" GameObject (the Player, the AudioManager, the HUD canvas, the LevelEnd trigger), I:

1. Extract its full YAML from a known-good source scene and save as `Resources/X.prefab`.
2. Write a tiny static class that hooks `RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)` and `SceneManager.sceneLoaded`.
3. On scene load, check whether the thing exists; if not, `Resources.Load<GameObject>("X")` and `Instantiate` it into the active scene.

The result is a game that **boots cleanly from any scene**. The recovery happened to be the trigger, but the pattern is good practice on its own — it removes the entire class of "I forgot to drag the X into this scene" bugs that plague Unity projects.

```csharp
public static class PlayerBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureOnInitialScene()
    {
        EnsurePlayer(SceneManager.GetActiveScene());
    }

    private static void EnsurePlayer(Scene scene)
    {
        if (GameObject.FindGameObjectWithTag("Player") != null) return;
        GameObject prefab = Resources.Load<GameObject>("Player");
        if (prefab == null) return;
        var instance = Object.Instantiate(prefab);
        SceneManager.MoveGameObjectToScene(instance, scene);
        // … wire camera follow, apply scene-specific HP, etc.
    }
}
```

The PlayerBootstrapper additionally **rewires the scene's `CameraFollow2D` target** and **applies a per-scene HP override** (10 for L1–L3, 25 for the boss) via reflection on `PlayerHealth.maxHp`. The HudBootstrapper unconditionally destroys any pre-existing `PlayerHealthUI` and builds a fresh self-contained canvas — scene-baked instances can get orphaned from their canvas after edits, and starting clean is cheaper than diagnosing every variant.

## Player-feel finals

The "stuck to the floor / can't jump sometimes" complaint from M2 turned out to be **two ground detectors disagreeing**: `PlayerMovement2D.IsGrounded` used an `OverlapCircle` at a child Transform, while `PlayerAnimatorController` read `GroundDetector2D.IsGrounded` (a `Collider.Cast` straight down). Between tiles, the OverlapCircle dropped through the gap and `_jumpsRemaining` never reset. The fix is one line:

```csharp
private bool IsGrounded()
{
    if (_groundDetector != null && _groundDetector.IsGrounded) return true;
    if (groundCheck == null) return false;
    return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
}
```

Then I added the two staples every platformer needs:

- **Coyote time (100 ms)**: a tiny window after leaving the ground where the jump button still triggers a ground-jump.
- **Jump buffer (100 ms)**: a tiny window before landing where a pressed jump button "remembers itself" and fires the moment your feet touch.

Both are 5 lines of code and they completely change how the controller feels to hold.

## What I learned

Three things, ranked by how painfully I learned them:

1. **Don't run batch scripts over scene files.** Especially on a project without git committed in good standing. Recovery is *much* more expensive than per-file targeted edits.
2. **Bootstrappers are not a hack.** They're a discipline that makes scenes lighter and the game more robust. I would build the next Unity project bootstrapper-first.
3. **Player feel is built out of 50 ms windows.** Coyote and buffer don't add capability; they hide tiny imperfections in timing that the player would otherwise blame on the game.

Next and final blog post is the showcase + retrospective.
