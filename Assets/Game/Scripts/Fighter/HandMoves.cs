using UnityEngine;

/// <summary>
/// Ready-made hand movements that brains can play: a swing is a path for the hand's aim point
/// (the arm solver and physics do the rest), a block puts a shield between the opponent's weapon and the body.
/// Shared by the player's swing/block buttons and the AI.
/// </summary>
public class SwingMove
{
    public enum Kind { Chop, Slash, Thrust, Spin, Bash }

    public bool Running { get; private set; }
    public Kind Current { get; private set; }
    /// <summary>Added to the target of aimed moves (thrusts): how far off this attempt is. Set after Start.</summary>
    public Vector2 AimOffset;

    /// <summary>
    /// Chops and slashes are driven by the real arm, not a clock: the aim point stays this many degrees ahead of where
    /// the hand actually is, so the arm sweeps the whole arc at its true top speed (a heavy weapon takes longer) and the
    /// strike lasts until the arc is done. A clock-driven path ran ~0.8 m ahead of the hand and blades went astray.
    /// </summary>
    public static float StrikeLead = 70f;
    /// <summary>Longest a strike may take before it's abandoned (a blocked or stuck swing), seconds.</summary>
    public static float MaxStrike = 0.6f;
    /// <summary>The wind-up waits (up to this long past its time) for the hand to actually reach the top of the arc.</summary>
    public static float MaxWindUpWait = 0.25f;

    float startTime, windUp;
    float spinAngle;
    bool striking;
    float strikeStart, lastAngle;

    /// <summary>Starts a swing (ignored while one is already running).</summary>
    public bool Start(Kind kind, float windUpSeconds)
    {
        if (Running) return false;
        Running = true;
        Current = kind;
        AimOffset = Vector2.zero;
        startTime = Time.time;
        windUp = Mathf.Max(0.08f, windUpSeconds);
        spinAngle = 30f;
        striking = false;
        return true;
    }

    public void Cancel() => Running = false;

    /// <summary>Driving at the target (past the wind-up): the body should commit, and only now can the weapon hurt fully.</summary>
    public bool InStrike => Running && (Current == Kind.Spin || striking);

    /// <summary>Where the hand really is, as an angle from the shoulder (same convention as <see cref="Polar"/>),
    /// unwrapped next to <paramref name="near"/> so arcs over the top (beyond 180°) stay continuous.</summary>
    static float HandAngle(Fighter self, Fighter.Hand hand, float near)
    {
        Vector2 d = hand.HandPosition - hand.ShoulderPosition;
        float raw = Mathf.Atan2(d.x * self.Facing, -d.y) * Mathf.Rad2Deg;
        return near + Mathf.DeltaAngle(near, raw);
    }

    /// <summary>
    /// Wind up to <paramref name="from"/>, then drive the hand from there to <paramref name="to"/> with the aim point
    /// <see cref="StrikeLead"/> degrees ahead of the real hand. False once the arc is complete.
    /// </summary>
    bool ArmDriven(Fighter self, Fighter.Hand hand, float rest, float from, float to, float radius, out Vector2 aim)
    {
        float t = Time.time - startTime;
        if (!striking)
        {
            aim = Polar(self, Mathf.Lerp(rest, from, Smooth(Mathf.Clamp01(t / windUp))), radius);
            float at = HandAngle(self, hand, from);
            if (t >= windUp && (Mathf.Abs(at - from) < 30f || t >= windUp + MaxWindUpWait))
            {
                striking = true;
                strikeStart = Time.time;
                lastAngle = at;
            }
            return true;
        }
        float actual = HandAngle(self, hand, lastAngle);
        lastAngle = actual;
        float direction = Mathf.Sign(to - from);
        bool done = (to - actual) * direction <= 4f || Time.time - strikeStart > MaxStrike;
        if (done) { aim = Vector2.zero; Running = false; return false; }
        aim = Polar(self, Mathf.MoveTowards(actual, to, StrikeLead), radius);
        return true;
    }

    /// <summary>
    /// Angle (0° = straight down, 90° = level forward, facing-right frame) from the shoulder to the opponent's
    /// head (<paramref name="head"/>) or chest, so swings arc through where they really are.
    /// </summary>
    float TargetAngle(Fighter self, Fighter.Hand hand, Fighter opponent, bool head)
    {
        if (opponent == null) return head ? 105f : 90f;
        Vector2 target = (head ? opponent.Head.position : opponent.Torso.position + Vector2.up * 0.12f) + AimOffset;
        Vector2 d = target - hand.ShoulderPosition;
        float angle = Mathf.Atan2(d.x * self.Facing, -d.y) * Mathf.Rad2Deg;
        return Mathf.Clamp(angle, 40f, 150f); // behind us or straight up/down: fall back to a sensible arc
    }

    /// <summary>Aim point (offset from the shoulder) for this frame; false once the swing has finished.</summary>
    public bool Aim(Fighter self, Fighter.Hand hand, Fighter opponent, out Vector2 aim)
    {
        aim = Vector2.zero;
        if (!Running) return false;
        float t = Time.time - startTime;
        float reach = hand.Reach;

        switch (Current)
        {
            case Kind.Chop:
            {
                // Lift the hand up and back over the shoulder, then bring it over the top and down *through the
                // opponent's head*: the arc is centred on where they actually are, and the wrist (see Weapon.wrist)
                // puts the blade in line with the arm, so the tip sweeps through them.
                float target = TargetAngle(self, hand, opponent, true);
                return ArmDriven(self, hand, 40f, Mathf.Min(target + 115f, 225f), target - 95f,
                                 striking ? SwingRadius(hand, opponent, true, reach) : reach, out aim);
            }
            case Kind.Slash:
            {
                // Draw the hand back, then whip it forward through the opponent's chest and on past.
                float target = TargetAngle(self, hand, opponent, false);
                return ArmDriven(self, hand, 60f, target + 70f, target - 80f,
                                 striking ? SwingRadius(hand, opponent, false, reach) : reach, out aim);
            }
            case Kind.Thrust:
            {
                // Draw back, then drive the hand at the opponent's head.
                const float lunge = 0.28f;
                striking = t >= windUp;
                if (t < windUp) aim = Polar(self, 70f, reach * 0.4f);
                else if (t < windUp + lunge)
                {
                    Vector2 target = opponent != null ? opponent.Head.position + AimOffset - hand.ShoulderPosition : Polar(self, 95f, reach);
                    aim = target.normalized * reach;
                }
                else { Running = false; return false; }
                return true;
            }
            case Kind.Bash:
            {
                // Pull the shield in to the chest, then punch it straight out into the opponent.
                const float punch = 0.18f;
                striking = t >= windUp;
                Vector2 chest = opponent != null ? opponent.Torso.position + Vector2.up * 0.1f : hand.ShoulderPosition + Polar(self, 90f, reach);
                if (t < windUp) aim = Polar(self, 70f, reach * 0.35f);
                else if (t < windUp + punch) aim = Vector2.ClampMagnitude(chest - hand.ShoulderPosition, reach) * 1.3f; // aim past them
                else { Running = false; return false; }
                return true;
            }
            case Kind.Spin:
            {
                // Whirl the weapon in full circles: momentum builds every turn.
                const float duration = 1.3f, degreesPerSecond = 540f;
                if (t > duration) { Running = false; return false; }
                spinAngle += degreesPerSecond * Time.deltaTime;
                aim = Polar(self, spinAngle, reach);
                return true;
            }
        }
        Running = false;
        return false;
    }

    /// <summary>
    /// Shield (or weapon) guard: between the opponent's weapon and our chest, or straight out in front if
    /// there's no clear threat.
    /// </summary>
    public static Vector2 BlockAim(Fighter self, Fighter.Hand hand, Fighter opponent)
    {
        Vector2 chest = self.Torso.position + Vector2.up * 0.22f;
        Vector2 threat = chest + new Vector2(self.Facing * 0.8f, 0.15f);
        if (opponent != null && opponent.MainHand.weaponObject != null)
        {
            Vector2 enemyWeapon = opponent.MainHand.weaponObject.transform.position;
            if (Vector2.Distance(enemyWeapon, chest) < opponent.Reach * 1.6f) threat = enemyWeapon;
        }
        Vector2 between = Vector2.Lerp(chest, threat, 0.5f) - hand.ShoulderPosition;
        // Never tuck the shield behind us.
        if (between.x * self.Facing < 0.12f) between.x = self.Facing * 0.12f;
        return Vector2.ClampMagnitude(between, hand.Reach * 0.95f);
    }

    /// <summary>Point at <paramref name="radius"/> from the shoulder; 0° = straight down, 90° = at the opponent.</summary>
    public static Vector2 Polar(Fighter self, float degrees, float radius)
    {
        float a = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(a) * self.Facing, -Mathf.Cos(a)) * radius;
    }

    static float Smooth(float x) => x * x * (3f - 2f * x);

    /// <summary>
    /// How far out to swing the hand so the target lands on the weapon's striking part (the wrist puts the blade in
    /// line with the arm while striking): full arm's length for a distant opponent, pulled in for a close one —
    /// otherwise a close chop sweeps the blade behind them and only the handle or forearm touches.
    /// </summary>
    public static bool AdaptiveRadius = true;

    static float SwingRadius(Fighter.Hand hand, Fighter opponent, bool head, float reach)
    {
        if (opponent == null || !AdaptiveRadius) return reach;
        float blade = hand.weapon != null && hand.weapon.sprite != null
            ? (hand.weapon.sprite.rect.height - hand.weapon.gripPixels) / Fighter.PixelsPerUnit : 0.12f;
        Vector2 target = head ? opponent.Head.position : opponent.Torso.position + Vector2.up * 0.12f;
        float distance = Vector2.Distance(target, hand.ShoulderPosition);
        float sweetSpot = Mathf.Lerp(hand.weapon != null ? hand.weapon.StrikeStart : 0f, 1f, 0.6f); // a bit past the middle of the striking part
        return Mathf.Clamp(distance - blade * sweetSpot, reach * 0.45f, reach);
    }
}
