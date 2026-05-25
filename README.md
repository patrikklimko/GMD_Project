# RoninRun

A 2D pixel-art action platformer built in Unity 6 (URP, 2D Renderer) for the GMD course at VIA University College.

The player controls a wandering Ronin moving through cursed lands populated by slimes, dark wizards, and a final demonic boss known as the **Bringer of Death**. Combat is a sword-and-jump affair: a two-attack combo, dual jump, and reactive enemy AI.

The deployable target is the VIA Arcade Machine in the XR Lab (NVIDIA GTX 980 Ti, i5-6600K, 8 GB RAM, custom dual-stick + 6-button input), with a build size cap of 500 MB. A WebGL build is also published to GitHub Pages so the game can be played in a browser.

---

## Author

| GitHub        | Full name      | Student number               |
|---------------|----------------|------------------------------|
| @patrikklimko | Patrik Klimko  | `<YOUR_STUDENT_NUMBER>`      |

This is a **solo** project. All commits in this repository represent my own work.

---

## Quick links

- **Playable WebGL build:** _coming soon — will be published at `https://patrikklimko.github.io/GMD_Project/`_
- **Gameplay video (~2 min, YouTube):** _coming soon — link will be added before submission_
- **Development roadmap:** [`ROADMAP.md`](ROADMAP.md)

---

## Blog posts

The course requires six blog posts, each living as a markdown file in this repository.

| #  | Title                                              | Status        |
|----|----------------------------------------------------|---------------|
| 1  | [Roll-a-Ball: How We Expanded the Game](Documentation/Roll%20a%20Ball/Roll%20a%20Ball.md) | Published |
| 2  | [Game Design Document & Milestones](Documentation/GDD%20and%20Milestones/GDD%20and%20Milestones.md) | Published |
| 3  | [Milestone 1 — Wizard, EnemyConfigSO, Pause Menu](Documentation/Milestone%201%20-%20Wizard%20and%20Pause/Milestone%201.md) | Published |
| 4  | [Milestone 2 — Audio Architecture and Slime Combat Polish](Documentation/Milestone%202%20-%20Audio%20and%20Slime%20Polish/Milestone%202.md) | Published |
| 5  | [Milestone 3 — Bringer of Death, Bootstrappers, and Player-Feel Polish](Documentation/Milestone%203%20-%20Boss%20and%20Bootstrappers/Milestone%203.md) | Published |
| 6  | [Final Showcase — RoninRun, Shipped](Documentation/Final%20Game%20Showcase/Final%20Game%20Product.md) | Published |

---

## Project layout

```
GMD_Project/
├── Documentation/                 # Blog posts and design notes
│   └── Roll a Ball/               # Blog post #1 + screenshots
├── UnityProject/RoninRun/         # Unity project root (open this in Unity 6)
│   ├── Assets/
│   │   ├── _Project/              # All gameplay code, scenes, and assets
│   │   │   ├── Animations/
│   │   │   ├── Art/
│   │   │   ├── Audio/             # (will be populated in milestone 2)
│   │   │   ├── Prefabs/
│   │   │   ├── Scenes/            # 7 scenes: menu, 4 levels, boss, victory
│   │   │   ├── Scripts/           # Core / Player / Enemies / UI / Level
│   │   │   ├── ScriptableObjects/
│   │   │   └── UI/
│   │   ├── FeonY/                 # 3rd-party: animated pixel backgrounds
│   │   ├── Martial Hero/          # 3rd-party: player sprite/animation pack
│   │   ├── Slime/                 # 3rd-party: slime enemy pack
│   │   └── TextMesh Pro/          # Unity built-in
│   ├── Packages/
│   └── ProjectSettings/
├── README.md                      # this file
└── ROADMAP.md                     # development plan and milestones
```

---

## How to run

### Open the Unity project
1. Install [Unity 6 (6000.3.7f1)](https://unity.com/releases/editor/qa/lts-releases) via Unity Hub.
2. In Unity Hub, click **Add → Add project from disk** and select `UnityProject/RoninRun`.
3. Open the project. Unity will re-import the asset library on first launch (this can take a few minutes).
4. In the Project window, open `Assets/_Project/Scenes/00_MainMenu.unity` and press **Play**.

### Default controls (keyboard)
| Action      | Key             |
|-------------|-----------------|
| Move        | A / D or Left / Right |
| Jump        | Space (double-tap for dual jump) |
| Attack 1    | J or Left Mouse |
| Attack 2    | (queued during attack 1) |
| Pause       | Esc             |

### Arcade machine controls
The build will support the VIA Arcade Machine's two 8-directional sticks and six buttons. Mapping is wired through Unity's new Input System (`Assets/InputSystem_Actions.inputactions`) and will be finalized in milestone 2.

---

## Third-party assets and references

All third-party assets are CC-0 or otherwise free for commercial and non-commercial use.

| Asset                                | Used for                                         | License |
|--------------------------------------|--------------------------------------------------|---------|
| Martial Hero Asset Pack              | Player character sprites and animations          | CC-0 — see [`License.txt`](UnityProject/RoninRun/Assets/Martial%20Hero/License.txt) |
| FeonY — Animated Pixel-Art Backgrounds | Parallax scrolling backgrounds for each level | Unity Asset Store EULA — _verify on the asset's store page before submission_ |
| Slime Asset Pack                     | Slime enemy sprites and animations               | Unity Asset Store EULA — _verify on the asset's store page before submission_ |
| [Evil Wizard](https://assetstore.unity.com/packages/2d/characters/evil-wizard-168007) by Luiz Melo | Wizard enemy sprites and animations | Unity Asset Store EULA (free) |
| [Bringer Of Death (free)](https://assetstore.unity.com/packages/2d/characters/bringer-of-death-free-204038) by Clembod | Bringer of Death boss sprites and animations | Unity Asset Store EULA (free) |
| TextMesh Pro                         | UI text rendering                                | Bundled with Unity |
| Unity Universal Render Pipeline (URP) | 2D Renderer                                     | Bundled with Unity |
| Unity Input System                   | Player input                                     | Bundled with Unity |

Audio sources will be added here as the audio system is implemented in milestone 2 (planned: [Kenney.nl](https://kenney.nl/) impact and UI sound packs, and selected CC-0 tracks from [Freesound](https://freesound.org/)).

### Tutorials and references
- Unity's official Roll-a-Ball tutorial — basis for blog post #1.
- _Any further tutorials referenced during development will be listed here as they are used._

---

## Build target & constraints

- Unity version: **6.3.7f1** (Unity 6 LTS).
- Render pipeline: **URP 2D**.
- Platform priorities: **Standalone Windows** (arcade machine) and **WebGL** (browser demo).
- Build size cap: **500 MB** (per assignment requirements).

---

## License

Source code in this repository (under `UnityProject/RoninRun/Assets/_Project/`) is © 2026 Patrik Klimko, released for educational review.
Third-party assets retain their original licenses as listed above.
