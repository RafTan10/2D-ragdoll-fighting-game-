using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Local player controls.
/// Hand actions — each hand does what its item is for: a weapon swings (hold to keep swinging, alternating an
/// overhead chop and a side slash), a shield blocks while held.
///   Right hand (main): on-screen R, keyboard J, gamepad right trigger.
///   Left hand (off):   on-screen L, keyboard K, gamepad left trigger.
/// Free aim — the hand reaches for a point you choose: hold left / right mouse (cursor), gamepad right / left stick,
///   or drag on the right / left half of a touchscreen.
/// Movement — keyboard A = step back, D = step forward (relative to where you face), Space/W jump, Shift lunge;
///   on-screen &lt; &gt; and JUMP; gamepad d-pad, A/south jump, B/east lunge; touch double-tap lunge.
///
/// Local 2-player (<see cref="Seat"/>): the keyboard is split and each player gets their own gamepad; mouse, touch
/// and the on-screen buttons are off.
///   Player 1: A back, D forward, W jump, Left Shift lunge, F left hand, G right hand; first gamepad.
///   Player 2: → back, ← forward (mirrors P1, who starts on the left), ↑ jump, Right Shift lunge, K left hand,
///             L right hand (or numpad 1 / 2); second gamepad.
/// </summary>
public class PlayerBrain : IFighterBrain
{
    public enum Seat { Solo, Player1, Player2 }

    const float TouchStickRadius = 110f;   // pixels of drag for a fully stretched arm
    const float StickDeadZone = 0.25f;
    const float SwingWindUp = 0.2f;

    readonly Seat seat;
    float lastTapTime = -10f;
    readonly Vector2[] touchStart = new Vector2[10];
    readonly SwingMove mainSwing = new SwingMove(), offSwing = new SwingMove();
    int mainSwingCount, offSwingCount;
    bool mainWasPressed, offWasPressed;

    public PlayerBrain(Seat seat = Seat.Solo) { this.seat = seat; }

    /// <summary>The gamepad for this seat: any pad when solo, otherwise the first / second one connected.</summary>
    Gamepad Pad
    {
        get
        {
            if (seat == Seat.Solo) return Gamepad.current;
            int i = seat == Seat.Player1 ? 0 : 1;
            return Gamepad.all.Count > i ? Gamepad.all[i] : null;
        }
    }

    public FighterInput Think(Fighter self, Fighter opponent)
    {
        var input = new FighterInput();
        float mainReach = self.MainHand.Reach, offReach = self.OffHand.Reach;
        bool solo = seat == Seat.Solo;
        var buttons = solo ? OnScreenControls.Current : null;
        bool mainAction = false, offAction = false;

        var keyboard = Keyboard.current;
        if (keyboard != null && seat == Seat.Player1)
        {
            if (keyboard.aKey.isPressed) input.move -= self.Facing;
            if (keyboard.dKey.isPressed) input.move += self.Facing;
            input.jump |= keyboard.wKey.wasPressedThisFrame;
            input.hop |= keyboard.leftShiftKey.wasPressedThisFrame;
            offAction |= keyboard.fKey.isPressed;
            mainAction |= keyboard.gKey.isPressed;
        }
        else if (keyboard != null && seat == Seat.Player2)
        {
            if (keyboard.rightArrowKey.isPressed) input.move -= self.Facing;
            if (keyboard.leftArrowKey.isPressed) input.move += self.Facing;
            input.jump |= keyboard.upArrowKey.wasPressedThisFrame;
            input.hop |= keyboard.rightShiftKey.wasPressedThisFrame;
            offAction |= keyboard.kKey.isPressed || keyboard.numpad1Key.isPressed;
            mainAction |= keyboard.lKey.isPressed || keyboard.numpad2Key.isPressed;
        }
        else if (keyboard != null)
        {
            // A / D are back / forward relative to the way the fighter faces.
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.move -= self.Facing;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.move += self.Facing;
            input.jump |= keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
            input.hop |= keyboard.leftShiftKey.wasPressedThisFrame;
            mainAction |= keyboard.jKey.isPressed;
            offAction |= keyboard.kKey.isPressed;
        }

        var mouse = Mouse.current;
        var cam = Camera.main;
        if (solo && mouse != null && cam != null)
        {
            Vector2 cursor = cam.ScreenToWorldPoint(mouse.position.ReadValue());
            bool onButtons = buttons != null && buttons.MouseCaptured;
            if (mouse.leftButton.isPressed && !onButtons) Set(ref input.mainActive, ref input.mainAim, cursor - self.MainHand.ShoulderPosition);
            if (mouse.rightButton.isPressed) Set(ref input.offActive, ref input.offAim, cursor - self.OffHand.ShoulderPosition);
        }

        var pad = Pad;
        if (pad != null)
        {
            Vector2 right = pad.rightStick.ReadValue(), left = pad.leftStick.ReadValue();
            if (right.magnitude > StickDeadZone) Set(ref input.mainActive, ref input.mainAim, right * mainReach);
            if (left.magnitude > StickDeadZone) Set(ref input.offActive, ref input.offAim, left * offReach);
            input.move += pad.dpad.ReadValue().x;
            input.jump |= pad.buttonSouth.wasPressedThisFrame;
            input.hop |= pad.buttonEast.wasPressedThisFrame;
            mainAction |= pad.rightTrigger.isPressed;
            offAction |= pad.leftTrigger.isPressed;
        }

        var touchscreen = solo ? Touchscreen.current : null;
        if (touchscreen != null)
        {
            var touches = touchscreen.touches;
            for (int i = 0; i < touches.Count && i < touchStart.Length; i++)
            {
                var touch = touches[i];
                Vector2 pos = touch.position.ReadValue();
                if (touch.press.wasPressedThisFrame)
                {
                    touchStart[i] = pos;
                    bool onButton = buttons != null && buttons.OwnsTouchStartedAt(pos);
                    if (!onButton && Time.time - lastTapTime < 0.3f) input.hop = true;
                    if (!onButton) lastTapTime = Time.time;
                }
                if (!touch.press.isPressed) continue;
                if (buttons != null && buttons.OwnsTouchStartedAt(touchStart[i])) continue; // handled by the buttons

                Vector2 drag = Vector2.ClampMagnitude((pos - touchStart[i]) / TouchStickRadius, 1f);
                Vector2 dir = Vector2.ClampMagnitude(new Vector2(self.Facing * 0.6f, 0f) + drag, 1f);
                if (touchStart[i].x > Screen.width * 0.5f) Set(ref input.mainActive, ref input.mainAim, dir * mainReach);
                else Set(ref input.offActive, ref input.offAim, dir * offReach);
            }
        }

        if (buttons != null)
        {
            if (buttons.MoveLeft) input.move -= 1f;
            if (buttons.MoveRight) input.move += 1f;
            input.jump |= buttons.JumpPressed;
            mainAction |= buttons.MainHeld;
            offAction |= buttons.OffHeld;
        }

        // A freely aimed weapon hand is attacking (its speed decides whether a contact hurts); a shield never is.
        input.mainStriking = input.mainActive && !self.MainHand.IsShield;
        input.offStriking = input.offActive && !self.OffHand.IsShield;

        // Hand actions win over free aim for that hand.
        HandAction(self, opponent, self.MainHand, mainSwing, ref mainSwingCount, mainAction, ref mainWasPressed, ref input.mainActive, ref input.mainAim, ref input.mainStriking);
        HandAction(self, opponent, self.OffHand, offSwing, ref offSwingCount, offAction, ref offWasPressed, ref input.offActive, ref input.offAim, ref input.offStriking);

        input.attacking = mainSwing.InStrike || offSwing.InStrike;
        input.move = Mathf.Clamp(input.move, -1f, 1f);
        return input;
    }

    /// <summary>
    /// Shield: pressing it punches the shield into a nearby opponent (a bash that shoves them), then it blocks while
    /// held. Weapon or fist: start a swing on press, keep swinging while held.
    /// </summary>
    static void HandAction(Fighter self, Fighter opponent, Fighter.Hand hand, SwingMove swing, ref int count,
                           bool pressed, ref bool wasPressed, ref bool active, ref Vector2 aim, ref bool striking)
    {
        bool justPressed = pressed && !wasPressed;
        wasPressed = pressed;
        if (hand.IsShield)
        {
            bool close = opponent != null && Mathf.Abs(opponent.Torso.position.x - self.Torso.position.x) < self.Reach * 1.1f;
            if (justPressed && close) swing.Start(SwingMove.Kind.Bash, 0.1f);
            if (swing.Running && swing.Aim(self, hand, opponent, out var bashAim)) { Set(ref active, ref aim, bashAim); striking = swing.InStrike; }
            else if (pressed) { Set(ref active, ref aim, SwingMove.BlockAim(self, hand, opponent)); striking = false; }
            return;
        }
        if (pressed && !swing.Running)
        {
            swing.Start(count % 2 == 0 ? SwingMove.Kind.Chop : SwingMove.Kind.Slash, SwingWindUp);
            count++;
        }
        // Only the driving part of a swing can hurt; the wind-up and recovery can't.
        if (swing.Running && swing.Aim(self, hand, opponent, out var swingAim)) { Set(ref active, ref aim, swingAim); striking = swing.InStrike; }
    }

    static void Set(ref bool active, ref Vector2 aim, Vector2 value)
    {
        active = true;
        aim = value;
    }
}
