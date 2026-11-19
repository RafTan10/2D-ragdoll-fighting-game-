using UnityEngine;

/// <summary>
/// Power Mode. A super meter fills from fighting — landing hits, taking hits (so whoever is losing charges faster),
/// blocking, and slowly over time. When it's full, one press fires the fighter's power
/// (<see cref="FighterDefinition.power"/>, with its own name and colour). Powers work through physics and the
/// fighter's buff fields (<see cref="Fighter.BuffDamage"/> etc.), never by script-animating the ragdoll; effects,
/// sound and the HUD banner only listen to <see cref="HitEvents.PowerUsed"/>.
/// Added by <see cref="Fighter.EnablePowers"/> when the match is in Power Mode.
/// </summary>
public class PowerController : MonoBehaviour
{
    [Header("Meter (0..1)")]
    [Tooltip("Meter per share of the victim's max health dealt (dealing a full life's worth fills it).")]
    [SerializeField] float dealGain = 1f;
    [Tooltip("Meter per share of your own max health lost: the fighter who's behind charges faster.")]
    [SerializeField] float takeGain = 0.85f;
    [SerializeField] float blockGain = 0.03f;
    [Tooltip("Meter per second just for being in the fight.")]
    [SerializeField] float passiveGain = 0.008f;

    [Header("Rage: hit harder, swing and move faster")]
    [SerializeField] float rageTime = 6f;
    [SerializeField] float rageDamage = 1.4f, rageArms = 1.35f, rageMove = 1.2f;

    [Header("Shockwave: throws a nearby opponent off their feet")]
    [SerializeField] float shockRadius = 2.8f;
    [SerializeField] float shockPush = 6f, shockLift = 3.5f, shockDamage = 8f, shockDownTime = 1.2f;

    [Header("Freeze: the opponent can't act")]
    [SerializeField] float freezeTime = 2f;

    [Header("Dash: rush in, one blow that can't be blocked")]
    [SerializeField] float dashTime = 0.45f;
    [SerializeField] float dashSpeed = 9f;
    [Tooltip("× the main weapon's listed damage.")]
    [SerializeField] float dashDamage = 1.5f;
    [SerializeField] float dashDownTime = 1f;

    [Header("Fortify: little damage, no knock-downs, blows bounce back")]
    [SerializeField] float fortifyTime = 5f;
    [SerializeField] float fortifyDefense = 0.25f, fortifyReflect = 7f;

    [Header("Giant weapon")]
    [SerializeField] float giantTime = 6f;
    [SerializeField] float giantScale = 1.7f, giantMass = 1.25f, giantDamage = 1.25f, giantArms = 1.3f;

    [Header("Flurry: both hands swing nonstop, untiring")]
    [SerializeField] float flurryTime = 4f;
    [SerializeField] float flurryArms = 1.5f, flurryWindUp = 0.06f, flurryDamage = 1.2f;

    [Header("Second wind")]
    [SerializeField] float windTime = 6f;
    [SerializeField] float windHeal = 0.2f, windMove = 1.4f;

    public float Meter { get; private set; }
    public bool Ready => Meter >= 1f;
    /// <summary>When the meter last became full (the AI waits a moment before using it).</summary>
    public float ReadySince { get; private set; }
    public bool Active => Time.time < activeUntil;
    /// <summary>1 → 0 over the power's duration (for the HUD).</summary>
    public float ActiveLeft => Active && activeLength > 0f ? (activeUntil - Time.time) / activeLength : 0f;
    public PowerKind Kind => fighter.Definition.power;
    public string Name => string.IsNullOrEmpty(fighter.Definition.powerName) ? Kind.ToString() : fighter.Definition.powerName;
    public Color Color => fighter.Definition.powerColor;

    Fighter fighter;
    float activeUntil, activeLength;
    bool dashing;
    float dashUntil;
    Weapon giant;
    Vector3 giantScaleBefore;
    float giantMassBefore;
    readonly SwingMove flurryMain = new SwingMove(), flurryOff = new SwingMove();
    int flurryCount;

    public void Init(Fighter owner) => fighter = owner;

    void OnEnable() { HitEvents.Hit += OnHit; HitEvents.Blocked += OnBlocked; }
    void OnDisable() { HitEvents.Hit -= OnHit; HitEvents.Blocked -= OnBlocked; EndGiant(); }

    // ================================================================ the meter

    void Gain(float amount)
    {
        if (fighter == null || Active || fighter.IsKnockedOut || Kind == PowerKind.None) return;
        bool was = Ready;
        Meter = Mathf.Clamp01(Meter + amount);
        if (Ready && !was) ReadySince = Time.time;
    }

    void OnHit(HitInfo hit)
    {
        if (fighter == null) return;
        if (hit.attacker == fighter && hit.victim != null) Gain(hit.damage / Mathf.Max(1f, hit.victim.MaxHealth) * dealGain);
        if (hit.victim != fighter) return;
        Gain(hit.damage / Mathf.Max(1f, fighter.MaxHealth) * takeGain);
        // Fortify: the blow bounces its owner back and winds them.
        if (Active && Kind == PowerKind.Fortify && hit.attacker != null && hit.attacker != fighter)
        {
            var away = new Vector2(Mathf.Sign(hit.attacker.Torso.position.x - fighter.Torso.position.x), 0f);
            hit.attacker.Balance.Bashed(away, fortifyReflect);
            hit.attacker.SpendStamina(6f);
        }
    }

    void OnBlocked(BlockInfo block)
    {
        if (block.blocker == fighter) Gain(blockGain);
        else if (block.attacker == fighter) Gain(blockGain * 0.5f);
    }

    // ================================================================ firing (from Fighter.Update)

    /// <summary>Fires the power on <see cref="FighterInput.power"/>; Flurry and Dash then drive the hands themselves.</summary>
    public void Handle(ref FighterInput input)
    {
        if (input.power && Ready && !Active && !fighter.IsHelpless) Activate();
        if (!Active) return;
        var opponent = fighter.Opponent;
        if (Kind == PowerKind.Flurry)
        {
            AutoSwing(fighter.MainHand, flurryMain, opponent, ref input.mainActive, ref input.mainAim, ref input.mainStriking);
            if (!fighter.OffHand.IsShield)
                AutoSwing(fighter.OffHand, flurryOff, opponent, ref input.offActive, ref input.offAim, ref input.offStriking);
            input.attacking = true;
        }
        else if (Kind == PowerKind.Dash && dashing)
        {
            // Rush in with the weapon held back in guard (a lowered weapon doesn't collide, so it can't snag on their
            // guard and trip us); the blow lands on arrival.
            input.mainActive = input.offActive = false;
            input.mainStriking = input.offStriking = false;
            input.attacking = true;
            input.move = 0f;
        }
    }

    void AutoSwing(Fighter.Hand hand, SwingMove swing, Fighter opponent, ref bool active, ref Vector2 aim, ref bool striking)
    {
        if (!swing.Running) swing.Start(flurryCount++ % 2 == 0 ? SwingMove.Kind.Chop : SwingMove.Kind.Slash, flurryWindUp);
        if (swing.Aim(fighter, hand, opponent, out var a)) { active = true; aim = a; striking = swing.InStrike; }
    }

    void Activate()
    {
        Meter = 0f;
        var opponent = fighter.Opponent;
        float duration = 0f;
        switch (Kind)
        {
            case PowerKind.Rage: duration = rageTime; break;
            case PowerKind.Shockwave: Shockwave(opponent); break;
            case PowerKind.Freeze:
                duration = freezeTime;
                if (opponent != null) opponent.StunnedUntil = Time.time + freezeTime;
                break;
            case PowerKind.Dash:
                duration = dashTime + 0.3f;
                dashing = true;
                dashUntil = Time.time + dashTime;
                break;
            case PowerKind.Fortify: duration = fortifyTime; break;
            case PowerKind.GiantWeapon: duration = giantTime; StartGiant(); break;
            case PowerKind.Flurry:
                duration = flurryTime;
                flurryMain.Cancel();
                flurryOff.Cancel();
                break;
            case PowerKind.SecondWind:
                duration = windTime;
                fighter.StaminaSystem.RefillTo(1f);
                fighter.Heal(fighter.MaxHealth * windHeal);
                break;
        }
        activeLength = duration;
        activeUntil = Time.time + duration;
        HitEvents.RaisePowerUsed(new PowerInfo
        {
            fighter = fighter, target = opponent, kind = Kind, name = Name, color = Color, duration = duration,
            point = fighter.Torso.position
        });
    }

    // ================================================================ every physics step (from Fighter.FixedUpdate)

    public void Tick(float dt)
    {
        Gain(passiveGain * dt);
        bool on = Active;
        var k = Kind;
        fighter.BuffDamage = !on ? 1f : k == PowerKind.Rage ? rageDamage : k == PowerKind.GiantWeapon ? giantDamage : k == PowerKind.Flurry ? flurryDamage : 1f;
        fighter.BuffArms = !on ? 1f : k == PowerKind.Rage ? rageArms : k == PowerKind.Flurry ? flurryArms : k == PowerKind.GiantWeapon ? giantArms : 1f;
        fighter.BuffMove = !on ? 1f : k == PowerKind.Rage ? rageMove : k == PowerKind.SecondWind ? windMove : 1f;
        fighter.BuffDefense = on && k == PowerKind.Fortify ? fortifyDefense : 1f;
        fighter.Unstoppable = on && (k == PowerKind.Fortify || (k == PowerKind.Dash && dashing));
        fighter.FreeStamina = on && (k == PowerKind.Flurry || k == PowerKind.SecondWind);
        if (dashing) TickDash();
        if (giant != null && !on) EndGiant();
    }

    // ================================================================ the powers

    /// <summary>A blast from the body: everything of the opponent is thrown away and up, and they go down.</summary>
    void Shockwave(Fighter opponent)
    {
        if (opponent == null || opponent.IsKnockedOut) return;
        Vector2 from = fighter.Torso.position;
        float dist = Vector2.Distance(opponent.Torso.position, from);
        if (dist > shockRadius) return; // out of range: all show
        float t = Mathf.Lerp(1f, 0.45f, dist / shockRadius);
        float side = Mathf.Sign(opponent.Torso.position.x - from.x);
        foreach (var rb in opponent.Body.Bodies)
            rb.AddForce(new Vector2(side * shockPush, shockLift) * t * rb.mass, ForceMode2D.Impulse);
        var part = opponent.Torso.GetComponent<BodyPart>();
        if (part != null)
            Weapon.ApplyBlow(fighter, part, null, shockDamage * t * fighter.Definition.strength * fighter.DamageMultiplier,
                             Weapon.ReferenceImpact * 1.5f * t, opponent.Torso.position, new Vector2(side, 0.4f).normalized, 0.2f);
        opponent.Knockdown.KnockDown(shockDownTime);
    }

    /// <summary>Rush in until in reach, then one blow that ignores the guard and knocks them down.</summary>
    void TickDash()
    {
        var opponent = fighter.Opponent;
        float dist = opponent != null ? Mathf.Abs(opponent.Torso.position.x - fighter.Torso.position.x) : 99f;
        bool arrived = dist <= fighter.Reach * 1.05f;
        if (!arrived && Time.time < dashUntil && !fighter.IsHelpless)
        {
            float vx = fighter.Facing * dashSpeed;
            foreach (var rb in fighter.Body.Bodies)
                rb.linearVelocity = new Vector2(Mathf.MoveTowards(rb.linearVelocity.x, vx, dashSpeed * 0.5f), rb.linearVelocity.y);
            return;
        }
        dashing = false;
        foreach (var rb in fighter.Body.Bodies) rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
        if (opponent == null || opponent.IsKnockedOut || dist > fighter.Reach * 1.5f) return; // fell short

        var part = opponent.Torso.GetComponent<BodyPart>();
        if (part == null) return;
        var w = fighter.MainHand.weapon;
        float raw = (w != null && !w.IsShield ? w.damage : 8f) * dashDamage * fighter.Definition.strength * fighter.DamageMultiplier;
        var dir = new Vector2(fighter.Facing, 0.2f).normalized;
        Vector2 point = (Vector2)opponent.Torso.position + new Vector2(-fighter.Facing * 0.15f, 0.1f);
        Weapon.ApplyBlow(fighter, part, w, raw, Weapon.ReferenceImpact * 1.6f, point, dir, 0.25f);
        foreach (var rb in opponent.Body.Bodies) rb.AddForce(dir * 2.5f * rb.mass, ForceMode2D.Impulse);
        opponent.Knockdown.KnockDown(dashDownTime);
    }

    void StartGiant()
    {
        var w = fighter.MainHand.weaponObject;
        if (w == null || w.IsShield) return;
        giant = w;
        giantScaleBefore = w.transform.localScale;
        var rb = w.GetComponent<Rigidbody2D>();
        giantMassBefore = rb != null ? rb.mass : 0f;
        w.transform.localScale = giantScaleBefore * giantScale;
        if (rb != null) rb.mass = giantMassBefore * giantMass;
    }

    void EndGiant()
    {
        if (giant == null) return;
        giant.transform.localScale = giantScaleBefore;
        var rb = giant.GetComponent<Rigidbody2D>();
        if (rb != null) rb.mass = giantMassBefore;
        giant = null;
    }
}
