# Final Showcase — RoninRun, Shipped

This is blog post #6, the last one. After ~14 days of solo development, *RoninRun* is complete: a four-level 2D pixel-art platformer with sword combat, three enemy types, a multi-phase boss, a full audio system, and a WebGL build live on GitHub Pages.

## The finished game

> [▶ Gameplay video (~2 min, YouTube)](https://youtu.be/REPLACE_WITH_FINAL_VIDEO_ID)
>
> [▶ Play in the browser (WebGL)](https://patrikklimko.github.io/GMD_Project/)

A 30-second elevator pitch: a wandering Ronin moves through three increasingly dangerous biomes — plains, woods, forest dusk — fighting slimes and the occasional dark wizard, then enters the boss arena to duel the **Bringer of Death**. Movement is tight (instant acceleration, dual jump, coyote time, jump buffer). Combat is a two-attack sword combo with hitstop knockback. The boss has a Phase 1 walk-slash-charge moveset, and a Phase 2 teleport-and-fan-cast moveset that triggers below 50% HP. Defeating him fades the screen to black and loads the Victory scene.

## What shipped vs. what was cut

| Promised in the GDD                       | Status              | Notes |
|-------------------------------------------|---------------------|-------|
| Player movement (dual jump, attack combo) | ✅ Shipped, polished | Coyote + buffer added late |
| Slime enemy                                | ✅ Shipped + polished | Spin-thrust attack, SFX, gravity fix |
| Wizard enemy                               | ✅ Shipped            | Patrol + cast purple bolt |
| Bringer of Death boss                      | ✅ Shipped            | Both phases, random teleport, fan cast |
| Three regular levels                       | ✅ Shipped            | All populated with enemies + LevelEnd |
| Boss HP bar + death sequence               | ✅ Shipped            | Fades into Victory scene |
| Audio system (BGM + SFX)                   | ✅ Shipped            | Enum-keyed library, pooled channels |
| Pause menu, main menu, lore intro          | ✅ Shipped            | Esc toggle, Time.timeScale freeze |
| Architecture refactor (M1 deferred)        | ⚠️ Partial           | EnemyConfigSO + bootstrappers landed; PlayerLocator + UnityEvent HUD hooks deferred to "if I shipped this commercially" |
| Warrior enemy                              | ❌ Cut intentionally  | Boss polish over enemy variety |
| Save system, lives, score, checkpoints    | ❌ Cut intentionally  | Out of scope per GDD |

The discipline of writing "out of scope" in the GDD up-front meant no scope creep — I never had to explain why something didn't make it; it was already on the cut list.

## Technical highlights

A few systems I'm particularly proud of and would build the same way again:

- **Bootstrapper pattern** — four static classes (`AudioBootstrapper`, `PlayerBootstrapper`, `HudBootstrapper`, `LevelEndBootstrapper`) that auto-spawn their respective prefabs from `Resources/` at scene load via `RuntimeInitializeOnLoadMethod`. Every scene "just works" whether you press Play from the main menu or from `04_BossLevel`. Born out of a real disaster (a batch edit corrupted L2 and L3), retained as a permanent improvement.
- **`SfxId` enum + `SfxLibrarySO`** — strongly-typed audio. `AudioManager.Instance.PlaySfx(SfxId.SlimeAttack)` instead of magic strings. Typos are compile errors; available sounds auto-complete.
- **Enum-FSM boss** — `BringerOfDeath` uses a plain `enum BossState` and a `ChangeState(newState, reason)` helper that logs every transition. The entire fight is debuggable from the console. No Animator-driven AI gymnastics.
- **Unified player ground detection** — `PlayerMovement2D.IsGrounded` defers to `GroundDetector2D` (collider-cast based) so the movement code and the animator can never disagree. Eliminates the "looks grounded but can't jump" bug class.
- **Self-building HUD** — `PlayerHealthUI` constructs its visuals procedurally in `Start()`. Drop the script on a canvas (or let the bootstrapper build the canvas too) and the bar appears. No per-scene UI wiring.

## What I would do differently

- **Commit to git from day one.** I'd kill for the rollback I gave up on the corrupted scenes. The bootstrapper pattern is now a permanent net but the lesson cost three scenes.
- **Build the bootstrapper layer first.** Knowing what I know now, the AudioManager and HUD would have been bootstrap-spawned from the first commit. It removes 100% of "I forgot to drag X into this scene" bugs.
- **Tune boss speed up earlier.** The boss `moveSpeed = 2.4` made him feel sluggish next to a player moving at 7. Phase 2 spends a lot of time waiting for the next teleport. I'd give him `moveSpeed = 4` and a shorter `teleportCooldown` from the start.

## What the course gave me

The GMD project is the first time I built a full game alone end-to-end — design, code, audio, UI, levels, build pipeline. The technical skills carry over (Unity, the Input System, ScriptableObjects, FSMs), but the more durable lesson is **scope discipline**. The GDD's explicit "out of scope" section, the per-milestone "what shipped / what slipped" framing, and the willingness to cut the Warrior enemy when the boss was at risk — those are the habits that turned a 14-day deadline into a finished game instead of an impressive demo.

Thanks for reading. The full source, blog series, and WebGL build are at [github.com/patrikklimko/GMD_Project](https://github.com/patrikklimko/GMD_Project).
