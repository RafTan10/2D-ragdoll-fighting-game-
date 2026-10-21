using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A 2D ragdoll fighter, built at runtime from a <see cref="FighterDefinition"/>.
/// Joints are HingeJoint2D motors steered towards pose angles; a head-up / feet-down force couple
/// keeps it standing. It can be knocked down or faint from exhaustion (goes limp, then gets back up),
/// and goes fully limp when knocked out.
///
/// Hands are aimed, not animated: each frame a brain gives a target point per hand, a two-joint
/// solver turns it into shoulder/elbow angles, and the motors chase those angles as fast as the
/// fighter's strength, the weapon's weight and the remaining stamina allow. The weapon is its own body,
/// so it carries momentum. Only the weapon's striking part (blade / head) deals damage.
///
/// Feet move in discrete steps (holding a direction repeats them) and stay planted in between; when the
/// body leans past its feet it takes a recovery step on its own.
///
/// Angles in this file are "CCW degrees for a fighter facing right" (for a limb hanging down,
/// positive = swings forward); <see cref="Facing"/> mirrors them.
/// </summary>
public class Fighter : MonoBehaviour
{
    public const float PixelsPerUnit = 64f;
    const float Px = 1f / PixelsPerUnit;

    // Maps our CCW angle convention onto HingeJoint2D motor speed / limits / jointAngle.
    const float JointSign = -1f;

    public enum Condition { Standing, Down, Fainted }

    public FighterDefinition Definition { get; private set; }
    public float Facing { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth => Definition.maxHealth;
    public float Stamina { get; private set; }
    public float MaxStamina { get; private set; }
    public bool IsKnockedOut { get; private set; }
    public Condition State { get; private set; }
    /// <summary>Knocked out, knocked down or fainted: can't act right now.</summary>
    public bool IsHelpless => IsKnockedOut || State != Condition.Standing;
    public float Reach { get; private set; }
    public bool HasShield => OffHand.weapon != null && OffHand.weapon.IsShield;

    public IFighterBrain Brain;
    public Fighter Opponent;
    public float DamageMultiplier = 1f;

    public event Action<Fighter> KnockedOut;

    public Rigidbody2D Torso { get; private set; }
    public Rigidbody2D Head { get; private set; }
    public Hand MainHand { get; private set; }
    public Hand OffHand { get; private set; }

    readonly List<Rigidbody2D> bodies = new List<Rigidbody2D>();
    readonly List<Drive> drives = new List<Drive>();
    readonly Dictionary<SpriteRenderer, Color> baseColors = new Dictionary<SpriteRenderer, Color>();
    readonly List<(SpriteRenderer renderer, float until)> flashes = new List<(SpriteRenderer, float)>();
    Leg frontLeg, backLeg;
    Drive neck;
    FighterInput input;
    bool hopQueued, jumpQueued;
    float hopCooldown;
    float crossedTime;
    float leanTarget;
    float totalMass, standingHeight, legTorque;
    float stepTimer, stepDir;
    int stepCount;
    float staminaRest;
    float conditionTimer, getUpTime, tippedTime, steadyUntil;

    // ---------------------------------------------------------------- tuning
    const float BalancePerKg = 8.5f;       // head up / feet down, N per kg of the whole fighter (weapons included)
    const float HeadSwayDamping = 3f;
    const float TorsoStiffness = 60f;      // N·m per radian away from the lean target
    const float TorsoDamping = 4f;
    const float GuardLean = 7f;            // degrees the torso leans towards the opponent
    const float WalkAcceleration = 10f;
    const float StepDuration = 0.3f;       // one step; holding a direction chains them
    const float StepSpeed = 1.45f;         // peak step speed as a multiple of walkSpeed
    const float LungeUp = 3.0f, LungeForward = 3.4f;
    const float JumpUp = 4.6f, JumpForward = 2.4f;
    const float ArmSpeed = 950f;           // deg/s an unburdened arm can turn at strength 1
    const float ArmWeightSlowdown = 0.55f; // each kg of weapon divides arm speed by (1 + this)
    const float ShoulderTorque = 110f, ElbowTorque = 65f;
    const float StaminaRegen = 18f;        // per second once rested
    const float FaintSeconds = 2.2f, KnockdownSeconds = 0.9f;

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
        internal Drive shoulder, elbow;
        public bool IsShield => weapon != null && weapon.IsShield;
    }

    class Leg { public Drive hip, knee; public Rigidbody2D foot; }

    internal class Drive
    {
        public HingeJoint2D joint;
        public Rigidbody2D body, parent;
        public float maxTorque;

        public void To(float target, float facing, float gain, float maxSpeed, float torque = -1f)
        {
            float current = Mathf.DeltaAngle(parent.rotation, body.rotation);
            float error = Mathf.DeltaAngle(current, target * facing);
            var motor = joint.motor;
            motor.motorSpeed = Mathf.Clamp(error * gain, -maxSpeed, maxSpeed) * JointSign;
            motor.maxMotorTorque = torque > 0f ? torque : maxTorque;
            joint.motor = motor;
            joint.useMotor = true;
        }
    }

    // ================================================================ building

    /// <summary>Builds a fighter with its feet at <paramref name="feet"/>.</summary>
    public static Fighter Spawn(FighterDefinition def, WeaponDefinition mainWeapon, WeaponDefinition offWeapon,
                                Vector2 feet, float facing, int sortBase, Material material)
    {
        var root = new GameObject("Fighter_" + def.displayName);
        var f = root.AddComponent<Fighter>();
        f.Definition = def;
        f.Facing = facing;
        f.Health = def.maxHealth;
        f.Build(mainWeapon, offWeapon, feet, sortBase, material);
        return f;
    }

    void Build(WeaponDefinition mainWeapon, WeaponDefinition offWeapon, Vector2 feet, int sortBase, Material material)
    {
        var def = Definition;
        float w = def.weight;

        float lowerLegH = Height(def.lowerLeg), upperLegH = Height(def.upperLeg);
        float torsoH = Height(def.torso), upperArmH = Height(def.upperArm), lowerArmH = Height(def.lowerArm);

        // Overlaps are in pixels so joints hide their seams.
        Vector2 hip = feet + Vector2.up * (lowerLegH + upperLegH - 2 * Px);
        Vector2 torsoCenter = hip + Vector2.up * (torsoH * 0.5f - 3 * Px);
        Vector2 neckPos = torsoCenter + Vector2.up * (torsoH * 0.5f - 3 * Px);
        Vector2 shoulder = neckPos + Vector2.down * (5 * Px);
        Vector2 knee = hip + Vector2.down * (upperLegH - 2 * Px);
        Vector2 elbow = shoulder + Vector2.down * (upperArmH - 2 * Px);
        Vector2 hand = elbow + Vector2.down * (lowerArmH - 4 * Px);
        standingHeight = torsoCenter.y - feet.y;
        legTorque = 170f * w;

        Color back = def.backLimbTint;
        Color front = Color.white;

        // Sorting: cape < main-hand weapon < back arm < back leg < torso < head < front leg < front arm < shield.
        Torso = Part("Torso", def.torso, torsoCenter, sortBase + 5, front, 4f * w, PartKind.Torso, 1f, material);
        Head = Part("Head", def.head, neckPos, sortBase + 6, front, 1.2f * w, PartKind.Head, 1.5f, material, circle: true);
        neck = Joint(Head, Torso, 18f * w, -25f, 25f);

        backLeg = MakeLeg("Back", hip, knee, sortBase + 3, back, material);
        frontLeg = MakeLeg("Front", hip, knee, sortBase + 7, front, material);

        MainHand = MakeArm("Main", shoulder, elbow, hand, sortBase + 1, back, mainWeapon, sortBase + 0, material);
        OffHand = MakeArm("Off", shoulder, elbow, hand, sortBase + 9, front, offWeapon, sortBase + 12, material);

        if (def.cape != null) MakeCape(neckPos + Vector2.down * (3 * Px), sortBase - 1, material);

        // A fighter never collides with itself.
        var colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            for (int j = i + 1; j < colliders.Length; j++)
                Physics2D.IgnoreCollision(colliders[i], colliders[j]);

        foreach (var b in bodies) totalMass += b.mass;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) baseColors[sr] = sr.color;

        float weaponLength = mainWeapon != null && !mainWeapon.IsShield ? Height(mainWeapon.sprite) : 0.1f;
        Reach = upperArmH + lowerArmH + weaponLength * 0.8f + 0.2f;

        // Heavy gear means less stamina.
        float gear = (mainWeapon != null ? mainWeapon.mass : 0f) + (offWeapon != null ? offWeapon.mass : 0f);
        MaxStamina = 100f * def.endurance * Mathf.Clamp(1.15f - gear * 0.1f, 0.7f, 1.1f);
        Stamina = MaxStamina;
    }

    Leg MakeLeg(string side, Vector2 hip, Vector2 knee, int order, Color tint, Material material)
    {
        var def = Definition;
        var upper = Part("UpperLeg_" + side, def.upperLeg, hip, order, tint, 1f * def.weight, PartKind.Leg, 0.8f, material);
        var lower = Part("LowerLeg_" + side, def.lowerLeg, knee, order, tint, 0.9f * def.weight, PartKind.Leg, 0.8f, material);
        return new Leg
        {
            hip = Joint(upper, Torso, legTorque, -60f, 110f),
            knee = Joint(lower, upper, legTorque, -130f, 0f),
            foot = lower
        };
    }

    Hand MakeArm(string side, Vector2 shoulder, Vector2 elbow, Vector2 handPos, int order, Color tint,
                 WeaponDefinition weapon, int weaponOrder, Material material)
    {
        var def = Definition;
        // Realistic arm masses matter: arms much lighter than the weapon they hold make the joint solver jitter.
        var upper = Part("UpperArm_" + side, def.upperArm, shoulder, order, tint, 1.6f * def.weight, PartKind.Arm, 0.7f, material);
        var lower = Part("LowerArm_" + side, def.lowerArm, elbow, order + 1, tint, 1.2f * def.weight, PartKind.Arm, 0.7f, material);
        var h = new Hand
        {
            weapon = weapon,
            upper = upper,
            lower = lower,
            upperLength = (elbow - shoulder).magnitude,
            lowerLength = (handPos - elbow).magnitude,
            shoulder = Joint(upper, Torso, ShoulderTorque * def.strength, 0f, 0f, limited: false),
            elbow = Joint(lower, upper, ElbowTorque * def.strength, 0f, 150f)
        };

        if (weapon != null && weapon.sprite != null)
            h.weaponObject = MakeWeapon(weapon, lower, handPos, weaponOrder, material);
        else
        {
            // Fists: the bottom third of the forearm sprite (the hand) is the striking part.
            var fist = lower.gameObject.AddComponent<BoxCollider2D>();
            FitBox(fist, def.lowerArm, 0f, 0.32f);
            h.weaponObject = lower.gameObject.AddComponent<Weapon>();
            h.weaponObject.strikeCollider = fist;
        }
        h.weaponObject.owner = this;
        h.weaponObject.hand = h;
        return h;
    }

    Weapon MakeWeapon(WeaponDefinition def, Rigidbody2D hand, Vector2 handPos, int order, Material material)
    {
        var go = new GameObject("Weapon_" + def.displayName);
        go.transform.SetParent(transform, false);
        float angle = 180f + def.gripAngle * Facing; // forearm points down; turn the blade forward
        Quaternion rot = Quaternion.Euler(0f, 0f, angle);
        Vector2 up = rot * Vector2.up;
        go.transform.SetPositionAndRotation(handPos - up * (def.gripPixels * Px), rot);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = def.sprite;
        sr.flipX = Facing < 0f;
        sr.sortingOrder = order;
        if (material != null) sr.sharedMaterial = material;

        // Two hitboxes, both fitted to the visible pixels: the handle (just bumps) and the striking part.
        var weapon = go.AddComponent<Weapon>();
        weapon.definition = def;
        float split = def.StrikeStart;
        if (def.IsShield || split <= 0f)
        {
            var whole = go.AddComponent<BoxCollider2D>();
            FitBox(whole, def.sprite, 0f, 1f);
            weapon.strikeCollider = def.IsShield ? null : whole;
        }
        else
        {
            var handle = go.AddComponent<BoxCollider2D>();
            FitBox(handle, def.sprite, 0f, split);
            var strike = go.AddComponent<BoxCollider2D>();
            FitBox(strike, def.sprite, split, 1f);
            weapon.strikeCollider = strike;
        }

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = def.mass;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        bodies.Add(rb);

        var fixedJoint = go.AddComponent<FixedJoint2D>();
        fixedJoint.connectedBody = hand;
        fixedJoint.frequency = 0f;
        return weapon;
    }

    void MakeCape(Vector2 anchor, int order, Material material)
    {
        var go = new GameObject("Cape");
        go.transform.SetParent(transform, false);
        go.transform.position = anchor; // cape sprites pivot at the top-centre
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Definition.cape;
        sr.flipX = Facing < 0f;
        sr.sortingOrder = order;
        if (material != null) sr.sharedMaterial = material;

        // No collider: the cape is cloth, it only swings.
        var rb = go.AddComponent<Rigidbody2D>();
        rb.useAutoMass = false;
        rb.mass = 0.15f;
        rb.angularDamping = 6f;
        rb.linearDamping = 2.5f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var joint = go.AddComponent<HingeJoint2D>();
        joint.connectedBody = Torso;
        joint.anchor = Vector2.zero;
        joint.useLimits = true;
        // Hangs behind; may flow backwards (negative = trailing edge swings back).
        float a = -45f * Facing * JointSign, b = 6f * Facing * JointSign;
        joint.limits = new JointAngleLimits2D { min = Mathf.Min(a, b), max = Mathf.Max(a, b) };
    }

    Rigidbody2D Part(string name, Sprite sprite, Vector2 pos, int order, Color tint, float mass,
                     PartKind kind, float damageMultiplier, Material material, bool circle = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipX = Facing < 0f;
        sr.sortingOrder = order;
        sr.color = tint;
        if (material != null) sr.sharedMaterial = material;

        // Hitboxes hug the visible pixels, not the sprite's (padded) rectangle.
        Rect r = PixelBounds.Opaque(sprite, 0f, 1f);
        if (circle)
        {
            var c = go.AddComponent<CircleCollider2D>();
            c.radius = Mathf.Max(Mathf.Min(r.width, r.height) * 0.5f - Px, Px);
            c.offset = new Vector2(r.center.x * Facing, r.center.y);
        }
        else
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(Mathf.Max(r.width - 2 * Px, Px), Mathf.Max(r.height - Px, Px));
            box.offset = new Vector2(r.center.x * Facing, r.center.y);
        }

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        bodies.Add(rb);

        var part = go.AddComponent<BodyPart>();
        part.fighter = this;
        part.kind = kind;
        part.damageMultiplier = damageMultiplier;
        part.armor = Definition.ArmorFor(kind);
        return rb;
    }

    /// <summary>Fits a box to the visible pixels of a horizontal band of the sprite (0 = bottom, 1 = top).</summary>
    void FitBox(BoxCollider2D box, Sprite sprite, float from01, float to01)
    {
        Rect r = PixelBounds.Opaque(sprite, from01, to01);
        box.size = new Vector2(Mathf.Max(r.width, Px), Mathf.Max(r.height, Px));
        box.offset = new Vector2(r.center.x * Facing, r.center.y);
    }

    Drive Joint(Rigidbody2D body, Rigidbody2D parent, float maxTorque, float min, float max, bool limited = true)
    {
        var joint = body.gameObject.AddComponent<HingeJoint2D>();
        joint.connectedBody = parent;
        joint.anchor = Vector2.zero; // every part's pivot is its joint
        joint.useLimits = limited;
        if (limited)
        {
            float a = min * Facing * JointSign, b = max * Facing * JointSign;
            joint.limits = new JointAngleLimits2D { min = Mathf.Min(a, b), max = Mathf.Max(a, b) };
        }
        var drive = new Drive { joint = joint, body = body, parent = parent, maxTorque = maxTorque };
        drives.Add(drive);
        return drive;
    }

    static float Height(Sprite s) => s != null ? s.rect.height / PixelsPerUnit : 0.3f;

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
        if (input.hop) hopQueued = true;
        if (input.jump) jumpQueued = true;
        MainHand.Active = input.mainActive;
        MainHand.Aim = input.mainAim;
        OffHand.Active = input.offActive;
        OffHand.Aim = input.offAim;
    }

    void FixedUpdate()
    {
        if (IsKnockedOut) return;
        float dt = Time.fixedDeltaTime;

        UpdateStamina(dt);

        if (State != Condition.Standing)
        {
            conditionTimer -= dt;
            if (conditionTimer > 0f) return; // limp on the ground
            GetUp(dt);
            return;
        }

        FaceOpponent(dt);
        float fatigue = Fatigue;
        DriveArm(MainHand, fatigue);
        DriveArm(OffHand, fatigue);
        neck.To(0f, Facing, 8f, 300f);

        bool grounded = IsGrounded();
        float vx = Torso.linearVelocity.x;
        if (grounded || hopCooldown < 0.35f) DriveLegs(dt, fatigue);
        else TuckLegs(); // just after take-off: knees up

        // Lean towards the opponent in a guard; a little more into a step.
        float stepping = stepTimer > 0f ? stepDir : 0f;
        leanTarget = -Facing * GuardLean - stepping * 4f;
        float leanError = Mathf.DeltaAngle(Torso.rotation, leanTarget) * Mathf.Deg2Rad;
        Torso.AddTorque((leanError * TorsoStiffness * (totalMass / 12f) - Torso.angularVelocity * Mathf.Deg2Rad * TorsoDamping) * fatigue);

        // Balance couple: head up, feet down — no net lift, but a strong righting torque. Tired fighters sag.
        float force = BalancePerKg * totalMass * fatigue;
        Vector2 sway = new Vector2(Head.linearVelocity.x, 0f);
        Head.AddForce(Vector2.up * force - sway * HeadSwayDamping);
        frontLeg.foot.AddForce(Vector2.down * force * 0.5f);
        backLeg.foot.AddForce(Vector2.down * force * 0.5f);

        if (grounded) Footwork(dt, vx);

        hopCooldown -= dt;
        if (hopQueued)
        {
            hopQueued = false;
            if (grounded && hopCooldown <= 0f && Stamina > 8f)
            {
                // A low lunge towards the opponent.
                hopCooldown = 0.7f;
                SpendStamina(7f);
                float dir = Opponent != null ? Mathf.Sign(Opponent.Torso.position.x - Torso.position.x) : Facing;
                foreach (var b in bodies) b.AddForce(new Vector2(dir * LungeForward, LungeUp) * b.mass, ForceMode2D.Impulse);
            }
        }
        if (jumpQueued)
        {
            jumpQueued = false;
            if (grounded && hopCooldown <= 0f && Stamina > 10f)
            {
                hopCooldown = 0.75f;
                SpendStamina(9f);
                foreach (var b in bodies) b.AddForce(new Vector2(input.move * JumpForward, JumpUp) * b.mass, ForceMode2D.Impulse);
            }
        }

        // Knocked off balance: tipped far over (or head near the ground) for a moment → down.
        float tilt = Mathf.Abs(Mathf.DeltaAngle(Torso.rotation, 0f));
        bool tipped = (tilt > 62f || Head.position.y < standingHeight * 0.5f) && Time.time > steadyUntil;
        tippedTime = tipped ? tippedTime + dt : 0f;
        if (tippedTime > 0.25f) GoDown(Condition.Down, KnockdownSeconds);
    }

    // ================================================================ stamina, falling, getting up

    /// <summary>1 when fresh, down to 0.55 when nearly exhausted: scales balance, arm speed and leg strength.</summary>
    public float Fatigue => Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(Stamina / (MaxStamina * 0.25f)));

    /// <summary>Spends stamina. Big costs (a jump, a blocked blow) pause recovery for a moment; small ones don't.</summary>
    public void SpendStamina(float amount)
    {
        if (amount <= 0f || IsKnockedOut) return;
        Stamina = Mathf.Max(0f, Stamina - amount);
        if (amount >= 2f) staminaRest = 0.5f;
        if (Stamina <= 0f && State == Condition.Standing) GoDown(Condition.Fainted, FaintSeconds);
    }

    void UpdateStamina(float dt)
    {
        // Fast hand movement costs stamina (about 2 per full swing), more with a heavy weapon.
        bool swinging = false;
        foreach (var hand in new[] { MainHand, OffHand })
        {
            if (!hand.Active) continue;
            float speed = Mathf.Abs(hand.upper.angularVelocity - Torso.angularVelocity);
            if (speed <= 250f) continue;
            swinging = true;
            SpendStamina((speed - 250f) * 0.006f * (1f + (hand.weapon != null ? hand.weapon.mass : 0f) * 0.4f) * dt);
        }
        staminaRest -= dt;
        if (staminaRest <= 0f)
            Stamina = Mathf.Min(MaxStamina, Stamina + StaminaRegen * (swinging ? 0.35f : MainHand.Active || OffHand.Active ? 0.6f : 1f) * dt);
    }

    void GoDown(Condition condition, float seconds)
    {
        if (IsKnockedOut) return;
        State = condition;
        conditionTimer = seconds;
        getUpTime = 0f;
        tippedTime = 0f;
        stepTimer = 0f;
        MainHand.Active = OffHand.Active = false;
        foreach (var d in drives) d.joint.useMotor = false;
    }

    void GetUp(float dt)
    {
        getUpTime += dt;
        // Muscles back on, back into guard, with a helping lift while the legs find the floor.
        DriveArm(MainHand, 1f);
        DriveArm(OffHand, 1f);
        neck.To(0f, Facing, 8f, 300f);
        DriveLegs(dt, 1f);
        leanTarget = -Facing * GuardLean;
        float leanError = Mathf.DeltaAngle(Torso.rotation, leanTarget) * Mathf.Deg2Rad;
        Torso.AddTorque(leanError * TorsoStiffness * 3f * (totalMass / 12f) - Torso.angularVelocity * Mathf.Deg2Rad * TorsoDamping * 2f);
        // Lift only until the torso is back near standing height, so getting up never turns into a hop.
        if (Torso.position.y < standingHeight * 0.85f)
        {
            float lift = Mathf.Lerp(0.95f, 0.6f, Mathf.Clamp01(getUpTime / 1.2f));
            Torso.AddForce(Vector2.up * totalMass * 9.81f * lift - new Vector2(0f, Torso.linearVelocity.y * totalMass * 2f));
        }
        Head.AddForce(Vector2.up * BalancePerKg * totalMass);
        frontLeg.foot.AddForce(Vector2.down * BalancePerKg * totalMass * 0.5f);
        backLeg.foot.AddForce(Vector2.down * BalancePerKg * totalMass * 0.5f);
        foreach (var b in bodies) b.AddForce(new Vector2(-b.linearVelocity.x * 4f * b.mass, 0f)); // don't slide while rising

        bool upright = Mathf.Abs(Mathf.DeltaAngle(Torso.rotation, 0f)) < 25f && Torso.position.y > standingHeight * 0.75f;
        if (upright && getUpTime > 0.3f)
        {
            if (State == Condition.Fainted) Stamina = Mathf.Max(Stamina, MaxStamina * 0.4f);
            State = Condition.Standing;
            tippedTime = 0f;
            steadyUntil = Time.time + 1.2f; // a moment to find balance before anything can tip us again
        }
        else if (getUpTime > 3f) getUpTime = 0f; // keep trying
    }

    // ================================================================ turning around

    /// <summary>
    /// When the opponent ends up behind us (jumped over, pushed past), mirror the whole ragdoll so we face them again.
    /// </summary>
    void FaceOpponent(float dt)
    {
        if (Opponent == null) return;
        float dx = Opponent.Torso.position.x - Torso.position.x;
        bool behind = Mathf.Sign(dx) != Facing && Mathf.Abs(dx) > 0.3f;
        crossedTime = behind ? crossedTime + dt : 0f;
        if (crossedTime > 0.12f) { crossedTime = 0f; TurnAround(); }
    }

    /// <summary>
    /// Mirrors every body about the torso's vertical axis and flips the facing. All joints are on the centre line
    /// and every pose angle is multiplied by <see cref="Facing"/>, so the mirrored pose is exactly the new pose.
    /// </summary>
    public void TurnAround()
    {
        float cx = Torso.position.x;
        Facing = -Facing;
        stepTimer = 0f;

        foreach (var rb in GetComponentsInChildren<Rigidbody2D>())
        {
            Vector2 p = rb.position;
            var mirrored = new Vector2(2f * cx - p.x, p.y);
            rb.position = mirrored;
            rb.rotation = -rb.rotation;
            rb.transform.SetPositionAndRotation(mirrored, Quaternion.Euler(0f, 0f, rb.rotation));
            rb.linearVelocity = new Vector2(-rb.linearVelocity.x, rb.linearVelocity.y);
            rb.angularVelocity = -rb.angularVelocity;
        }
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.flipX = Facing < 0f;
        foreach (var box in GetComponentsInChildren<BoxCollider2D>()) box.offset = new Vector2(-box.offset.x, box.offset.y);
        foreach (var circle in GetComponentsInChildren<CircleCollider2D>()) circle.offset = new Vector2(-circle.offset.x, circle.offset.y);

        foreach (var joint in GetComponentsInChildren<HingeJoint2D>())
        {
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = new Vector2(-joint.anchor.x, joint.anchor.y);
            joint.connectedAnchor = new Vector2(-joint.connectedAnchor.x, joint.connectedAnchor.y);
            if (joint.useLimits)
            {
                var l = joint.limits;
                joint.limits = new JointAngleLimits2D { min = -l.max, max = -l.min };
            }
        }

        // A weapon's grip angle is baked into its fixed joint, so rebuild that joint at the mirrored angle.
        foreach (var fixedJoint in GetComponentsInChildren<FixedJoint2D>())
        {
            var go = fixedJoint.gameObject;
            var hand = fixedJoint.connectedBody;
            DestroyImmediate(fixedJoint);
            var rebuilt = go.AddComponent<FixedJoint2D>();
            rebuilt.connectedBody = hand;
            rebuilt.frequency = 0f;
        }
    }

    // ================================================================ arms

    void DriveArm(Hand hand, float fatigue)
    {
        // Heavier weapons make the arm slower: that lag is what makes a hammer feel heavy. Tired arms are slower too.
        float weaponMass = hand.weapon != null ? hand.weapon.mass : 0f;
        float speedLimit = ArmSpeed * Definition.strength * fatigue / (1f + weaponMass * ArmWeightSlowdown);

        float shoulder, elbow, gain, maxSpeed;
        if (hand.Active)
        {
            SolveArm(hand, hand.Aim, out shoulder, out elbow);
            gain = 14f;
            maxSpeed = speedLimit;
        }
        else
        {
            // Guard: the weapon rests low and points at the opponent; the shield covers the body.
            if (hand.IsShield) { shoulder = 45f; elbow = 50f; }
            else if (hand == MainHand) { shoulder = -10f; elbow = 22f; }
            else { shoulder = 5f; elbow = 35f; }
            gain = 7f;
            maxSpeed = speedLimit * 0.45f;
        }
        hand.shoulder.To(shoulder, Facing, gain, maxSpeed);
        hand.elbow.To(elbow, Facing, gain, maxSpeed);
    }

    /// <summary>
    /// Two-joint arm solver. Works in the facing-right frame with angles measured from straight down
    /// (CCW positive, so 90 = pointing at the opponent). The elbow only bends forward.
    /// </summary>
    void SolveArm(Hand hand, Vector2 aim, out float shoulder, out float elbow)
    {
        Vector2 local = new Vector2(aim.x * Facing, aim.y);
        float a = hand.upperLength, b = hand.lowerLength;
        float d = Mathf.Clamp(local.magnitude, Mathf.Abs(a - b) + 0.02f, a + b - 0.001f);

        float towardTarget = Mathf.Atan2(local.x, -local.y) * Mathf.Rad2Deg;
        float elbowBend = 180f - Mathf.Acos(Mathf.Clamp((a * a + b * b - d * d) / (2f * a * b), -1f, 1f)) * Mathf.Rad2Deg;
        float upperOffset = Mathf.Acos(Mathf.Clamp((a * a + d * d - b * b) / (2f * a * d), -1f, 1f)) * Mathf.Rad2Deg;

        // Relative to the torso's *intended* lean, not its actual angle: using the actual angle feeds the
        // arm's own reaction torque back into its target and the arm spirals out of control.
        shoulder = towardTarget - upperOffset - leanTarget * Facing;
        elbow = Mathf.Clamp(elbowBend, 0f, 150f);
    }

    // ================================================================ legs

    /// <summary>Discrete steps: start one when asked (or when the body leans past the feet), brake in between.</summary>
    void Footwork(float dt, float vx)
    {
        if (stepTimer <= 0f)
        {
            float want = input.move;
            float support = (frontLeg.foot.position.x + backLeg.foot.position.x) * 0.5f;
            float lead = Torso.position.x - support;
            if (Mathf.Abs(want) < 0.1f && Mathf.Abs(lead) > 0.32f) want = Mathf.Sign(lead); // recovery step
            if (Mathf.Abs(want) > 0.1f)
            {
                stepDir = Mathf.Sign(want);
                stepTimer = StepDuration;
                stepCount++;
                SpendStamina(0.4f);
            }
        }

        float targetVx = 0f;
        if (stepTimer > 0f)
        {
            stepTimer -= dt;
            float progress = 1f - Mathf.Clamp01(stepTimer / StepDuration);
            targetVx = stepDir * Definition.walkSpeed * StepSpeed * Mathf.Sin(progress * Mathf.PI);
        }
        // Planted between steps: brake hard.
        float accel = (targetVx - vx) * WalkAcceleration * (stepTimer > 0f ? 1f : 0.7f);
        foreach (var b in bodies) b.AddForce(new Vector2(accel * b.mass, 0f));
    }

    void DriveLegs(float dt, float fatigue)
    {
        // Crouched guard: front leg forward, back leg behind, knees bent (more when tired).
        float sag = (1f - fatigue) * 50f;
        float frontHip = 26f, backHip = -16f, frontKnee = -32f - sag, backKnee = -24f - sag;

        if (stepTimer > 0f)
        {
            // One leg swings (lifted, from behind to in front of the other), the other pushes; legs alternate.
            float progress = 1f - Mathf.Clamp01(stepTimer / StepDuration);
            float lift = Mathf.Sin(progress * Mathf.PI);
            float sweep = stepDir * Facing * (progress - 0.5f) * 34f;
            if (stepCount % 2 == 0) { frontHip += sweep; frontKnee -= 40f * lift; backHip -= sweep * 0.5f; }
            else { backHip += sweep; backKnee -= 40f * lift; frontHip -= sweep * 0.5f; }
        }

        float torque = legTorque * fatigue;
        frontLeg.hip.To(frontHip, Facing, 8f, 420f, torque);
        backLeg.hip.To(backHip, Facing, 8f, 420f, torque);
        frontLeg.knee.To(frontKnee, Facing, 8f, 420f, torque);
        backLeg.knee.To(backKnee, Facing, 8f, 420f, torque);
    }

    void TuckLegs()
    {
        frontLeg.hip.To(80f, Facing, 12f, 700f);
        backLeg.hip.To(50f, Facing, 12f, 700f);
        frontLeg.knee.To(-105f, Facing, 12f, 700f);
        backLeg.knee.To(-100f, Facing, 12f, 700f);
    }

    bool IsGrounded()
    {
        var ground = ArenaView.Ground;
        return ground != null && (frontLeg.foot.IsTouching(ground) || backLeg.foot.IsTouching(ground));
    }

    // ================================================================ damage

    /// <summary>Applies damage (already reduced by armour). Heavy hits knock the fighter down.</summary>
    public void TakeDamage(float amount, BodyPart part)
    {
        if (IsKnockedOut) return;
        Health = Mathf.Max(0f, Health - amount);
        if (part != null) Flash(part.GetComponent<SpriteRenderer>());
        if (Health <= 0f) { KnockOut(); return; }
        if (amount >= 26f && State == Condition.Standing && Time.time > steadyUntil) GoDown(Condition.Down, KnockdownSeconds * 0.8f);
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
            if (sr != null && baseColors.TryGetValue(sr, out var c)) sr.color = c;
            flashes.RemoveAt(i);
        }
    }

    public void KnockOut()
    {
        if (IsKnockedOut) return;
        IsKnockedOut = true;
        Health = 0f;
        MainHand.Active = OffHand.Active = false;
        foreach (var d in drives) d.joint.useMotor = false;
        BloodFX.Spray(Head.position, Vector2.up, 24);
        KnockedOut?.Invoke(this);
    }
}

/// <summary>Bounds of the visible (non-transparent) pixels of a sprite, in the sprite's local units. Cached.</summary>
public static class PixelBounds
{
    static readonly Dictionary<(Sprite, int, int), Rect> cache = new Dictionary<(Sprite, int, int), Rect>();

    /// <param name="from01">Bottom of the band to measure, 0..1 of the sprite height.</param>
    /// <param name="to01">Top of the band.</param>
    public static Rect Opaque(Sprite sprite, float from01, float to01)
    {
        var key = (sprite, Mathf.RoundToInt(from01 * 1000f), Mathf.RoundToInt(to01 * 1000f));
        if (cache.TryGetValue(key, out var cached)) return cached;

        Rect r = sprite.rect;
        float ppu = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;
        int y0 = Mathf.FloorToInt(r.height * from01), y1 = Mathf.CeilToInt(r.height * to01);
        Rect fallback = Rect.MinMaxRect(-pivot.x / ppu, (y0 - pivot.y) / ppu, (r.width - pivot.x) / ppu, (y1 - pivot.y) / ppu);

        Rect result = fallback;
        if (sprite.texture != null && sprite.texture.isReadable)
        {
            Color32[] px = sprite.texture.GetPixels32();
            int texW = sprite.texture.width;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = y0; y < y1; y++)
                for (int x = 0; x < (int)r.width; x++)
                {
                    if (px[((int)r.y + y) * texW + (int)r.x + x].a < 128) continue;
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            if (maxX >= 0)
                result = Rect.MinMaxRect((minX - pivot.x) / ppu, (minY - pivot.y) / ppu, (maxX + 1 - pivot.x) / ppu, (maxY + 1 - pivot.y) / ppu);
        }
        cache[key] = result;
        return result;
    }
}
