using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hit detection and damage for one weapon (or a bare forearm, definition = null, for fists).
/// Only the <see cref="strikeCollider"/> (blade / head / fist) counts. How hard a hit is comes from the
/// collision <b>impulse</b> — speed × mass — so a heavy mace lands harder than a dagger at the same speed and a
/// resting weapon does nothing. Weapon on weapon or shield is a block. Results go out through <see cref="HitEvents"/>;
/// this class applies damage, knockback and stagger but plays no effects itself.
/// </summary>
public class Weapon : MonoBehaviour
{
    public Fighter owner;
    public Fighter.Hand hand;
    public WeaponDefinition definition;
    /// <summary>The collider that deals damage. Null for shields.</summary>
    public Collider2D strikeCollider;

    // Shared tuning (static so every weapon agrees; edit here or via code at runtime).
    const float FistDamage = 4f;
    const float FistMinSpeed = 3f;
    const float FistMass = 0.6f;
    /// <summary>A hit with this impulse (N·s) does the weapon's listed damage.</summary>
    public static float ReferenceImpact = 5f; // measured: landed hits average ~3.4 N·s, solid ones 5–10
    /// <summary>Hits below this impulse do nothing (resting contact, light grazes).</summary>
    public static float MinImpact = 1.2f;
    public static float MaxImpact = 30f;
    /// <summary>Same body part can't be hit again by this weapon for this long.</summary>
    public static float PartCooldown = 0.2f;
    /// <summary>Extra push on the struck part, per N·s of impact.</summary>
    public static float KnockbackScale = 0.25f;
    public static float MaxKnockback = 6f;
    /// <summary>Impact thresholds (multiples of ReferenceImpact) for medium and heavy hits.</summary>
    public static float MediumAt = 0.8f, HeavyAt = 1.4f;
    /// <summary>Muscle strength left after a medium / heavy hit (recovers over ~0.6 s).</summary>
    public static float MediumStagger = 0.55f, HeavyStagger = 0.3f;
    /// <summary>A shield must move at least this fast (m/s) into the opponent to bash.</summary>
    public static float BashMinSpeed = 2.2f;

    /// <summary>Running totals for tuning: how many strike contacts were too soft, blocked, or landed.</summary>
    public static int StatContacts, StatTooSlow, StatBlocked, StatHits;
    public static float StatDamage, StatImpact;

    readonly Dictionary<Collider2D, float> lastHitOn = new Dictionary<Collider2D, float>();
    static readonly ContactPoint2D[] contacts = new ContactPoint2D[8];

    public bool IsShield => definition != null && definition.IsShield;

    /// <summary>Tip speed (m/s) at which a swing whooshes.</summary>
    public static float WhooshSpeed = 8f;
    /// <summary>Tip speed (m/s) above which the blade leaves a motion trail.</summary>
    public static float TrailSpeed = 6f;
    /// <summary>Motion trail at the blade tip (set up by RagdollBody); null for fists and shields.</summary>
    public TrailRenderer trail;
    Rigidbody2D body;
    bool whooshing;

    void FixedUpdate()
    {
        // Whoosh when the striking part rushes through the air (each time it crosses the speed).
        if (strikeCollider == null || owner == null || owner.IsHelpless) return;
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (trail != null) trail.emitting = !IsShield && body.GetPointVelocity(strikeCollider.bounds.center).magnitude > TrailSpeed;
        if (IsShield) return;
        float speed = body.GetPointVelocity(strikeCollider.bounds.center).magnitude;
        if (!whooshing && speed > WhooshSpeed) { whooshing = true; GameFeel.Play(GameFeel.Sound.Whoosh, Mathf.Clamp01(speed / 20f) * 0.5f); }
        else if (whooshing && speed < WhooshSpeed * 0.6f) whooshing = false;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (owner == null || owner.IsHelpless || strikeCollider == null) return; // limp flailing doesn't count
        if (collision.otherCollider != strikeCollider) return; // the handle only bumps
        if (lastHitOn.TryGetValue(collision.collider, out float last) && Time.time - last < PartCooldown) return;

        // How hard: the solver's impulse at the contacts, or speed × mass if it reports none.
        int n = collision.GetContacts(contacts);
        float impulse = 0f;
        Vector2 point = n > 0 ? contacts[0].point : (Vector2)transform.position;
        Vector2 normal = n > 0 ? contacts[0].normal : Vector2.zero;
        float strongest = -1f;
        for (int i = 0; i < n; i++)
        {
            impulse += contacts[i].normalImpulse;
            if (contacts[i].normalImpulse > strongest) { strongest = contacts[i].normalImpulse; point = contacts[i].point; normal = contacts[i].normal; }
        }
        float speed = ContactSpeed(collision, point);
        float mass = definition != null ? definition.mass : FistMass;
        float impact = impulse > 0.01f ? impulse : speed * mass;
        if (float.IsNaN(impact) || float.IsInfinity(impact)) return;
        impact = Mathf.Min(impact, MaxImpact);

        // Shield bash: a shield driven into the opponent's body shoves them back and rocks them, no damage.
        if (IsShield)
        {
            var bashed = collision.collider.GetComponent<BodyPart>();
            if (bashed == null || bashed.fighter == owner || bashed.fighter.IsHelpless) return;
            if (speed < BashMinSpeed || hand == null || !hand.Active) return;
            lastHitOn[collision.collider] = Time.time;
            Vector2 push = new Vector2(Mathf.Sign(bashed.fighter.Torso.position.x - owner.Torso.position.x), 0f);
            float bashImpact = Mathf.Max(impact, speed * mass);
            bashed.fighter.Balance.Bashed(push, bashImpact);
            bashed.fighter.SpendStamina(bashImpact * 0.8f);
            HitEvents.RaiseBashed(new BashInfo { attacker = owner, victim = bashed.fighter, point = point, impact = bashImpact });
            return;
        }

        float minSpeed = definition != null ? definition.minImpactSpeed : FistMinSpeed;
        StatContacts++;
        if (speed < minSpeed || impact < MinImpact) { StatTooSlow++; return; }

        // Steel on steel, or on a shield: a block.
        var otherWeapon = collision.collider.GetComponent<Weapon>();
        if (otherWeapon != null && otherWeapon.owner != owner && otherWeapon.definition != null)
        {
            lastHitOn[collision.collider] = Time.time;
            StatBlocked++;
            float cost = impact * (otherWeapon.IsShield ? 1.2f : 0.8f);
            otherWeapon.owner.SpendStamina(cost);
            HitEvents.RaiseBlocked(new BlockInfo { attacker = owner, blocker = otherWeapon.owner, point = point, impact = impact, shield = otherWeapon.IsShield });
            return;
        }

        var part = collision.collider.GetComponent<BodyPart>();
        if (part == null || part.fighter == owner || part.fighter.IsKnockedOut) return;

        float force = Mathf.Clamp(impact / ReferenceImpact, 0.3f, 1.8f);
        float damage = (definition != null ? definition.damage : FistDamage)
                       * force
                       * part.damageMultiplier
                       * FighterDefinition.DamageThrough(part.armor)
                       * owner.Definition.strength
                       * owner.DamageMultiplier;

        lastHitOn[collision.collider] = Time.time;
        StatHits++;
        StatDamage += damage;
        StatImpact += impact;

        // Direction of the blow: along the contact normal, pointing into the victim.
        Vector2 toVictim = ((Vector2)part.transform.position - owner.Torso.position).normalized;
        Vector2 dir = normal.sqrMagnitude > 0.01f ? (Vector2.Dot(normal, toVictim) >= 0f ? normal : -normal) : toVictim;
        var strength = impact >= ReferenceImpact * HeavyAt ? HitStrength.Heavy
                     : impact >= ReferenceImpact * MediumAt ? HitStrength.Medium
                     : HitStrength.Light;

        // Physical consequences: knock the struck part along the blow, and rock the victim's balance.
        var rb = part.GetComponent<Rigidbody2D>();
        if (rb != null) rb.AddForce(dir * Mathf.Min(impact * KnockbackScale, MaxKnockback), ForceMode2D.Impulse);
        if (strength == HitStrength.Heavy) part.fighter.Balance.Stagger(HeavyStagger);
        else if (strength == HitStrength.Medium) part.fighter.Balance.Stagger(MediumStagger);
        part.fighter.Balance.Flinch(dir, impact, part.kind == PartKind.Head);

        HitEvents.RaiseHit(new HitInfo
        {
            attacker = owner, victim = part.fighter, part = part, damage = damage, impact = impact,
            point = point, direction = dir, strength = strength, armoured = FighterDefinition.IsHard(part.armor)
        });
        part.fighter.TakeDamage(damage, part);
    }

    /// <summary>Speed of this weapon relative to what it hit, measured at the contact point itself.</summary>
    static float ContactSpeed(Collision2D collision, Vector2 point)
    {
        Vector2 mine = collision.otherRigidbody != null ? collision.otherRigidbody.GetPointVelocity(point) : Vector2.zero;
        Vector2 theirs = collision.rigidbody != null ? collision.rigidbody.GetPointVelocity(point) : Vector2.zero;
        return (mine - theirs).magnitude;
    }
}
