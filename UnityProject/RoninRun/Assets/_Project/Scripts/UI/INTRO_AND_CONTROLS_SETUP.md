# Intro Scene + Main Menu Controls Panel — Setup

Step-by-step guide. Follow in order. Everything except the font
conversion and final scene-order tweak is automated by menu items.

---

## 1) Convert Bruno.ttf into a TextMeshPro Font Asset

TMP can't use raw `.ttf` files directly — it needs its own SDF font
asset baked from the `.ttf`. This is a one-time, 60-second task.

1. Make sure you've imported TMP Essentials. If you haven't, go to
   **Window → TextMeshPro → Import TMP Essential Resources** and
   accept the prompt.
2. Open **Window → TextMeshPro → Font Asset Creator**.
3. In **Source Font File**, drag in `Assets/Bruno ttf/Bruno.ttf`
   (or wherever your file lives — yours is in `Bruno ttf/`).
4. Defaults are fine for most settings. Recommended tweaks:
   - **Atlas Resolution**: 1024 × 1024.
   - **Character Set**: *ASCII* (covers what we need).
   - **Render Mode**: SDFAA.
5. Click **Generate Font Atlas**. Wait ~10 seconds.
6. Click **Save**, save it as
   `Assets/Bruno ttf/Bruno SDF.asset`.

You now have a `Bruno SDF` TMP font asset you can drag into any
TMP component's **Font Asset** field.

---

## 2) Refresh Unity so it sees the new scripts

In Unity, click somewhere in the editor to give it focus, then
**Ctrl+R** (or **Assets → Refresh**). New menu items appear under
**RoninRun → Setup**.

Watch the Console — there should be no compile errors. If there are,
paste them to me.

---

## 3) Build the Intro Scene (one click)

In the menu bar: **RoninRun → Setup → Build Intro Scene**.

This:
- Creates `Assets/_Project/Scenes/00b_Intro.unity`.
- Adds a Main Camera (orthographic, black background).
- Adds an `IntroCanvas` with a black background image, a TMP body
  text, a faint "Press SPACE to begin" prompt, and an EventSystem.
- Drops in a `LoreIntroController` GameObject with the body text
  and prompt references already wired.
- Adds the new scene to **Build Settings** (at the end of the list).

After it runs, the new scene is open in the editor for inspection.

**Wire the Bruno font:**
1. Select **IntroCanvas → BodyText** in the Hierarchy.
2. In the Inspector, find the **TextMeshPro - Text (UI)** component's
   **Font Asset** field.
3. Drag `Bruno SDF.asset` from `Assets/Bruno ttf/` into the field.
4. Repeat for **ContinuePrompt → Label**.

(Optional: tweak the body text font size — 56 may need to come down
to ~42 with Bruno because Bruno is a wider display face.)

**Edit the lore text if you want different lines:**
1. Select **LoreIntroController** in the Hierarchy.
2. In the Inspector, expand **Paragraphs**.
3. Edit the five strings, or change the array size to add more.

**Re-order in Build Settings:**
1. Open **File → Build Profiles** (or **File → Build Settings** on
   older menu paths).
2. Drag `00b_Intro` so its build index is **right after**
   `00_MainMenu` — i.e. just before `01_Level1`.
3. Save the scene (Ctrl+S).

---

## 4) Build the Main Menu Controls Panel prefab (one click)

In the menu bar: **RoninRun → Setup → Build Main Menu Controls
Panel Prefab**.

This produces
`Assets/_Project/Prefabs/UI/MainMenuControlsPanel.prefab` with the
"CONTROLS" heading, four mapping rows, a tip line, and a Back
button. If your `MainMenuUI` was in the open scene at the time, the
Back button is auto-wired to its `OnControlsBackButton` method.

**Wire it into your existing main menu:**
1. Open `00_MainMenu.unity`.
2. Drag `MainMenuControlsPanel.prefab` from
   `Assets/_Project/Prefabs/UI/` into the scene **as a child of
   your existing main menu Canvas** (so it inherits the same
   CanvasScaler).
3. Select the canvas root that holds your existing Start / Quit
   buttons. The **MainMenuUI** component now has two new fields:
   - **Main Panel** — drag the existing button container (the
     GameObject that holds your Start Game / Quit buttons).
   - **Controls Panel** — drag the `MainMenuControlsPanel` instance
     you just placed.
4. The `MainMenuUI.firstSceneName` field is already set to
   `00b_Intro`. Leave it.
5. Add a new **"Controls"** button to your existing menu (between
   Start Game and Quit). Set its OnClick to call
   `MainMenuUI.OnControlsButton`.

**Apply the Bruno font:**
1. Open `MainMenuControlsPanel` (prefab or instance).
2. For every TMP component (Heading, MoveRow, JumpRow, AttackRow,
   PauseRow, TipRow, Back button label), drag `Bruno SDF.asset`
   into the **Font Asset** field.

Optionally apply Bruno to the existing menu title and buttons too
for a unified look — same drag-drop process.

---

## 5) Test

1. Press Play from `00_MainMenu`.
2. Click **Controls**. The Controls panel should appear over the
   menu; the Start/Quit buttons should hide.
3. Click **Back**. Returns to the main menu.
4. Click **Start Game**. The intro scene should load: black
   background, first lore paragraph fades in, holds, fades out,
   next paragraph appears, and so on.
5. Press **any key** to skip the current paragraph. Press it again
   to skip the final-paragraph hold and load Level 1.
6. If you do nothing, the intro auto-advances and ends after about
   18 seconds, then loads Level 1.

If the intro loads but the text is invisible: you probably haven't
assigned the Bruno SDF font asset yet, or the text material is
hiding it on a black background. Set its color to white via the
LoreIntroController (it's already light cream by default).

---

## 6) Troubleshooting

- **No "RoninRun" menu**: scripts haven't compiled. Ctrl+R; check
  Console for errors.
- **Build Intro Scene asks if I want to save current scene**: yes,
  click Save. Unrelated to the intro build.
- **The body text looks tiny**: Bruno is a wide display face; lower
  the **Font Size** on `BodyText` to ~36-42.
- **Continue prompt never shows**: that's a bug in the controller
  if `continuePromptGroup` is null — verify the field is wired on
  the LoreIntroController.
- **Skipping doesn't work**: the controller uses Unity's new Input
  System (`Keyboard.current`). If your project's Active Input
  Handling is set to "Old", switch to "Both" in
  **Edit → Project Settings → Player → Active Input Handling**.

---

## Optional — apply Bruno to the rest of the game

Bruno is a strong display face but a poor body face. Reasonable
deployment:

- **Bruno** for titles: main menu title, "BRINGER OF DEATH" boss bar
  label, "PAUSED" header, "VICTORY" header, "CONTROLS" header.
- **TMP default (LiberationSans SDF)** for body text and button
  labels at small sizes.

If you want everything in Bruno, just drag the font asset onto each
TMP component — no code changes needed.
