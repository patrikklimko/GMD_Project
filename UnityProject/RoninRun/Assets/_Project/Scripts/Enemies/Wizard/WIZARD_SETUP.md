# Wizard Enemy — Unity-side Setup

The C# scripts are done. The remaining work is editor-only: build the
config asset, the projectile prefab, the wizard prefab, and place
instances in `02_Level2.unity`. Step-by-step below.

---

## 1) Pick wizard art

You don't yet have a wizard sprite pack in the project. Pick one:

- **Quickest:** drag any spare sprite into the scene as a placeholder
  (a colored rectangle works for testing the AI).
- **Recommended (free):** import the **Evil Wizard** pack from
  [LuizMelo on itch.io](https://luizmelo.itch.io/) (look for
  *Evil Wizard 2* or similar — CC-0 / free for commercial use).
  Drop it into `Assets/Evil Wizard/` and add an attribution row to
  the README's third-party-assets table.

Whichever sprite you pick, you need at least: `Idle`, `Walk`/`Run`,
`Attack` (cast), and `Death` clips. If the pack ships with an
animator controller, you can re-use it; otherwise build one as below.

---

## 2) Create the EnemyConfigSO asset

1. In the Project window, navigate to
   `Assets/_Project/ScriptableObjects/Enemies/`.
2. **Right-click → Create → RoninRun → Enemy Config**.
3. Name it `Wizard_Config`.
4. Set values (suggested starting tuning):
   - `Max HP`: 3 *(actual HP lives on the Health component on the prefab — leave this as a reference value)*
   - `Contact Damage`: 1
   - `Ranged Damage`: 1
   - `Move Speed`: 1.8
   - `Detection Range`: 7
   - `Attack Range`: 6 *(wizard doesn't melee; this is when it becomes "in range to cast")*
   - `Attack Cooldown`: 2.5
   - `Attack Wind Up`: 0.6

Repeat for `Slime_Config` later if you want to migrate the slime to the same pattern (Milestone 1 task).

---

## 3) Build the projectile prefab

1. **GameObject → Create Empty**, name it `WizardBolt`.
2. Add components:
   - `Sprite Renderer` (assign a purple bolt sprite, or any
     placeholder — a small magenta circle works).
   - `Rigidbody 2D`: **Body Type = Dynamic**, **Gravity Scale = 0**,
     **Collision Detection = Continuous**, freeze rotation Z.
   - `Circle Collider 2D`: **Is Trigger = ON**, radius ~0.15.
   - `Projectile` (the script we just added).
3. On the `Projectile` component:
   - `Speed`: 8
   - `Damage`: 1
   - `Damage Mask`: select the **Player** layer.
   - `Block Mask`: select the **Ground** layer.
   - `Lifetime`: 4
   - `Impact Vfx Prefab`: leave empty for now (hook up in M2 polish).
4. Drag the `WizardBolt` GameObject into
   `Assets/_Project/Prefabs/Projectiles/` (create the folder if
   missing) to make it a prefab. Delete the scene instance.

---

## 4) Build the wizard prefab

1. Drag your wizard sprite into the scene.
2. Add components:
   - `Rigidbody 2D` (Dynamic, freeze rotation Z, gravity scale 3).
   - `Capsule Collider 2D` sized roughly to the sprite.
   - `Health` (set Max HP to 3).
   - `Animator` with the wizard's controller assigned. Parameters
     the script expects: `Speed` (Float), `Cast` (Trigger). Both
     are optional — if you skip the animator the AI still runs.
   - `WizardEnemy` (the script we just added).
3. Configure the `WizardEnemy` component:
   - `Player`: leave empty — `EnemyBase.Start()` finds it by tag.
   - `Sprite Renderer`, `Rigidbody`, `Health`: auto-fill from
     `RequireComponent`.
   - `Detection Range`: 7. `Attack Range`: 6.
   - `Move Speed`: 1.8. `Can Flip Sprite`: ON.
   - `Config`: drag `Wizard_Config.asset` here.
   - `Left Point` / `Right Point`: empty children of the wizard placed
     at the patrol bounds (instantiate two empty GameObjects in the
     scene as children of an environmental anchor, *not* the wizard
     itself, otherwise they move with the wizard).
   - `Start Moving Right`: ON.
   - `Projectile Prefab`: drag the `WizardBolt` prefab.
   - `Cast Point`: empty child positioned at the wizard's hand.
   - `Projectile Speed`: 8. `Projectile Damage`: 1.
   - `Animator`, `Speed Param`, `Cast Trigger Param`: leave defaults.
4. Drag the configured wizard into
   `Assets/_Project/Prefabs/Enemies/` as `Wizard.prefab`.

---

## 5) Place in Level 2

1. Open `Assets/_Project/Scenes/02_Level2.unity`.
2. Drag the `Wizard.prefab` into the scene at two or three vantage
   points — typically on raised platforms where they can shoot down
   at the player.
3. For each instance, place its `Left Point` and `Right Point`
   waypoints in the world (as siblings under a `Patrol_Bounds`
   empty parent) and assign them in the wizard's inspector.
4. Make sure each wizard's `Cast Point` is positioned so projectiles
   spawn outside the wizard's own collider.

---

## 6) Test

Press **Play**. Move into the wizard's detection range. Expected:

- The wizard stops patrolling, turns to face you.
- After ~0.6s wind-up, a `WizardBolt` fires towards you, dealing
  1 damage on hit and despawning on hit, on terrain, or after 4s.
- Wizard goes on a 2.5s cooldown, then casts again if you're still
  in range. Otherwise resumes patrol.
- Hitting the wizard with the sword reduces its HP; it dies after
  3 hits.

If anything misbehaves: check the `Damage Mask` on the projectile,
make sure the player is on the **Player** layer, and check the gizmos
(yellow detection circle, red attack circle, magenta cast point and
launch ray) in the Scene view with the wizard selected.
