# Audio System — Setup & Usage

The C# side is done: `AudioManager`, `SceneMusicBinder`, `SfxId`, and
`SfxLibrarySO`. To get audio playing in the game you need to:

1. **Create the AudioMixer** (one manual click in Unity).
2. **Run the editor menu** to create the SfxLibrary and AudioManager prefab.
3. **Download audio files** (CC-0 / royalty-free) and drop them into the project.
4. **Wire the BGM clips** to each scene via `SceneMusicBinder`.
5. **Wire the SFX clips** in the SfxLibrary inspector.

Steps below.

---

## 1) Create the AudioMixer (manual, ~30 s)

Unity's mixer asset format isn't friendly to programmatic creation,
so this step is manual.

1. In the Project window, navigate to `Assets/_Project/Audio/`.
2. **Right-click → Create → Audio Mixer**. Name it **MasterMixer**.
3. Open it (double-click). The Audio Mixer window opens.
4. In the **Groups** column, click the **+** next to *Master* twice to add two child groups: **Music** and **SFX**.
5. **Expose volume parameters:**
   - Click the **Master** group. In the inspector, right-click **Volume** → **Expose 'Volume (of Master)' to script**.
   - Repeat for **Music** and **SFX**.
   - Switch to the **Exposed Parameters** dropdown (top-right of the mixer window) and rename:
     - `MyExposedParam` (Master) → **MasterVolume**
     - The Music one → **MusicVolume**
     - The SFX one → **SfxVolume**

The names matter — `AudioManager.SetMasterVolume01` and friends look
them up by string.

---

## 2) Create the audio system assets (one menu click)

In the menu bar: **RoninRun → Setup → Create Audio System (Library + Manager)**.

This produces:

- `Assets/_Project/ScriptableObjects/Audio/SfxLibrary.asset` — pre-populated with one entry per `SfxId` (clip slots empty).
- `Assets/_Project/Prefabs/Core/AudioManager.prefab` — with the SfxLibrary auto-linked.

Open the AudioManager prefab and:

- Drag your **MasterMixer** asset to the **Mixer** field.
- Set **Music Group** to the **Music** child group of the mixer.
- Set **SFX Group** to the **SFX** child group.

---

## 3) Download audio (you do this; I cannot)

I can't generate or fetch audio files. You'll need to download
royalty-free / CC-0 tracks. Recommended sources, all free for
commercial and non-commercial use:

| Source                                            | Best for                  | Licensing                                            |
|---------------------------------------------------|---------------------------|------------------------------------------------------|
| [Pixabay Music](https://pixabay.com/music/)       | BGM (long loops)          | Pixabay Content License — free, no attribution required for projects.  |
| [Kenney.nl](https://kenney.nl/assets/category:Audio) | SFX packs (impact, UI) | CC-0 — public domain.                                |
| [Freesound](https://freesound.org/)               | Individual SFX            | Various; **filter to "Creative Commons 0"**.         |
| [OpenGameArt.org](https://opengameart.org/)       | Both, lower quality       | Various; **filter to CC0 or CC-BY 3.0**.             |

### Suggested search terms for BGM

| Scene         | Mood you want                  | Search term ideas                                                |
|---------------|--------------------------------|------------------------------------------------------------------|
| 00_MainMenu   | Ambient, mysterious, samurai   | "japanese ambient", "shamisen calm", "fantasy menu loop"         |
| 01_Level1     | Forest exploration, tense      | "dark forest", "fantasy adventure loop", "celtic battle"         |
| 02_Level2     | Heightened action              | "battle medieval", "action fantasy", "samurai duel"              |
| 03_Level3     | Foreboding, climactic          | "dark dungeon", "epic dark fantasy"                              |
| 04_BossLevel  | Boss encounter                 | "epic boss battle", "intense orchestral", "demon battle"         |
| 05_Victory    | Triumphant, short              | "victory fanfare", "heroic ending", "fantasy triumph"            |

Aim for ~2–4 minute loopable tracks for BGM (Unity will loop them).

### Suggested SFX shopping list

The `SfxId` enum already lists every sound the game wants. From
Kenney's *Impact* and *UI* packs and Freesound CC-0 you can cover:

- **PlayerSwordSwing** — short whoosh, ~150 ms.
- **PlayerSwordHit** — meaty thud, ~200 ms.
- **PlayerHurt** — short male grunt or filtered hit.
- **PlayerDeath** — longer fall/death sound.
- **EnemyHit** — generic flesh / slime hit.
- **EnemyDeath** — softer pop / squelch (slime) — boss can override later.
- **WizardCast** — magic woosh, ~300 ms.
- **PlayerJump** — short rising whoosh.
- **PlayerLand** — soft thud.
- **UiButtonClick / UiButtonHover** — clean UI clicks.
- **UiPause / UiUnpause** — short woosh up / down.
- **LevelComplete** — uplifting chime.
- **Victory** — short fanfare (can reuse the Victory BGM intro).

### File format & target paths

Drop downloaded files here, named exactly so it's easy to re-import:

```
Assets/_Project/Audio/BGM/
    Menu_BGM.ogg
    Level1_BGM.ogg
    Level2_BGM.ogg
    Level3_BGM.ogg
    Boss_BGM.ogg
    Victory_BGM.ogg

Assets/_Project/Audio/SFX/
    PlayerSwordSwing.wav
    PlayerSwordHit.wav
    PlayerHurt.wav
    PlayerDeath.wav
    EnemyHit.wav
    EnemyDeath.wav
    WizardCast.wav
    PlayerJump.wav
    PlayerLand.wav
    UiButtonClick.wav
    UiButtonHover.wav
    UiPause.wav
    UiUnpause.wav
    LevelComplete.wav
    Victory.wav
```

Format notes:
- BGM as `.ogg` is best for WebGL builds (smaller, decoded efficiently). MP3 also works.
- SFX as `.wav` keeps quality at small file sizes.
- After dropping in, select the BGM clips in Unity and set
  **Load Type = Streaming** (saves memory, fine for long tracks).
  Set SFX clips to **Load Type = Decompress on Load** (lowest latency).

**Don't forget:** add an attribution row in the README's third-party
table for every track / pack you use, even if attribution isn't
required by license. Course examiners specifically look for this.

---

## 4) Wire BGM per scene

For every scene that should play music:

1. Open the scene.
2. **GameObject → Create Empty**, name it `SceneMusic`.
3. Add component **Scene Music Binder**.
4. Drag the appropriate BGM clip (e.g. `Level1_BGM.ogg`) to **Bgm Clip**.
5. Save the scene.

Scenes that should be silent (e.g. dialog or transition scenes):
toggle **Silence Scene** instead of assigning a clip.

The AudioManager prefab itself goes in **00_MainMenu only** — it
persists via `DontDestroyOnLoad`. If you accidentally drop a second
copy in another scene, the duplicate self-destroys on `Awake`.

---

## 5) Wire SFX clips in the library

1. Open `Assets/_Project/ScriptableObjects/Audio/SfxLibrary.asset`.
2. For each entry (one per SfxId), drag the matching audio clip to the **Clip** column.
3. Adjust **Default Volume** if a clip is too loud or too soft.

To play an SFX from gameplay code:

```csharp
AudioManager.Instance.PlaySfx(SfxId.PlayerSwordSwing);
```

Add the call inside `PlayerCombat` (sword swing), `PlayerHealth`
(hurt), `WizardEnemy.SpawnProjectile` (cast), `PauseMenu.Pause` and
`Resume` (UI ones), etc. We'll wire those in the next milestone-2
polish pass — for now the system is in place and ready.

---

## 6) Smoke test

1. Press Play from the **00_MainMenu** scene.
2. Menu BGM should fade in.
3. Click **Start Game** → BGM crossfades to Level 1 BGM.
4. Walk to the level-end → BGM crossfades to Level 2.
5. Press Esc → pause panel appears, audio keeps playing. Resume → continue.

If the menu BGM doesn't play: check that the AudioManager prefab is
in the menu scene and the SceneMusic binder has the clip assigned.
If nothing plays at all: check the mixer's Master volume is not
muted (the slider should be near 0 dB, not -80 dB).
