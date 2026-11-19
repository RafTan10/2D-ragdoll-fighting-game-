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
    [Tooltip("Local 2-player: both fighters are humans sharing the keyboard (and one gamepad each). No AI, no difficulty handicaps.")]
    public bool twoPlayer;
    [Tooltip("Power Mode: a super meter fills while fighting; when full, each fighter can fire their super power.")]
    public bool powerMode;

    [Tooltip("Skip the select screen and fight straight away (handy for testing).")]
    public bool autoStart;
    [Tooltip("Show the on-screen move / jump / hand buttons during fights.")]
    public bool showOnScreenControls = true;

    [Tooltip("Rounds needed to win the match (2 = best of 3).")]
    public int roundsToWin = 2;
    [Tooltip("Seconds after a knockout before the next round (the body settles, slow motion plays).")]
    public float roundEndDelay = 3f;

    OnScreenControls controls;

    enum State { Select, Fighting, RoundOver, MatchOver }
    State state = State.Select;

    Fighter player, enemy;
    GameObject arenaObject;
    int playerScore, enemyScore;
    float roundOverTimer;
    string roundMessage;

    const float SpawnX = 1.6f;

    // "Torso armour broken!" under the fighter's bars for a moment.
    string playerNotice, enemyNotice;
    float playerNoticeUntil, enemyNoticeUntil;

    void OnEnable() { HitEvents.ArmorBroken += OnArmorBroken; HitEvents.PowerUsed += OnPowerUsed; }
    void OnDisable() { HitEvents.ArmorBroken -= OnArmorBroken; HitEvents.PowerUsed -= OnPowerUsed; }

    // "TANIM — BIKER RAGE!" across the top for a moment when a power fires.
    string powerBanner;
    Color powerBannerColor;
    float powerBannerUntil;

    void OnPowerUsed(PowerInfo info)
    {
        powerBanner = info.fighter.Definition.displayName.ToUpperInvariant() + " — " + info.name.ToUpperInvariant() + "!";
        powerBannerColor = info.color;
        powerBannerUntil = Time.unscaledTime + 1.6f;
    }

    void OnArmorBroken(ArmorBreakInfo info)
    {
        string text = info.region + " armour broken!";
        if (info.fighter == player) { playerNotice = text; playerNoticeUntil = Time.unscaledTime + 2.5f; }
        else if (info.fighter == enemy) { enemyNotice = text; enemyNoticeUntil = Time.unscaledTime + 2.5f; }
    }

    void Start()
    {
        if (database == null) { Debug.LogError("MatchManager needs a GameDatabase."); enabled = false; return; }
        BloodFX.Material = database.spriteMaterial;
        ImpactFX.Material = database.spriteMaterial;
        PowerAura.Material = database.spriteMaterial;
        // Ragdoll chains (torso > arm > forearm > weapon) jitter badly with the default 8/3 solver iterations.
        Physics2D.velocityIterations = 20;
        Physics2D.positionIterations = 8;
        GameFeel.Reset(); // also sets the 0.01 s physics step
        if (GetComponent<HitFeedback>() == null) gameObject.AddComponent<HitFeedback>();
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

        // 2-player: an even fight — full hitboxes, no damage handicaps either way.
        player = Fighter.Spawn(playerDef, playerDef.mainHand, playerDef.offHand, new Vector2(-SpawnX, 0f), 1f, 0, mat, twoPlayer ? 1f : ai.playerHitboxWidth);
        enemy = Fighter.Spawn(enemyDef, enemyDef.mainHand, enemyDef.offHand, new Vector2(SpawnX, 0f), -1f, 20, mat);
        SetupCamera(arena);
        player.Opponent = enemy;
        enemy.Opponent = player;
        if (twoPlayer)
        {
            player.Brain = new PlayerBrain(PlayerBrain.Seat.Player1);
            enemy.Brain = new PlayerBrain(PlayerBrain.Seat.Player2);
        }
        else
        {
            player.Brain = new PlayerBrain();
            enemy.Brain = new AIBrain(ai);
            enemy.DamageMultiplier = ai.damageMultiplier;
            enemy.TouchesHurt = ai.touchesHurt;
            player.DamageTaken = ai.playerDamageTaken;
            player.DamageMultiplier = ai.playerDamageDealt;
        }
        player.KnockedOut += OnKnockedOut;
        enemy.KnockedOut += OnKnockedOut;
        if (powerMode) { player.EnablePowers(); enemy.EnablePowers(); }
        powerBannerUntil = 0f;

        state = State.Fighting;

        if (controls == null) controls = gameObject.AddComponent<OnScreenControls>();
        controls.player = player;
        controls.enabled = showOnScreenControls && !twoPlayer; // one set of buttons can't serve two players
        controls.showPower = powerMode;
    }

    void OnKnockedOut(Fighter loser)
    {
        if (state != State.Fighting) return;
        bool playerWon = loser == enemy;
        if (playerWon) playerScore++; else enemyScore++;
        string winner = (playerWon ? player : enemy).Definition.displayName;
        if (twoPlayer) winner = (playerWon ? "P1 " : "P2 ") + winner;
        bool matchWon = playerScore >= roundsToWin || enemyScore >= roundsToWin;
        roundMessage = matchWon ? winner + " wins the match!" : winner + " wins the round";
        state = matchWon ? State.MatchOver : State.RoundOver;
        roundOverTimer = matchWon ? roundEndDelay + 4f : roundEndDelay;
        GameFeel.SlowMo(1.2f); // knockout slow motion
        if (rig != null) rig.AddTrauma(0.35f);
    }

    CameraRig rig;
    HitboxOverlay hitboxes;

    void Update()
    {
        var kb = Keyboard.current;
        GameFeel.Tick();
        if (state == State.RoundOver || state == State.MatchOver)
        {
            roundOverTimer -= Time.unscaledDeltaTime;
            // Next round; after a match, a rematch (Esc goes back to the menu instead).
            if (roundOverTimer <= 0f) { if (state == State.MatchOver) StartMatch(); else StartRound(); }
        }
        if (kb == null) return;
        if (kb.hKey.wasPressedThisFrame)
        {
            if (hitboxes == null) hitboxes = gameObject.AddComponent<HitboxOverlay>();
            else hitboxes.enabled = !hitboxes.enabled;
        }
        if (kb.f3Key.wasPressedThisFrame) HitEvents.LogHits = !HitEvents.LogHits;
        if (kb.bKey.wasPressedThisFrame) BloodFX.Level = (BloodFX.BloodLevel)(((int)BloodFX.Level + 1) % 3);
        if (state == State.MatchOver && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) StartMatch();
        if (state != State.Select && kb.rKey.wasPressedThisFrame) StartRound();
        if (state != State.Select && kb.escapeKey.wasPressedThisFrame)
        {
            Cleanup();
            state = State.Select;
            if (controls != null) controls.enabled = false;
        }
        if (state == State.Select)
        {
            // Menu keys: A / D change the left fighter, ← / → the right one, Tab switches mode, Enter fights.
            int n = database.fighters.Count;
            if (kb.aKey.wasPressedThisFrame) playerIndex = Wrap(playerIndex - 1, n);
            if (kb.dKey.wasPressedThisFrame) playerIndex = Wrap(playerIndex + 1, n);
            if (kb.leftArrowKey.wasPressedThisFrame) opponentIndex = Wrap(opponentIndex - 1, n);
            if (kb.rightArrowKey.wasPressedThisFrame) opponentIndex = Wrap(opponentIndex + 1, n);
            if (kb.tabKey.wasPressedThisFrame) twoPlayer = !twoPlayer;
            if (kb.pKey.wasPressedThisFrame) powerMode = !powerMode;
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) StartMatch();
        }
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

    // ---------------------------------------------------------------- landing menu

    static readonly Color Gold = new Color(0.93f, 0.74f, 0.3f);
    static readonly Color P1Color = new Color(0.38f, 0.64f, 1f);
    static readonly Color P2Color = new Color(0.96f, 0.38f, 0.32f);
    static readonly Color PanelColor = new Color(0.05f, 0.04f, 0.05f, 0.78f);
    static readonly Color PowerAccent = new Color(0.72f, 0.5f, 1f);

    /// <summary>What each kind of power does, in a few words (menu cards).</summary>
    static string PowerBlurb(PowerKind k)
    {
        switch (k)
        {
            case PowerKind.Rage: return "hits harder and faster";
            case PowerKind.Shockwave: return "a blast that knocks them flat";
            case PowerKind.Freeze: return "freezes the opponent";
            case PowerKind.Dash: return "unblockable dash strike";
            case PowerKind.Fortify: return "blows bounce off";
            case PowerKind.GiantWeapon: return "weapon grows huge";
            case PowerKind.Flurry: return "nonstop flurry of swings";
            case PowerKind.SecondWind: return "stamina, health and speed";
            default: return "";
        }
    }

    /// <summary>The power key for this fighter, or null for the AI.</summary>
    string PowerKey(Fighter f)
    {
        if (twoPlayer) return f == player ? "Q" : "R-Ctrl";
        return f == player ? "E" : null;
    }

    GUIStyle Style(int size, FontStyle fs = FontStyle.Normal, TextAnchor a = TextAnchor.MiddleCenter, Color? c = null)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fs, alignment = a, wordWrap = false, clipping = TextClipping.Overflow };
        SetTextColor(s, c ?? Color.white);
        return s;
    }

    void Shadowed(Rect r, string text, GUIStyle s)
    {
        var col = s.normal.textColor;
        SetTextColor(s, new Color(0f, 0f, 0f, 0.8f));
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, s);
        SetTextColor(s, col);
        GUI.Label(r, text, s);
    }

    /// <summary>Same colour in every state: IMGUI labels switch to the skin's hover colour under the mouse.</summary>
    static void SetTextColor(GUIStyle s, Color c) => s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = c;

    /// <summary>A flat button: dark, outlined in <paramref name="accent"/>, filled with it when lit (selected or hovered).</summary>
    bool FlatButton(Rect r, string text, Color accent, bool selected, int size = 15)
    {
        bool hover = r.Contains(Event.current.mousePosition);
        MenuArt.Fill(r, selected ? new Color(accent.r, accent.g, accent.b, 0.85f) : hover ? new Color(accent.r, accent.g, accent.b, 0.35f) : PanelColor);
        MenuArt.Frame(r, new Color(accent.r, accent.g, accent.b, selected || hover ? 1f : 0.6f));
        GUI.Label(r, text, Style(size, FontStyle.Bold, TextAnchor.MiddleCenter, selected ? new Color(0.08f, 0.06f, 0.06f) : Color.white));
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    int Wrap(int i, int count) => count == 0 ? 0 : ((i % count) + count) % count;

    void DrawSelect(float w)
    {
        const float h = 540f;
        var arena = Pick(database.arenas, arenaIndex);
        MenuArt.Arena(new Rect(0, 0, w, h), arena);
        MenuArt.Fill(new Rect(0, 0, w, h), new Color(0.03f, 0.02f, 0.03f, 0.45f));
        MenuArt.Fill(new Rect(0, 0, w, 120), new Color(0f, 0f, 0f, 0.35f));
        MenuArt.Fill(new Rect(0, h - 92, w, 92), new Color(0f, 0f, 0f, 0.55f));

        // Title + mode tabs
        Shadowed(new Rect(0, 10, w, 56), "RAGDOLL DUEL", Style(46, FontStyle.Bold, TextAnchor.MiddleCenter, Gold));
        GUI.Label(new Rect(0, 58, w, 22), "physics duels starring real people", Style(14, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.8f)));
        if (FlatButton(new Rect(w * 0.5f - 196, 86, 190, 30), "1 PLAYER  vs  CPU", Gold, !twoPlayer)) twoPlayer = false;
        if (FlatButton(new Rect(w * 0.5f + 6, 86, 190, 30), "2 PLAYERS", Gold, twoPlayer)) twoPlayer = true;

        int n = database.fighters.Count;
        playerIndex = Wrap(playerIndex, n);
        opponentIndex = Wrap(opponentIndex, n);

        // Fighter cards
        float cardW = Mathf.Min(300f, (w - 300f) * 0.5f - 20f);
        playerIndex = FighterCard(new Rect(20, 126, cardW, 318), playerIndex, twoPlayer ? "PLAYER 1" : "YOU", P1Color, false, "A / D");
        opponentIndex = FighterCard(new Rect(w - 20 - cardW, 126, cardW, 318), opponentIndex, twoPlayer ? "PLAYER 2" : "CPU", P2Color, true, "← / →");

        // Middle column: VS, arena, difficulty, FIGHT
        float cx = w * 0.5f;
        Shadowed(new Rect(cx - 80, 128, 160, 60), "VS", Style(50, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        arenaIndex = Stepper(new Rect(cx - 130, 222, 260, 34), "ARENA", database.arenas, arenaIndex, a => a.displayName);
        if (!twoPlayer) difficultyIndex = Stepper(new Rect(cx - 130, 276, 260, 34), "DIFFICULTY", database.aiProfiles, difficultyIndex, p => p.displayName);
        else GUI.Label(new Rect(cx - 130, 276, 260, 34), "even fight · no handicaps", Style(13, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f)));
        if (FlatButton(new Rect(cx - 130, 318, 260, 32), powerMode ? "★ SUPER POWERS: ON" : "SUPER POWERS: OFF", PowerAccent, powerMode, 14)) powerMode = !powerMode;
        if (FlatButton(new Rect(cx - 110, 362, 220, 50), "FIGHT!", Gold, true, 26)) StartMatch();
        GUI.Label(new Rect(cx - 110, 412, 220, 20), "Enter", Style(12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.6f)));

        RosterStrip(w, 462f);

        // Controls for the chosen mode
        var hint = Style(12, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));
        string keys = twoPlayer
            ? "<color=#7fb0ff>P1</color>  A / D move · W jump · L-Shift lunge · F / G hands" + (powerMode ? " · Q power" : "") + " · pad 1        " +
              "<color=#ff7a6e>P2</color>  ← / → move · ↑ jump · R-Shift lunge · K / L hands" + (powerMode ? " · R-Ctrl power" : "") + " · pad 2"
            : "J / K  right / left hand (weapons swing, shields block) · A / D back / forward · Space jump · Shift lunge" + (powerMode ? " · E super power" : "") + " · hold mouse buttons to aim freely";
        hint.richText = true;
        GUI.Label(new Rect(0, h - 36, w, 18), keys, hint);
        GUI.Label(new Rect(0, h - 19, w, 18), "Tab: switch mode · P: super powers · H hitboxes · B blood · R restart round · Esc menu", hint);
    }

    /// <summary>One side's fighter: header, full-body portrait, name, tagline, loadout, stat bars, prev / next.</summary>
    int FighterCard(Rect r, int index, string header, Color accent, bool faceLeft, string keyHint)
    {
        int n = database.fighters.Count;
        var def = n > 0 ? database.fighters[index] : null;
        MenuArt.Fill(r, PanelColor);
        MenuArt.Fill(new Rect(r.x, r.y, r.width, 26), new Color(accent.r, accent.g, accent.b, 0.9f));
        GUI.Label(new Rect(r.x + 10, r.y, r.width - 20, 26), header, Style(15, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.06f, 0.05f, 0.05f)));
        GUI.Label(new Rect(r.x + 10, r.y, r.width - 20, 26), (index + 1) + " / " + n + "   " + keyHint, Style(12, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.06f, 0.05f, 0.05f, 0.8f)));
        MenuArt.Frame(r, new Color(accent.r, accent.g, accent.b, 0.8f));
        if (def == null) return index;

        // Portrait with prev / next arrows either side
        var stage = new Rect(r.x + 40, r.y + 34, r.width - 80, 126);
        MenuArt.Fighter(stage, def, faceLeft);
        if (FlatButton(new Rect(r.x + 8, stage.center.y - 20, 28, 40), "<", accent, false, 18)) index = Wrap(index - 1, n);
        if (FlatButton(new Rect(r.xMax - 36, stage.center.y - 20, 28, 40), ">", accent, false, 18)) index = Wrap(index + 1, n);

        float y = stage.yMax + 6;
        Shadowed(new Rect(r.x, y, r.width, 28), def.displayName.ToUpperInvariant(), Style(22, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
        y += 28;
        if (!string.IsNullOrEmpty(def.tagline))
            GUI.Label(new Rect(r.x + 8, y, r.width - 16, 18), def.tagline, Style(12, FontStyle.Italic, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f)));
        y += 20;
        string main = def.mainHand != null ? def.mainHand.displayName : "Fist";
        string off = def.offHand != null ? def.offHand.displayName : "Fist";
        GUI.Label(new Rect(r.x, y, r.width, 18), main + "  +  " + off, Style(13, FontStyle.Bold, TextAnchor.MiddleCenter, Gold));
        y += 20;
        if (powerMode && def.power != PowerKind.None)
        {
            var pc = Color.Lerp(def.powerColor, Color.white, 0.25f);
            GUI.Label(new Rect(r.x, y, r.width, 18), "★ " + def.powerName + "  ·  " + PowerBlurb(def.power), Style(12, FontStyle.Bold, TextAnchor.MiddleCenter, pc));
        }
        y += 20;

        StatBar(new Rect(r.x + 14, y, r.width - 28, 10), "HEALTH", Mathf.InverseLerp(70f, 130f, def.maxHealth), accent); y += 16;
        StatBar(new Rect(r.x + 14, y, r.width - 28, 10), "POWER", Mathf.InverseLerp(0.8f, 1.25f, def.strength), accent); y += 16;
        StatBar(new Rect(r.x + 14, y, r.width - 28, 10), "SPEED", Mathf.InverseLerp(1.6f, 2.8f, def.walkSpeed), accent); y += 16;
        StatBar(new Rect(r.x + 14, y, r.width - 28, 10), "ARMOUR", ArmorScore(def), accent);
        return index;
    }

    void StatBar(Rect r, string label, float t, Color accent)
    {
        GUI.Label(new Rect(r.x, r.y - 4, 70, r.height + 8), label, Style(10, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(1f, 1f, 1f, 0.7f)));
        var bar = new Rect(r.x + 70, r.y, r.width - 70, r.height);
        MenuArt.Fill(bar, new Color(1f, 1f, 1f, 0.12f));
        MenuArt.Fill(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp(t, 0.06f, 1f), bar.height), accent);
    }

    /// <summary>0..1 from what the four regions are covered with (plate counts most).</summary>
    static float ArmorScore(FighterDefinition f)
    {
        float Score(ArmorMaterial m) => m == ArmorMaterial.Plate ? 1f : m == ArmorMaterial.Mail ? 0.75f : m == ArmorMaterial.Leather ? 0.45f : m == ArmorMaterial.Cloth ? 0.15f : 0f;
        return (Score(f.headArmor) + Score(f.torsoArmor) * 1.5f + Score(f.armArmor) + Score(f.legArmor)) / 4.5f;
    }

    /// <summary>Label above, then &lt; name &gt;.</summary>
    int Stepper<T>(Rect r, string label, System.Collections.Generic.List<T> list, int index, System.Func<T, string> name) where T : Object
    {
        int n = list.Count;
        index = Wrap(index, n);
        GUI.Label(new Rect(r.x, r.y - 18, r.width, 18), label, Style(11, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f)));
        MenuArt.Fill(r, PanelColor);
        if (FlatButton(new Rect(r.x, r.y, 34, r.height), "<", Gold, false, 16)) index = Wrap(index - 1, n);
        if (FlatButton(new Rect(r.xMax - 34, r.y, 34, r.height), ">", Gold, false, 16)) index = Wrap(index + 1, n);
        GUI.Label(new Rect(r.x + 34, r.y, r.width - 68, r.height), n > 0 ? name(list[index]) : "(none)", Style(15, FontStyle.Bold));
        return index;
    }

    /// <summary>Every fighter's head in a row: click picks the left fighter, right-click the right one.</summary>
    void RosterStrip(float w, float y)
    {
        int n = database.fighters.Count;
        if (n == 0) return;
        float size = Mathf.Min(40f, (w - 40f) / n - 4f), step = size + 4f;
        float x0 = w * 0.5f - step * n * 0.5f + 2f;
        string hovered = null;
        for (int i = 0; i < n; i++)
        {
            var box = new Rect(x0 + i * step, y, size, size);
            var def = database.fighters[i];
            bool p1 = i == playerIndex, p2 = i == opponentIndex;
            MenuArt.Fill(box, PanelColor);
            MenuArt.Head(new Rect(box.x + 3, box.y + 3, box.width - 6, box.height - 6), def);
            if (p1 && p2) { MenuArt.Frame(box, P1Color); MenuArt.Frame(new Rect(box.x + 2, box.y + 2, box.width - 4, box.height - 4), P2Color); }
            else if (p1) MenuArt.Frame(box, P1Color);
            else if (p2) MenuArt.Frame(box, P2Color);
            else MenuArt.Frame(box, new Color(1f, 1f, 1f, 0.15f), 1f);

            var e = Event.current;
            if (box.Contains(e.mousePosition))
            {
                hovered = def.displayName;
                if (e.type == EventType.MouseDown && (e.button == 0 || e.button == 1))
                {
                    if (e.button == 0) playerIndex = i; else opponentIndex = i;
                    e.Use();
                }
            }
        }
        string caption = hovered ?? "click = left fighter · right-click = right fighter";
        GUI.Label(new Rect(0, y - 18, w, 16), caption, Style(11, hovered != null ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, hovered != null ? 0.95f : 0.55f)));
    }

    void DrawHud(float w)
    {
        if (player == null || enemy == null) return;
        HealthBar(new Rect(20, 20, w * 0.4f, 22), player, false);
        HealthBar(new Rect(w - 20 - w * 0.4f, 20, w * 0.4f, 22), enemy, true);
        GUI.Label(new Rect(w * 0.5f - 60f, 14, 120, 34), playerScore + " - " + enemyScore, center);
        GUI.Label(new Rect(w * 0.5f - 90f, 40, 180, 24), "Best of " + (roundsToWin * 2 - 1), SmallCentered());

        if (Time.unscaledTime < powerBannerUntil && !string.IsNullOrEmpty(powerBanner))
        {
            // Pops in big, settles, fades out.
            float age = 1.6f - (powerBannerUntil - Time.unscaledTime);
            int size = Mathf.RoundToInt(Mathf.Lerp(44f, 32f, Mathf.Clamp01(age / 0.15f)));
            var c = Color.Lerp(powerBannerColor, Color.white, 0.2f);
            c.a = Mathf.Clamp01((powerBannerUntil - Time.unscaledTime) / 0.3f);
            Shadowed(new Rect(0, 78, w, 50), powerBanner, Style(size, FontStyle.Bold, TextAnchor.MiddleCenter, c));
        }

        if (state == State.RoundOver || state == State.MatchOver)
            GUI.Label(new Rect(0, 200, w, 60), roundMessage, big);
        if (state == State.MatchOver)
            GUI.Label(new Rect(0, 262, w, 30), "Enter: rematch    Esc: menu", center);
    }

    GUIStyle SmallCentered() => new GUIStyle(normal) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };

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

        float below = ArmorBar(new Rect(r.x, stam.yMax + 2, r.width * 0.5f, 7), f, rightAligned, r.xMax);
        string powerNote = null;
        if (f.Powers != null) below = PowerMeter(new Rect(r.x, below + 2, r.width * 0.5f, 7), f, rightAligned, r.xMax, out powerNote);

        var label = new Rect(r.x, below + 2, r.width, 22);
        normal.alignment = rightAligned ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        string status = f.IsKnockedOut ? "" : f.State == Fighter.Condition.Fainted ? "   FAINTED" : f.State == Fighter.Condition.Down ? "   DOWN" : "";
        bool mine = f == player;
        string notice = (mine ? playerNoticeUntil : enemyNoticeUntil) > Time.unscaledTime ? (mine ? playerNotice : enemyNotice) : null;
        if (notice != null) status += "   " + notice;
        if (powerNote != null) status += "   " + powerNote;
        string name = f.Definition.displayName;
        if (twoPlayer) name = mine ? "P1  " + name : name + "  P2";
        GUI.Label(label, rightAligned ? status.Trim() + "   " + name : name + status, normal);
    }

    /// <summary>
    /// The super meter: violet while charging, the power's colour (pulsing) when full, draining while it's active.
    /// Returns the bottom edge; <paramref name="note"/> says READY (with the key) or names the power while it runs.
    /// </summary>
    float PowerMeter(Rect r, Fighter f, bool rightAligned, float rightEdge, out string note)
    {
        var p = f.Powers;
        note = null;
        if (rightAligned) r.x = rightEdge - r.width;
        MenuArt.Fill(r, new Color(0f, 0f, 0f, 0.8f));
        float t;
        Color c;
        if (p.Active) { t = p.ActiveLeft; c = p.Color; note = "★ " + p.Name.ToUpperInvariant(); }
        else if (p.Ready)
        {
            t = 1f;
            c = Color.Lerp(p.Color, Color.white, 0.35f + 0.35f * Mathf.Sin(Time.unscaledTime * 10f));
            string key = PowerKey(f);
            note = "★ POWER READY" + (key != null ? " [" + key + "]" : "");
        }
        else { t = p.Meter; c = new Color(0.62f, 0.42f, 0.95f); }
        var fill = new Rect(r.x + 1, r.y + 1, (r.width - 2) * t, r.height - 2);
        if (rightAligned) fill.x = r.xMax - 1 - fill.width;
        MenuArt.Fill(fill, c);
        return r.yMax;
    }

    static readonly PartKind[] ArmorRegions = { PartKind.Head, PartKind.Torso, PartKind.Arm, PartKind.Leg };

    /// <summary>
    /// Four small segments (head, torso, arms, legs), steel-blue by how much armour is left; returns the bottom edge.
    /// Regions with no breakable armour stay dark; broken ones turn dark red.
    /// </summary>
    float ArmorBar(Rect r, Fighter f, bool rightAligned, float rightEdge)
    {
        if (f.Armor == null || !f.Armor.HasAny) return r.y - 2;
        if (rightAligned) r.x = rightEdge - r.width;
        float gap = 3f, seg = (r.width - gap * 3f) / 4f;
        for (int i = 0; i < 4; i++)
        {
            int slot = rightAligned ? 3 - i : i;
            var box = new Rect(r.x + slot * (seg + gap), r.y, seg, r.height);
            var kind = ArmorRegions[i];
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            if (!f.Armor.Has(kind)) continue;
            float t = f.Armor.Integrity(kind);
            if (t <= 0f) { GUI.color = new Color(0.45f, 0.08f, 0.08f); GUI.DrawTexture(new Rect(box.x + 1, box.y + 1, box.width - 2, box.height - 2), Texture2D.whiteTexture); continue; }
            var fill = new Rect(box.x + 1, box.y + 1, (box.width - 2) * t, box.height - 2);
            if (rightAligned) fill.x = box.xMax - 1 - fill.width;
            GUI.color = Color.Lerp(new Color(0.85f, 0.45f, 0.2f), new Color(0.62f, 0.75f, 0.9f), t);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
        }
        GUI.color = Color.white;
        return r.yMax;
    }
}
