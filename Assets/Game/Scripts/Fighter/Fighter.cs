using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A 2D ragdoll fighter: the thin coordinator. It holds health, the brain and its input, and runs the one-job
/// components in a fixed order every physics step:
///   <see cref="RagdollBody"/> (the parts and joints), <see cref="StaminaController"/>, <see cref="KnockdownController"/>,
///   <see cref="ArmController"/>, <see cref="LocomotionController"/>, <see cref="BalanceController"/>
///   (plus <see cref="ArmorController"/>, breakable armour, which only reacts to hits).
/// Hands are aimed, not animated: a brain gives an aim point per hand and the arm motors chase it,
/// so weapons carry real momentum. Only a weapon's striking part deals damage (see <see cref="Weapon"/>).
/// </summary>
public class Fighter : MonoBehaviour
{
    public const float PixelsPerUnit = 64f;

    public enum Condition { Standing, Down, Fainted }

    /// <summary>One arm and what it holds. Brains read these; the arm controller drives them.</summary>
    public class Hand
    {
        /// <summary>The player/AI is steering this hand right now (otherwise it relaxes into guard).</summary>
        public bool Active;
        /// <summary>World offset from the shoulder that the hand reaches for.</summary>
        public Vector2 Aim;
        /// <summary>Attacking right now (not guarding, blocking or resting): its hits count fully; other touches hurt less.</summary>
        public bool Striking;
        public WeaponDefinition weapon;
        public Weapon weaponObject;
        public Rigidbody2D upper, lower;
        /// <summary>Shoulder-to-elbow and elbow-to-grip lengths.</summary>
        public float upperLength, lowerLength;
        public float Reach => upperLength + lowerLength;
        public Vector2 ShoulderPosition => upper.position;
        /// <summary>Where the fist actually is right now (end of the forearm).</summary>
        public Vector2 HandPosition => (Vector2)lower.position + (Vector2)(lower.transform.rotation * Vector2.down) * lowerLength;
        public MotorDrive shoulder, elbow;
        public bool IsShield => weapon != null && weapon.IsShield;
    }

    // ---------------------------------------------------------------- components
    public RagdollBody Body { get; private set; }
    public StaminaController StaminaSystem { get; private set; }
    public KnockdownController Knockdown { get; private set; }
    public ArmController Arms { get; private set; }
    public LocomotionController Locomotion { get; private set; }
    public BalanceController Balance { get; private set; }
    public ArmorController Armor { get; private set; }

    // ---------------------------------------------------------------- state
    public FighterDefinition Definition { get; private set; }
    public float Facing => Body.Facing;
    public float Health { get; private set; }
    public float MaxHealth => Definition.maxHealth;
    public float Stamina => StaminaSystem.Stamina;
    public float MaxStamina => StaminaSystem.MaxStamina;
    public float Fatigue => StaminaSystem.Fatigue;
    public bool IsKnockedOut { get; private set; }
    public Condition State => Knockdown.State;
    /// <summary>Knocked out, knocked down or fainted: can't act right now.</summary>
    public bool IsHelpless => IsKnockedOut || State != Condition.Standing;
    public float Reach => Body.Reach;
    public bool HasShield => OffHand.weapon != null && OffHand.weapon.IsShield;
    public FighterInput CurrentInput => input;

    public Rigidbody2D Torso => Body.Torso;
    public Rigidbody2D Head => Body.Head;
    public Hand MainHand => Body.MainHand;
    public Hand OffHand => Body.OffHand;

    public IFighterBrain Brain;
    public Fighter Opponent;
    /// <summary>Multiplies the damage this fighter deals (the AI's difficulty).</summary>
    public float DamageMultiplier = 1f;
    /// <summary>Multiplies the damage this fighter takes (the player's handicap on easier difficulties).</summary>
    public float DamageTaken = 1f;
    /// <summary>Whether this fighter's non-swing weapon touches can hurt (off for the AI on Easy / Normal).</summary>
    public bool TouchesHurt = true;

    // ---------------------------------------------------------------- super powers (Power Mode)
    /// <summary>The super meter and power; null unless the match is in Power Mode.</summary>
    public PowerController Powers { get; private set; }
    /// <summary>Set every step by an active power (1 = normal): damage dealt, damage taken, arm speed, walk speed.</summary>
    [NonSerialized] public float BuffDamage = 1f, BuffDefense = 1f, BuffArms = 1f, BuffMove = 1f;
    /// <summary>An active power: can't be staggered or knocked down / doesn't tire.</summary>
    [NonSerialized] public bool Unstoppable, FreeStamina;
    /// <summary>Frozen by the opponent's power until this time: no input, the hands drop.</summary>
    [NonSerialized] public float StunnedUntil;
    public bool IsStunned => Time.time < StunnedUntil;

    public PowerController EnablePowers()
    {
        if (Powers == null) { Powers = gameObject.AddComponent<PowerController>(); Powers.Init(this); }
        return Powers;
    }

    public event Action<Fighter> KnockedOut;

    FighterInput input;
    readonly List<(SpriteRenderer renderer, float until)> flashes = new List<(SpriteRenderer, float)>();

    // ================================================================ building

    /// <summary>Builds a fighter with its feet at <paramref name="feet"/>.</summary>
    /// <param name="hitboxWidth">Scale of the body colliders across limbs, torso and head (the player's edge on easier
    /// difficulties); the lower legs keep full width so the feet stand the same.</param>
    public static Fighter Spawn(FighterDefinition def, WeaponDefinition mainWeapon, WeaponDefinition offWeapon,
                                Vector2 feet, float facing, int sortBase, Material material, float hitboxWidth = 1f)
    {
        var root = new GameObject("Fighter_" + def.displayName);
        var f = root.AddComponent<Fighter>();
        f.Definition = def;
        f.Health = def.maxHealth;

        f.Body = root.AddComponent<RagdollBody>();
        f.StaminaSystem = root.AddComponent<StaminaController>();
        f.Knockdown = root.AddComponent<KnockdownController>();
        f.Arms = root.AddComponent<ArmController>();
        f.Locomotion = root.AddComponent<LocomotionController>();
        f.Balance = root.AddComponent<BalanceController>();
        f.Armor = root.AddComponent<ArmorController>();

        f.Body.Build(f, def, mainWeapon, offWeapon, feet, facing, sortBase, material, hitboxWidth);
        f.StaminaSystem.Init(f, def, mainWeapon, offWeapon);
        f.Knockdown.Init(f);
        f.Arms.Init(f);
        f.Locomotion.Init(f);
        f.Balance.Init(f);
        f.Armor.Init(f, def);
        return f;
    }

    // ================================================================ runtime

    void Update()
    {
        UpdateFlashes();
        if (IsHelpless || Brain == null || IsStunned)
        {
            input = default;
            MainHand.Active = OffHand.Active = false;
            MainHand.Striking = OffHand.Striking = false;
            return;
        }
        input = Brain.Think(this, Opponent);
        if (Powers != null) Powers.Handle(ref input); // fires the power; some powers drive the hands themselves
        if (input.hop) Locomotion.QueueHop();
        if (input.jump) Locomotion.QueueJump();
        MainHand.Active = input.mainActive;
        MainHand.Aim = input.mainAim;
        OffHand.Active = input.offActive;
        OffHand.Aim = input.offAim;
        MainHand.Striking = input.mainActive && input.mainStriking;
        OffHand.Striking = input.offActive && input.offStriking;
    }

    void FixedUpdate()
    {
        FallThud();
        if (IsKnockedOut) return;
        float dt = Time.fixedDeltaTime;

        if (Powers != null) Powers.Tick(dt);
        StaminaSystem.Tick(dt);
        if (State != Condition.Standing)
        {
            Knockdown.TickHelpless(dt);
            return;
        }

        Locomotion.FaceOpponent(dt);
        // Everything the muscles do is scaled by tiredness and by stagger from recent hits.
        Balance.RecoverMuscle(dt);
        float strength = Fatigue * Balance.Muscle;
        Arms.Tick(strength * BuffArms);
        Body.Neck.To(0f, Facing, 8f, 300f);

        bool grounded = Body.IsGrounded();
        Locomotion.TickLegs(dt, strength, grounded);
        Balance.Tick(strength, Locomotion.SteppingDir, grounded);
        if (grounded) Locomotion.Footwork(dt);
        else Locomotion.AirControl();
        Locomotion.TickJumps(dt, grounded);
        Knockdown.CheckTipped(dt);
    }

    /// <summary>A falling body hitting the floor goes thud, once per fall.</summary>
    void FallThud()
    {
        if (!IsHelpless) { thudded = false; return; }
        if (thudded || !Body.BodyOnGround()) return;
        thudded = true;
        GameFeel.Play(GameFeel.Sound.Thud, Mathf.Clamp01(0.4f + Mathf.Abs(Torso.linearVelocity.y) * 0.15f));
    }

    bool thudded;

    // ================================================================ used by the components and by combat

    public void SpendStamina(float amount) => StaminaSystem.Spend(amount);

    /// <summary>The opponent is behind us: mirror the ragdoll so we face them again.</summary>
    public void TurnAround()
    {
        Locomotion.CancelStep();
        Body.Mirror();
    }

    /// <summary>Called by the knock-down controller when the fighter falls or faints.</summary>
    public void OnWentDown()
    {
        Locomotion.CancelStep();
        MainHand.Active = OffHand.Active = false;
    }

    /// <summary>Applies damage (already reduced by armour). Heavy hits knock the fighter down.</summary>
    public void TakeDamage(float amount, BodyPart part)
    {
        if (IsKnockedOut) return;
        Health = Mathf.Max(0f, Health - amount);
        if (part != null)
        {
            var sr = part.GetComponent<SpriteRenderer>();
            Body.Bruise(sr, amount);
            Flash(sr);
        }
        if (Health <= 0f) { KnockOut(); return; }
        Knockdown.OnDamaged(amount);
    }

    /// <summary>Health back (a power), never above the maximum.</summary>
    public void Heal(float amount)
    {
        if (!IsKnockedOut) Health = Mathf.Min(MaxHealth, Health + amount);
    }

    void Flash(SpriteRenderer sr)
    {
        if (sr == null) return;
        sr.color = new Color(1f, 0.35f, 0.3f);
        flashes.Add((sr, Time.unscaledTime + 0.09f));
    }

    void UpdateFlashes()
    {
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            if (Time.unscaledTime < flashes[i].until) continue;
            var sr = flashes[i].renderer;
            if (sr != null && Body.TryGetBaseColor(sr, out var c)) sr.color = c;
            flashes.RemoveAt(i);
        }
    }

    public void KnockOut()
    {
        if (IsKnockedOut) return;
        IsKnockedOut = true;
        Health = 0f;
        MainHand.Active = OffHand.Active = false;
        Body.RelaxAllMotors();
        BloodFX.Spray(Head.position, Vector2.up, 24);
        KnockedOut?.Invoke(this);
    }
}
