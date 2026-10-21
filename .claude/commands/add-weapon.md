---
description: Add a new weapon or shield from a pixel-art PNG
argument-hint: <name> <Dagger|Sword|Axe|Mace|Hammer|Shield> [fighter to equip it on]
---

Add the weapon **$ARGUMENTS** to the game. Read CLAUDE.md ("Pixel art conventions") first.

1. Check `Assets/Game/Art/Weapons/<name>.png` exists. View it: it must point **up** with the grip at the
   bottom (shields: centred, front face). If missing, offer to draw a placeholder by adding a `Draw<Name>`
   method to `Assets/Game/Editor/PlaceholderArt.cs` modelled on the existing ones.
2. Editor connected and not in Play mode (`unity status`).
3. Eval file in `%TEMP%\stickman_cc\`, run with `unity command script eval_file --result-only <file>`:
   ```csharp
   var w = ContentTools.CreateWeapon("Assets/Game/Art/Weapons/<name>.png", WeaponType.<Type>);
   return w.displayName + " dmg=" + w.damage + " mass=" + w.mass;
   ```
   Type defaults come from `ContentTools.ApplyTypeDefaults`; adjust `damage`, `mass`, `gripAngle`,
   `gripPixels` (pixels from the bottom where the hand holds it) on the asset if the user wants.
4. If a fighter was named, set its `mainHand` (or `offHand` for shields) to the new weapon.
5. Verify in Play mode as in `/add-fighter` step 5 (the weapon should sit in the hand, not float), then report.
