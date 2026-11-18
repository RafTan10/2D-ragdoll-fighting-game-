using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Everything that describes a built fighter. Saved as JSON in Assets/Game/Data/Recipes/&lt;Name&gt;.json.</summary>
[Serializable]
public class FighterRecipe
{
    public string name = "New Fighter";

    [Header("Look (option names from FighterParts)")]
    public string hair = "Short";
    public string facialHair = "None";
    public string eyewear = "None";
    public string headwear = "None";
    public string top = "T-shirt";
    public string shoes = "Shoes";
    public bool belt;
    public bool watch;
    public bool handbag;

    [Header("Colours")]
    public Color skin = new Color32(0xB9, 0x7A, 0x58, 255);
    public Color hairColor = new Color32(0x1E, 0x1C, 0x24, 255);
    public Color topColor = new Color32(0x3E, 0x5C, 0x8A, 255);
    public Color topAccent = new Color32(0xD3, 0xD6, 0xDC, 255);
    public Color innerColor = new Color32(0xE9, 0xE3, 0xD3, 255);
    public Color trousers = new Color32(0x2A, 0x2C, 0x36, 255);
    public Color shoeColor = new Color32(0x1A, 0x14, 0x14, 255);
    public Color hatColor = new Color32(0x2E, 0x40, 0x73, 255);
    public Color frameColor = new Color32(0xA7, 0xAD, 0xB8, 255);
    public Color lensColor = new Color32(0x8A, 0x22, 0x38, 255);

    [Header("Fighting")]
    public string mainWeapon = "Sword";
    /// <summary>Empty = bare fist.</summary>
    public string offWeapon = "";
    public float maxHealth = 100f, strength = 1f, weight = 1f, walkSpeed = 2.2f, endurance = 1f;
}

/// <summary>
/// Builds a fighter's sprites from a <see cref="FighterRecipe"/>: the chosen <see cref="FighterParts"/> layers are
/// stacked per body part and their colour roles are filled from the recipe's colours (shadows and highlights are
/// derived), then the PNGs are written to Assets/Game/Art/Fighters/&lt;Name&gt;/ and the fighter is created.
/// Used by <see cref="FighterBuilderWindow"/> (Tools → Game → Fighter Builder) and from code:
///   FighterBuilder.Create(FighterBuilder.LoadRecipe("Assets/Game/Data/Recipes/Rafid.json"));
/// </summary>
public static class FighterBuilder
{
    public const string RecipeFolder = "Assets/Game/Data/Recipes";
    const string ArtRoot = "Assets/Game/Art/Fighters";

    static readonly Color Outline = new Color32(0x1E, 0x14, 0x18, 255);

    // ================================================================ options

    public static FighterPart Find(FighterPart[] options, string name)
    {
        foreach (var p in options) if (p.name == name) return p;
        return options[0];
    }

    public static string[] Names(FighterPart[] options)
    {
        var names = new string[options.Length];
        for (int i = 0; i < options.Length; i++) names[i] = options[i].name;
        return names;
    }

    // ================================================================ composing

    /// <summary>All the fighter's sprites as textures (not saved): head, torso, upper_arm, lower_arm, upper_leg,
    /// lower_leg, and cape / skirt when the recipe has them.</summary>
    public static Dictionary<string, Texture2D> Compose(FighterRecipe r)
    {
        var colours = Palette(r);
        int seed = r.name.GetHashCode();
        var hair = Find(FighterParts.Hair, r.hair);
        var face = Find(FighterParts.FacialHair, r.facialHair);
        var eyes = Find(FighterParts.Eyewear, r.eyewear);
        var hat = Find(FighterParts.Headwear, r.headwear);
        var top = Find(FighterParts.Tops, r.top);
        var shoes = Find(FighterParts.Shoes, r.shoes);

        var parts = new Dictionary<string, Texture2D>
        {
            ["head"] = Render(24, 24, colours, seed, FighterParts.BaseHead, hair.head, face.head, eyes.head, hat.head),
            ["torso"] = Render(24, 36, colours, seed + 1, FighterParts.TrouserHips, top.torso, r.belt && top.tucked ? FighterParts.Belt : null),
            ["upper_arm"] = Render(10, 20, colours, seed + 2, top.upperArm),
            ["lower_arm"] = Render(10, 20, colours, seed + 3, WithHand(top.lowerArm), r.watch && top.showsWrist ? FighterParts.Watch : null),
            ["upper_leg"] = Render(12, 24, colours, seed + 4, FighterParts.TrouserThigh),
            ["lower_leg"] = Render(12, 24, colours, seed + 5, FighterParts.TrouserShin, shoes.lowerLeg),
        };
        var cape = hat.cape ?? hair.cape;
        if (cape != null) parts["cape"] = Render(24, cape.Length, colours, seed + 6, cape);
        if (top.skirt != null) parts["skirt"] = Render(24, top.skirt.Length, colours, seed + 7, top.skirt);
        return parts;
    }

    static string[] WithHand(string[] forearm)
    {
        var rows = new string[20];
        for (int i = 0; i < 13; i++) rows[i] = forearm != null && i < forearm.Length ? forearm[i] : "";
        for (int i = 0; i < 7; i++) rows[13 + i] = FighterParts.Hand[i];
        return rows;
    }

    /// <summary>Colour for every role letter. '~' roles (hair, cloth) get a little texture noise when drawn.</summary>
    static Dictionary<char, (Color colour, bool textured)> Palette(FighterRecipe r)
    {
        var p = new Dictionary<char, (Color, bool)>();
        void Set(char k, Color c, bool textured = false) => p[k] = (c, textured);

        Set('a', Outline);
        Set('s', r.skin); Set('S', Shade(r.skin, 0.76f)); Set('l', Light(r.skin, 0.18f)); Set('k', Shade(r.skin, 0.55f));
        Set('r', Color.Lerp(r.skin, new Color32(0x9A, 0x3A, 0x36, 255), 0.45f));
        Set('w', new Color32(0xEE, 0xE6, 0xDA, 255));
        Set('b', Shade(r.hairColor, 0.8f));
        Set('h', r.hairColor, true); Set('H', Shade(r.hairColor, 0.72f), true); Set('g', Light(r.hairColor, 0.22f));
        Set('B', Color.Lerp(r.hairColor, r.skin, 0.28f), true);
        Set('c', r.topColor, true); Set('C', Shade(r.topColor, 0.72f), true); Set('x', Light(r.topColor, 0.2f), true);
        Set('X', Light(r.topColor, 0.45f)); Set('n', r.topAccent);
        Set('i', r.innerColor, true); Set('I', Shade(r.innerColor, 0.82f));
        Set('t', r.trousers, true); Set('T', Shade(r.trousers, 0.72f)); Set('y', Light(r.trousers, 0.16f), true);
        Set('o', r.shoeColor); Set('O', Light(r.shoeColor, 0.25f)); Set('u', new Color32(0xB9, 0xB6, 0xAE, 255));
        Set('p', r.hatColor, true); Set('P', Shade(r.hatColor, 0.7f)); Set('Y', Light(r.hatColor, 0.2f)); Set('z', Shade(r.hatColor, 0.35f));
        Set('G', r.frameColor); Set('F', Shade(r.frameColor, 0.5f));
        Set('L', r.lensColor); Set('E', Light(r.lensColor, 0.45f));
        Set('m', new Color32(0xD3, 0xD6, 0xDC, 255)); Set('M', new Color32(0x8D, 0x91, 0x99, 255));
        Set('q', new Color32(0x14, 0x14, 0x16, 255)); Set('J', new Color32(0x3D, 0x3F, 0x46, 255));
        return p;
    }

    /// <summary>Darker and a touch cooler, the way shadows look in the game's art.</summary>
    static Color Shade(Color c, float f) => new Color(c.r * f, c.g * f, Mathf.Min(1f, c.b * Mathf.Min(1f, f + 0.08f)), 1f);
    /// <summary>Towards a warm white.</summary>
    static Color Light(Color c, float t) => Color.Lerp(c, new Color(1f, 0.97f, 0.9f), t);

    /// <summary>Stacks layers (later ones on top) into a texture; '.' keeps what's below, '_' erases it.</summary>
    static Texture2D Render(int w, int h, Dictionary<char, (Color colour, bool textured)> pal, int seed, params string[][] layers)
    {
        var px = new Color[w * h];
        var rand = new System.Random(seed);
        foreach (var layer in layers)
        {
            if (layer == null) continue;
            for (int y = 0; y < h && y < layer.Length; y++)
            {
                string row = Expand(layer[y], w);
                for (int x = 0; x < w; x++)
                {
                    char ch = row[x];
                    int i = (h - 1 - y) * w + x; // textures are stored bottom-up
                    if (ch == '.') continue;
                    if (ch == '_') { px[i] = Color.clear; continue; }
                    if (!pal.TryGetValue(ch, out var role)) throw new Exception($"Unknown colour role '{ch}'");
                    var c = role.colour;
                    if (role.textured) { float d = rand.Next(-8, 9) / 255f; c = new Color(c.r + d, c.g + d, c.b + d, 1f); }
                    px[i] = c;
                }
            }
        }
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    /// <summary>Run-length row → exactly <paramref name="width"/> characters ("" = all transparent).</summary>
    public static string Expand(string row, int width)
    {
        if (string.IsNullOrEmpty(row)) return new string('.', width);
        var sb = new System.Text.StringBuilder();
        foreach (var tok in row.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            int digits = 0;
            while (digits < tok.Length - 1 && char.IsDigit(tok[digits])) digits++;
            if (digits > 0 && digits == tok.Length - 1) sb.Append(tok[digits], int.Parse(tok.Substring(0, digits)));
            else sb.Append(tok);
        }
        if (sb.Length != width) throw new Exception($"Row is {sb.Length} px, expected {width}: \"{row}\"");
        return sb.ToString();
    }

    /// <summary>Checks every template row's width. Returns "" when all good, else one line per problem.</summary>
    public static string Validate()
    {
        var problems = new System.Text.StringBuilder();
        void Check(string what, string[] rows, int width)
        {
            if (rows == null) return;
            for (int i = 0; i < rows.Length; i++)
            {
                try { Expand(rows[i], width); }
                catch (Exception e) { problems.AppendLine($"{what} row {i}: {e.Message}"); }
            }
        }
        Check("BaseHead", FighterParts.BaseHead, 24);
        Check("Hand", FighterParts.Hand, 10);
        Check("TrouserHips", FighterParts.TrouserHips, 24);
        Check("TrouserThigh", FighterParts.TrouserThigh, 12);
        Check("TrouserShin", FighterParts.TrouserShin, 12);
        Check("Belt", FighterParts.Belt, 24);
        Check("Watch", FighterParts.Watch, 10);
        foreach (var set in new[] { FighterParts.Hair, FighterParts.FacialHair, FighterParts.Eyewear, FighterParts.Headwear, FighterParts.Tops, FighterParts.Shoes })
            foreach (var p in set)
            {
                Check(p.name + " head", p.head, 24); Check(p.name + " torso", p.torso, 24);
                Check(p.name + " upper arm", p.upperArm, 10); Check(p.name + " lower arm", p.lowerArm, 10);
                Check(p.name + " shin", p.lowerLeg, 12); Check(p.name + " cape", p.cape, 24); Check(p.name + " skirt", p.skirt, 24);
            }
        return problems.ToString();
    }

    /// <summary>
    /// The whole fighter standing, as one image (like the window's preview): parts placed with RagdollBody's
    /// spacing, back limbs darker. <paramref name="scale"/> enlarges it pixel-for-pixel.
    /// </summary>
    public static Texture2D Assemble(FighterRecipe r, int scale = 1)
    {
        var p = Compose(r);
        Texture2D T(string k) => p.TryGetValue(k, out var t) ? t : null;
        var head = T("head"); var torso = T("torso"); var ua = T("upper_arm"); var la = T("lower_arm");
        var ul = T("upper_leg"); var ll = T("lower_leg"); var cape = T("cape"); var skirt = T("skirt");

        int W = 40, H = 128, cx = W / 2, feet = H - 2;                  // y grows downwards here
        int hip = feet - (ll.height + ul.height - 2);
        int torsoTop = hip - (torso.height - 3);                        // torso centre = hip - (h/2 - 3)
        int neck = torsoTop + 3;
        int shoulder = neck + 5, knee = hip + (ul.height - 2), elbow = shoulder + (ua.height - 2);
        var canvas = new Color[W * H];
        var back = new Color(0.75f, 0.75f, 0.75f, 1f);

        void Blit(Texture2D t, int left, int top, Color tint)
        {
            if (t == null) return;
            var src = t.GetPixels();
            for (int y = 0; y < t.height; y++)
                for (int x = 0; x < t.width; x++)
                {
                    var c = src[(t.height - 1 - y) * t.width + x];
                    int X = left + x, Y = top + y;
                    if (c.a < 0.5f || X < 0 || Y < 0 || X >= W || Y >= H) continue;
                    canvas[Y * W + X] = c * tint;
                }
        }
        void Limb(Texture2D t, int jointY, Color tint) { if (t != null) Blit(t, cx - t.width / 2, jointY, tint); }

        Limb(cape, neck + 3, Color.white);
        Limb(ua, shoulder, back); Limb(la, elbow, back); Limb(ul, hip, back); Limb(ll, knee, back);
        Blit(torso, cx - torso.width / 2, torsoTop, Color.white);
        Blit(head, cx - head.width / 2, neck - head.height, Color.white);
        Limb(ul, hip, Color.white); Limb(ll, knee, Color.white); Limb(skirt, hip, Color.white);
        Limb(ua, shoulder, Color.white); Limb(la, elbow, Color.white);
        foreach (var t in p.Values) UnityEngine.Object.DestroyImmediate(t);

        var outTex = new Texture2D(W * scale, H * scale, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var pixels = new Color[W * scale * H * scale];
        for (int y = 0; y < H * scale; y++)
            for (int x = 0; x < W * scale; x++)
                pixels[(H * scale - 1 - y) * W * scale + x] = canvas[(y / scale) * W + x / scale];
        outTex.SetPixels(pixels);
        outTex.Apply();
        return outTex;
    }

    // ================================================================ creating the fighter

    /// <summary>Writes the sprites, creates or updates the fighter asset (stats, armour, weapons) and saves the recipe.</summary>
    public static FighterDefinition Create(FighterRecipe r)
    {
        string folderName = SafeName(r.name);
        if (string.IsNullOrEmpty(folderName)) throw new Exception("The fighter needs a name.");
        string folder = ArtRoot + "/" + folderName;
        Directory.CreateDirectory(folder);
        foreach (var old in new[] { "cape", "skirt", "hand_prop" })   // remove extras the new look doesn't have
            if (File.Exists($"{folder}/{old}.png")) AssetDatabase.DeleteAsset($"{folder}/{old}.png");

        foreach (var kv in Compose(r))
        {
            File.WriteAllBytes($"{folder}/{kv.Key}.png", kv.Value.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(kv.Value);
        }
        if (r.handbag && File.Exists(ArtRoot + "/Nuha/hand_prop.png"))
            File.Copy(ArtRoot + "/Nuha/hand_prop.png", folder + "/hand_prop.png", true);
        AssetDatabase.Refresh();

        var def = ContentTools.CreateFighter(folder, r.mainWeapon, string.IsNullOrEmpty(r.offWeapon) ? null : r.offWeapon);
        if (string.IsNullOrEmpty(r.offWeapon)) def.offHand = null;
        def.displayName = r.name;
        def.maxHealth = r.maxHealth; def.strength = r.strength; def.weight = r.weight; def.walkSpeed = r.walkSpeed; def.endurance = r.endurance;
        var top = Find(FighterParts.Tops, r.top);
        def.headArmor = Find(FighterParts.Headwear, r.headwear).armor;
        def.torsoArmor = top.armor;
        def.armArmor = top.armArmor ?? top.armor;
        def.legArmor = ArmorMaterial.Cloth;
        EditorUtility.SetDirty(def);
        AssetDatabase.SaveAssets();
        SaveRecipe(r);
        return def;
    }

    public static string SafeName(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (char ch in name ?? "") if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        return sb.ToString();
    }

    public static string RecipePath(string name) => $"{RecipeFolder}/{SafeName(name)}.json";

    public static void SaveRecipe(FighterRecipe r)
    {
        Directory.CreateDirectory(RecipeFolder);
        File.WriteAllText(RecipePath(r.name), JsonUtility.ToJson(r, true));
        AssetDatabase.ImportAsset(RecipePath(r.name));
    }

    public static FighterRecipe LoadRecipe(string path) => JsonUtility.FromJson<FighterRecipe>(File.ReadAllText(path));
}
