using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A 2D ragdoll fighter: the thin coordinator. It holds health, the brain and its input, and runs the one-job
/// components in a fixed order every physics step:
///   <see cref="RagdollBody"/> (the parts and joints), <see cref="StaminaController"/>, <see cref="KnockdownController"/>,
///   <see cref="ArmController"/>, <see cref="LocomotionController"/>, <see cref="BalanceController"/>.
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
        public WeaponDefinition weapon;
        public Weapon weaponObject;
        public Rigidbody2D upper, lower;
        /// <summary>Shoulder-to-elbow and elbow-to-grip lengths.</summary>
        public float upperLength, lowerLength;
        public float Reach => upperLength + lowerLength;
        public Vector2 ShoulderPosition => upper.position;
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
    public float DamageMultiplier = 1f;

    public event Action<Fighter> KnockedOut;

    FighterInput input;
    readonly List<(SpriteRenderer renderer, float until)> flashes = new List<(SpriteRenderer, float)>();

    // ================================================================ building

    /// <summary>Builds a fighter with its feet at <paramref name="feet"/>.</summary>
    public static Fighter Spawn(FighterDefinition def, WeaponDefinition mainWeapon, WeaponDefinition offWeapon,
                                Vector2 feet, float facing, int sortBase, Material material)
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

        f.Body.Build(f, def, mainWeapon, offWeapon, feet, facing, sortBase, material);
        f.StaminaSystem.Init(f, def, mainWeapon, offWeapon);
        f.Knockdown.Init(f);
        f.Arms.Init(f);
        f.Locomotion.Init(f);
        f.Balance.Init(f);
        return f;
    }

    // ================================================================ runtime

    void Update()
    {
        UpdateFlashes();
        if (IsHelpless || Brain == null)
        {
            input = default;
            MainHand.Active = OffHand.Active = false;
            return;
        }
        input = Brain.Think(this, Opponent);
        if (input.hop) Locomotion.QueueHop();
        if (input.jump) Locomotion.QueueJump();
        MainHand.Active = input.mainActive;
        MainHand.Aim = input.mainAim;
        OffHand.Active = input.offActive;
        OffHand.Aim = input.offAim;
    }

    void FixedUpdate()
    {
        FallThud();
        if (IsKnockedOut) return;
        float dt = Time.fixedDeltaTime;

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
        Arms.Tick(strength);
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
