using UnityEngine;

/// <summary>
/// Computer opponent. It fights through the same controls as a player: hand aim points driven by
/// <see cref="SwingMove"/> (chop, slash, thrust, spin) and <see cref="SwingMove.BlockAim"/>.
/// It manages stamina like a person would: presses while fresh, backs off to recover when tired,
/// and rushes in to finish an opponent who is down or has fainted. Tuned by an <see cref="AIProfile"/>.
/// </summary>
public class AIBrain : IFighterBrain
{
    enum Plan { Engage, Recover, Punish }

    readonly AIProfile profile;
    readonly SwingMove mainSwing = new SwingMove(), offSwing = new SwingMove();

    Plan plan;
    float nextThink, cooldownUntil, parryUntil, nextJumpAllowed;
    float move;
    bool hop, jump, blocking;

    /// <summary>What the AI noticed about its opponent at one moment.</summary>
    struct Seen { public float time; public Vector2 torso, head, weapon; public bool swinging, helpless; }
    readonly System.Collections.Generic.List<Seen> memory = new System.Collections.Generic.List<Seen>();
    Seen seen;

    public AIBrain(AIProfile profile) { this.profile = profile; }

    /// <summary>
    /// Like a person, the AI reacts to what it saw a moment ago, not to this exact frame: observations go into a
    /// short memory and it acts on the one that is <c>reactionTime × 0.6</c> old.
    /// </summary>
    void Observe(Fighter opponent, float now)
    {
        var weapon = opponent.MainHand.weaponObject;
        memory.Add(new Seen
        {
            time = now, torso = opponent.Torso.position, head = opponent.Head.position,
            weapon = weapon != null ? (Vector2)weapon.transform.position : opponent.Torso.position,
            swinging = opponent.MainHand.Active, helpless = opponent.IsHelpless
        });
        float delay = profile.reactionTime * 0.6f;
        while (memory.Count > 1 && memory[1].time <= now - delay) memory.RemoveAt(0);
        seen = memory[0];
    }

    public FighterInput Think(Fighter self, Fighter opponent)
    {
        var input = new FighterInput();
        if (opponent == null || opponent.IsKnockedOut) return input;

        float now = Time.time;
        Observe(opponent, now);
        float dx = seen.torso.x - self.Torso.position.x;
        float dist = Mathf.Abs(dx);
        float dir = Mathf.Sign(dx);
        float reach = self.Reach;
        float stamina = self.Stamina / self.MaxStamina;

        // ---- choose a plan
        if (seen.helpless) plan = Plan.Punish;
        else if (plan == Plan.Recover) { if (stamina > 0.6f) plan = Plan.Engage; }
        else plan = stamina < 0.28f ? Plan.Recover : Plan.Engage;

        if (now >= nextThink)
        {
            nextThink = now + profile.reactionTime * Random.Range(0.7f, 1.3f);
            Decide(self, opponent, dist, dir, reach, stamina, now);
        }

        // ---- main hand: swing path, a quick parry, or guard
        if (now < parryUntil && !mainSwing.Running)
        {
            input.mainActive = true;
            input.mainAim = SwingMove.Polar(self, 125f, self.MainHand.Reach * 0.85f); // weapon up across the line of attack
        }
        else if (mainSwing.Running && mainSwing.Aim(self, self.MainHand, opponent, out var aim))
        {
            input.mainActive = true;
            input.mainAim = aim;
            if (!mainSwing.Running) cooldownUntil = now + Random.Range(profile.recovery.x, profile.recovery.y);
        }

        // ---- off hand: shield block or a second-weapon jab
        if (self.HasShield)
        {
            bool threatened = seen.swinging && Vector2.Distance(seen.weapon, self.Torso.position) < opponent.Reach * 1.4f;
            // Notice the threat with a per-second chance, then keep the guard up while it lasts. Tired = more careful.
            float chance = profile.blockChance * (plan == Plan.Recover ? 2f : 1f) * 6f * Time.deltaTime;
            if (!threatened) blocking = false;
            else if (!blocking && Random.value < chance) blocking = true;
            if (blocking) { input.offActive = true; input.offAim = SwingMove.BlockAim(self, self.OffHand, opponent); }
        }
        else if (offSwing.Running && offSwing.Aim(self, self.OffHand, opponent, out var offAim))
        {
            input.offActive = true;
            input.offAim = offAim;
        }

        input.move = move;
        input.hop = hop;
        input.jump = jump;
        hop = jump = false;
        return input;
    }

    void Decide(Fighter self, Fighter opponent, float dist, float dir, float reach, float stamina, float now)
    {
        float aggression = plan == Plan.Punish ? 1f : profile.aggression;

        // ---- footwork
        switch (plan)
        {
            case Plan.Recover:
                // Keep out of reach until the stamina comes back.
                move = dist < opponent.Reach * 1.5f ? -dir : (Random.value < 0.3f ? -dir : 0f);
                break;
            case Plan.Punish:
                move = dist > reach * 0.7f ? dir : 0f;
                break;
            default:
            {
                float want = mainSwing.Running ? reach * 0.65f : reach * 0.85f;
                if (dist > want) move = Random.value < 0.35f + aggression * 0.65f ? dir : 0f;
                else if (dist < reach * 0.4f) move = Random.value < 0.6f ? -dir : 0f;
                else move = Random.value < 0.15f ? -dir : 0f;

                // Rush in from mid range.
                if (dist > reach * 1.3f && dist < reach * 2.3f && stamina > 0.5f && Random.value < aggression * 0.25f) hop = true;
                break;
            }
        }

        // Cornered against a wall with the opponent close: jump over them.
        float wallBehind = ArenaView.HalfWidth - Mathf.Abs(self.Torso.position.x);
        bool backToWall = Mathf.Sign(self.Torso.position.x) == -dir && wallBehind < 0.8f;
        if (backToWall && dist < reach && stamina > 0.5f && now > nextJumpAllowed && Random.value < profile.hopChance * 0.5f)
        {
            jump = true;
            move = dir;
            nextJumpAllowed = now + 4f;
        }

        // ---- attacks
        bool canAttack = plan != Plan.Recover && !mainSwing.Running && now >= cooldownUntil && dist < reach * 1.35f;
        if (canAttack && Random.value < 0.35f + aggression * 0.6f)
        {
            float r = Random.value;
            SwingMove.Kind kind = r < 0.4f ? SwingMove.Kind.Chop
                                : r < 0.7f ? SwingMove.Kind.Slash
                                : r < 0.9f || stamina < 0.6f ? SwingMove.Kind.Thrust
                                : SwingMove.Kind.Spin;
            if (plan == Plan.Punish) kind = SwingMove.Kind.Chop;
            if (mainSwing.Start(kind, Random.Range(profile.windUp.x, profile.windUp.y)))
                mainSwing.AimOffset = Random.insideUnitCircle * profile.aimError; // nobody aims perfectly
        }

        // No shield: sometimes parry an incoming swing with the weapon instead.
        if (!self.HasShield && seen.swinging && !mainSwing.Running && dist < opponent.Reach * 1.3f
            && Random.value < profile.blockChance * 0.6f)
            parryUntil = now + 0.35f;

        // Second weapon: a quick thrust now and then.
        if (!self.HasShield && self.OffHand.weapon != null && !offSwing.Running && plan != Plan.Recover
            && dist < reach * 1.1f && Random.value < aggression * 0.3f)
            offSwing.Start(SwingMove.Kind.Thrust, 0.12f);
    }
}
