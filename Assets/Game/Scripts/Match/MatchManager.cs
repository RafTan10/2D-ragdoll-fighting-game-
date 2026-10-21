using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runs the game in the Arena scene: a select screen (fighter, opponent, arena, difficulty),
/// then rounds of player vs AI until someone is knocked out. The HUD is IMGUI for now (placeholder UI).
/// </summary>
public class MatchManager : MonoBehaviour
{
    public GameDatabase database;

    [Header("Selection (indices into the database)")]
    public int playerIndex;
    public int opponentIndex = 1;
    public int arenaIndex;
    public int difficultyIndex = 1;

    [Tooltip("Skip the select screen and fight straight away (handy for testing).")]
    public bool autoStart;
    [Tooltip("Show the on-screen move / jump / hand buttons during fights.")]
    public bool showOnScreenControls = true;

    OnScreenControls controls;

    enum State { Select, Fighting, RoundOver }
    State state = State.Select;

    Fighter player, enemy;
    GameObject arenaObject;
    int playerScore, enemyScore;
    float roundOverTimer;
    string roundMessage;

    const float SpawnX = 1.6f;

    void Start()
    {
        if (database == null) { Debug.LogError("MatchManager needs a GameDatabase."); enabled = false; return; }
        BloodFX.Material = database.spriteMaterial;
        // Ragdoll chains (torso > arm > forearm > weapon) jitter badly with the default 8/3 solver iterations.
        Physics2D.velocityIterations = 20;
        Physics2D.positionIterations = 8;
        if (autoStart) StartMatch();
    }

    public void StartMatch()
    {
        playerScore = enemyScore = 0;
        StartRound();
    }

    void StartRound()
    {
        Cleanup();
        var arena = Pick(database.arenas, arenaIndex);
        var playerDef = Pick(database.fighters, playerIndex);
        var enemyDef = Pick(database.fighters, opponentIndex);
        var ai = Pick(database.aiProfiles, difficultyIndex);
        var mat = database.spriteMaterial;

        arenaObject = ArenaView.Build(arena, mat);
        if (arena.rain) RainFX.Create(mat).transform.SetParent(arenaObject.transform, true);

        player = Fighter.Spawn(playerDef, playerDef.mainHand, playerDef.offHand, new Vector2(-SpawnX, 0f), 1f, 0, mat);
        enemy = Fighter.Spawn(enemyDef, enemyDef.mainHand, enemyDef.offHand, new Vector2(SpawnX, 0f), -1f, 20, mat);
        SetupCamera(arena);
        player.Opponent = enemy;
        enemy.Opponent = player;
        player.Brain = new PlayerBrain();
        enemy.Brain = new AIBrain(ai);
        enemy.DamageMultiplier = ai.damageMultiplier;
        player.KnockedOut += OnKnockedOut;
        enemy.KnockedOut += OnKnockedOut;

        state = State.Fighting;

        if (controls == null) controls = gameObject.AddComponent<OnScreenControls>();
        controls.player = player;
        controls.enabled = showOnScreenControls;
    }

    void OnKnockedOut(Fighter loser)
    {
        if (state != State.Fighting) return;
        bool playerWon = loser == enemy;
        if (playerWon) playerScore++; else enemyScore++;
        roundMessage = (playerWon ? player : enemy).Definition.displayName + " wins!";
        state = State.RoundOver;
        roundOverTimer = 3f;
        GameFeel.SlowMo(1.2f); // knockout slow motion
        if (rig != null) rig.Shake(0.08f);
    }

    CameraRig rig;
    HitboxOverlay hitboxes;

    void Update()
    {
        var kb = Keyboard.current;
        GameFeel.Tick();
        if (state == State.RoundOver)
        {
            roundOverTimer -= Time.unscaledDeltaTime;
            if (roundOverTimer <= 0f) StartRound();
        }
        if (kb == null) return;
        if (kb.hKey.wasPressedThisFrame)
        {
            if (hitboxes == null) hitboxes = gameObject.AddComponent<HitboxOverlay>();
            else hitboxes.enabled = !hitboxes.enabled;
        }
        if (state != State.Select && kb.rKey.wasPressedThisFrame) StartRound();
        if (state != State.Select && kb.escapeKey.wasPressedThisFrame)
        {
            Cleanup();
            state = State.Select;
            if (controls != null) controls.enabled = false;
        }
        if (state == State.Select && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) StartMatch();
    }

    void Cleanup()
    {
        if (player != null) Destroy(player.gameObject);
        if (enemy != null) Destroy(enemy.gameObject);
        if (arenaObject != null) Destroy(arenaObject);
        BloodFX.Clear();
        GameFeel.Reset();
    }

    void SetupCamera(ArenaDefinition arena)
    {
        var cam = Camera.main;
        if (cam == null) return;
        cam.orthographic = true;
        cam.transform.rotation = Quaternion.identity;
        cam.backgroundColor = arena.fogColor;
        rig = cam.GetComponent<CameraRig>();
        if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
        rig.a = player;
        rig.b = enemy;
        rig.arena = arena;
        rig.SnapNow();
    }

    static T Pick<T>(System.Collections.Generic.List<T> list, int index) where T : Object
        => list.Count == 0 ? null : list[Mathf.Clamp(index, 0, list.Count - 1)];

    // ================================================================ IMGUI (placeholder UI)

    GUIStyle big, normal, center;

    void OnGUI()
    {
        float scale = Screen.height / 540f;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float w = Screen.width / scale;
        // Rebuilt every frame: cached IMGUI styles were losing their settings across play sessions.
        {
            big = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            normal = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            center = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        }

        if (state == State.Select) DrawSelect(w);
        else DrawHud(w);
    }

    void DrawSelect(float w)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, w, 540), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(0, 50, w, 50), "RAGDOLL DUEL", big);
        float x = w * 0.5f - 200f, y = 140f;
        playerIndex = Selector(x, ref y, "Your fighter", database.fighters, playerIndex, f => f.displayName + Loadout(f));
        opponentIndex = Selector(x, ref y, "Opponent", database.fighters, opponentIndex, f => f.displayName + Loadout(f));
        arenaIndex = Selector(x, ref y, "Arena", database.arenas, arenaIndex, a => a.displayName);
        difficultyIndex = Selector(x, ref y, "Difficulty", database.aiProfiles, difficultyIndex, p => p.displayName);

        if (GUI.Button(new Rect(w * 0.5f - 100f, y + 20f, 200f, 50f), "FIGHT  (Enter)")) StartMatch();

        GUI.Label(new Rect(0, 470, w, 60),
            "J / R button: right hand swings    K / L button: left hand swings (a shield blocks)    A: back   D: forward   Space: jump   Shift: lunge\n" +
            "Free aim: hold left / right mouse and move the cursor.    H: show hitboxes    R: restart    Esc: menu", center);
    }

    static string Loadout(FighterDefinition f)
    {
        string main = f.mainHand != null ? f.mainHand.displayName : "Fist";
        string off = f.offHand != null ? f.offHand.displayName : "Fist";
        return "  (" + main + " + " + off + ")";
    }

    int Selector<T>(float x, ref float y, string label, System.Collections.Generic.List<T> list, int index, System.Func<T, string> name)
        where T : Object
    {
        int count = Mathf.Max(list.Count, 1);
        index = ((index % count) + count) % count;
        GUI.Label(new Rect(x, y, 140, 34), label, normal);
        if (GUI.Button(new Rect(x + 140, y, 34, 34), "<")) index = (index - 1 + count) % count;
        GUI.Label(new Rect(x + 180, y, 186, 34), list.Count > 0 ? name(list[index]) : "(none)", center);
        if (GUI.Button(new Rect(x + 370, y, 34, 34), ">")) index = (index + 1) % count;
        y += 50f;
        return index;
    }

    void DrawHud(float w)
    {
        if (player == null || enemy == null) return;
        HealthBar(new Rect(20, 20, w * 0.4f, 22), player, false);
        HealthBar(new Rect(w - 20 - w * 0.4f, 20, w * 0.4f, 22), enemy, true);
        GUI.Label(new Rect(w * 0.5f - 50f, 14, 100, 34), playerScore + " - " + enemyScore, center);

        if (state == State.RoundOver)
            GUI.Label(new Rect(0, 200, w, 60), roundMessage, big);
    }

    void HealthBar(Rect r, Fighter f, bool rightAligned)
    {
        GUI.color = Color.black;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        float t = f.Health / f.MaxHealth;
        var fill = new Rect(r.x + 2, r.y + 2, (r.width - 4) * t, r.height - 4);
        if (rightAligned) fill.x = r.xMax - 2 - fill.width;
        GUI.color = Color.Lerp(new Color(0.8f, 0.1f, 0.1f), new Color(0.2f, 0.8f, 0.2f), t);
        GUI.DrawTexture(fill, Texture2D.whiteTexture);
        // Stamina underneath: yellow, turning orange when it's low enough to make the fighter sag.
        var stam = new Rect(r.x, r.yMax + 1, r.width, 8);
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.DrawTexture(stam, Texture2D.whiteTexture);
        float s = f.MaxStamina > 0f ? f.Stamina / f.MaxStamina : 0f;
        var sFill = new Rect(stam.x + 1, stam.y + 1, (stam.width - 2) * s, stam.height - 2);
        if (rightAligned) sFill.x = stam.xMax - 1 - sFill.width;
        GUI.color = s < 0.25f ? new Color(0.95f, 0.45f, 0.1f) : new Color(0.95f, 0.82f, 0.25f);
        GUI.DrawTexture(sFill, Texture2D.whiteTexture);
        GUI.color = Color.white;

        var label = new Rect(r.x, stam.yMax + 2, r.width, 22);
        normal.alignment = rightAligned ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        string status = f.IsKnockedOut ? "" : f.State == Fighter.Condition.Fainted ? "   FAINTED" : f.State == Fighter.Condition.Down ? "   DOWN" : "";
        GUI.Label(label, rightAligned ? status.Trim() + "   " + f.Definition.displayName : f.Definition.displayName + status, normal);
    }
}
