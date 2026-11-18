using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Game → Fighter Builder: pick a hairstyle, top, shoes, extras and colours, see the fighter assembled live,
/// choose weapons and stats, and press Create. Recipes are saved and can be loaded back to tweak a fighter.
/// </summary>
public class FighterBuilderWindow : EditorWindow
{
    FighterRecipe recipe = new FighterRecipe();
    Dictionary<string, Texture2D> preview;
    Vector2 scroll;
    string problems;
    WeaponDefinition[] weapons;

    static readonly Color[] SkinTones =
    {
        new Color32(0xF1, 0xC8, 0xA4, 255), new Color32(0xE0, 0xAC, 0x82, 255), new Color32(0xCF, 0x96, 0x70, 255),
        new Color32(0xB9, 0x7A, 0x58, 255), new Color32(0xA0, 0x64, 0x44, 255), new Color32(0x7E, 0x4C, 0x32, 255),
        new Color32(0x5C, 0x38, 0x26, 255),
    };
    static readonly Color[] HairTones =
    {
        new Color32(0x1B, 0x1A, 0x21, 255), new Color32(0x3A, 0x26, 0x1C, 255), new Color32(0x6E, 0x40, 0x22, 255),
        new Color32(0xA8, 0x4A, 0x22, 255), new Color32(0xC9, 0xA2, 0x5A, 255), new Color32(0x9A, 0x9A, 0x9A, 255),
    };

    [MenuItem("Tools/Game/Fighter Builder")]
    public static void Open() => GetWindow<FighterBuilderWindow>("Fighter Builder").minSize = new Vector2(760, 560);

    void OnEnable()
    {
        problems = FighterBuilder.Validate();
        weapons = AssetDatabase.FindAssets("t:WeaponDefinition").Select(g => AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                               .Where(w => w != null).OrderBy(w => w.displayName).ToArray();
        Rebuild();
    }

    void OnDisable() => Clear();

    void Clear()
    {
        if (preview == null) return;
        foreach (var t in preview.Values) if (t != null) DestroyImmediate(t);
        preview = null;
    }

    void Rebuild()
    {
        Clear();
        if (!string.IsNullOrEmpty(problems)) return;
        try { preview = FighterBuilder.Compose(recipe); }
        catch (System.Exception e) { problems = e.Message; }
    }

    void OnGUI()
    {
        if (!string.IsNullOrEmpty(problems)) EditorGUILayout.HelpBox("Part library problem:\n" + problems, MessageType.Error);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.BeginVertical(GUILayout.Width(380));
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUI.BeginChangeCheck();
        DrawControls();
        if (EditorGUI.EndChangeCheck()) Rebuild();
        EditorGUILayout.EndScrollView();
        DrawButtons();
        EditorGUILayout.EndVertical();

        var area = GUILayoutUtility.GetRect(10, 10, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        DrawPreview(area);
        EditorGUILayout.EndHorizontal();
    }

    // ---------------------------------------------------------------- controls

    void DrawControls()
    {
        EditorGUILayout.LabelField("Who", EditorStyles.boldLabel);
        recipe.name = EditorGUILayout.TextField("Name", recipe.name);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Look", EditorStyles.boldLabel);
        recipe.hair = Choice("Hair", FighterParts.Hair, recipe.hair);
        recipe.facialHair = Choice("Facial hair", FighterParts.FacialHair, recipe.facialHair);
        recipe.eyewear = Choice("Eyewear", FighterParts.Eyewear, recipe.eyewear);
        recipe.headwear = Choice("Headwear", FighterParts.Headwear, recipe.headwear);
        recipe.top = Choice("Top", FighterParts.Tops, recipe.top);
        recipe.shoes = Choice("Shoes", FighterParts.Shoes, recipe.shoes);
        var top = FighterBuilder.Find(FighterParts.Tops, recipe.top);
        using (new EditorGUI.DisabledScope(!top.tucked)) recipe.belt = EditorGUILayout.Toggle("Belt (tucked tops)", recipe.belt);
        using (new EditorGUI.DisabledScope(!top.showsWrist)) recipe.watch = EditorGUILayout.Toggle("Watch (bare wrist)", recipe.watch);
        recipe.handbag = EditorGUILayout.Toggle("Handbag (off hand)", recipe.handbag);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Colours", EditorStyles.boldLabel);
        recipe.skin = EditorGUILayout.ColorField("Skin", recipe.skin);
        Swatches(SkinTones, c => recipe.skin = c);
        recipe.hairColor = EditorGUILayout.ColorField("Hair / beard", recipe.hairColor);
        Swatches(HairTones, c => recipe.hairColor = c);
        recipe.topColor = EditorGUILayout.ColorField("Top", recipe.topColor);
        recipe.topAccent = EditorGUILayout.ColorField("Buttons / strings", recipe.topAccent);
        recipe.innerColor = EditorGUILayout.ColorField("Shirt underneath", recipe.innerColor);
        recipe.trousers = EditorGUILayout.ColorField("Trousers", recipe.trousers);
        recipe.shoeColor = EditorGUILayout.ColorField("Shoes", recipe.shoeColor);
        recipe.hatColor = EditorGUILayout.ColorField("Hat / hijab", recipe.hatColor);
        recipe.frameColor = EditorGUILayout.ColorField("Glasses frame", recipe.frameColor);
        recipe.lensColor = EditorGUILayout.ColorField("Shades lens", recipe.lensColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Fighting", EditorStyles.boldLabel);
        var names = weapons.Select(w => w.displayName).ToList();
        int main = Mathf.Max(0, names.FindIndex(n => n == recipe.mainWeapon));
        if (names.Count > 0) recipe.mainWeapon = names[EditorGUILayout.Popup("Main hand", main, names.ToArray())];
        var offNames = new List<string> { "(bare fist)" }; offNames.AddRange(names);
        int off = string.IsNullOrEmpty(recipe.offWeapon) ? 0 : Mathf.Max(0, offNames.FindIndex(n => n == recipe.offWeapon));
        off = EditorGUILayout.Popup("Off hand", off, offNames.ToArray());
        recipe.offWeapon = off == 0 ? "" : offNames[off];
        recipe.maxHealth = EditorGUILayout.Slider("Health", recipe.maxHealth, 60f, 140f);
        recipe.strength = EditorGUILayout.Slider("Strength", recipe.strength, 0.7f, 1.3f);
        recipe.weight = EditorGUILayout.Slider("Weight", recipe.weight, 0.8f, 1.3f);
        recipe.walkSpeed = EditorGUILayout.Slider("Walk speed", recipe.walkSpeed, 1.6f, 2.8f);
        recipe.endurance = EditorGUILayout.Slider("Endurance", recipe.endurance, 0.7f, 1.4f);
        EditorGUILayout.HelpBox($"Armour from the look: head {FighterBuilder.Find(FighterParts.Headwear, recipe.headwear).armor}, " +
                                $"torso + arms {top.armor}, legs Cloth.", MessageType.None);
    }

    static string Choice(string label, FighterPart[] options, string current)
    {
        var names = FighterBuilder.Names(options);
        int i = Mathf.Max(0, System.Array.IndexOf(names, current));
        return names[EditorGUILayout.Popup(label, i, names)];
    }

    void Swatches(Color[] colours, System.Action<Color> pick)
    {
        var rect = EditorGUILayout.GetControlRect(false, 18);
        rect.xMin += EditorGUIUtility.labelWidth;
        float w = Mathf.Min(26f, rect.width / colours.Length);
        for (int i = 0; i < colours.Length; i++)
        {
            var r = new Rect(rect.x + i * w, rect.y, w - 2, rect.height);
            EditorGUI.DrawRect(r, colours[i]);
            if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
            {
                pick(colours[i]); GUI.changed = true; Event.current.Use();
            }
        }
    }

    void DrawButtons()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Randomize look")) { Randomize(); Rebuild(); }
        if (GUILayout.Button("Load recipe…"))
        {
            string path = EditorUtility.OpenFilePanel("Load fighter recipe", FighterBuilder.RecipeFolder, "json");
            if (!string.IsNullOrEmpty(path)) { recipe = FighterBuilder.LoadRecipe(path); Rebuild(); }
        }
        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(!string.IsNullOrEmpty(problems) || string.IsNullOrEmpty(FighterBuilder.SafeName(recipe.name))))
        {
            if (GUILayout.Button("Create / update fighter", GUILayout.Height(32)))
            {
                bool exists = File.Exists($"Assets/Game/Art/Fighters/{FighterBuilder.SafeName(recipe.name)}/head.png");
                if (!exists || EditorUtility.DisplayDialog("Update fighter?",
                        $"{recipe.name} already exists. Replace their sprites, stats and weapons with this recipe?", "Replace", "Cancel"))
                {
                    var def = FighterBuilder.Create(recipe);
                    EditorGUIUtility.PingObject(def);
                    ShowNotification(new GUIContent($"{def.displayName} is ready — pick them in Play mode"));
                }
            }
        }
    }

    void Randomize()
    {
        var rnd = new System.Random();
        string Pick(FighterPart[] o) => o[rnd.Next(o.Length)].name;
        recipe.hair = Pick(FighterParts.Hair); recipe.facialHair = Pick(FighterParts.FacialHair);
        recipe.eyewear = rnd.Next(3) == 0 ? Pick(FighterParts.Eyewear) : "None";
        recipe.headwear = rnd.Next(3) == 0 ? Pick(FighterParts.Headwear) : "None";
        recipe.top = Pick(FighterParts.Tops); recipe.shoes = Pick(FighterParts.Shoes);
        recipe.skin = SkinTones[rnd.Next(SkinTones.Length)]; recipe.hairColor = HairTones[rnd.Next(HairTones.Length)];
        recipe.topColor = Random.ColorHSV(0f, 1f, 0.25f, 0.65f, 0.25f, 0.75f);
        recipe.trousers = Random.ColorHSV(0f, 1f, 0.1f, 0.4f, 0.12f, 0.35f);
        recipe.hatColor = Random.ColorHSV(0f, 1f, 0.3f, 0.7f, 0.25f, 0.6f);
    }

    // ---------------------------------------------------------------- preview

    /// <summary>Assembles the parts with the rig's own spacing (RagdollBody.Build): front limbs in front, back ones darker.</summary>
    void DrawPreview(Rect area)
    {
        EditorGUI.DrawRect(area, new Color(0.29f, 0.27f, 0.33f));
        if (preview == null) return;
        Texture2D T(string k) => preview.TryGetValue(k, out var t) ? t : null;
        var head = T("head"); var torso = T("torso"); var ua = T("upper_arm"); var la = T("lower_arm");
        var ul = T("upper_leg"); var ll = T("lower_leg"); var cape = T("cape"); var skirt = T("skirt");

        float k = Mathf.Floor(Mathf.Min((area.height - 40) / 120f, (area.width - 40) / 40f));
        k = Mathf.Max(2, k);
        float cx = area.center.x, feet = area.yMax - 20;
        float hip = feet - (ll.height + ul.height - 2) * k;
        float torsoCentre = hip - (torso.height * 0.5f - 3) * k;
        float neck = torsoCentre - (torso.height * 0.5f - 3) * k;
        float shoulder = neck + 5 * k, knee = hip + (ul.height - 2) * k, elbow = shoulder + (ua.height - 2) * k;
        var back = new Color(0.75f, 0.75f, 0.75f);

        void Top(Texture2D t, float jointY, Color tint) { if (t == null) return; GUI.color = tint; GUI.DrawTexture(new Rect(cx - t.width * k / 2, jointY, t.width * k, t.height * k), t); }
        void Bottom(Texture2D t, float jointY) { GUI.color = Color.white; GUI.DrawTexture(new Rect(cx - t.width * k / 2, jointY - t.height * k, t.width * k, t.height * k), t); }

        Top(cape, neck + 3 * k, Color.white);
        Top(ua, shoulder, back); Top(la, elbow, back);
        Top(ul, hip, back); Top(ll, knee, back);
        GUI.color = Color.white; GUI.DrawTexture(new Rect(cx - torso.width * k / 2, torsoCentre - torso.height * k / 2, torso.width * k, torso.height * k), torso);
        Bottom(head, neck);
        Top(ul, hip, Color.white); Top(ll, knee, Color.white);
        Top(skirt, hip, Color.white);
        Top(ua, shoulder, Color.white); Top(la, elbow, Color.white);
        GUI.color = Color.white;
        GUI.Label(new Rect(area.x + 8, area.y + 6, area.width, 20), recipe.name, EditorStyles.whiteLargeLabel);
    }
}
