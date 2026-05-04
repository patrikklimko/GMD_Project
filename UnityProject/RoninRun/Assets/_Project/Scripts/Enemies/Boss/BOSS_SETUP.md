# Bringer of Death — Unity-side Setup

The C# scripts are done. This guide walks through the editor
work to wire the boss into `04_BossLevel.unity`.

---

## 1) Pick boss art

Same situation as the wizard. You need a sprite + animations:
**Idle**, **Walk**, **Slash**, optional **Cast** and **Death**.

Recommendations (CC-0 / free for commercial use):
- LuizMelo's **Death** or **Skeleton Lord** packs on itch.io — search "skeleton lord pixel art".
- OpenGameArt — search "demon" or "death pixel".
- Stand-in: re-tint the existing Martial Hero pack with a dark
  shader and ship it as a "shadow form" of the boss — fast and
  readable.

Drop the imported pack into `Assets/Bringer_Of_Death/` (or any
sensible folder). **Add an attribution row to the README's
third-party-assets table.**

---

## 2) Create the Boss config asset

Already done if you ran `RoninRun → Setup → Create All Enemy
Configs`. If not, run **RoninRun → Setup → Create Boss Config Asset**.
The asset lands at `Assets/_Project/ScriptableObjects/Enemies/Boss_Config.asset`.

Defaults are tuned for ~5-combo kill: HP 25, Move 2.4, Detection 14,
Slash range 2.0, Slash cooldown 1.6s, Wind-up 0.4s.

---

## 3) Run the boss-scene helper menu (one click)

In the menu bar: **RoninRun → Setup → Build Boss Scene Helpers
(HP bar + Fade + Anchors)**.

This creates:

- `Assets/_Project/Prefabs/UI/BossHealthBarCanvas.prefab` —
  top-of-screen HP bar with title, frame, and red fill image,
  hidden via CanvasGroup until the boss takes damage. Already wired
  to the `BossHealthBar` component.
- `Assets/_Project/Prefabs/UI/FadeOverlayCanvas.prefab` — full-screen
  black canvas (alpha 0 by default) used by `BossDeathSequence`
  for the fade-to-victory.
- `BossArena_Anchors` empty GameObject in the **currently open**
  scene with four `TeleportAnchor_*` children placed at default
  positions around (0, 0). You will drag these into the actual
  arena.

(Open the boss scene first so the anchors land in the right place.)

---

## 4) Assemble the Boss prefab

1. Drag your boss sprite into the open `04_BossLevel` scene.
2. Add components:
   - **Rigidbody 2D** — Dynamic, freeze rotation Z, gravity scale 3.
   - **Capsule Collider 2D** — sized to the sprite.
   - **Animator** — assign the boss's controller (parameters
     expected: `Speed` Float, `Slash` Trigger, `Cast` Trigger,
     `Death` Trigger; all optional but the boss looks dead without
     them).
   - **Health** — set Max HP to **25**.
   - **BringerOfDeath** — the script.
   - **BossDeathSequence** — the script.
3. Configure the **BringerOfDeath** inspector:
   - `Player`: leave empty — `EnemyBase.Start()` finds it by tag.
   - `Sprite Renderer`, `Rigidbody`, `Health`: auto-fill via
     `RequireComponent`.
   - `Detection Range`: 14.
   - `Move Speed`: 2.4. `Can Flip Sprite`: ON.
   - `Config`: drag `Boss_Config.asset`.
   - `Phase Two Threshold`: 0.5.
   - **Slash:** range 2.0, damage 3, wind-up 0.4, hit moment 0.2,
     cooldown 1.6, hitbox size (2.4, 1.6), **Player Mask**: select the
     **Player** layer.
   - **Charge:** min 4, max 6, damage 3, wind-up 0.6, duration 0.6,
     speed multiplier 2, cooldown 4.5.
   - **Phase 2:** drag the four `TeleportAnchor_*` transforms into
     the `Teleport Anchors[]` array. Fade out 0.2, fade in 0.2,
     teleport cooldown 2.0.
   - **Projectile Prefab**: drag your existing `WizardBolt.prefab`
     (the boss reuses it). Cast Point: empty child at hand.
     Projectile damage 2, speed 7. Cast wind-up 0.5, cast cooldown
     2.5. Fan angle 25.
   - **Phase Transition:** duration 1.5, invuln 1.0.
   - **Animator + parameter names**: leave defaults.
   - **SFX:** assign SfxId values you'll wire up in the SFX library
     (cast already defaults to `WizardCast`).
   - **Death Sequence:** drag the `BossDeathSequence` component on
     this same GameObject.
4. Configure the **BossDeathSequence** component:
   - `Death Hold Seconds`: 2.
   - `Fade Duration`: 1.
   - `Fade Overlay`: drag the **FadeOverlayCanvas → CanvasGroup**
     from the scene.
5. Drag the boss into `Assets/_Project/Prefabs/Enemies/` as
   `BringerOfDeath.prefab`.

---

## 5) Set up the arena trigger

1. **GameObject → Create Empty**, name it `BossArenaTrigger`.
2. Add **Box Collider 2D**, **Is Trigger** = ON. Size it to span the
   walkway leading into the arena (a strip the player can't avoid).
3. Add the **BossArenaTrigger** component.
4. Configure:
   - `Boss`: drag the `BringerOfDeath` GameObject from the scene.
   - `Intro Duration`: 1.5.
   - `Camera Follower`: drag the Main Camera (it has the
     `CameraFollow2D` component).
   - `Camera Intro Target`: an empty GameObject placed at the
     boss's mid-air position (camera will pan there during the
     intro). Optional — leave empty if you don't want the pan.
   - `Boss Bgm`: drag `Boss_BGM.ogg` (or whichever boss music
     you've imported).
   - `Intro Sfx`: optional roar SFX.
   - `Intro Title Group`: drag a CanvasGroup containing a
     "BRINGER OF DEATH" TMP text (you build this; not auto-built).

---

## 6) Drop in the HP bar + fade overlay

1. Drag `BossHealthBarCanvas.prefab` into the scene.
2. Select it. In the **BossHealthBar** component, drag the boss's
   **Health** component to **Boss Health**.
3. Drag `FadeOverlayCanvas.prefab` into the scene.
4. Make sure **CanvasGroup.alpha = 0** on both prefab instances (the
   prefabs ship that way; verify after drop).

---

## 7) Wire the SceneMusicBinder for the boss scene

If you haven't already (see `AUDIO_SETUP.md`):
1. Add a `SceneMusic` GameObject to `04_BossLevel`.
2. Attach **SceneMusicBinder**.
3. Assign your boss BGM clip.

If you'd rather only start the boss music when the player crosses
the trigger (not on scene load), leave the `SceneMusicBinder` BGM
clip empty and rely on `BossArenaTrigger`'s own `Boss Bgm` field.

---

## 8) Test checklist

Press Play from `04_BossLevel`:

- [ ] Walk into the trigger zone — title fades in, BGM crossfades.
- [ ] Boss begins walking toward you and slashes when in melee.
- [ ] At medium range the boss occasionally charges.
- [ ] At ~50% HP a phase-transition flash plays, then the boss
      starts teleporting between the four anchors and casting
      3-bolt fans.
- [ ] If you crowd the boss in melee during phase 2, it slashes.
- [ ] Boss HP bar fades in on first damage; depletes smoothly.
- [ ] On boss death, hold ~2 s, fade-to-black, Victory scene loads.

If teleport positions look bad: re-position `TeleportAnchor_1..4` in
the scene. If projectiles miss the player constantly: lower
`fanAngleDegrees` to ~15. If the fight feels too easy: reduce
`slashCooldown` to 1.2 and `castCooldown` to 1.8.
