---
description: Add a new background arena from four 320x180 pixel-art layers
argument-hint: <Name>
---

Add the arena **$ARGUMENTS** to the game. Read CLAUDE.md ("Pixel art conventions") first.

1. Check `Assets/Game/Art/Arenas/<Name>/` has `sky.png` (opaque) and `ground.png`, plus optional
   `far.png` / `near.png` (transparent). All **640×360** (64 px per unit). View them. The ground layer's top edge
   is the floor: measure how many pixels from the bottom the walkable surface is (default 56).
   If the folder is missing, offer to copy `Assets/Game/Art/Arenas/CastleField` as a template.
2. Editor connected and not in Play mode.
3. Eval file in `%TEMP%\stickman_cc\`, run with `unity command script eval_file --result-only <file>`:
   ```csharp
   var a = ContentTools.CreateArena("Assets/Game/Art/Arenas/<Name>");
   a.floorPixels = <measured floor>; UnityEditor.EditorUtility.SetDirty(a); UnityEditor.AssetDatabase.SaveAssets();
   return a.displayName;
   ```
4. Verify: set MatchManager `arenaIndex` to the new arena with `autoStart` on, play, check the fighters'
   feet stand on the drawn ground (capture `Temp/<Name>.png` and look), stop, set `autoStart` back off.
5. Report what was added.
