using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns pixel art into game content and registers it in the GameDatabase.
/// Menu: Tools > Game > ... ; the static methods are also called from the Claude commands via `script eval`.
/// </summary>
public static class ContentTools
{
    public const string DataRoot = "Assets/Game/Data";
    public const string DatabasePath = DataRoot + "/GameDatabase.asset";
    public const string MaterialPath = DataRoot + "/SpriteUnlit.mat";

    public static readonly string[] FighterParts = { "head", "torso", "upper_arm", "lower_arm", "upper_leg", "lower_leg" };
    public static readonly string[] ArenaLayers = { "sky", "far", "near", "ground" };

    // ---------------------------------------------------------------- database

    public static GameDatabase GetDatabase()
    {
        var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
        if (db == null)
        {
            EnsureFolder(DataRoot);
            db = ScriptableObject.CreateInstance<GameDatabase>();
            AssetDatabase.CreateAsset(db, DatabasePath);
        }

        if (db.spriteMaterial == null)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }
            db.spriteMaterial = mat;
        }

        if (db.aiProfiles.Count == 0)
        {
            db.aiProfiles.Add(MakeAI("Easy", 0.6f, new Vector2(0.5f, 0.9f), new Vector2(0.8f, 1.5f), 0.35f, 0.15f, 0.05f, 0.7f));
            db.aiProfiles.Add(MakeAI("Normal", 0.35f, new Vector2(0.3f, 0.6f), new Vector2(0.4f, 0.9f), 0.6f, 0.4f, 0.1f, 1f));
            db.aiProfiles.Add(MakeAI("Hard", 0.18f, new Vector2(0.2f, 0.4f), new Vector2(0.2f, 0.5f), 0.85f, 0.7f, 0.15f, 1.2f));
        }
        EditorUtility.SetDirty(db);
        return db;
    }

    static AIProfile MakeAI(string name, float reaction, Vector2 windUp, Vector2 recovery, float aggression,
                            float block, float hop, float damage)
    {
        string path = DataRoot + "/AI/" + name + ".asset";
        var ai = AssetDatabase.LoadAssetAtPath<AIProfile>(path);
        if (ai == null)
        {
            EnsureFolder(DataRoot + "/AI");
            ai = ScriptableObject.CreateInstance<AIProfile>();
            AssetDatabase.CreateAsset(ai, path);
        }
        ai.displayName = name;
        ai.reactionTime = reaction;
        ai.windUp = windUp;
        ai.recovery = recovery;
        ai.aggression = aggression;
        ai.blockChance = block;
        ai.hopChance = hop;
        ai.damageMultiplier = damage;
        EditorUtility.SetDirty(ai);
        return ai;
    }

    // ---------------------------------------------------------------- fighters

    /// <summary>
    /// Creates (or updates) a fighter from a folder holding head/torso/upper_arm/lower_arm/upper_leg/lower_leg PNGs.
    /// Keeps existing stats and loadout when the fighter already exists.
    /// </summary>
    public static FighterDefinition CreateFighter(string folder, string mainWeapon = null, string offWeapon = null)
    {
        folder = folder.TrimEnd('/', '\\').Replace('\\', '/');
        string name = Path.GetFileName(folder);
        AssetDatabase.ImportAsset(folder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

        foreach (string part in FighterParts)
            if (LoadSprite(folder, part) == null)
                throw new System.Exception($"Fighter '{name}' is missing {folder}/{part}.png");

        string path = DataRoot + "/Fighters/" + name + ".asset";
        var def = AssetDatabase.LoadAssetAtPath<FighterDefinition>(path);
        bool isNew = def == null;
        if (isNew)
        {
            EnsureFolder(DataRoot + "/Fighters");
            def = ScriptableObject.CreateInstance<FighterDefinition>();
            def.displayName = ObjectNames.NicifyVariableName(name);
            AssetDatabase.CreateAsset(def, path);
        }

        def.head = LoadSprite(folder, "head");
        def.torso = LoadSprite(folder, "torso");
        def.upperArm = LoadSprite(folder, "upper_arm");
        def.lowerArm = LoadSprite(folder, "lower_arm");
        def.upperLeg = LoadSprite(folder, "upper_leg");
        def.lowerLeg = LoadSprite(folder, "lower_leg");
        def.cape = LoadSprite(folder, "cape"); // optional

        var db = GetDatabase();
        if (!string.IsNullOrEmpty(mainWeapon)) def.mainHand = FindWeapon(db, mainWeapon);
        if (!string.IsNullOrEmpty(offWeapon)) def.offHand = FindWeapon(db, offWeapon);
        if (isNew && def.mainHand == null) def.mainHand = FindWeapon(db, "Sword");
        if (isNew && def.offHand == null && string.IsNullOrEmpty(offWeapon)) def.offHand = FindWeapon(db, "Shield");

        if (!db.fighters.Contains(def)) db.fighters.Add(def);
        EditorUtility.SetDirty(def);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"Fighter ready: {path}");
        return def;
    }

    /// <summary>
    /// Starts a new fighter's art folder by copying another fighter's PNGs (right sizes, right pivots)
    /// for the artist to paint over. Returns the folder.
    /// </summary>
    public static string CreateFighterTemplate(string name, string copyFrom = "Knight")
    {
        string source = "Assets/Game/Art/Fighters/" + copyFrom;
        string folder = "Assets/Game/Art/Fighters/" + name;
        if (AssetDatabase.IsValidFolder(folder)) throw new System.Exception(folder + " already exists.");
        EnsureFolder(folder);
        foreach (string part in FighterParts)
            AssetDatabase.CopyAsset(source + "/" + part + ".png", folder + "/" + part + ".png");
        if (LoadSprite(source, "cape") != null)
            AssetDatabase.CopyAsset(source + "/cape.png", folder + "/cape.png");
        AssetDatabase.Refresh();
        Debug.Log($"Template ready: {folder} — paint over the PNGs, then run CreateFighter.");
        return folder;
    }

    public static WeaponDefinition FindWeapon(GameDatabase db, string name)
    {
        foreach (var w in db.weapons)
            if (w != null && (w.name.ToLowerInvariant() == name.ToLowerInvariant() ||
                              w.displayName.ToLowerInvariant() == name.ToLowerInvariant()))
                return w;
        return null;
    }

    // ---------------------------------------------------------------- weapons

    /// <summary>Creates (or updates the sprite of) a weapon from a single PNG drawn pointing up.</summary>
    public static WeaponDefinition CreateWeapon(string pngPath, WeaponType type)
    {
        pngPath = pngPath.Replace('\\', '/');
        AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        if (sprite == null) throw new System.Exception("No sprite at " + pngPath);

        string name = Path.GetFileNameWithoutExtension(pngPath);
        string path = DataRoot + "/Weapons/" + name + ".asset";
        var def = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
        if (def == null)
        {
            EnsureFolder(DataRoot + "/Weapons");
            def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.displayName = ObjectNames.NicifyVariableName(name);
            def.type = type;
            ApplyTypeDefaults(def);
            AssetDatabase.CreateAsset(def, path);
        }
        def.sprite = sprite;

        var db = GetDatabase();
        if (!db.weapons.Contains(def)) db.weapons.Add(def);
        EditorUtility.SetDirty(def);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"Weapon ready: {path}");
        return def;
    }

    /// <summary>Sensible starting numbers per weapon type; tweak the asset afterwards.</summary>
    public static void ApplyTypeDefaults(WeaponDefinition w)
    {
        switch (w.type)
        {
            case WeaponType.Dagger: Set(w, 8f, 0.3f, 2f, 80f, 2f, false); break;
            case WeaponType.Sword: Set(w, 13f, 0.7f, 2.5f, 80f, 2f, false); break;
            case WeaponType.Axe: Set(w, 17f, 1.1f, 2.5f, 75f, 2f, false); break;
            case WeaponType.Mace: Set(w, 15f, 1.2f, 2.5f, 75f, 2f, false); break;
            case WeaponType.Hammer: Set(w, 20f, 1.6f, 3f, 70f, 2f, true); break;
            case WeaponType.Shield: Set(w, 0f, 1f, 99f, 90f, 0f, false); break;
        }
    }

    static void Set(WeaponDefinition w, float damage, float mass, float minSpeed, float grip, float gripPx, bool twoHanded)
    {
        w.damage = damage; w.mass = mass; w.minImpactSpeed = minSpeed;
        w.gripAngle = grip; w.gripPixels = gripPx; w.twoHanded = twoHanded;
    }

    // ---------------------------------------------------------------- arenas

    /// <summary>Creates (or updates) an arena from a folder holding sky/far/near/ground PNGs (640x360).</summary>
    public static ArenaDefinition CreateArena(string folder)
    {
        folder = folder.TrimEnd('/', '\\').Replace('\\', '/');
        string name = Path.GetFileName(folder);
        AssetDatabase.ImportAsset(folder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);

        string path = DataRoot + "/Arenas/" + name + ".asset";
        var def = AssetDatabase.LoadAssetAtPath<ArenaDefinition>(path);
        if (def == null)
        {
            EnsureFolder(DataRoot + "/Arenas");
            def = ScriptableObject.CreateInstance<ArenaDefinition>();
            def.displayName = ObjectNames.NicifyVariableName(name);
            AssetDatabase.CreateAsset(def, path);
        }
        def.sky = LoadSprite(folder, "sky");
        def.far = LoadSprite(folder, "far");
        def.near = LoadSprite(folder, "near");
        def.ground = LoadSprite(folder, "ground");
        if (def.sky == null || def.ground == null)
            throw new System.Exception($"Arena '{name}' needs at least {folder}/sky.png and {folder}/ground.png");

        var db = GetDatabase();
        if (!db.arenas.Contains(def)) db.arenas.Add(def);
        EditorUtility.SetDirty(def);
        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"Arena ready: {path}");
        return def;
    }

    // ---------------------------------------------------------------- menus

    [MenuItem("Tools/Game/Create Fighter From Selected Folder")]
    static void CreateFighterMenu() => CreateFighter(SelectedFolder());

    [MenuItem("Tools/Game/Create Arena From Selected Folder")]
    static void CreateArenaMenu() => CreateArena(SelectedFolder());

    static string SelectedFolder()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
            throw new System.Exception("Select a folder in the Project window first.");
        return path;
    }

    // ---------------------------------------------------------------- helpers

    static Sprite LoadSprite(string folder, string file) => AssetDatabase.LoadAssetAtPath<Sprite>(folder + "/" + file + ".png");

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
