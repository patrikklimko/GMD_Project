# Pause Menu — Unity-side Setup

The `PauseMenu` and `SceneLoader` C# scripts are done. You only need
to build the canvas, wire button callbacks, and hook up the Esc
input action. Steps below assume you'll build it once and prefab it,
then drop the prefab into every gameplay scene.

---

## 1) Create the pause input action

1. Open `Assets/InputSystem_Actions.inputactions`.
2. Under the **UI** action map, click **+** to add a new action.
3. Name it **Pause**, type **Button**.
4. Add a binding: **Path = Keyboard / Escape**. (Optionally also bind a gamepad button — `Start` on most pads.)
5. Save the asset.

The wizard fix-up step: `PauseMenu.cs` will fall back to polling
`Keyboard.current.escapeKey` if you don't wire this, so it still
works without the action — but wiring it is cleaner and required
for the arcade machine which won't have a keyboard.

---

## 2) Build the canvas hierarchy

Build this once in any scene (e.g., `01_Level1`):

```
PauseMenuCanvas               (Canvas, Render Mode = Screen Space - Overlay,
                               GraphicRaycaster, CanvasScaler = Scale With
                               Screen Size, reference 1920x1080)
├─ PausePanel                 (GameObject — disabled by default)
│  ├─ Background              (Image, color = black 0.6 alpha, full screen)
│  ├─ Title                   (TMP - "PAUSED")
│  ├─ Resume     (Button)
│  ├─ Restart    (Button)
│  ├─ Controls   (Button)
│  └─ MainMenu   (Button)
├─ ControlsPanel              (GameObject — disabled by default)
│  ├─ Background
│  ├─ Heading                 (TMP - "CONTROLS")
│  ├─ MovementRow             (TMP - "Move: A / D or arrows")
│  ├─ JumpRow                 (TMP - "Jump: Space (double-tap = double jump)")
│  ├─ AttackRow               (TMP - "Attack: J or LMB")
│  ├─ PauseRow                (TMP - "Pause: Esc")
│  └─ Back        (Button)
└─ EventSystem                (only if the scene doesn't already have one)
```

Tip: for prototyping, use Unity's **GameObject → UI → Panel** twice
(one for `PausePanel`, one for `ControlsPanel`) and lay the buttons
out vertically with a `Vertical Layout Group`.

---

## 3) Add the PauseMenu component

1. Select `PauseMenuCanvas`.
2. Add component → **Pause Menu**.
3. Configure the inspector:
   - `Pause Panel`: drag `PausePanel`.
   - `Controls Panel`: drag `ControlsPanel`.
   - `Pause Action`: drag the **UI/Pause** action reference from the
     InputSystem_Actions asset.
   - `Start Paused`: OFF.

---

## 4) Wire the buttons

Pause panel buttons → `PauseMenuCanvas` (PauseMenu component):

| Button     | OnClick                  |
|------------|--------------------------|
| Resume     | `PauseMenu.OnResumeButton`     |
| Restart    | `PauseMenu.OnRestartButton`    |
| Controls   | `PauseMenu.OnControlsButton`   |
| MainMenu   | `PauseMenu.OnMainMenuButton`   |

Controls panel:

| Button     | OnClick                          |
|------------|----------------------------------|
| Back       | `PauseMenu.OnControlsBackButton` |

(There's also `OnQuitButton` if you want a hard-quit option — we
don't recommend it inside gameplay; the main menu has a Quit button.)

---

## 5) Make it a prefab

1. Drag `PauseMenuCanvas` from the Hierarchy into
   `Assets/_Project/Prefabs/UI/` as **PauseMenuCanvas.prefab**.
2. Delete the scene instance.
3. Drag the prefab into:
   - `01_Level1`
   - `02_Level2`
   - `03_Level3`
   - `04_BossLevel`

The main menu and victory scenes don't need it.

---

## 6) Test

- **Resume**: opens with Esc, closes with Esc *or* the Resume button. World freezes (`Time.timeScale = 0`) when paused.
- **Restart**: reloads the current level, resumes time.
- **Controls**: shows the controls subpanel; Back returns to the
  main pause panel.
- **Main Menu**: loads `00_MainMenu`.

If pause doesn't open: check the action reference on the canvas, or
fall back to the keyboard fallback — Esc should still work either
way.
