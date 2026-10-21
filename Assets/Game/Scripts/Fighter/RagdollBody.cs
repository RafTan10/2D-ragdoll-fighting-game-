using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the ragdoll from a <see cref="FighterDefinition"/>: one Rigidbody2D + collider + sprite per part, joined by
/// HingeJoint2Ds at the sprite pivots, weapons on FixedJoint2Ds. Also owns things that touch every part:
/// mirroring the whole body when the fighter turns round, the ground check, and switching the muscles off.
/// </summary>
public class RagdollBody : MonoBehaviour
{
    const float Px = 1f / Fighter.PixelsPerUnit;

    [Header("Masses (kg, multiplied by the fighter's weight)")]
    [Tooltip("The torso is the root of the body.")]
    [SerializeField] float torsoMass = 4f;
    [SerializeField] float headMass = 1.2f;
    [Tooltip("Arms must stay heavy relative to the weapon they hold, or the joint solver jitters.")]
    [SerializeField] float upperArmMass = 1.6f;
    [SerializeField] float lowerArmMass = 1.2f;
    [Tooltip("Legs heavier than arms, like a real body: a top-heavy ragdoll gets pushed around and its feet slide.")]
    [SerializeField] float upperLegMass = 1.8f;
    [SerializeField] float lowerLegMass = 1.2f;

    [Header("Muscle strength (motor torque caps)")]
    [Tooltip("Shoulder torque is what limits a swing: a heavier weapon gets up to speed more slowly.")]
    [SerializeField] float shoulderTorque = 70f;
    [SerializeField] float elbowTorque = 45f;
    [SerializeField] float legTorque = 200f;
    [SerializeField] float neckTorque = 18f;

    [Header("Physics")]
    [Tooltip("Spin damping on every limb, so flailing settles instead of going on forever.")]
    [SerializeField] float limbAngularDamping = 0.6f;
    [Tooltip("Gravity multiplier. Fighters are ~1.8 units tall, so 1 is real-world gravity for their size.")]
    [SerializeField] float gravityScale = 1f;
    [Tooltip("Friction of the feet (lower legs). High so feet grip instead of skating.")]
    [SerializeField] float feetFriction = 1f;
    [Tooltip("Friction of the rest of the body.")]
    [SerializeField] float bodyFriction = 0.4f;
    [Tooltip("Friction of weapons, low so blades glance off instead of sticking.")]
    [SerializeField] float weaponFriction = 0.2f;

    PhysicsMaterial2D feetMaterial, bodyMaterial, weaponMaterial;

    public Rigidbody2D Torso { get; private set; }
    public Rigidbody2D Head { get; private set; }
    public Fighter.Hand MainHand { get; private set; }
    public Fighter.Hand OffHand { get; private set; }
    public Leg FrontLeg { get; private set; }
    public Leg BackLeg { get; private set; }
    public MotorDrive Neck { get; private set; }
    /// <summary>All simulated parts with colliders, weapons included (not the cape).</summary>
    public List<Rigidbody2D> Bodies { get; } = new List<Rigidbody2D>();
    public float TotalMass { get; private set; }
    /// <summary>Height of the torso centre above the feet in the spawn pose.</summary>
    public float StandingHeight { get; private set; }
    public float LegTorque => legTorque * definition.weight;
    public float Reach { get; private set; }
    public float Facing { get; private set; }

    public class Leg { public MotorDrive hip, knee; public Rigidbody2D foot; }

    readonly List<MotorDrive> drives = new List<MotorDrive>();
    readonly Dictionary<SpriteRenderer, Color> baseColors = new Dictionary<SpriteRenderer, Color>();
    FighterDefinition definition;
    Fighter fighter;

    // ================================================================ building

    public void Build(Fighter owner, FighterDefinition def, WeaponDefinition mainWeapon, WeaponDefinition offWeapon,
                      Vector2 feet, float facing, int sortBase, Material material)
    {
        fighter = owner;
        definition = def;
        Facing = facing;
        float w = def.weight;
        feetMaterial = new PhysicsMaterial2D("Feet") { friction = feetFriction, bounciness = 0f };
        bodyMaterial = new PhysicsMaterial2D("Body") { friction = bodyFriction, bounciness = 0f };
        weaponMaterial = new PhysicsMaterial2D("Weapon") { friction = weaponFriction, bounciness = 0f };

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
        StandingHeight = torsoCenter.y - feet.y;

        Color back = def.backLimbTint;
        Color front = Color.white;

        // Sorting: cape < main-hand weapon < back arm < back leg < torso < head < front leg < front arm < shield.
        Torso = Part("Torso", def.torso, torsoCenter, sortBase + 5, front, torsoMass * w, PartKind.Torso, 1f, material);
        Head = Part("Head", def.head, neckPos, sortBase + 6, front, headMass * w, PartKind.Head, 1.5f, material, circle: true);
        Neck = Joint(Head, Torso, neckTorque * w, -25f, 25f);

        BackLeg = MakeLeg("Back", hip, knee, sortBase + 3, back, material);
        FrontLeg = MakeLeg("Front", hip, knee, sortBase + 7, front, material);

        MainHand = MakeArm("Main", shoulder, elbow, hand, sortBase + 1, back, mainWeapon, sortBase + 0, material);
        OffHand = MakeArm("Off", shoulder, elbow, hand, sortBase + 9, front, offWeapon, sortBase + 12, material);

        if (def.cape != null) MakeCape(neckPos + Vector2.down * (3 * Px), sortBase - 1, material);

        // A fighter never collides with itself.
        var colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
            for (int j = i + 1; j < colliders.Length; j++)
                Physics2D.IgnoreCollision(colliders[i], colliders[j]);

        foreach (var b in Bodies) TotalMass += b.mass;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) baseColors[sr] = sr.color;

        float weaponLength = mainWeapon != null && !mainWeapon.IsShield ? Height(mainWeapon.sprite) : 0.1f;
        Reach = upperArmH + lowerArmH + weaponLength * 0.8f + 0.2f;

        // Feet grip, weapons glance, everything else in between.
        foreach (var col in FrontLeg.foot.GetComponents<Collider2D>()) col.sharedMaterial = feetMaterial;
        foreach (var col in BackLeg.foot.GetComponents<Collider2D>()) col.sharedMaterial = feetMaterial;
        MainHand.lower.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        OffHand.lower.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        WarnIfOutsideLimits();
    }

    /// <summary>A joint that starts outside its limits snaps violently on the first step; say so.</summary>
    void WarnIfOutsideLimits()
    {
        foreach (var d in drives)
        {
            if (!d.joint.useLimits) continue;
            float a = d.joint.jointAngle;
            var l = d.joint.limits;
            if (a < l.min - 0.5f || a > l.max + 0.5f)
                Debug.LogWarning($"{name}: joint {d.body.name} starts at {a:F1}°, outside its limits {l.min:F0}..{l.max:F0}");
        }
    }

    Leg MakeLeg(string side, Vector2 hip, Vector2 knee, int order, Color tint, Material material)
    {
        var def = definition;
        var upper = Part("UpperLeg_" + side, def.upperLeg, hip, order, tint, upperLegMass * def.weight, PartKind.Leg, 0.8f, material);
        var lower = Part("LowerLeg_" + side, def.lowerLeg, knee, order, tint, lowerLegMass * def.weight, PartKind.Leg, 0.8f, material);
        return new Leg
        {
            hip = Joint(upper, Torso, LegTorque, -60f, 110f),
            knee = Joint(lower, upper, LegTorque, -130f, 0f),
            foot = lower
        };
    }

    Fighter.Hand MakeArm(string side, Vector2 shoulder, Vector2 elbow, Vector2 handPos, int order, Color tint,
                         WeaponDefinition weapon, int weaponOrder, Material material)
    {
        var def = definition;
        var upper = Part("UpperArm_" + side, def.upperArm, shoulder, order, tint, upperArmMass * def.weight, PartKind.Arm, 0.7f, material);
        var lower = Part("LowerArm_" + side, def.lowerArm, elbow, order + 1, tint, lowerArmMass * def.weight, PartKind.Arm, 0.7f, material);
        var h = new Fighter.Hand
        {
            weapon = weapon,
            upper = upper,
            lower = lower,
            upperLength = (elbow - shoulder).magnitude,
            lowerLength = (handPos - elbow).magnitude,
            shoulder = Joint(upper, Torso, shoulderTorque * def.strength, 0f, 0f, limited: false),
            elbow = Joint(lower, upper, elbowTorque * def.strength, 0f, 150f)
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
        h.weaponObject.owner = fighter;
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
            weapon.strikeCollider = whole; // a shield's whole face can bash
        }
        else
        {
            var handle = go.AddComponent<BoxCollider2D>();
            FitBox(handle, def.sprite, 0f, split);
            var strike = go.AddComponent<BoxCollider2D>();
            FitBox(strike, def.sprite, split, 1f);
            weapon.strikeCollider = strike;
        }

        foreach (var col in go.GetComponents<Collider2D>()) col.sharedMaterial = weaponMaterial;
        if (!def.IsShield) weapon.trail = MakeTrail(go.transform, def.sprite, order - 1, material);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = def.mass;
        rb.gravityScale = gravityScale;
        rb.angularDamping = limbAngularDamping;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.StartAwake;
        Bodies.Add(rb);

        var fixedJoint = go.AddComponent<FixedJoint2D>();
        fixedJoint.connectedBody = hand;
        fixedJoint.frequency = 0f;
        return weapon;
    }

    [Header("Weapon trails")]
    [Tooltip("Seconds a motion trail lingers behind a fast blade.")]
    [SerializeField] float trailTime = 0.09f;
    [SerializeField] float trailWidth = 0.07f;
    [SerializeField] Color trailColor = new Color(1f, 0.97f, 0.9f, 0.55f);

    /// <summary>A short motion streak at the weapon's tip; the Weapon switches it on while the blade is fast.</summary>
    TrailRenderer MakeTrail(Transform weapon, Sprite sprite, int order, Material material)
    {
        var go = new GameObject("Trail");
        go.transform.SetParent(weapon, false);
        go.transform.localPosition = new Vector3(0f, sprite.bounds.max.y * 0.92f, 0f);
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.minVertexDistance = 0.02f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, trailWidth), new Keyframe(1f, 0f));
        trail.colorGradient = new Gradient
        {
            colorKeys = new[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
            alphaKeys = new[] { new GradientAlphaKey(trailColor.a, 0f), new GradientAlphaKey(0f, 1f) }
        };
        trail.emitting = false;
        trail.sortingOrder = order;
        trail.numCapVertices = 2;
        if (material != null) trail.sharedMaterial = material;
        return trail;
    }

    void MakeCape(Vector2 anchor, int order, Material material)
    {
        var go = new GameObject("Cape");
        go.transform.SetParent(transform, false);
        go.transform.position = anchor; // cape sprites pivot at the top-centre
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = definition.cape;
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
        float a = -45f * Facing * MotorDrive.JointSign, b = 6f * Facing * MotorDrive.JointSign;
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

        foreach (var col in go.GetComponents<Collider2D>()) col.sharedMaterial = bodyMaterial;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.mass = mass;
        rb.gravityScale = gravityScale;
        rb.angularDamping = limbAngularDamping;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.sleepMode = RigidbodySleepMode2D.StartAwake;
        Bodies.Add(rb);

        var part = go.AddComponent<BodyPart>();
        part.fighter = fighter;
        part.kind = kind;
        part.damageMultiplier = damageMultiplier;
        part.armor = definition.ArmorFor(kind);
        return rb;
    }

    /// <summary>Fits a box to the visible pixels of a horizontal band of the sprite (0 = bottom, 1 = top).</summary>
    void FitBox(BoxCollider2D box, Sprite sprite, float from01, float to01)
    {
        Rect r = PixelBounds.Opaque(sprite, from01, to01);
        box.size = new Vector2(Mathf.Max(r.width, Px), Mathf.Max(r.height, Px));
        box.offset = new Vector2(r.center.x * Facing, r.center.y);
    }

    MotorDrive Joint(Rigidbody2D body, Rigidbody2D parent, float maxTorque, float min, float max, bool limited = true)
    {
        var joint = body.gameObject.AddComponent<HingeJoint2D>();
        joint.connectedBody = parent;
        joint.anchor = Vector2.zero; // every part's pivot is its joint
        joint.useLimits = limited;
        if (limited)
        {
            float a = min * Facing * MotorDrive.JointSign, b = max * Facing * MotorDrive.JointSign;
            joint.limits = new JointAngleLimits2D { min = Mathf.Min(a, b), max = Mathf.Max(a, b) };
        }
        var drive = new MotorDrive { joint = joint, body = body, parent = parent, maxTorque = maxTorque };
        drives.Add(drive);
        return drive;
    }

    static float Height(Sprite s) => s != null ? s.rect.height / Fighter.PixelsPerUnit : 0.3f;

    // ================================================================ runtime helpers

    /// <summary>Muscles off: every joint motor stops (the body goes limp).</summary>
    public void RelaxAllMotors()
    {
        foreach (var d in drives) d.joint.useMotor = false;
    }

    public bool IsGrounded()
    {
        var ground = ArenaView.Ground;
        return ground != null && (FrontLeg.foot.IsTouching(ground) || BackLeg.foot.IsTouching(ground));
    }

    public bool TryGetBaseColor(SpriteRenderer sr, out Color color) => baseColors.TryGetValue(sr, out color);

    /// <summary>Darkens a struck part a little towards a bruise red (new fighters are built each round, so it resets).</summary>
    public void Bruise(SpriteRenderer sr, float damage)
    {
        if (sr == null || !baseColors.TryGetValue(sr, out var c)) return;
        var bruise = new Color(c.r * 0.62f, c.g * 0.36f, c.b * 0.36f, c.a);
        baseColors[sr] = Color.Lerp(c, bruise, Mathf.Clamp01(damage * 0.012f));
    }

    /// <summary>Torso or head touching the floor (a fallen body).</summary>
    public bool BodyOnGround()
    {
        var ground = ArenaView.Ground;
        return ground != null && (Torso.IsTouching(ground) || Head.IsTouching(ground));
    }

    /// <summary>
    /// Mirrors every body about the torso's vertical axis and flips the facing. All joints are on the centre line
    /// and every pose angle is multiplied by <see cref="Facing"/>, so the mirrored pose is exactly the new pose.
    /// This (and spawning) is the only place the ragdoll's positions are set directly.
    /// </summary>
    public void Mirror()
    {
        float cx = Torso.position.x;
        Facing = -Facing;

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
}
