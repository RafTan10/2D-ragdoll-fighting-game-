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

    float startTime, windUp;
    float spinAngle;

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
        return true;
    }

    public void Cancel() => Running = false;

    /// <summary>Past the wind-up and driving at the target: the body should commit (lean and step in).</summary>
    public bool InStrike => Running && (Current == Kind.Spin || Time.time - startTime >= windUp);

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

    static float EaseIn(float x) => x * x * (2f - x) * 0.5f + x * 0.5f; // accelerates into the target

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
                // opponent's head*: the arc is centred on where they actually are, so the blade connects.
                const float swing = 0.28f;
                float target = TargetAngle(self, hand, opponent, true);
                float top = Mathf.Min(target + 115f, 225f), bottom = target - 95f;
                float angle;
                if (t < windUp) angle = Mathf.Lerp(40f, top, Smooth(t / windUp));
                else if (t < windUp + swing) angle = Mathf.Lerp(top, bottom, EaseIn((t - windUp) / swing));
                else { Running = false; return false; }
                aim = Polar(self, angle, reach);
                return true;
            }
            case Kind.Slash:
            {
                // Draw the hand back, then whip it forward through the opponent's chest and on past.
                const float swing = 0.24f;
                float target = TargetAngle(self, hand, opponent, false);
                float back = target + 70f, through = target - 80f;
                float angle;
                if (t < windUp) angle = Mathf.Lerp(60f, back, Smooth(t / windUp));
                else if (t < windUp + swing) angle = Mathf.Lerp(back, through, EaseIn((t - windUp) / swing));
                else { Running = false; return false; }
                float radius = Mathf.Lerp(reach * 0.75f, reach, Mathf.Sin(Mathf.Clamp01(t / (windUp + swing)) * Mathf.PI));
                aim = Polar(self, angle, radius) + (t >= windUp ? Vector2.zero : new Vector2(-self.Facing * 0.12f, 0f));
                return true;
            }
            case Kind.Thrust:
            {
                // Draw back, then drive the hand at the opponent's head.
                const float lunge = 0.28f;
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
}
