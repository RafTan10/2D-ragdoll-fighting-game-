using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On-screen buttons for the player, usable with touch or the mouse:
/// move left / right, jump, and one button per hand. A hand button swings that hand's weapon
/// (hold to keep swinging); if the hand holds a shield it blocks while held instead.
/// Runs before the fighters so <see cref="PlayerBrain"/> sees this frame's state.
/// Drawn with IMGUI for now, like the rest of the placeholder HUD.
/// </summary>
[DefaultExecutionOrder(-100)]
public class OnScreenControls : MonoBehaviour
{
    public static OnScreenControls Current { get; private set; }

    public enum Button { Left, Right, Jump, OffHand, MainHand }

    /// <summary>Set by the match: the player fighter, for the hand labels.</summary>
    public Fighter player;

    // ---- state read by PlayerBrain
    public bool MoveLeft => held[(int)Button.Left];
    public bool MoveRight => held[(int)Button.Right];
    public bool JumpPressed { get; private set; }
    /// <summary>Right hand (main) button held.</summary>
    public bool MainHeld => held[(int)Button.MainHand];
    /// <summary>Left hand (off) button held.</summary>
    public bool OffHeld => held[(int)Button.OffHand];
    /// <summary>True while the mouse's left button is pressing one of these buttons (so it isn't also aiming).</summary>
    public bool MouseCaptured => mouseButton >= 0;
    /// <summary>Whether a touch that began at this screen position belongs to the buttons.</summary>
    public bool OwnsTouchStartedAt(Vector2 screenPos) => HitTest(ToGui(screenPos)) >= 0;

    const float DesignHeight = 540f;

    readonly bool[] held = new bool[5];
    readonly bool[] forced = new bool[5];

    /// <summary>Holds a button from code (tests, tutorials, replays) as if a finger were on it.</summary>
    public void Force(Button b, bool on)
    {
        if (on && !forced[(int)b] && b == Button.Jump) forcedJump = true;
        forced[(int)b] = on;
    }
    bool forcedJump;
    readonly int[] touchOnButton = new int[16];
    int mouseButton = -1;
    readonly Rect[] rects = new Rect[5];

    GUIStyle label, small;

    void OnEnable() { Current = this; for (int i = 0; i < touchOnButton.Length; i++) touchOnButton[i] = -1; }
    void OnDisable() { if (Current == this) Current = null; for (int i = 0; i < held.Length; i++) held[i] = false; mouseButton = -1; }

    float Scale => Screen.height / DesignHeight;
    float GuiWidth => Screen.width / Scale;
    Vector2 ToGui(Vector2 screen) => new Vector2(screen.x / Scale, (Screen.height - screen.y) / Scale);

    void Layout()
    {
        float w = GuiWidth, h = DesignHeight;
        rects[(int)Button.Left] = new Rect(24, h - 118, 92, 92);
        rects[(int)Button.Right] = new Rect(128, h - 118, 92, 92);
        rects[(int)Button.MainHand] = new Rect(w - 140, h - 150, 116, 116);
        rects[(int)Button.OffHand] = new Rect(w - 268, h - 122, 104, 104);
        rects[(int)Button.Jump] = new Rect(w - 128, h - 240, 92, 70);
    }

    int HitTest(Vector2 gui)
    {
        Layout();
        for (int i = 0; i < rects.Length; i++)
            if (rects[i].Contains(gui)) return i;
        return -1;
    }

    void Update()
    {
        Layout();
        JumpPressed = forcedJump;
        forcedJump = false;
        for (int i = 0; i < held.Length; i++) held[i] = forced[i];

        // Mouse: a press that starts on a button is captured by it until release.
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 gui = ToGui(mouse.position.ReadValue());
            if (mouse.leftButton.wasPressedThisFrame)
            {
                mouseButton = HitTest(gui);
                if (mouseButton == (int)Button.Jump) JumpPressed = true;
            }
            if (!mouse.leftButton.isPressed) mouseButton = -1;
            if (mouseButton >= 0) held[mouseButton] = true;
        }

        // Touch: same idea per finger, so several buttons can be held at once.
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count && i < touchOnButton.Length; i++)
            {
                var touch = touches[i];
                Vector2 gui = ToGui(touch.position.ReadValue());
                if (touch.press.wasPressedThisFrame)
                {
                    touchOnButton[i] = HitTest(gui);
                    if (touchOnButton[i] == (int)Button.Jump) JumpPressed = true;
                }
                if (!touch.press.isPressed) touchOnButton[i] = -1;
                if (touchOnButton[i] >= 0) held[touchOnButton[i]] = true;
            }
        }
    }

    // ================================================================ drawing

    void OnGUI()
    {
        Layout();
        GUI.matrix = Matrix4x4.Scale(new Vector3(Scale, Scale, 1f));
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 26, fontStyle = FontStyle.Bold };
            small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter, fontSize = 13 };
        }
        label.normal.textColor = small.normal.textColor = new Color(1f, 1f, 1f, 0.92f);

        DrawButton(Button.Left, "<", null);
        DrawButton(Button.Right, ">", null);
        DrawButton(Button.Jump, "JUMP", null);
        DrawButton(Button.OffHand, "L", HandLabel(player != null ? player.OffHand : null));
        DrawButton(Button.MainHand, "R", HandLabel(player != null ? player.MainHand : null));
    }

    /// <summary>"Shield: block" / "Sword: swing" — what the button will do with what's in that hand.</summary>
    static string HandLabel(Fighter.Hand hand)
    {
        if (hand == null) return null;
        string item = hand.weapon != null ? hand.weapon.displayName : "Fist";
        return item + (hand.IsShield ? " · block" : " · swing");
    }

    void DrawButton(Button b, string text, string caption)
    {
        Rect r = rects[(int)b];
        bool down = held[(int)b];
        GUI.color = down ? new Color(0.75f, 0.2f, 0.15f, 0.8f) : new Color(0.06f, 0.05f, 0.04f, 0.72f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, 0.35f);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.yMax - 2, r.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.x, r.y, 2, r.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(r.xMax - 2, r.y, 2, r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(r.x, r.y - (caption != null ? 8 : 0), r.width, r.height), text, label);
        if (caption != null) GUI.Label(new Rect(r.x, r.y, r.width, r.height - 8), caption, small);
    }
}
