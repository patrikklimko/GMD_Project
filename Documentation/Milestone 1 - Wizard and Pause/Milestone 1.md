# Milestone 1 — Wizard Enemy, EnemyConfigSO, and a Pause Menu

This is blog post #3, reporting against the Milestone 1 contract from [blog post #2](../GDD%20and%20Milestones/GDD%20and%20Milestones.md). I'll start with what shipped, then admit what slipped, then walk through the two design choices I think are worth defending.

## What shipped

- **Wizard enemy** — a ranged caster that patrols between two waypoints, detects the player, stops to face them, winds up for ~0.6 s, fires a purple projectile, and goes on cooldown. Implemented as `WizardEnemy : EnemyBase` so it inherits the existing detection, facing, and HP scaffolding.
- **Projectile** — reusable trigger-based 2D bolt with layer-masked damage, a separate "block" mask for terrain, and a lifetime timeout. The boss in Milestone 3 will reuse the same prefab for its fan-cast.
- **EnemyConfigSO** — a `ScriptableObject` for designer-tunable enemy stats (HP, damage, speed, ranges, cooldowns, wind-up). Each enemy variant gets its own `.asset` so balancing is "duplicate the asset, tweak fields" rather than "duplicate the script, tweak constants".
- **Pause Menu + SceneLoader** — Esc toggle, `Time.timeScale` freeze, Resume / Restart / Controls / Main Menu buttons, plus a `SceneLoader` static helper that centralises scene transitions and always restores `timeScale = 1` before loading. The pause menu was originally a Milestone 2 task but the cost of dropping it in alongside the Wizard was minimal.

What landed in this milestone is in commits `feat(enemies): Wizard ranged enemy + Projectile + EnemyConfigSO` and `feat(ui): pause menu + central SceneLoader` on `main`.

## What slipped

The GDD listed an **architecture refactor** as a Milestone 1 deliverable (compose `PlayerHealth` from `Health`, replace direct UI references with `UnityEvent` hooks, cache animator parameter hashes, introduce a `PlayerLocator` service). That work is **deferred**.

The honest reason: with 14 days, solo, and the Wizard + boss + audio + polish all still ahead, I traded refactor cleanliness for content velocity. The existing code already follows enough SOLID to defend in a reflection — the deferral mostly costs me a couple of awkward bullets in the final reflection PDF rather than breaking anything. I'll either fold the refactor into Milestone 2 if there's slack after audio + polish, or land it as an explicit cleanup commit in the final week and call it out in blog post #6.

## Design choices worth defending

### EnemyConfigSO — composition over hard-coded numbers

`WizardEnemy` exposes a `[SerializeField] EnemyConfigSO config`. If the field is empty, the enemy uses the per-instance values on the script. If it's filled, those values override the inspector ones on `Awake`. This is a soft form of dependency injection: enemy AI logic stays in the script, tuning data lives in version-controlled `.asset` files.

The pay-off shows up the moment I add a second enemy variant. A "Frost Wizard" with a slower projectile and longer cooldown is a duplicated asset, not a duplicated class. The boss in Milestone 3 will read its tunables the same way. It also makes the eventual difficulty pass trivial — swap a config asset, ship a different balance.

### Optional animator parameters

`WizardEnemy` references its animator parameters by string (`"Speed"`, `"Cast"`) but only sets them if the animator reference is non-null. This sounds tiny but it lets me smoke-test the AI before I have wizard art in the project — drop a placeholder sprite cube into the scene, give it a `Health`, attach `WizardEnemy`, and the patrol-detect-cast loop runs even with no animations wired up. The real animator gets bolted on later without code changes.

The trade-off is that I'm still using string parameter names, which is fragile and slow. Caching them as hashed `int`s is on the refactor list above — if it lands, the change is purely additive and still transparent to the optional-animator design.

## What I learned

The most useful debugging trick this milestone was wiring the Wizard's gizmos hard. `OnDrawGizmosSelected` draws the detection range (yellow), attack range (red), and the projectile spawn point and launch ray (magenta). When the wizard "doesn't fire", the gizmo immediately tells you whether it's a detection problem, an attack-range problem, or a cast-point placement problem. Five minutes of gizmo code saves an hour of `Debug.Log` later.

The other lesson: writing a setup `.md` next to a script (`WIZARD_SETUP.md`, `PAUSE_SETUP.md`) is high leverage. It forced me to spell out the editor-side wiring exactly, which exposed two cases where the script needed an inspector default I'd forgotten.

Next post is Milestone 2 — audio system, screen shake, hit pause, damage flash, particles. The pause menu is already in, so M2 is mostly polish on existing combat plus sound.
