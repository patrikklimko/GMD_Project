# Milestone 3 — Building the Ronin

This is blog post #5, the last development blog before the final showcase. With the audio system (Milestone 1) and the boss + bootstrapper pattern (Milestone 2) already shipped, this milestone was about the one piece every other system orbits around: the **player character**.

The Ronin had to feel *tight*. He's on screen every second of every play-through, so any sluggishness, animation rough edge, or "I pressed jump and nothing happened" moment compounds into the dominant impression of the game. This post walks through the four systems that make him work — sprite + Animator, movement, combat, and health/death — and the design choices that I would defend to an examiner.

## The sprite and the Animator FSM

The Ronin's visuals come from Craftpix's free **Warrior** pixel-art sprite sheet — clean 12-frame run cycle, two distinct attack swings, a death animation, plus idle/jump/fall. The Animator Controller (`Player.controller`) splits those into seven states arranged in a flat FSM:

```
Idle ──Speed>0.1──> run ──Speed<0.1──> Idle
Idle ──IsGrounded=false & YVel>0.1──> jump
Idle ──IsGrounded=false & YVel<-0.1──> fall
Any State ──Attack trigger──> attack1 ──ComboQueued──> attack2
Any State ──Die trigger──> Player_Death
```

Two design decisions worth calling out:

- **Conditions are on parameters, not on `Has Exit Time`.** Every transition uses `IsGrounded`, `Speed`, `YVelocity` or a trigger. None of them rely on the animation finishing first. That means the player can interrupt a run with a jump, a jump with an attack, and an attack with another attack — all instantly. The animator never "owes" the player a frame they didn't ask for.
- **`Any State → Death` is the safety net.** No matter what state the player is in when their HP hits zero, the death animation fires. I don't have to write transitions out of every other state, and I don't have to remember which state I was in.

## `PlayerMovement2D` — movement, jump, and feel

`PlayerMovement2D` is driven entirely by Unity's new **Input System**. Move and Jump are exposed as `InputActionReference` fields so the keyboard, gamepad, and the VIA arcade joystick all bind through the same `.inputactions` asset:

```csharp
[SerializeField] private InputActionReference moveAction;
[SerializeField] private InputActionReference jumpAction;
```

Movement itself is velocity-based on the X axis with gravity preserved on Y:

```csharp
_rb.linearVelocity = new Vector2(MoveX * moveSpeed, _rb.linearVelocity.y);
```

Jump uses `AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse)` with a `_jumpsRemaining` counter that allows a second mid-air jump at `secondJumpMultiplier * jumpForce`. The second jump is intentionally weaker so the dual-jump feels like a recovery, not a flight.

The two pieces of polish I'm proudest of are also the tiniest:

- **Coyote time (100 ms).** A `_lastGroundedTime` field records the last frame we were on the ground. The jump check is `(grounded || inCoyote)` instead of just `grounded`, so a tap-jump fired the very instant after running off a ledge still counts.
- **Jump buffer (100 ms).** A `_lastJumpPressedTime` field records when Jump was pressed. The check fires if the press is recent and the player is now grounded — so a press 50 ms *before* landing still triggers a jump the moment the feet touch.

Both windows are 5-line additions. Without them, the controller feels like it's "fighting" the player; with them, it disappears.

A final defensive touch: ground detection is **unified** between movement and animator. `PlayerMovement2D.IsGrounded()` defers to the `GroundDetector2D` component (a `Collider.Cast` down by 0.08 units) if one exists, falling back to a `Physics2D.OverlapCircle` otherwise. The animator reads the same `GroundDetector2D`. That eliminates the "looks grounded but can't jump" bug class.

## `PlayerCombat` — combo system on animation events

The two-attack sword combo is driven from **animation events** rather than from code timers. The Animator clips for `attack1` and `attack2` each fire three named events: `PlayAttack1SlashSound`, `DoAttack1Hit`, then the animation ends and (if `ComboQueued` is set) chains into `attack2` which fires its own pair. `PlayerCombat.cs` only owns the orchestration coroutine — the actual *moments* when sounds play and damage lands are baked into the animation timeline, frame-perfect:

```csharp
public void DoAttack1Hit()
{
    if (_attack1HitAlreadyProcessed) return;
    _attack1HitAlreadyProcessed = true;
    DoAttackHitInternal();
}
```

`DoAttackHitInternal` snaps the `attackPoint` local position to match the Ronin's facing (`_move.FacingDir`), then runs a `Physics2D.OverlapBoxAll` against `enemyLayer`. Every enemy with a `Health` component takes damage; every enemy with a `Rigidbody2D` gets knockback proportional to the contact direction. If at least one enemy was hit, the meaty `SwordImpact` SFX plays — silence on a whiff, satisfying *thunk* on a connect.

`ComboQueued` is set when the player presses Attack during a small **queue window** at the start of `attack1`. Pressing too early or too late just plays a single swing. This is what gives the combat its rhythm — the player has to feel the beat of the swing to chain it.

## `PlayerHealth` + `PlayerDeathController` — surviving and dying

`PlayerHealth` is intentionally separate from the generic `Health.cs` used by enemies. Player damage is more involved: knockback as a `Vector2` impulse, movement lock for `movementLockTime` seconds (~0.15 s) so the player can't immediately push back into the slime, and an invincibility window (`invincibleTime` ~1 s) that ignores all subsequent damage. When HP hits zero, control hands off to `PlayerDeathController` which plays the death animation, freezes input, and routes to the Game Over flow.

The `PlayerBootstrapper` (from Milestone 2) writes `maxHp` via reflection per-scene: 10 for the regular levels, **25** for the boss arena. This was easier than maintaining a separate "boss Ronin" prefab.

## What I learned

Three things, in order of how often I now reach for them in *every* Unity project:

1. **Animation events beat polling.** Damage frames, sound cues, particle spawns — bake them into the clip via named events. The script becomes a thin orchestrator instead of a guess-the-timing mess.
2. **Coyote time + jump buffer are free.** Ten lines of code, dramatic feel improvement, zero downside. Add them to the controller before you ship anything.
3. **Unify your ground detection.** The animator and the movement script *must* agree on whether the player is on the ground. If they don't, you will spend an hour on a "why won't he jump?" bug that has a one-line fix.

Next and final blog post is the showcase + retrospective.
