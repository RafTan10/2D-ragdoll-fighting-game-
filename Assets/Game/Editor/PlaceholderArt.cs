using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws the placeholder pixel art (3 fighters with capes, 6 weapons, 1 arena) at 64 px per unit:
/// shaded materials (plate, chainmail, cloth, fur, lacquer), dark outlines and a muted, overcast palette.
/// Replace any PNG with real art and re-run the matching ContentTools command — definitions keep their stats.
/// </summary>
public static class PlaceholderArt
{
    const string Art = "Assets/Game/Art";

    [MenuItem("Tools/Game/Regenerate Placeholder Art")]
    public static void GenerateAll()
    {
        // Weapons first so fighters can reference them.
        SaveWeapon("dagger", DrawDagger(), WeaponType.Dagger, grip: 4);
        SaveWeapon("sword", DrawSword(), WeaponType.Sword, grip: 6);
        SaveWeapon("axe", DrawAxe(), WeaponType.Axe, grip: 4);
        SaveWeapon("mace", DrawMace(), WeaponType.Mace, grip: 4);
        SaveWeapon("hammer", DrawHammer(), WeaponType.Hammer, grip: 4);
        SaveWeapon("shield", DrawShield(), WeaponType.Shield, grip: 0);

        DrawKnight(Art + "/Fighters/Knight");
        DrawBarbarian(Art + "/Fighters/Barbarian");
        DrawRonin(Art + "/Fighters/Ronin");
        DrawArena(Art + "/Arenas/CastleField");
        AssetDatabase.Refresh();

        ContentTools.CreateFighter(Art + "/Fighters/Knight", "Sword", "Shield");
        ContentTools.CreateFighter(Art + "/Fighters/Barbarian", "Axe", "Mace");
        ContentTools.CreateFighter(Art + "/Fighters/Ronin", "Sword", "Dagger");
        SetStats("Knight", health: 100f, strength: 1f, walk: 2.1f, weight: 1f, tint: new Color(0.78f, 0.76f, 0.74f));
        SetStats("Barbarian", health: 120f, strength: 1.15f, walk: 1.9f, weight: 1.15f, tint: new Color(0.76f, 0.74f, 0.72f));
        SetStats("Ronin", health: 90f, strength: 1f, walk: 2.5f, weight: 0.9f, tint: new Color(0.76f, 0.74f, 0.74f));
        SetArmor("Knight", ArmorMaterial.Plate, ArmorMaterial.Plate, ArmorMaterial.Plate, ArmorMaterial.Plate);
        SetArmor("Barbarian", ArmorMaterial.Mail, ArmorMaterial.Mail, ArmorMaterial.Skin, ArmorMaterial.Cloth);
        SetArmor("Ronin", ArmorMaterial.Plate, ArmorMaterial.Plate, ArmorMaterial.Leather, ArmorMaterial.Cloth);

        var arena = ContentTools.CreateArena(Art + "/Arenas/CastleField");
        arena.displayName = "Forest Road";
        arena.floorPixels = 56f;
        arena.fogColor = new Color(0.55f, 0.53f, 0.4f);
        arena.rain = true;
        EditorUtility.SetDirty(arena);
        AssetDatabase.SaveAssets();
        Debug.Log("Placeholder art generated.");
    }

    static void SaveWeapon(string name, Canvas c, WeaponType type, float grip)
    {
        c.Save(Art + "/Weapons/" + name + ".png");
        AssetDatabase.Refresh();
        var w = ContentTools.CreateWeapon(Art + "/Weapons/" + name + ".png", type);
        w.gripPixels = grip;
        EditorUtility.SetDirty(w);
    }

    public static void SetArmor(string fighter, ArmorMaterial head, ArmorMaterial torso, ArmorMaterial arms, ArmorMaterial legs)
    {
        var def = AssetDatabase.LoadAssetAtPath<FighterDefinition>(ContentTools.DataRoot + "/Fighters/" + fighter + ".asset");
        def.headArmor = head; def.torsoArmor = torso; def.armArmor = arms; def.legArmor = legs;
        EditorUtility.SetDirty(def);
        AssetDatabase.SaveAssets();
    }

    static void SetStats(string fighter, float health, float strength, float walk, float weight, Color tint)
    {
        var def = AssetDatabase.LoadAssetAtPath<FighterDefinition>(ContentTools.DataRoot + "/Fighters/" + fighter + ".asset");
        def.maxHealth = health; def.strength = strength; def.walkSpeed = walk; def.weight = weight; def.backLimbTint = tint;
        EditorUtility.SetDirty(def);
        AssetDatabase.SaveAssets();
    }

    // ================================================================ palette

    static readonly Color Ink = new Color(0.11f, 0.08f, 0.07f);

    /// <summary>5-step ramp: 0 darkest, 1 shadow, 2 base, 3 light, 4 highlight. Shadows drift blue, lights drift yellow.</summary>
    static Color[] R(float r, float g, float b)
    {
        Color.RGBToHSV(new Color(r, g, b), out float h, out float s, out float v);
        Color At(float hueTarget, float hueAmount, float ds, float vm) =>
            Color.HSVToRGB(Mathf.Repeat(h + Mathf.DeltaAngle(h * 360f, hueTarget * 360f) / 360f * hueAmount, 1f),
                           Mathf.Clamp01(s + ds), Mathf.Clamp01(v * vm));
        return new[]
        {
            At(0.7f, 0.12f, 0.12f, 0.36f),
            At(0.7f, 0.06f, 0.07f, 0.64f),
            new Color(r, g, b),
            At(0.12f, 0.05f, -0.06f, 1.2f),
            At(0.12f, 0.1f, -0.2f, 1.42f),
        };
    }

    static readonly Color[] Skin = R(0.82f, 0.62f, 0.48f);
    static readonly Color[] Plate = R(0.74f, 0.68f, 0.52f);       // cream-gold plate, like the reference
    static readonly Color[] Steel = R(0.6f, 0.61f, 0.6f);
    static readonly Color[] Chain = R(0.5f, 0.51f, 0.52f);
    static readonly Color[] Gold = R(0.74f, 0.58f, 0.26f);
    static readonly Color[] Leather = R(0.4f, 0.27f, 0.16f);
    static readonly Color[] Wood = R(0.44f, 0.3f, 0.18f);
    static readonly Color[] RedCloth = R(0.56f, 0.13f, 0.11f);
    static readonly Color[] Fur = R(0.4f, 0.29f, 0.19f);
    static readonly Color[] Wool = R(0.42f, 0.38f, 0.29f);
    static readonly Color[] Lacquer = R(0.58f, 0.14f, 0.12f);
    static readonly Color[] BlackLacquer = R(0.2f, 0.18f, 0.2f);
    static readonly Color[] Navy = R(0.2f, 0.22f, 0.32f);
    static readonly Color[] HairBrown = R(0.3f, 0.19f, 0.11f);
    static readonly Color[] HairRed = R(0.55f, 0.27f, 0.12f);
    static readonly Color[] HairBlack = R(0.12f, 0.11f, 0.12f);
    static readonly Color[] White = R(0.82f, 0.8f, 0.74f);

    // ================================================================ shared body pieces

    static void Face(Canvas c, Color[] hair, bool beard, bool bigMustache)
    {
        c.Fill(Canvas.Rect(9, 0, 14, 5), Skin, Canvas.CylV(9, 14));                    // neck
        c.Fill(Canvas.Ellipse(12.5f, 11f, 6.5f, 7.5f), Skin, Canvas.Sphere(14f, 13f, 9f)); // head
        c.Fill(Canvas.Ellipse(7.5f, 11f, 1.6f, 2.2f), Skin, Canvas.Flat(0.25f));        // ear
        c.Fill(Canvas.Rect(18, 9, 19, 12), Skin, Canvas.Flat(0.6f));                    // nose
        c.Set(19, 9, Skin[1]);
        // eyes (3/4 view: near eye big, far eye small)
        c.Set(15, 12, White[3]); c.Set(16, 12, Ink); c.Set(11, 12, Ink);
        c.Line(14, 14, 17, 14, hair[1]); c.Line(10, 14, 12, 14, hair[1]);              // brows
        c.Line(15, 6, 17, 6, Skin[0]);                                                  // mouth
        if (beard)
        {
            c.Fill(Canvas.Ellipse(14f, 4.5f, 6.5f, 5f), hair, Canvas.Sphere(15f, 6f, 7f), 0.15f);
            c.Fill(Canvas.Rect(10, 1, 19, 4), hair, Canvas.Sphere(15f, 3f, 8f), 0.15f);
        }
        if (bigMustache)
        {
            c.Fill(Canvas.Rect(13, 7, 20, 8), hair, Canvas.Flat(0.5f));
            c.Set(13, 6, hair[1]); c.Set(12, 5, hair[1]); c.Set(20, 6, hair[1]); c.Set(21, 5, hair[1]);
        }
    }

    // Standard limb masks: arms 10x20, legs 12x24, joint at the top edge.
    static Func<int, int, bool> ArmMask => Canvas.Rect(2, 0, 7, 19);
    static Func<int, int, bool> ForearmMask => Canvas.Rect(2, 6, 7, 19);
    static Func<int, int, bool> FistMask => Canvas.Or(Canvas.Rect(2, 0, 8, 6), Canvas.Rect(8, 1, 8, 4));
    static Func<int, int, bool> ThighMask => Canvas.Rect(2, 0, 9, 23);
    static Func<int, int, bool> ShinMask => Canvas.Rect(2, 6, 9, 23);
    static Func<int, int, bool> BootMask => Canvas.Or(Canvas.Rect(2, 0, 10, 7), Canvas.Rect(10, 0, 11, 3));

    static Canvas Cape(Color[] cloth, bool ragged, float noise)
    {
        var c = new Canvas(24, 44);
        Func<int, int, bool> shape = (x, y) =>
        {
            float t = y / 43f;                                     // 0 bottom, 1 top
            float half = Mathf.Lerp(10f, 5f, t);
            float hem = 1.5f + Mathf.Sin(x * 0.9f) * 1.2f + (ragged ? Mathf.Abs(Mathf.Sin(x * 2.3f)) * 3f : 0f);
            return Mathf.Abs(x - 11.5f) <= half && y >= hem && y <= 42;
        };
        c.Fill(shape, cloth, (x, y) => 0.45f + 0.35f * Mathf.Sin(x * 0.85f + y * 0.04f) - (42 - y) * 0f, noise);
        c.Fill((x, y) => shape(x, y) && y > 38, cloth, Canvas.Flat(0.3f)); // collar shadow
        return c.Outline(Ink);
    }

    // ================================================================ Knight: cream plate, red tabard & cape

    static void DrawKnight(string dir)
    {
        var head = new Canvas(24, 24);
        // aventail (mail around neck/back of head) first, face over it
        head.FillPattern(Canvas.Or(Canvas.Rect(4, 1, 9, 15), Canvas.Rect(8, 0, 15, 3)), Chain, Canvas.CylV(4, 15), Canvas.ChainPattern);
        Face(head, HairBrown, beard: false, bigMustache: true);
        head.Fill((x, y) => Canvas.Ellipse(11.5f, 16f, 8.5f, 7f)(x, y) && y >= 14, Plate, Canvas.Sphere(9f, 21f, 9f));
        head.Line(4, 14, 19, 14, Plate[1]);                                       // brim
        head.Fill(Canvas.Ellipse(5f, 21.5f, 3.2f, 1.8f), RedCloth, Canvas.Sphere(4f, 22f, 3f)); // plume
        head.Fill(Canvas.Rect(11, 21, 12, 23), Gold, Canvas.Flat(0.7f));          // crest knob
        head.Outline(Ink).Save(dir + "/head.png");

        var torso = new Canvas(24, 36);
        torso.FillPattern(Canvas.Rect(3, 1, 20, 12), Chain, Canvas.CylV(3, 20), Canvas.ChainPattern); // mail skirt
        torso.Fill(Canvas.Or(Canvas.Rect(4, 13, 19, 31), Canvas.Rect(3, 22, 20, 30)), Plate, Canvas.CylV(3, 20));
        for (int y = 15; y <= 21; y += 3) { torso.Line(4, y, 19, y, Plate[1]); torso.Line(4, y - 1, 19, y - 1, Plate[3]); }
        torso.Fill(Canvas.Rect(7, 31, 16, 34), Plate, Canvas.CylV(7, 16));       // gorget
        torso.Fill(Canvas.Rect(8, 2, 15, 21), RedCloth, Canvas.CylV(8, 15), 0.05f); // tabard
        torso.Line(8, 2, 8, 21, Gold[2]); torso.Line(15, 2, 15, 21, Gold[2]);
        torso.Fill(Canvas.Rect(4, 11, 19, 12), Leather, Canvas.Flat(0.4f));       // belt
        torso.Fill(Canvas.Rect(11, 11, 12, 12), Gold, Canvas.Flat(0.8f));
        torso.Outline(Ink).Save(dir + "/torso.png");

        var upperArm = new Canvas(10, 20);
        upperArm.Fill(ArmMask, Plate, Canvas.CylV(2, 7));
        upperArm.Fill(Canvas.Ellipse(4.5f, 15.5f, 4.2f, 3.8f), Plate, Canvas.Sphere(3f, 18f, 5f)); // pauldron
        upperArm.Line(1, 14, 8, 14, Plate[1]); upperArm.Line(2, 7, 7, 7, Plate[1]);
        upperArm.Outline(Ink).Save(dir + "/upper_arm.png");

        var lowerArm = new Canvas(10, 20);
        lowerArm.Fill(ForearmMask, Plate, Canvas.CylV(2, 7));
        lowerArm.Fill(Canvas.Ellipse(4.5f, 17f, 3.4f, 2.6f), Plate, Canvas.Sphere(3f, 18f, 4f)); // couter
        lowerArm.Fill(FistMask, Steel, Canvas.Sphere(4f, 4f, 5f));                // gauntlet
        lowerArm.Line(3, 2, 7, 2, Steel[1]); lowerArm.Line(2, 6, 7, 6, Steel[1]);
        lowerArm.Outline(Ink).Save(dir + "/lower_arm.png");

        var upperLeg = new Canvas(12, 24);
        upperLeg.FillPattern(Canvas.Rect(2, 15, 9, 23), Chain, Canvas.CylV(2, 9), Canvas.ChainPattern);
        upperLeg.Fill(Canvas.Rect(2, 3, 9, 15), Plate, Canvas.CylV(2, 9));
        upperLeg.Fill(Canvas.Ellipse(6f, 2.5f, 4f, 2.8f), Plate, Canvas.Sphere(4.5f, 4f, 4f)); // knee cop
        upperLeg.Outline(Ink).Save(dir + "/upper_leg.png");

        var lowerLeg = new Canvas(12, 24);
        lowerLeg.Fill(ShinMask, Plate, Canvas.CylV(2, 9));
        lowerLeg.Fill(BootMask, Steel, Canvas.CylH(0, 7));
        for (int x = 4; x <= 10; x += 3) lowerLeg.Line(x, 0, x, 4, Steel[1]);       // sabaton lames
        lowerLeg.Line(2, 15, 9, 15, Plate[3]);
        lowerLeg.Outline(Ink).Save(dir + "/lower_leg.png");

        Cape(RedCloth, ragged: false, noise: 0.04f).Save(dir + "/cape.png");
    }

    // ================================================================ Barbarian: mail, fur, nasal helm, big beard

    static void DrawBarbarian(string dir)
    {
        var head = new Canvas(24, 24);
        head.Fill(Canvas.Rect(3, 3, 9, 17), HairRed, Canvas.CylV(3, 9), 0.25f);        // long hair at the back
        Face(head, HairRed, beard: true, bigMustache: true);
        head.Fill(Canvas.Poly((4, 15), (20, 15), (14, 23), (10, 23)), Steel, Canvas.Sphere(9f, 21f, 9f));
        head.Fill(Canvas.Rect(4, 14, 20, 15), Gold, Canvas.CylV(4, 20));             // bronze rim
        head.Fill(Canvas.Rect(17, 9, 18, 14), Steel, Canvas.Flat(0.7f));             // nasal
        head.Outline(Ink).Save(dir + "/head.png");

        var torso = new Canvas(24, 36);
        torso.FillPattern(Canvas.Or(Canvas.Rect(3, 1, 20, 30), Canvas.Rect(2, 22, 21, 30)), Chain, Canvas.CylV(2, 21), Canvas.ChainPattern);
        torso.Fill(Canvas.Or(Canvas.Rect(1, 27, 22, 34), Canvas.Ellipse(11.5f, 27f, 10f, 3f)), Fur, Canvas.CylH(26, 34), 0.3f);
        torso.Fill(Canvas.Rect(3, 9, 20, 11), Leather, Canvas.Flat(0.45f));
        torso.Fill(Canvas.Rect(10, 9, 13, 11), Gold, Canvas.Flat(0.7f));
        torso.Line(5, 12, 17, 26, Leather[1]); torso.Line(6, 12, 18, 26, Leather[2]);  // strap
        torso.Outline(Ink).Save(dir + "/torso.png");

        var upperArm = new Canvas(10, 20);
        upperArm.Fill(ArmMask, Skin, Canvas.CylV(2, 7));
        upperArm.Fill(Canvas.Rect(1, 13, 8, 19), Fur, Canvas.CylH(13, 19), 0.3f);   // fur shoulder
        upperArm.Fill(Canvas.Rect(2, 6, 7, 7), Gold, Canvas.CylV(2, 7));             // arm ring
        upperArm.Outline(Ink).Save(dir + "/upper_arm.png");

        var lowerArm = new Canvas(10, 20);
        lowerArm.Fill(ForearmMask, Skin, Canvas.CylV(2, 7));
        lowerArm.Fill(Canvas.Rect(2, 6, 7, 13), Leather, Canvas.CylV(2, 7), 0.1f);  // bracer
        lowerArm.Line(2, 9, 7, 9, Leather[0]);
        lowerArm.Fill(FistMask, Skin, Canvas.Sphere(4f, 4f, 5f));
        lowerArm.Line(4, 1, 4, 4, Skin[1]); lowerArm.Line(6, 1, 6, 4, Skin[1]);
        lowerArm.Outline(Ink).Save(dir + "/lower_arm.png");

        var upperLeg = new Canvas(12, 24);
        upperLeg.Fill(ThighMask, Wool, Canvas.CylV(2, 9), 0.08f);
        upperLeg.FillPattern(Canvas.Rect(2, 17, 9, 23), Chain, Canvas.CylV(2, 9), Canvas.ChainPattern); // mail hem
        upperLeg.Outline(Ink).Save(dir + "/upper_leg.png");

        var lowerLeg = new Canvas(12, 24);
        lowerLeg.Fill(ShinMask, Wool, Canvas.CylV(2, 9), 0.08f);
        for (int y = 8; y < 22; y += 4) { lowerLeg.Line(2, y, 9, y + 2, Leather[1]); lowerLeg.Line(2, y + 2, 9, y, Leather[1]); } // cross-gartering
        lowerLeg.Fill(BootMask, Leather, Canvas.CylH(0, 7));
        lowerLeg.Fill(Canvas.Rect(2, 6, 10, 8), Fur, Canvas.Flat(0.5f), 0.3f);
        lowerLeg.Outline(Ink).Save(dir + "/lower_leg.png");

        Cape(R(0.27f, 0.31f, 0.21f), ragged: true, noise: 0.08f).Save(dir + "/cape.png"); // worn green cloak
    }

    // ================================================================ Ronin: red lamellar, black kabuto, hakama

    static void DrawRonin(string dir)
    {
        var head = new Canvas(24, 24);
        // shikoro (neck guard) behind the face
        head.Fill(Canvas.Rect(3, 6, 10, 16), BlackLacquer, Canvas.CylV(3, 10));
        for (int y = 8; y <= 14; y += 3) head.Line(3, y, 10, y, Lacquer[1]);
        Face(head, HairBlack, beard: false, bigMustache: false);
        head.Line(15, 8, 19, 8, HairBlack[2]); head.Set(14, 7, HairBlack[2]); head.Set(15, 3, HairBlack[2]); head.Set(15, 2, HairBlack[2]);
        head.Fill((x, y) => Canvas.Ellipse(12f, 16.5f, 8.5f, 5.5f)(x, y) && y >= 15, BlackLacquer, Canvas.Sphere(9f, 20f, 9f));
        head.Fill(Canvas.Rect(2, 14, 21, 15), BlackLacquer, Canvas.Flat(0.55f));     // brim
        head.Fill(Canvas.Rect(2, 15, 5, 18), Lacquer, Canvas.Flat(0.55f));           // fukigaeshi
        for (int i = 0; i < 6; i++) { head.Set(9 + i, 22 + (i == 0 || i == 5 ? 1 : 0), Gold[3]); } // crescent crest
        head.Set(8, 23, Gold[2]); head.Set(15, 23, Gold[2]);
        head.Outline(Ink).Save(dir + "/head.png");

        var torso = new Canvas(24, 36);
        torso.Fill(Canvas.Rect(4, 1, 19, 8), Navy, Canvas.CylV(4, 19));              // hakama top
        for (int x = 6; x <= 18; x += 4) torso.Line(x, 1, x, 7, Navy[1]);
        torso.Fill(Canvas.Or(Canvas.Rect(4, 11, 19, 31), Canvas.Rect(3, 20, 20, 30)), Lacquer, Canvas.CylV(3, 20));
        for (int y = 13; y <= 29; y += 4)
        {
            torso.Line(3, y, 20, y, Lacquer[0]);
            for (int x = 5; x <= 19; x += 3) torso.Set(x, y + 1, Gold[3]);           // lacing knots
        }
        torso.Fill(Canvas.Rect(4, 8, 19, 10), Navy, Canvas.Flat(0.3f));              // obi
        torso.Fill(Canvas.Rect(8, 31, 15, 34), BlackLacquer, Canvas.CylV(8, 15));     // collar
        torso.Outline(Ink).Save(dir + "/torso.png");

        var upperArm = new Canvas(10, 20);
        upperArm.Fill(ArmMask, Navy, Canvas.CylV(2, 7));
        upperArm.Fill(Canvas.Rect(1, 7, 8, 19), Lacquer, Canvas.CylV(1, 8));          // sode
        for (int y = 9; y <= 17; y += 4) { upperArm.Line(1, y, 8, y, Lacquer[0]); upperArm.Set(4, y + 1, Gold[3]); }
        upperArm.Outline(Ink).Save(dir + "/upper_arm.png");

        var lowerArm = new Canvas(10, 20);
        lowerArm.FillPattern(ForearmMask, Chain, Canvas.CylV(2, 7), Canvas.ChainPattern);
        lowerArm.Fill(Canvas.Rect(2, 7, 7, 15), BlackLacquer, Canvas.CylV(2, 7));       // kote plate
        lowerArm.Fill(FistMask, BlackLacquer, Canvas.Sphere(4f, 4f, 5f));
        lowerArm.Outline(Ink).Save(dir + "/lower_arm.png");

        var upperLeg = new Canvas(12, 24);
        upperLeg.Fill(Canvas.Rect(1, 0, 10, 23), Navy, Canvas.CylV(1, 10));            // wide hakama
        for (int x = 3; x <= 9; x += 3) upperLeg.Line(x, 0, x, 23, Navy[1]);
        upperLeg.Fill(Canvas.Rect(2, 14, 9, 23), Lacquer, Canvas.CylV(2, 9));          // haidate
        for (int y = 16; y <= 22; y += 3) upperLeg.Line(2, y, 9, y, Lacquer[0]);
        upperLeg.Outline(Ink).Save(dir + "/upper_leg.png");

        var lowerLeg = new Canvas(12, 24);
        lowerLeg.Fill(Canvas.Rect(1, 13, 10, 23), Navy, Canvas.CylV(1, 10));
        for (int x = 3; x <= 9; x += 3) lowerLeg.Line(x, 13, x, 23, Navy[1]);
        lowerLeg.Fill(Canvas.Rect(3, 6, 8, 12), BlackLacquer, Canvas.CylV(3, 8));      // suneate
        lowerLeg.Fill(BootMask, White, Canvas.CylH(0, 7));                              // tabi
        lowerLeg.Fill(Canvas.Rect(2, 0, 11, 1), Wood, Canvas.Flat(0.6f));             // sandal
        lowerLeg.Outline(Ink).Save(dir + "/lower_leg.png");
        // No cape for the Ronin; remove one left over from an earlier generation.
        AssetDatabase.DeleteAsset(dir + "/cape.png");
    }

    // ================================================================ weapons (pointing up, grip at the bottom)

    static Canvas DrawSword()
    {
        var c = new Canvas(12, 52);
        c.Fill(Canvas.Ellipse(5.5f, 2f, 2.2f, 1.6f), Gold, Canvas.Sphere(5f, 3f, 3f));
        c.Fill(Canvas.Rect(5, 3, 6, 9), Leather, Canvas.CylV(5, 6));
        for (int y = 4; y <= 8; y += 2) c.Line(5, y, 6, y, Leather[0]);
        c.Fill(Canvas.Rect(1, 10, 10, 11), Gold, Canvas.CylH(10, 11));
        c.Fill(Canvas.Or(Canvas.Rect(4, 12, 7, 46), Canvas.Poly((4, 46), (7, 46), (5.5f, 51))), Steel, Canvas.CylV(4, 7));
        c.Line(5, 14, 5, 42, Steel[1]);
        return c.Outline(Ink);
    }

    static Canvas DrawDagger()
    {
        var c = new Canvas(10, 24);
        c.Fill(Canvas.Ellipse(4.5f, 1.5f, 1.6f, 1.2f), Gold, Canvas.Flat(0.7f));
        c.Fill(Canvas.Rect(4, 2, 5, 6), Leather, Canvas.CylV(4, 5));
        c.Fill(Canvas.Rect(1, 7, 8, 7), Gold, Canvas.Flat(0.6f));
        c.Fill(Canvas.Or(Canvas.Rect(3, 8, 6, 19), Canvas.Poly((3, 19), (6, 19), (4.5f, 23))), Steel, Canvas.CylV(3, 6));
        return c.Outline(Ink);
    }

    static Canvas DrawAxe()
    {
        var c = new Canvas(24, 48);
        c.Fill(Canvas.Rect(10, 1, 12, 44), Wood, Canvas.CylV(10, 12));
        c.Fill(Canvas.Rect(10, 1, 12, 7), Leather, Canvas.CylV(10, 12));
        c.Fill(Canvas.Poly((12, 34), (12, 44), (17, 43), (22, 46), (22, 28), (17, 32)), Steel, (x, y) => 0.3f + (x - 12) * 0.06f);
        c.Line(21, 29, 21, 45, Steel[4]);
        c.Fill(Canvas.Poly((10, 38), (10, 42), (6, 40)), Steel, Canvas.Flat(0.4f));   // back spike
        return c.Outline(Ink);
    }

    static Canvas DrawMace()
    {
        var c = new Canvas(18, 40);
        c.Fill(Canvas.Rect(8, 1, 9, 29), Wood, Canvas.CylV(8, 9));
        c.Fill(Canvas.Rect(7, 1, 10, 7), Leather, Canvas.CylV(7, 10));
        c.Fill(Canvas.Ellipse(8.5f, 33f, 4.5f, 5f), Steel, Canvas.Sphere(7f, 35f, 5f));
        foreach (var (x0, x1) in new[] { (1, 4), (13, 16) }) c.Fill(Canvas.Rect(x0, 31, x1, 35), Steel, Canvas.Flat(0.45f)); // flanges
        c.Fill(Canvas.Rect(8, 38, 9, 39), Steel, Canvas.Flat(0.8f));
        return c.Outline(Ink);
    }

    static Canvas DrawHammer()
    {
        var c = new Canvas(24, 44);
        c.Fill(Canvas.Rect(10, 1, 12, 36), Wood, Canvas.CylV(10, 12));
        c.Fill(Canvas.Rect(10, 1, 12, 7), Leather, Canvas.CylV(10, 12));
        c.Fill(Canvas.Rect(5, 34, 19, 41), Steel, Canvas.CylH(34, 41));
        c.Fill(Canvas.Rect(19, 33, 22, 42), Steel, Canvas.Flat(0.65f));               // striking face
        c.Fill(Canvas.Poly((5, 35), (5, 40), (1, 38)), Steel, Canvas.Flat(0.4f));      // back spike
        return c.Outline(Ink);
    }

    static Canvas DrawShield()
    {
        // Tall red shield with a gold border and lattice, like the reference.
        var c = new Canvas(24, 36);
        Func<int, int, bool> body = (x, y) => x >= 1 && x <= 22 && y >= 1 && y <= 34 && !(Mathf.Abs(x - 11.5f) > 8.5f && (y < 4 || y > 31));
        c.Fill(body, RedCloth, Canvas.Sphere(8f, 26f, 22f), 0.04f);
        for (int y = 0; y < 36; y++)
            for (int x = 0; x < 24; x++)
                if (body(x, y) && ((x + y) % 7 == 0 || (x - y + 70) % 7 == 0)) c.Set(x, y, RedCloth[1]);
        c.Fill((x, y) => body(x, y) && !(x >= 3 && x <= 20 && y >= 3 && y <= 32 && !(Mathf.Abs(x - 11.5f) > 6.5f && (y < 6 || y > 29))), Gold, Canvas.Sphere(8f, 26f, 22f));
        c.Fill(Canvas.Ellipse(11.5f, 17.5f, 3f, 3f), Gold, Canvas.Sphere(10.5f, 18.5f, 3f));
        return c.Outline(Ink);
    }

    // ================================================================ arena (640x360 layers, floor 56 px up)

    static void DrawArena(string dir)
    {
        var rng = new System.Random(11);
        const int W = 640, H = 360, Floor = 56;

        // Sky: overcast olive-beige gradient with soft dithered cloud bands.
        var sky = new Canvas(W, H);
        Color horizon = new Color(0.72f, 0.68f, 0.52f), top = new Color(0.47f, 0.47f, 0.38f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float t = y / (float)H;
                float band = Mathf.Sin(y * 0.09f + Mathf.Sin(x * 0.012f) * 2f) * 0.5f + 0.5f;
                float k = Mathf.Clamp01(t + (band > 0.8f ? -0.05f : 0f));
                float steps = 14f;
                float q = Mathf.Floor(k * steps + Canvas.Bayer(x, y)) / steps;
                sky.Set(x, y, Color.Lerp(horizon, top, q));
            }
        sky.Save(dir + "/sky.png");

        // Far: rolling hills and a hazy distant treeline.
        var far = new Canvas(W, H);
        float[] hill = Profile(rng, W, 150f, 30f, 0.006f);
        for (int x = 0; x < W; x++)
            for (int y = 0; y < hill[x]; y++)
                far.Set(x, y, Canvas.Bayer(x, y) > 0.85f ? new Color(0.53f, 0.52f, 0.41f) : new Color(0.5f, 0.5f, 0.39f));
        for (int i = 0; i < 160; i++)
        {
            int tx = rng.Next(0, W), h = rng.Next(10, 26);
            int baseY = Mathf.RoundToInt(hill[Mathf.Clamp(tx, 0, W - 1)]) - 4;
            Pine(far, tx, baseY, h, new[] { new Color(0.38f, 0.4f, 0.31f), new Color(0.41f, 0.43f, 0.33f) }, rng);
        }
        far.Save(dir + "/far.png");

        // Near: dense pine forest, a wooden watchtower and a fence along the road.
        var near = new Canvas(W, H);
        var forest = new[] { new Color(0.16f, 0.21f, 0.13f), new Color(0.22f, 0.28f, 0.16f), new Color(0.3f, 0.35f, 0.2f) };
        near.Fill((x, y) => y >= Floor - 2 && y < Floor + 22, R(0.2f, 0.24f, 0.14f), Canvas.Flat(0.4f), 0.1f);
        for (int layer = 0; layer < 2; layer++)
            for (int i = 0; i < 70; i++)
            {
                int tx = rng.Next(-20, W + 20);
                if (tx > 410 && tx < 490 && layer == 1) continue;                 // leave the tower visible
                int h = layer == 0 ? rng.Next(55, 95) : rng.Next(70, 125);
                Pine(near, tx, Floor + 14 + layer * 4, h, layer == 0 ? new[] { forest[0], forest[1] } : new[] { forest[1], forest[2] }, rng);
            }
        Tower(near, 450, Floor + 14);
        for (int x = 0; x < W; x += 34) near.Fill(Canvas.Rect(x, Floor - 2, x + 2, Floor + 18), Wood, Canvas.CylV(x, x + 2)); // posts
        foreach (int ry in new[] { Floor + 7, Floor + 14 }) near.Fill(Canvas.Rect(0, ry, W - 1, ry + 1), Wood, Canvas.CylH(ry, ry + 1));
        near.Outline(new Color(0.12f, 0.12f, 0.09f)).Save(dir + "/near.png");

        // Ground: dark soil, olive grass band with tufts. Its top edge (y = 56) is the floor.
        var ground = new Canvas(W, H);
        var soil = R(0.3f, 0.24f, 0.16f);
        ground.Fill(Canvas.Rect(0, 0, W - 1, Floor - 12), soil, (x, y) => 0.25f + y / (float)Floor * 0.4f, 0.12f);
        for (int i = 0; i < 120; i++) ground.Fill(Canvas.Ellipse(rng.Next(0, W), rng.Next(2, Floor - 16), rng.Next(2, 5), rng.Next(1, 3)), Steel, Canvas.Flat(0.3f));
        var grass = R(0.52f, 0.52f, 0.24f);
        ground.Fill(Canvas.Rect(0, Floor - 12, W - 1, Floor - 1), grass, (x, y) => 0.25f + (y - (Floor - 12)) / 12f * 0.7f, 0.12f);
        for (int x = 0; x < W; x += 2)
        {
            int h = rng.Next(0, 6);
            for (int y = 0; y < h; y++) ground.Set(x + (y > 2 ? 1 : 0), Floor + y, grass[y > 2 ? 4 : 3]);
        }
        ground.Save(dir + "/ground.png");
    }

    static float[] Profile(System.Random rng, int w, float baseH, float amp, float freq)
    {
        var p = new float[w];
        float o1 = (float)rng.NextDouble() * 100f, o2 = (float)rng.NextDouble() * 100f;
        for (int x = 0; x < w; x++)
            p[x] = baseH + Mathf.Sin(x * freq + o1) * amp + Mathf.Sin(x * freq * 3.1f + o2) * amp * 0.35f;
        return p;
    }

    static void Pine(Canvas c, int x, int baseY, int h, Color[] shades, System.Random rng)
    {
        c.Fill(Canvas.Rect(x - 1, baseY - 4, x + 1, baseY + h / 5), Wood, Canvas.Flat(0.2f));
        int tiers = Mathf.Max(3, h / 14);
        for (int t = 0; t < tiers; t++)
        {
            float y0 = baseY + h * 0.15f + t * (h * 0.85f / tiers) * 0.85f;
            float th = h * 0.85f / tiers * 1.5f;
            float half = (h * 0.32f) * (1f - t / (float)(tiers + 1));
            for (int yy = 0; yy < th; yy++)
            {
                float hw = half * (1f - yy / th);
                for (int xx = -Mathf.CeilToInt(hw); xx <= Mathf.CeilToInt(hw); xx++)
                {
                    bool lit = xx < -hw * 0.3f && rng.Next(4) > 0;
                    c.Set(x + xx, Mathf.RoundToInt(y0 + yy), lit ? shades[1] : shades[0]);
                }
            }
        }
    }

    static void Tower(Canvas c, int cx, int baseY)
    {
        foreach (int lx in new[] { cx - 22, cx + 20 }) c.Fill(Canvas.Rect(lx, baseY, lx + 3, baseY + 120), Wood, Canvas.CylV(lx, lx + 3));
        for (int y = baseY + 10; y < baseY + 110; y += 30) { c.Line(cx - 20, y, cx + 20, y + 28, Wood[1]); c.Line(cx + 20, y, cx - 20, y + 28, Wood[1]); }
        c.Fill(Canvas.Rect(cx - 30, baseY + 116, cx + 30, baseY + 150), Wood, (x, y) => 0.3f + ((x - cx) % 6 == 0 ? -0.2f : 0.2f));
        for (int wx = cx - 22; wx < cx + 22; wx += 14) c.Fill(Canvas.Rect(wx, baseY + 128, wx + 7, baseY + 142), R(0.12f, 0.1f, 0.08f), Canvas.Flat(0.3f));
        c.Fill(Canvas.Poly((cx - 36, baseY + 150), (cx + 36, baseY + 150), (cx, baseY + 176)), R(0.32f, 0.24f, 0.17f), (x, y) => 0.35f + ((x + y) % 5 == 0 ? -0.2f : 0f));
    }

    // ================================================================ tiny canvas with shading

    class Canvas
    {
        readonly int w, h;
        readonly Color[] px;

        public Canvas(int w, int h) { this.w = w; this.h = h; px = new Color[w * h]; }

        public void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = c; }

        public void Line(int x0, int y0, int x1, int y1, Color c)
        {
            int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        static readonly float[] BayerMatrix = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        public static float Bayer(int x, int y) => (BayerMatrix[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;

        /// <summary>Fills a mask with a ramp; light (0..1) picks shades 1..4 with ordered dithering. noise adds texture.</summary>
        public void Fill(Func<int, int, bool> mask, Color[] ramp, Func<int, int, float> light, float noise = 0f)
            => FillPattern(mask, ramp, light, null, noise);

        public void FillPattern(Func<int, int, bool> mask, Color[] ramp, Func<int, int, float> light,
                                Func<int, int, float> pattern, float noise = 0f)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    if (!mask(x, y)) continue;
                    float l = Mathf.Clamp01(light(x, y));
                    if (pattern != null) l += pattern(x, y);
                    if (noise > 0f) l += (Hash(x, y) - 0.5f) * noise * 2f;
                    int level = 1 + Mathf.FloorToInt(Mathf.Clamp(l * 3f + Bayer(x, y) - 0.5f, 0f, 3f));
                    Set(x, y, ramp[Mathf.Clamp(level, 1, 4)]);
                }
        }

        static float Hash(int x, int y)
        {
            uint n = (uint)(x * 374761393 + y * 668265263);
            n = (n ^ (n >> 13)) * 1274126177u;
            return (n & 0xFFFF) / 65535f;
        }

        /// <summary>Adds a 1px outline around every shape (on transparent pixels touching opaque ones).</summary>
        public Canvas Outline(Color ink)
        {
            var copy = (Color[])px.Clone();
            bool Opaque(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && copy[y * w + x].a > 0.5f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (!Opaque(x, y) && (Opaque(x - 1, y) || Opaque(x + 1, y) || Opaque(x, y - 1) || Opaque(x, y + 1)))
                        px[y * w + x] = ink;
            return this;
        }

        public void Save(string assetPath)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            string full = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllBytes(full, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        // ---- masks
        public static Func<int, int, bool> Rect(int x0, int y0, int x1, int y1) => (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1;
        public static Func<int, int, bool> Ellipse(float cx, float cy, float rx, float ry) =>
            (x, y) => (x - cx) * (x - cx) / (rx * rx) + (y - cy) * (y - cy) / (ry * ry) <= 1f;
        public static Func<int, int, bool> Or(Func<int, int, bool> a, Func<int, int, bool> b) => (x, y) => a(x, y) || b(x, y);
        public static Func<int, int, bool> Poly(params (float x, float y)[] pts) => (x, y) =>
        {
            bool inside = false;
            for (int i = 0, j = pts.Length - 1; i < pts.Length; j = i++)
                if ((pts[i].y > y) != (pts[j].y > y) &&
                    x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)
                    inside = !inside;
            return inside;
        };

        // ---- light models (light comes from the upper left)
        public static Func<int, int, float> Flat(float l) => (x, y) => l;
        public static Func<int, int, float> CylV(int x0, int x1) => (x, y) =>
        {
            float u = (x - x0 + 0.5f) / (x1 - x0 + 1f);
            return Mathf.Clamp01(1f - Mathf.Abs(u - 0.33f) * 1.8f) * 0.85f + 0.08f;
        };
        public static Func<int, int, float> CylH(int y0, int y1) => (x, y) =>
        {
            float u = (y - y0 + 0.5f) / (y1 - y0 + 1f);
            return Mathf.Clamp01(1f - Mathf.Abs(u - 0.7f) * 1.8f) * 0.85f + 0.08f;
        };
        public static Func<int, int, float> Sphere(float lx, float ly, float r) => (x, y) =>
            Mathf.Clamp01(1f - Mathf.Sqrt((x - lx) * (x - lx) + (y - ly) * (y - ly)) / (r * 1.6f)) * 0.9f + 0.05f;

        /// <summary>Chainmail: alternating ring highlights.</summary>
        public static float ChainPattern(int x, int y) => ((x + (y / 2) % 2) % 2 == 0 ? 0.18f : -0.22f) + (y % 2 == 0 ? -0.08f : 0f);
    }
}
