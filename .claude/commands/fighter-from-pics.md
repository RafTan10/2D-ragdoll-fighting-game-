---
description: Turn photos of a person (or an art pack made from them) into a playable fighter, following the style guide
argument-hint: <Name> <photos or art-pack path> [main weapon] [off-hand item]
---

Make the fighter described by **$ARGUMENTS**. Follow `docs/Ragdoll_Style_Guide.md` exactly (read all of it first),
plus CLAUDE.md ("Pixel art conventions", "Adding content").

1. **Look at what was given** (view every image).
   - **Photos only** → write the likeness notes (guide section 2) and suggest armour, stats and loadout
     (sections 7–8). Show them to the user and agree before drawing.
   - **An art pack** (zip or folder of part images, like `SourceArt/Nuha/`) → unzip it to `%TEMP%`, never into
     `Assets/` (packs may contain their own Unity scripts). Check it against the guide (view, joints, sizes,
     gradients, outline thickness) and tell the user what you'll have to adapt.
2. **Make the parts.**
   - Drawing them yourself: write a small script in `SourceArt/<Name>/` that draws each part as pixels at the final
     sizes (guide section 4), or at 10× and converts. Keep the script, so the art can be regenerated.
   - Converting a pack: copy the pack into `SourceArt/<Name>/` and write `SourceArt/<Name>/convert.ps1` on top of
     `SourceArt/tools/PixelParts.ps1` (see `SourceArt/Nuha/convert.ps1`): crop every part so its joint sits on the
     edge the rig expects, map long hair → `cape`, anything below the hips → `skirt`, a carried item → `hand_prop`,
     and pick the scale so the figure is ~110 px tall (neck ≈ 78 px above the feet).
   - Output goes to `Assets/Game/Art/Fighters/<Name>/`. Build an enlarged contact sheet of the parts and look at it.
3. **Create the fighter** (Editor connected, not in Play mode): `AssetDatabase.Refresh()`, then
   `ContentTools.CreateFighter("Assets/Game/Art/Fighters/<Name>", "<main weapon>", "<off weapon or null>")`. Set
   `offHand = null` explicitly for a bare / `hand_prop` off hand (null keeps the default shield on a new fighter).
   Set armour with `PlaceholderArt.SetArmor(...)` and the stats on the asset.
4. **Test** (CLAUDE.md "Quick test"): play a Hard AI vs Hard AI round against at least the Knight and the
   Barbarian. Check the pose (torso ≈ 0.9 units up, upright), that props and capes stay attached after a turn-around
   and a knock-down, and capture a screenshot to look at. If the fighter loses every round in under ~15 s, tune the
   stats (guide section 8) and test again.
5. Stop Play mode, Run In Background off, delete `Assets/Temp`. Add the fighter to CLAUDE.md's roadmap and report
   what was made, what was adapted from the source, and the test results.
