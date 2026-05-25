# RoninRun — Submission PDF

**Author:** Patrik Klimko
**Student number:** `<INSERT YOUR VIA STUDENT NUMBER>`
**GitHub username:** `@patrikklimko`
**Repository:** https://github.com/patrikklimko/GMD_Project
**Live WebGL build:** https://patrikklimko.github.io/GMD_Project/
**Gameplay video (~2 min):** `<INSERT YOUTUBE LINK>`

This is a solo project. All commits in the repository represent my own work.

---

# Personal Reflection

## What I built

RoninRun is a 2D pixel-art action platformer in Unity 6 (URP 2D). The player controls a wandering Ronin who moves through three combat levels populated by slime enemies and then enters a boss arena to fight the Bringer of Death. The full game flow is: main menu → lore intro → Level 1 → Level 2 → Level 3 → Boss Level → Victory.

On the gameplay side, the player has a velocity-driven movement controller with a dual jump (`PlayerMovement2D`), a two-attack sword combo wired through Animator animation events (`PlayerCombat`), an invincibility-window health system (`PlayerHealth`), and a death sequence that hands off to the death controller and reloads the level. Slimes patrol between waypoints, chase on detection, and execute a spin-and-thrust attack that deals damage plus knockback via a `Physics2D.OverlapBoxAll` hitbox (`SlimeEnemy` + `SlimeAttack`). The boss runs a plain-enum FSM (`BringerOfDeath`) with two phases — Phase 1 is walk-and-slash with the occasional charge, Phase 2 (below 50% HP) is teleport-to-a-random-anchor plus a three-bolt fan-cast — and ends in a fade-to-black sequence that loads the Victory scene.

On the systems side, I built an `AudioManager` singleton with a pooled SFX channel and crossfading BGM, backed by an `SfxLibrarySO` ScriptableObject and a strongly-typed `SfxId` enum so audio calls are typo-proof. I also built a full HUD that constructs itself procedurally (`PlayerHealthUI`) — drop the script on a canvas and a color-graded red→amber→green health bar appears.

## The technical challenge I'm proudest of solving

About halfway through development I ran a Python regex script over the scene files to batch-fix some enemy properties, and it corrupted Level 2, Level 3, and parts of Level 4. The Player GameObject got deleted from the middle of L2 and L3, and the scenes were truncated mid-file. There were no git auto-snapshots big enough to recover the full scenes, and I almost panicked because the project was three days from submission.

The fix I'm proud of isn't restoring the corrupted scenes — it's the *bootstrapper pattern* that came out of it. I extracted the Player hierarchy from the one untouched scene (`00_TestGround`) into `Resources/Player.prefab`, then wrote a static class `PlayerBootstrapper` that hooks `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` and `SceneManager.sceneLoaded`. On every scene load it checks whether a Player exists; if not, it instantiates the prefab, wires the scene's `CameraFollow2D` to it, and applies a per-scene HP override (10 for L1–L3, 25 for the boss) via reflection on `PlayerHealth.maxHp`. I did the same thing for the AudioManager, the HUD canvas, and the LevelEnd trigger — four small `Bootstrapper` classes that mean the game *just works* when you press Play from any scene.

What started as panic-driven recovery became the most robust thing in the codebase. I would build my next Unity project bootstrapper-first.

## Scope discipline

My GDD originally listed a Wizard ranged enemy as part of Milestone 1 and a three-phase rage-mode boss as a stretch goal. By the end of Milestone 2 I could see I was either going to ship the Wizard or polish the boss, not both. I cut the Wizard — explicitly, in the "out of scope" section of the GDD — and shipped a much stronger boss instead. The willingness to write things on a cut list and stick to it is the habit I'll take from this project more than any specific Unity technique.

I also deferred the Milestone 1 architecture refactor (composing `PlayerHealth` out of `Health`, replacing `GameObject.Find` with a `PlayerLocator`, hashing animator parameters). I wrote up the deferral honestly in the Milestone 1 blog post — it's the kind of thing that's easy to hide and harder to admit, but the examiner can see the same trade-off in the commit history anyway, so being upfront about it felt like the right call.

## What I would do differently

Three things I'd change if I started over tomorrow:

1. **Commit to git from day one with clean atomic commits.** I had git from the start but my commits were too coarse — when the corruption hit, even the commits I did have weren't granular enough to rollback selectively. Smaller, more frequent commits are cheap insurance.
2. **Build the bootstrapper layer first.** Knowing what I know now, the AudioManager, HUD, and Player would have been bootstrap-spawned from the first commit. It removes 100% of the "I forgot to drag X into this scene" bugs that plague every Unity project. The pattern emerged from a disaster but it's good practice on its own.
3. **Tune boss aggression up earlier.** My boss spends a lot of time waiting between teleports because the cooldown was too long. I tweaked it down in the final week, but a more aggressive boss earlier would have made playtesting reveal balance issues sooner.

## What the course gave me

Before this project I had written individual Unity scripts but never built a full game end-to-end alone — design, code, audio, UI, levels, build pipeline. The technical skills carry over (Unity, the new Input System, ScriptableObjects, FSMs, animation events, the Resources pattern), but the more durable lesson is *scope discipline*. The GDD's explicit "out of scope" section, the per-milestone "what shipped / what slipped" framing in the blog posts, and the willingness to cut features when they threaten the whole — those are the habits that turned a 14-day deadline into a finished game instead of an impressive demo with broken edges.

Submitting feels like the right kind of tired. Thanks for reading.
