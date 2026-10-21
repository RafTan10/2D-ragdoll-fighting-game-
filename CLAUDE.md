# MyRagdollVersion — 2D ragdoll duel game

A 2D physics ragdoll fighting game in the style of *Bloody Bastards* (Tibith): side view, pixel art,
1-v-1 duels, each hand swung separately, melee weapons and shields. Original name, art and characters
only — never copy Bloody Bastards' assets.

- Unity **6000.6.6f1**, URP, **new Input System only** (`activeInputHandler: 1`) — use `UnityEngine.InputSystem`, never `UnityEngine.Input`.
- Drive the open Editor with the Unity CLI (`C:\Users\User\AppData\Local\Unity\bin\unity.exe`).
  **Always pass `--project-path "F:\unity projects\MyRagdollVersion"`** — the user often has another
  Editor (e.g. `2d-project-main`) open too, and the CLI otherwise picks whichever it finds.
  Useful commands (names change between Pipeline versions — `unity command --query <word>` to find them):
  `eval_file --result-only <file.cs>`, `recompile` / `recompile_status`, `editor.playmode start|stop`,
  `editor.playmode set_run_in_background` (needed so Play mode runs while the Editor is unfocused —
  turn it off again with `--enable false`), `capture game_view --save_path Temp/x.png` (lands in
  `Assets/Temp/` — delete it afterwards), `scene list_open`.
- Keep eval files in a SHORT path (e.g. `%TEMP%\stickman_cc\`) — long paths fail on this machine.
- New files aren't seen until `AssetDatabase.Refresh()` (run it via eval_file), then `recompile`.
- Check `Application.isPlaying` inside evals that start matches — otherwise you spawn fighters into the saved scene.
- Quick test: Play mode → eval `FindAnyObjectByType<MatchManager>().StartMatch()`; give both fighters
  `new AIBrain(db.aiProfiles[2])` for an AI-vs-AI bout. `StartMatch` destroys the old fighters only at the end of
  the frame, so pick fighters (`FindObjectsByType<Fighter>()`) in a *later* eval.
- Evals can't declare classes; script a fighter with a lambda: `f.Brain = new ScriptBrain((self, opp) => new FighterInput { ... })`.
  Keep data across evals with `System.AppDomain.CurrentDomain.SetData/GetData`.
- Simulated mouse/keyboard (`runtime simulate_pointer`) does NOT reach gameplay while the Editor is unfocused
  (Input System background behaviour) — test brains directly (`new PlayerBrain().Think(p, e)`) instead.
- `Time.timeScale = 0.25f; Time.fixedDeltaTime = 0.005f` + repeated `capture game_view` gives slow-motion frame sequences.
- Git: `main` = baseline before the realism upgrade; work happens on branches (e.g. `realism-upgrade`). Commit
  before each risky step. `Library/` is ignored; if Unity reports "Database corruption detected", close Unity,
  rename `Library` (e.g. to `Library_corrupt`, ignored) and reopen — `--args -rebuildLibrary` via the CLI did not work.
- The Editor sometimes reopens `GetStarted_Scene`; open `Assets/Game/Scenes/Arena.unity` before testing.

## Roadmap (update the checkboxes as work lands)

### Phase 1 — Base game (current)
- [x] CLAUDE.md + roadmap
- [x] Data-driven content: `FighterDefinition`, `WeaponDefinition`, `ArenaDefinition`, `AIProfile`, all listed in `GameDatabase`
- [x] 2D ragdoll fighter built at runtime from a `FighterDefinition` (HingeJoint2D motors + balance)
- [x] Controls: each hand separately (hold = wind up / guard, release = strike), walk, hop
- [x] Weapons: dagger, sword, axe, mace, hammer, shield (block) — damage from impact speed, head = x1.5
- [x] AI opponents with Easy / Normal / Hard profiles
- [x] Match flow: rounds, KO, score, auto restart; pre-match select screen (fighter, opponent, arena, difficulty)
- [x] Pixel blood, 3 placeholder fighters, 1 placeholder arena (generated pixel art, no downloads)
- [x] Editor tools + Claude commands to add fighters, weapons and arenas
- [x] Feedback round 1 (inspiration video `inpiration_gameplay.mp4` in the project root): only released swings
      deal damage (no more "auto attacks"), 64 PPU detailed shaded art + capes, close follow camera
      (fighters ~55% of screen height), crouched guard stance, stepping walk, lunge hop, full 270° swings
      with charge (hold longer = harder), rain, KO slow-mo, hit shake
- [ ] Tune feel after the user plays it again (swing speed, balance, damage). Knobs: `Fighter.cs` tuning consts,
      arm pose angles in `DriveArm`, leg stance in `DriveLegs`, `Weapon.cs` damage clamp, `CameraRig` sizes,
      weapon/fighter/AI assets in `Assets/Game/Data`.
- [ ] Known rough edges: heavy weapons make the elbow sag a little; a jump into a raised weapon bounces off

Reference look (from the video): fighters fill ~60% of screen height; detailed pixel art (~110 px tall),
muted warm/olive overcast palette, dark outlines, plate + chainmail + capes, tall ornate red shields,
layered pine forest + fence + watchtower, rain; crouched wide stances, huge swing arcs, close clinches.

### Phase 1b — "Feel like Bloody Bastards" rework (approved plan: https://claude.ai/artifact/KUnMMo7uirzaXaQKT99yUR)
Key research finding: in BB each hand **follows the player's thumb** (drag / sweep / circle); there is no attack
button. Stamina drains with swinging (faint at 0), weight shrinks reach/stamina, damage depends on material and
contact point (tip hurts most), double-tap steps. User's decisions: PC hands follow the mouse cursor; steps
(holding A/D repeats steps); fainting in now; dismemberment later.
- [x] **Step 1 — Direct hand control.** `FighterInput` carries an aim point per hand; `Fighter.SolveArm` (2-bone IK)
      → shoulder/elbow motor targets, arm speed limited by strength and weapon mass; released hand relaxes to guard
      (weapon low, pointing at the opponent). Damage = weapon speed **at the contact point** (`Weapon.ContactSpeed`),
      no strike states. AI chops / thrusts / spins / blocks by moving aim points. Fixes found on the way: Physics2D
      solver iterations 20/8 (set in `MatchManager.Start`) and realistic arm masses (1.6 / 1.2 kg) — together they
      removed heavy arm jitter; balance force now scales with total mass.
- [x] Extra (user request): on-screen buttons (< > JUMP L R), real jump with tucked legs, camera follows jumps,
      fighters turn around when they cross (`Fighter.FaceOpponent` / `TurnAround`)
- [x] User round 3: hand buttons are **actions** — a weapon swings (hold = keep swinging, alternating chop / slash),
      a shield blocks while held (`SwingMove` in `HandMoves.cs`, shared with the AI); A = back, D = forward
      (relative to facing); smaller jump (JumpUp 4.6); hitboxes fitted to visible pixels (`PixelBounds`), weapons
      split into handle (bumps) + striking part (`WeaponDefinition.StrikeStart`); H toggles `HitboxOverlay`.
- [x] Step 2 — Armour materials per body region (`FighterDefinition.headArmor`…; damage through: plate 0.5, mail 0.7,
      leather 0.85, cloth 1, skin 1.15). Hit FX in `Weapon.HitEffects`: blood (flesh) or dust + sparks (armour),
      red flash on the struck part, knockback, hit-stop (`GameFeel.HitStop`), shake, generated sounds (`GameFeel.Play`).
- [x] Step 3 — Stamina (`Fighter.Stamina`, yellow bar under health): fast hand movement ~2 per swing, blocked blow
      on a shield costs the blocker, jump 9, lunge 7, step 0.4; regen 18/s after 0.5 s rest. Below 25% the fighter
      sags (balance/arms/legs × `Fatigue` 0.6–1). At 0 → `Condition.Fainted` (limp 2.2 s, gets up with 40%).
- [x] Step 4 — Footwork: discrete 0.3 s steps (holding repeats), planted feet with braking, automatic recovery
      steps when the body leans past the feet, knees bend with fatigue; knock-downs (tipped > 62° or head low, or a
      26+ hit) → `Condition.Down` 0.9 s → `GetUp` (assisted lift, 1.2 s steady grace). Get-up measured ≈ 1 s.
- [x] Step 5 — AI rewrite (`AIBrain`): plans Engage / Recover (stamina < 28%, back off until 60%) / Punish (opponent
      down or fainted); lunges in from mid range, chop / slash / thrust / spin, shield blocks or weapon parries,
      second-weapon thrusts, jumps out of a corner (max every 4 s).
- [x] Step 6 — Camera: clinch zoom (min ortho size 1.08 ≈ fighters fill most of the screen), always keeps the floor
      and both heads in view (zooms out for jumps). KO slow-mo moved into `GameFeel`.
- Measured (Hard AI vs Hard AI): rounds ≈ 40–50 s, ~95% of time standing, ~4% down, faints only after long
      exchanges; of all strike contacts ~75% too slow (ignored), ~15% blocked, ~6% real hits (`Weapon.Stat*` counters).
- [ ] Next: the user plays it; tune from their notes. Not verified with real mouse/keyboard/touch (Editor was
      unfocused) — buttons were driven through `OnScreenControls.Force` instead.

### Phase 1c — Realism upgrade (doc "Ragdoll Duel Realism Upgrade Prompts", branch `realism-upgrade`)
- [x] P1 audit → `Assets/Game/AUDIT.md`. P2 split `Fighter.cs` into `RagdollBody`, `BalanceController`,
      `LocomotionController`, `ArmController`, `StaminaController`, `KnockdownController` (+ `MotorDrive`, `PixelBounds`);
      `Fighter` runs them in a fixed order in FixedUpdate. Tuning = `[SerializeField]` fields with tooltips.
- [x] P3 physics: legs heavier than arms (1.8/1.2 vs arms 1.6/1.2 — arms stay heavy vs the weapon or joints jitter),
      angular damping 0.6, forearms + weapons Continuous, physics materials (feet 1.0, body 0.4, weapon 0.2, no bounce),
      start-pose-inside-limits warning, **fixed step 0.01** (`GameFeel.BaseFixedDelta`). Gravity left at 1 (fighters ≈1.8 u tall).
- [x] P4 balance: hip-height support force (legs push torso to 90% of spawn height, feet down by the same), `Muscle`
      0..1 that drops on hits (stagger) and recovers in 0.6 s; arms/legs/balance all × Fatigue × Muscle.
- [x] P5 movement: landing squash, 15% air control (steps / jump / lunge kept — user asked for them earlier).
- [x] P6 arms: no faked speed-by-mass; shoulder/elbow **torque** (70/45) has to accelerate the real weapon. Masses:
      dagger 0.45, sword 1, axe 1.6, mace 2, hammer 2.4, shield 1.5. Measured: sword peaks 0.63 s into the strike,
      hammer 0.87 s.
- [x] P7 hits: damage from collision **impulse** (`Weapon.ReferenceImpact` 5 N·s = listed damage, min 1.2), per-body-part
      cooldown 0.2 s, Light/Medium/Heavy buckets, knockback, stagger; `HitEvents` (Hit / Blocked) → `HitFeedback`
      (blood/dust/sparks, sound, hit-stop, camera trauma); bruises darken struck parts; F3 logs every hit.
- [x] P8 best of 3 (`MatchManager.roundsToWin`), match-over screen (Enter rematch). P9 trauma/Perlin shake as an offset,
      KO slow-mo zooms toward the final blow, blood Off/Low/Normal (B, default Low), stains fade after 10 s,
      whoosh when a blade passes 8 m/s, body-fall thud. P10 camera SmoothDamp on torsos.
- [x] P11 AI reacts to a delayed view of the opponent (`reactionTime × 0.6`: 0.36 / 0.21 / 0.11 s) and has aim error
      (`AIProfile.aimError` 0.3 / 0.15 / 0.07).
- Not done on purpose: hiding Ronin / extra weapons from the menu and disabling on-screen buttons on PC (the doc's
  scope suggestions conflict with earlier requests); per-fighter physics layers (IgnoreCollision already does this);
  a white hit flash (the unlit sprite material can't brighten, so the flash stays red).
- Measured after (Hard vs Hard, Knight vs Barbarian): ~36 s rounds, standing ~100% of the time, 36 hits avg 5.7 dmg.
- [x] Attack feel: chop/slash arcs are centred on the opponent's head/chest (`SwingMove.TargetAngle`), the body leans
      13° into a strike (`FighterInput.attacking` → `BalanceController.attackLean`) and takes a committed step in
      (`LocomotionController.commitStep*`) with a lunge (front leg reaches, back leg pushes). Blade hits make the
      victim flinch (`Balance.Flinch`: torso jerk + head snap); shield **bash** (press L/K near the opponent, AI does it
      when crowded) shoves the whole body (`Balance.Bashed`) without damage → `HitEvents.Bashed`. Visuals: impact star
      + streak (`ImpactFX`), bash ring, blade motion trails (TrailRenderer on the tip, on above 6 m/s). Stance widened,
      higher foot lift. Measured: ~40 s rounds, 16 bashes per round, fighters stayed standing.
- [x] User fix: **touching never hurts.** Only a hand that is attacking (`Fighter.Hand.Striking`, from
      `FighterInput.mainStriking/offStriking` = the driving part of a swing/bash, or a free-aimed weapon hand) can deal
      damage, and only with impulse ≥ `Weapon.MinImpact` 2. A **shield only blocks while held** (L/R/K/J button, or the
      AI blocking): lowered, it hangs at the side and its colliders become triggers (`Weapon.UpdateShieldGuard`).
      Measured (Hard vs Hard, 40 s): crowding without swings = 0 damage; 12 hits (avg impulse 4.7), 20 blocks.
- [x] User round 5 ("too few hits, boring"): **every touch of a striking part hurts again** — a swing
      (`Hand.Striking`) by impulse (share 0.25–1.8 of listed damage), any other touch by contact speed
      (`Weapon.TouchFullSpeed` 8 m/s, share 0.08–0.3; impulse would make a pushed heavy weapon hit like a swing).
      Weapon-on-weapon only counts as a block above the weapon's `minImpactSpeed` and 1.2 N·s.
      **Breakable armour** (`ArmorController`, per region): plate 80 / mail 55 / leather 32 durability; a blow wears
      it by its full damage while only part reaches the body (protection fades to 50% as it wears); at 0 it breaks →
      region becomes Skin (×1.15, bleeds), `HitEvents.ArmorBroken` → shards/sparks/clank/hit-stop, HUD notice.
      Worn armour sprites dull (`RagdollBody.Wear`); HUD shows 4 armour segments (head/torso/arms/legs) under stamina.
      Measured (Knight vs Barbarian, Hard, 60 s): rounds ~25–30 s, ~110 hits; Barbarian (axe + mace, 120 hp) wins
      most rounds — it did before this change too, so tune fighters/AI if the Knight feels weak.

### Phase 2 — Weapons & armour depth
- [ ] Armour slots (head/body/legs) that reduce damage, shown as overlay sprites
- [ ] Two-handed weapons (big swords can't use a shield), weapon weight affects swing speed
- [ ] Real hit sounds (placeholder synthesized ones are in `GameFeel`)
- [ ] Optional head knock-off on KO

### Phase 3 — Modes & menus (replace IMGUI placeholders with uGUI/UI Toolkit)
- [ ] Main menu, loadout screen
- [ ] Campaign: list of levels, escalating AI, 3 stars (speed + damage taken), bosses
- [ ] Practice mode, local 2-player on one device

### Phase 4 — Progression
- [ ] Coins, shop, unlocks, save data

### Phase 5 — Online multiplayer (hard — only after offline feels good)
- [ ] Unity Multiplayer Services / Netcode, server-authoritative, matchmaking, ranked

### Phase 6 — Monetisation & release
- [ ] IAP, ads, Android/iOS builds

## Project layout

```
Assets/Game/
  Scripts/Data/      FighterDefinition, WeaponDefinition, ArenaDefinition, AIProfile, GameDatabase
  Scripts/Fighter/   Fighter (coordinator: health, input, events), RagdollBody (build, mirror, ground), BalanceController,
                     LocomotionController, ArmController (IK), StaminaController, KnockdownController, ArmorController
                     (breakable armour), MotorDrive, PixelBounds,
                     BodyPart, Weapon (hit detection + damage), HandMoves (SwingMove, BlockAim), FighterInput,
                     PlayerBrain, AIBrain, ScriptBrain (tests)
  Scripts/Combat/    HitEvents (Hit / Blocked events), HitFeedback (effects, sound, hit-stop, camera trauma)
  Scripts/Match/     MatchManager (best-of-3 rounds + IMGUI HUD/select screen), OnScreenControls, GameFeel (time scale, sounds),
                     ArenaView, CameraRig, BloodFX (blood/sparks/dust), RainFX, HitboxOverlay
  Editor/            PixelArtImporter, ContentTools (add fighter/weapon/arena), PlaceholderArt, GameSceneBuilder
  Art/Fighters/<Name>/   head.png torso.png upper_arm.png lower_arm.png upper_leg.png lower_leg.png [cape.png]
  Art/Weapons/<name>.png
  Art/Arenas/<Name>/     sky.png far.png near.png ground.png
  Data/              GameDatabase.asset, Fighters/, Weapons/, Arenas/, AI/
  Scenes/Arena.unity     the only game scene (everything spawns from data at runtime)
Assets/Stickman/     earlier 3D stickman experiment — not part of the game
.claude/commands/    add-fighter.md, add-weapon.md, add-arena.md
```

## Pixel art conventions (what the importer expects)

- **64 pixels per unit**, Point filter, no compression — applied automatically to anything under `Assets/Game/Art/`.
- Draw every fighter **facing right**, standing straight, limbs hanging down. Transparent background.
  Style target: shaded, muted palette, 1px dark outline (see "Reference look" above).
- Fighter part sizes (px, W×H) — other sizes work, the rig adapts to the sprite size:
  `head 24×24`, `torso 24×36`, `upper_arm 10×20`, `lower_arm 10×20` (hand at the bottom),
  `upper_leg 12×24`, `lower_leg 12×24` (foot/boot at the bottom, toe pointing right),
  optional `cape 24×44`. One arm and one leg — they're reused for both sides (the back ones are tinted darker).
  Leave a couple of pixels of overlap at each joint edge; the rig overlaps parts so seams hide.
- Pivots are set by file name: head = bottom-centre (neck), torso = centre, limbs and cape = top-centre (the joint).
- Hitboxes are fitted to the **visible pixels** at runtime (fighter/weapon textures are imported readable), so
  transparent padding doesn't matter. For a new weapon, set `strikeFrom` (0–1 from the grip end) if the type
  default doesn't match where its blade/head starts; check with `H` in Play mode.
- Set each fighter's armour (`headArmor`, `torsoArmor`, `armArmor`, `legArmor`) to match what the art shows.
- Weapons are drawn **pointing up**, grip near the bottom (pivot bottom-centre; `gripPixels` on the asset
  says how far up the hand holds it); shields are centred. Sizes used: sword 12×52, axe 24×48, shield 24×36.
- Arena layers are **640×360** px: `sky` (opaque), `far`, `near` (transparent), `ground` (the strip the
  fighters stand on — its top edge is the floor, default 56 px up). The camera zooms in, so only part is visible at once.

## Adding content (use the Claude commands, or the menu)

- No art yet? `ContentTools.CreateFighterTemplate("<Name>")` copies the Knight's PNGs into a new folder to paint over.
- New fighter: put the 6 PNGs in `Assets/Game/Art/Fighters/<Name>/` → `/add-fighter <Name>`
  (menu: **Tools → Game → Create Fighter From Selected Folder**). Creates `Data/Fighters/<Name>.asset` and registers it.
- New weapon: `Assets/Game/Art/Weapons/<name>.png` → `/add-weapon <name> <type>`.
- New arena: 4 PNGs in `Assets/Game/Art/Arenas/<Name>/` → `/add-arena <Name>`.
- Code entry points (callable via `script eval`): `ContentTools.CreateFighter(folder)`,
  `ContentTools.CreateWeapon(pngPath, WeaponType)`, `ContentTools.CreateArena(folder)`.
- `Tools → Game → Regenerate Placeholder Art` re-creates the placeholder fighters/weapons/arena.
- `Tools → Game → Build Arena Scene` re-creates `Scenes/Arena.unity`.

## Design rules

- Gameplay never references a specific character/arena — everything comes from definitions in `GameDatabase`.
- Brains (`PlayerBrain`, `AIBrain`) only produce a `FighterInput`; `Fighter` applies it. New control
  schemes (touch, gamepad, network) = new brain, no fighter changes.
- A fighter's own colliders ignore each other (`Physics2D.IgnoreCollision`), so no layer setup is needed.
- Joints are driven by HingeJoint2D **motors** (stable); avoid raw high-gain torques on light parts.
- Never set positions/rotations of a live ragdoll part, except spawning and `RagdollBody.Mirror` (turning round).
- Combat code raises `HitEvents`; effects/sound/camera only listen (`HitFeedback`). Don't play effects from `Weapon`.

## Controls (PC / touch)

| Action | On-screen buttons (touch or mouse) | Keyboard | Gamepad |
|---|---|---|---|
| Right hand action (weapon: swing; shield: block) | **R** (hold = keep swinging) | `J` | right trigger |
| Left hand action (shield: block while held; weapon: swing) | **L** | `K` | left trigger |
| Step back / forward | **<** / **>** (screen direction) | `A` = back, `D` = forward (relative to facing) | d-pad |
| Jump | **JUMP** | `Space` / `W` | A / south |
| Lunge | — | `Shift` | B / east |
| Free aim (advanced) | — | hold left / right mouse: that hand reaches for the cursor | right / left stick |
| Hitboxes on/off | — | `H` | — |
| Blood Off / Low / Normal | — | `B` | — |
| Log every hit to the Console | — | `F3` | — |
| Restart / menu | — | `R` / `Esc` | — |

Swings alternate an overhead chop and a side slash. On a touchscreen, holding/dragging anywhere that isn't a
button also free-aims (right half = right hand), and a double-tap lunges.

On-screen buttons: `OnScreenControls` (IMGUI, added by `MatchManager` during fights; toggle `showOnScreenControls`).
The hand buttons say what they'll do ("Sword · swing", "Shield · block"). A press that starts on a button belongs
to the button, so it doesn't also aim the hand at the cursor. `OnScreenControls.Force(button, on)` holds a button
from code — used for tests.

Facing: when a fighter ends up on the other side of the opponent (jump-over, pushed past) for 0.12 s,
`Fighter.TurnAround()` mirrors the whole ragdoll (bodies, sprites, collider offsets, hinge anchors and limits;
weapon fixed joints are rebuilt) so both face each other again. Jumps tuck the legs; a jump into a raised weapon
still collides, so clearing the opponent depends on distance and their guard.

Damage only comes from a weapon's **striking part** (blade / head / fist). Every touch counts (≥ 0.5 m/s): a swing
(`Hand.Striking`) does damage × (impulse / 5 N·s, clamped 0.25–1.8), any other touch damage × (speed / 8 m/s,
clamped 0.08–0.3); then × body part (head 1.5) × fighter strength, and armour (`ArmorController`) soaks part of it
and wears down until it breaks. Heavier weapons swing harder at the same speed. A shield only blocks while held.
Weapon on weapon or shield = block (sparks, clank, blocker loses stamina). A helpless (down / fainted) fighter's
flailing never deals damage.
