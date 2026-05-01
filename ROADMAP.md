# RoninRun — Development Roadmap

**Owner:** Patrik Klimko  •  **Mode:** Solo  •  **Window:** ~14 days  •  **Status:** Phase 0 complete

This document is the single source of truth for what's left to ship. It exists so any AI assistant or human collaborator can be onboarded in 2 minutes.

---

## 1. Where We Are (verified by audit)

**Working systems:**
- Player: movement, dual-jump, ground detect, attack combo (Attack1 → Attack2), knockback. Uses new InputSystem. ~5 scripts.
- Slime enemy: patrol → detect → chase → dash attack → knockback → HP bar. Genuine state machine.
- Health architecture: `Health.cs` (generic, with `IDamageable` interface) + `EnemyHealthBar.cs` UI binding.
- Scenes: 7 scenes wired (`00_MainMenu`, `00_TestGround`, `01_Level1`–`03_Level3`, `04_BossLevel`, `05_Victory`).
- Level transitions via `LevelEndTrigger.cs`.

**Verified gaps (don't trust earlier doc — audit caught these):**
- Audio folder is **empty**. No `AudioSource`, no `AudioClip`, no AudioManager. Earlier doc claimed audio was missing — confirmed.
- **No Git repository.** No `.git`, no commits, no GitHub. Critical blocker for assignment.
- `PlayerHealth.cs` duplicates `Health.cs` instead of using it (DRY violation).
- `EnemyBase` calls `GameObject.FindGameObjectWithTag("Player")` — fragile coupling.
- UI scripts hold direct references to specific health components — no event decoupling.
- Animator parameters are string-based (`"Speed"`, `"IsAttacking"`) — not hashed.
- Only one prefab (`Platform_Small`). Player and enemies are not prefabricated → no reuse across scenes.
- No `README.md`, no blog posts, no asset attribution, no WebGL build configured.

---

## 2. Strategy: What Gets Cut

The submission has hard pass/fail items (Git, README, WebGL, video, blog posts) and soft items (extra enemies, juice). With 14 days solo, we **cut to ship**:

| Originally planned     | Decision | Why |
|------------------------|----------|-----|
| Wizard enemy           | **Keep** | Easiest new enemy with biggest visual impact (projectiles). |
| Warrior enemy          | **CUT**  | Time sink for marginal grade gain — boss + polish reads better. |
| 3-phase boss           | **Simplify to 2 phases** | Phase 1 melee, Phase 2 teleport+spell. Cuts dev time ~40%. |
| Lives / score / checkpoints | **CUT** | Optional, no time. |
| 4 enemies total        | **2 enemies + 1 boss** | Quality over quantity. |
| Full audio (10+ tracks) | **Keep, lean** | 4 BGM tracks, 8 SFX. CC0 sources. |
| Blog post depth        | **Keep, minimum bar** | 6 posts × ~2000 chars each. |

---

## 3. Day-by-Day Timeline (14-day window)

### Days 1–2 · Foundations (DO NOT SKIP)
1. **Git init** with Unity-aware `.gitignore`.
2. **GitHub repo** (public).
3. **README.md** skeleton with placeholders for: blog post links, video link, WebGL build link, source attribution.
4. **WebGL smoke build** — 30-min experiment to confirm the project actually compiles to WebGL on this machine. Catch issues early.
5. **GitHub Pages** set up on `gh-pages` branch (or `/docs` folder) so future builds just need `git push`.
6. **Blog post #1: Roll-a-ball** (~2000 chars). You did this earlier in the course — write a brief recap.
7. **Blog post #2: GDD + Milestones** (~2500 chars). Defines:
   - **Milestone 1:** Architecture refactor + Wizard enemy.
   - **Milestone 2:** Audio + polish + pause menu.
   - **Milestone 3:** Boss + final integration.

**Commit cadence:** at least one commit per task. Commits are how the assignment grades your contributions.

### Days 3–4 · Architecture Refactor (Milestone 1 part A)
Goal: convert "tutorial code" into "production-ready code" before adding new content.

1. **Health unification:**
   - Make `PlayerHealth` *use* `Health` via composition (not duplicate it). PlayerHealth keeps i-frames + scene reload, but underlying HP lives in Health.
2. **UnityEvent decoupling:**
   - Add `OnHealthChanged(int current, int max)` and `OnDied` as `UnityEvent` on `Health.cs`.
   - `EnemyHealthBar` and `PlayerHealthUI` subscribe via inspector — no more hard refs.
3. **Animator hash caching:**
   - `static readonly int Speed = Animator.StringToHash("Speed");` etc. in `PlayerAnimatorController`.
4. **PlayerLocator service:**
   - Single static `PlayerLocator.Player` cached on `Awake`. EnemyBase reads from it. No more `GameObject.Find`.
5. **Prefabricate:** Player, Slime, all UI prefabs. So Level 2/3/Boss reuse them cleanly.

### Days 5–6 · Wizard Enemy (Milestone 1 part B)
1. **`EnemyConfigSO`** ScriptableObject: HP, damage, move speed, detection range, attack cooldown. Slime + Wizard each get one asset. SOLID expansion you can call out in the GDD.
2. **`WizardEnemy : EnemyBase`**: patrol → detect → stop → cast → cooldown.
3. **`Projectile.cs` + prefab**: travels in fixed direction, deals damage on `IDamageable`, despawns after lifetime or on hit. Particle trail optional.
4. **Place 2 wizards in `02_Level2`**.
5. **Commit milestone: "Milestone 1 complete — wizard + refactor"**.
6. **Blog post #3** (~2000 chars): explain refactor + wizard. This is your milestone post.

### Days 7–8 · Audio + Polish (Milestone 2)
1. **`AudioManager` singleton**: `DontDestroyOnLoad`, plays BGM with crossfade, has `PlaySfx(AudioClip)` method, optional `AudioMixer` for master/music/sfx volume sliders.
2. **Source audio** (CC0): freesound.org, kenney.nl/assets/ui-audio, kenney.nl/assets/impact-sounds. **Document every source in README.**
3. **BGM:** menu, gameplay loop, boss theme, victory. 4 tracks total.
4. **SFX:** sword swing, hit, jump, land, enemy death, player hurt, button click, projectile cast.
5. **Game feel pass:**
   - `CameraShake.cs` — transform-based, `Shake(duration, magnitude)`. Hook into player damage + sword hits.
   - `HitStop.cs` — coroutine setting `Time.timeScale = 0.05f` for 80ms on landed hits. Single line of juice that makes combat feel 10× better.
   - **Damage flash:** material tint white for 0.1s when `Health.TakeDamage` fires.
   - **Hit-spark particle prefab** — instantiate at hit location.
6. **Pause menu** prefab with Resume / Restart / Menu. Esc toggles.
7. **Commit milestone: "Milestone 2 complete — audio + polish"**.
8. **Blog post #4** (~2000 chars).

### Days 9–11 · Boss Fight (Milestone 3)
1. **`BringerOfDeath : EnemyBase`** with state machine (`Idle`, `Walk`, `Slash`, `Teleport`, `CastBarrage`, `Dead`).
2. **Phase 1 (HP 100%–50%)**: walk + slash combo, occasional charge.
3. **Phase 2 (HP <50%)**: teleport to player, fire 3-projectile fan, repeat.
4. **Boss HP bar** at top of screen. World-space camera lock to arena via `BossArenaTrigger`.
5. **Boss death sequence**: slow-mo, fade, load `05_Victory`.
6. **Blog post #5** (~2000 chars).

### Days 12–13 · Final Cut + Submission Prep
1. **Bug bash:** play through all 4 levels + boss. Fix anything embarrassing.
2. **Final WebGL build**, push to `gh-pages`. Verify it loads at `https://<user>.github.io/RoninRun/`.
3. **Verify build size** under 500MB (compress audio if needed).
4. **Record video** with OBS — 2-minute walkthrough showing menu, each level, boss, victory. Upload as **unlisted** YouTube.
5. **Blog post #6** (~2500 chars): final showcase + reflection.
6. **Update README** with all real links (no placeholders).

### Day 14 · Submit
1. **PDF**: 1–2 pages, includes GitHub link, your name + student number, blog-post links, video link, WebGL link, individual reflection (with code references — name specific scripts).
2. **Source zip**: exclude `Library/`, `Temp/`, `Logs/`, `Build/`, `obj/`, `.vs/`. Use `git archive` or hand-zip from a clean checkout.
3. **WISEflow upload** PDF + zip.
4. **Verification dry-run**: clone repo to a fresh folder, click every README link, confirm WebGL loads.

---

## 4. Architecture Decisions (record these in blog posts)

### 4.1 Health = composition, not inheritance
`PlayerHealth` and enemies both *contain* a `Health` component instead of duplicating its logic. Lets us swap behaviour (i-frames, knockback) without touching HP arithmetic.

### 4.2 EnemyConfigSO ScriptableObjects
Tunables (HP, damage, speed, ranges) live in `.asset` files, not hardcoded in scripts. Designer-friendly + easy to balance + makes it trivial to add more enemy variants later. Concrete SOLID demo.

### 4.3 UnityEvents for UI decoupling
`Health.OnHealthChanged` is a `UnityEvent<int,int>`. Health bars subscribe via inspector. The Health script doesn't know UI exists → Single Responsibility + Dependency Inversion.

### 4.4 PlayerLocator service
One static cached reference instead of `GameObject.Find` per enemy per scene. Faster + decoupled.

### 4.5 State Machine for Boss
Plain enum-based FSM (no Animator-driven states for AI). Easier to reason about, easier to debug, easier to extend.

### 4.6 AudioManager singleton with mixer routing
Persistent across scenes. SFX go through a pooled `AudioSource` array (no per-call instantiation).

---

## 5. Folder Structure (target)

```
Assets/_Project/
  Scripts/
    Core/
      Health.cs
      IDamageable.cs
      PlayerLocator.cs        (NEW)
      AudioManager.cs         (NEW)
      CameraShake.cs          (NEW)
      HitStop.cs              (NEW)
    Player/
      PlayerMovement2D.cs
      PlayerCombat.cs
      PlayerHealth.cs         (refactor: composes Health)
      PlayerAnimatorController.cs
      DamageFlash.cs          (NEW, reusable on enemies too)
    Enemies/
      Base/
        EnemyBase.cs
        EnemyConfigSO.cs      (NEW)
      Slime/
        SlimeEnemy.cs
        SlimeAttack.cs
      Wizard/                 (NEW)
        WizardEnemy.cs
        Projectile.cs
      Boss/                   (NEW)
        BringerOfDeath.cs
        BossArenaTrigger.cs
    UI/
      MainMenuUI.cs
      VictoryUI.cs
      PauseMenu.cs            (NEW)
      EnemyHealthBar.cs
      PlayerHealthUI.cs
      BossHealthBar.cs        (NEW)
    Level/
      LevelEndTrigger.cs
      SceneLoader.cs          (NEW, central scene mgmt)
  ScriptableObjects/
    Enemies/
      Slime_Config.asset      (NEW)
      Wizard_Config.asset     (NEW)
      Boss_Config.asset       (NEW)
  Audio/
    BGM/  SFX/                (POPULATE)
  Prefabs/
    Player.prefab             (NEW)
    Enemies/
      Slime.prefab            (NEW)
      Wizard.prefab           (NEW)
      BringerOfDeath.prefab   (NEW)
    Projectiles/
      WizardBolt.prefab       (NEW)
    UI/
      PauseMenu.prefab        (NEW)
      BossHealthBar.prefab    (NEW)
```

---

## 6. Risks & Contingencies

| Risk | Likelihood | Mitigation |
|---|---|---|
| WebGL build broken | Medium | Smoke-test on Day 1, not Day 13. |
| Boss takes longer than 3 days | High | Start Phase 1 only. Ship Phase 1 if time runs out, write Phase 2 in blog post #5 as "stretch goal not yet polished". |
| Audio licensing trip | Low | Stick to CC0 (kenney.nl, freesound CC0 filter). Document in README. |
| Refactor breaks Slime | Medium | Branch per refactor step. Keep `01_Level1` playable at all times — that's your fallback demo. |
| Video recording fails | Low | Practice recording on Day 7. Don't leave to Day 13. |

---

## 7. Definition of "Submittable"

Hard requirements (no partial credit):
- [ ] Public GitHub repo with README and 6 markdown blog posts
- [ ] WebGL build live at GitHub Pages URL, opens in browser
- [ ] YouTube video link in README, ~2 min, public/unlisted
- [ ] PDF in WISEflow with name + student number + all the above links + reflection
- [ ] Source code zip in WISEflow (no `Library/` etc.)
- [ ] Build under 500MB

Anything below is gravy.

---

*Last updated by audit: see git log.*
