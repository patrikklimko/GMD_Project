# RoninRun — Game Design Document & Milestones

This is blog post #2 in the GMD course series. It defines what RoninRun is supposed to be, what is intentionally cut, and the three milestones the rest of development will be graded against.

## Vision

**RoninRun is a four-level 2D action platformer where a wandering Ronin sword-fights through cursed lands, ending in a duel with the Bringer of Death.** The pitch is "GBA-era Castlevania pacing meets pixel-art Souls aggression": short levels, reactive enemies, precise jumps, and a boss with more behaviour than the regular cast.

## Core gameplay loop

The player **moves and jumps** through a horizontally scrolling pixel-art level (instant acceleration, dual jump, no air drift), **engages enemies** with a two-attack sword combo (hits register on the active animation frame via an `OverlapBox` hitbox), **survives damage** with a 5-HP pool plus invincibility frames, **reaches the level-end trigger** to advance, and finally **defeats the boss** to roll credits. The loop is intentionally simple: I would rather ship a tight 10-minute experience than a sloppy 30-minute one.

## Cast and content

Player movement, dual jump, two-attack combo, HP, and death are working today. A Slime enemy with a patrol → detect → chase → dash-attack state machine is wired up with an HP bar. Three normal levels exist as scenes (one populated, two thin) and the boss arena is empty. The audio folder is empty. Pause menu and game-feel polish (camera shake, hit pause, damage flash) are not yet implemented.

The Wizard ranged enemy and the Bringer of Death boss are the two new combat units to build. The Warrior enemy planned in early notes is **cut** — better to ship the boss with polish than four mediocre enemies in a 14-day solo window.

## Architecture overview

Today the codebase already follows SOLID enough that examiners can point at concrete examples: an `IDamageable` interface, a generic `Health` component, an abstract `EnemyBase` with virtual hooks. It also has technical debt I'm explicit about: `PlayerHealth` duplicates `Health.cs`, enemies use `GameObject.Find` to locate the player, and UI references its targets directly. Milestone 1 pays this debt down — `PlayerHealth` will *contain* a `Health`, an `EnemyConfigSO` ScriptableObject will hold per-enemy tunables, `Health` will fire `UnityEvent` hooks for the HUD, and a `PlayerLocator` will replace the tag lookups. The boss in M3 will run on a plain enum-driven state machine — no Animator-driven AI logic — because it is easier to debug and easier to write a blog post about.

## Three milestones

**Milestone 1 — Architecture refactor + Wizard enemy** (target ~3–4 days). Refactor `PlayerHealth` to compose `Health`, add `UnityEvent` hooks, cache animator hashes, introduce `PlayerLocator`, convert Player and Slime into prefabs, build the Wizard enemy (patrol → cast purple projectile → cooldown), and add the `EnemyConfigSO` ScriptableObject. **Deliverable:** Level 2 populated with wizards plus a clean architecture diff. Reported in blog post #3.

**Milestone 2 — Audio + game-feel polish + pause menu** (~2–3 days after M1). `AudioManager` singleton with crossfade and a pooled SFX channel; 4 BGM tracks and 8 SFX from CC-0 sources; camera shake, hit pause, damage flash, hit-spark particles; pause menu (Esc) with Resume / Restart / Quit and a Controls panel. **Deliverable:** the existing content *feels* dramatically better. Reported in blog post #4.

**Milestone 3 — Bringer of Death boss + final integration** (~3 days after M2). `BringerOfDeath : EnemyBase` with an FSM (Idle, Walk, Slash, Teleport, CastBarrage, Dead). Phase 1 above 50% HP is walk-and-slash with the occasional charge; Phase 2 below 50% is teleport-and-fan-cast. Boss HP bar at the top of the screen, camera locked to the arena, death sequence into the existing victory scene. **Deliverable:** the game is completable start-to-finish. Reported in blog post #5.

A showcase blog post (#6), the ~2-minute gameplay video, and the public WebGL build land after M3.

## Out of scope (explicit cuts)

Warrior enemy, three-phase rage-mode boss, save system, lives, score, checkpoints, multiplayer, difficulty settings. Recording these here so I cannot quietly drop them later without the examiner noticing.

## Risks

The most likely failure mode is the boss running long. Mitigation: ship Phase 1 by day two of M3 so something is always playable, then build Phase 2 on top. WebGL trip-ups are mitigated by smoke-testing builds *before* M1 starts. Audio licensing risk is mitigated by CC-0-only sources (Kenney, Freesound CC-0 filter) recorded in the README at the moment each track is added. Refactor risk is mitigated by branching per step and keeping Level 1 playable at all times as a fallback demo.

## Definition of done

The submission is finished only when there is a public GitHub repo with README and 6 blog posts, a WebGL build live on GitHub Pages and confirmed playable in a browser, a ~2-minute YouTube video linked from the README, a PDF on WISEflow with name, student number and links, a source-code zip excluding `Library/` `Temp/` `Logs/` `Build/`, and a build under the 500 MB cap. Anything else is gravy.

That is the contract for the next 14 days. The Milestone 1 post reports against the targets above.
