---
description: Add a new playable fighter from pixel art (or start a template to paint over)
argument-hint: <Name> [mainWeapon] [offWeapon]
---

Add the fighter **$ARGUMENTS** to the game. Read CLAUDE.md ("Pixel art conventions", "Adding content") first.

Arguments: `<Name> [mainWeapon] [offWeapon]` — weapons by name from `Assets/Game/Data/Weapons/`
(dagger, sword, axe, mace, hammer, shield, or any added later). Omit to keep sword + shield.

Steps:

1. Check `Assets/Game/Art/Fighters/<Name>/` has all six PNGs: `head`, `torso`, `upper_arm`, `lower_arm`,
   `upper_leg`, `lower_leg` (plus optional `cape`). Expected sizes at 64 px per unit: head 24×24, torso 24×36,
   arms 10×20, legs 12×24, cape 24×44 — other sizes work, but say so if they're far off.
   - **Folder missing?** Offer to create a template the user can paint over: run
     `ContentTools.CreateFighterTemplate("<Name>")` (copies the Knight's PNGs), tell them the folder and the
     part sizes, and stop there.
   - **Files present?** Read each PNG (view it) and check it against the conventions: facing right, transparent
     background, limbs hanging straight down with the joint at the top edge, hand/foot at the bottom.
     Report anything off (wrong facing, padding above a joint, huge sizes) before continuing.
2. Make sure the Editor is connected (`unity status`, state "ready"). If it is in Play mode, stop it first.
3. Write a short eval file in `%TEMP%\stickman_cc\` (short path!) and run it with
   `unity command script eval_file --result-only <file>`:
   ```csharp
   var def = ContentTools.CreateFighter("Assets/Game/Art/Fighters/<Name>", "<mainWeapon or null>", "<offWeapon or null>");
   return def.displayName + " main=" + (def.mainHand ? def.mainHand.displayName : "fist") + " off=" + (def.offHand ? def.offHand.displayName : "fist");
   ```
4. Set the armour to match the art (`PlaceholderArt.SetArmor("<Name>", head, torso, arms, legs)` with
   `ArmorMaterial.Skin/Cloth/Leather/Mail/Plate`) — say what you chose. Ask whether to tweak stats on
   `Assets/Game/Data/Fighters/<Name>.asset` (maxHealth, strength, walkSpeed, weight, endurance, backLimbTint).
5. Verify (see CLAUDE.md "Quick test"): enter Play mode with Run In Background on, then via eval set the
   MatchManager's `playerIndex` to the new fighter and call `StartMatch()`. Wait ~3 s and check the fighter's
   torso is roughly upright and about 0.9 units above the floor. Capture the game view to `Temp/<Name>.png`
   and look at it. Then stop Play mode, turn Run In Background off, and delete `Assets/Temp`.
6. Tick or add the fighter in CLAUDE.md if relevant, and report what was added.
