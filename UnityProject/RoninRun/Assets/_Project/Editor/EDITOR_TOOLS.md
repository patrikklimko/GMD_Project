# Editor Tools — RoninRun → Setup menu

These scripts live in `Assets/_Project/Editor/` and only run inside
the Unity Editor (they're guarded by `#if UNITY_EDITOR` and stripped
from runtime builds). They add menu items under **RoninRun → Setup**
that automate the editor-side setup work that would otherwise mean
30+ clicks and easy-to-miss field assignments.

Open Unity, wait for the scripts to compile, then look at the menu
bar for **RoninRun → Setup**.

## Available actions

| Menu item                                    | What it does                                                                 |
|----------------------------------------------|------------------------------------------------------------------------------|
| Create Wizard Config Asset                    | Creates `Assets/_Project/ScriptableObjects/Enemies/Wizard_Config.asset` with the suggested wizard tuning values. Re-runnable. |
| Create Slime Config Asset                     | Same for `Slime_Config.asset`. Use when you migrate the slime to read from a config.                                          |
| Create Boss Config Asset                      | Same for `Boss_Config.asset`. Used in Milestone 3.                                                                            |
| Create All Enemy Configs                      | Runs the three above in sequence.                                                                                             |
| Build Pause Menu Canvas Prefab                | Builds the full pause-menu canvas (Canvas, panels, buttons, EventSystem), wires every button OnClick to the right `PauseMenu` method, and saves the prefab to `Assets/_Project/Prefabs/UI/PauseMenuCanvas.prefab`. Re-runnable (overwrites). |

## What's still manual

- **Wizard prefab** — needs an imported wizard sprite pack first. See [`Scripts/Enemies/Wizard/WIZARD_SETUP.md`](../Scripts/Enemies/Wizard/WIZARD_SETUP.md).
- **Wizard projectile prefab** — same caveat (can use a placeholder sprite, but you choose the visual).
- **Placing pause-menu / wizard prefabs into scenes** — you do this once per scene.
- **Wiring the Pause input action** — has to be edited in `InputSystem_Actions.inputactions`.

These were all left manual deliberately because they either depend
on assets you choose, or the editor scene-edit API is fragile in
ways that can silently corrupt scenes.

## Re-running

All of the menu items are idempotent: existing config assets are
loaded and updated in-place rather than duplicated, and the pause
menu prefab is deleted and rebuilt cleanly. You can re-run them
freely if you change values in the source script.

## Troubleshooting

If a menu item doesn't appear, check the **Console** for compile
errors first. The scripts depend on:
- `EnemyConfigSO` (in `Scripts/Enemies/Base/`) — for the configs.
- `PauseMenu` (in `Scripts/UI/`) — for the pause menu builder.
- `TextMeshProUGUI` (TextMesh Pro package) — for menu text.

If TextMesh Pro hasn't been imported (Unity sometimes asks you to
import the Essentials on first use), open any scene using TMP and
accept the prompt, then re-run the builder.
