using UnityEngine;

/// <summary>
/// Turns contact into damage and hit effects. Only the <see cref="strikeCollider"/> (blade / head / fist) hurts;
/// damage scales with how fast it moves <b>at the contact point</b> and with the armour it lands on.
/// Steel on steel or on a shield is a block: sparks, a clank, and the blocker pays stamina.
/// Sits on a held weapon, or on a bare forearm (definition = null) for fists. Shields never deal damage.
/// </summary>
public class Weapon : MonoBehaviour
{
    public Fighter owner;
    public Fighter.Hand hand;
    public WeaponDefinition definition;
    /// <summary>The collider that deals damage. Null for shields.</summary>
    public Collider2D strikeCollider;

    const float FistDamage = 4f;
    const float FistMinSpeed = 3f;
    const float ReferenceSpeed = 5f;   // a hit at this contact speed does the weapon's listed damage
    const float HitCooldown = 0.22f;

    float lastHitTime = -10f;

    /// <summary>Running totals for tuning: how many strike contacts were too slow, blocked, or landed.</summary>
    public static int StatContacts, StatTooSlow, StatBlocked, StatHits;
    public static float StatDamage;

    public bool IsShield => definition != null && definition.IsShield;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (owner == null || owner.IsHelpless || strikeCollider == null) return; // limp flailing doesn't count
        if (collision.otherCollider != strikeCollider) return; // the handle only bumps
        if (Time.time - lastHitTime < HitCooldown) return;

        Vector2 point = collision.GetContact(0).point;
        float speed = ContactSpeed(collision, point);
        float minSpeed = definition != null ? definition.minImpactSpeed : FistMinSpeed;
        StatContacts++;
        if (speed < minSpeed) { StatTooSlow++; return; }
        float force = Mathf.Clamp(speed / ReferenceSpeed, 0.4f, 1.6f);

        // Steel on steel, or on a shield: a block.
        var otherWeapon = collision.collider.GetComponent<Weapon>();
        if (otherWeapon != null && otherWeapon.owner != owner && otherWeapon.definition != null)
        {
            lastHitTime = Time.time;
            StatBlocked++;
            BloodFX.Sparks(point, 10);
            GameFeel.Play(otherWeapon.IsShield ? GameFeel.Sound.Wood : GameFeel.Sound.Metal, 0.35f + force * 0.3f);
            float cost = (definition != null ? definition.damage : FistDamage) * force * (otherWeapon.IsShield ? 0.45f : 0.3f);
            otherWeapon.owner.SpendStamina(cost);
            if (CameraRig.Current != null) CameraRig.Current.Shake(0.01f);
            return;
        }

        var part = collision.collider.GetComponent<BodyPart>();
        if (part == null || part.fighter == owner || part.fighter.IsKnockedOut) return;

        float damage = (definition != null ? definition.damage : FistDamage)
                       * force
                       * part.damageMultiplier
                       * FighterDefinition.DamageThrough(part.armor)
                       * owner.Definition.strength
                       * owner.DamageMultiplier;

        lastHitTime = Time.time;
        StatHits++;
        StatDamage += damage;
        Vector2 away =((Vector2)part.transform.position - owner.Torso.position).normalized;
        Vector2 swingDir = collision.relativeVelocity.sqrMagnitude > 0.01f ? -collision.relativeVelocity.normalized : away;
        HitEffects(part, point, swingDir, damage);
        part.fighter.TakeDamage(damage, part);
    }

    static void HitEffects(BodyPart part, Vector2 point, Vector2 dir, float damage)
    {
        bool hard = FighterDefinition.IsHard(part.armor);
        if (hard)
        {
            // Armour takes it: a puff of grit and a few sparks, a little blood only on big hits.
            BloodFX.Dust(point, dir, 8 + Mathf.RoundToInt(damage * 0.8f));
            BloodFX.Sparks(point, 5);
            if (damage > 8f) BloodFX.Spray(point, dir, Mathf.RoundToInt(damage * 0.8f));
            GameFeel.Play(GameFeel.Sound.Metal, 0.3f + damage * 0.025f);
        }
        else
        {
            BloodFX.Spray(point, dir, 8 + Mathf.RoundToInt(damage * 1.6f));
            GameFeel.Play(GameFeel.Sound.Flesh, 0.35f + damage * 0.03f);
        }

        // Knock the struck part along the swing, freeze a beat on big hits, shake the camera.
        var rb = part.GetComponent<Rigidbody2D>();
        if (rb != null) rb.AddForce(dir * Mathf.Min(damage, 25f) * 0.12f, ForceMode2D.Impulse);
        if (damage >= 7f) GameFeel.HitStop(Mathf.Lerp(0.04f, 0.1f, Mathf.InverseLerp(7f, 25f, damage)));
        if (CameraRig.Current != null) CameraRig.Current.Shake(0.015f + damage * 0.0025f);
    }

    /// <summary>Speed of this weapon relative to what it hit, measured at the contact point itself.</summary>
    static float ContactSpeed(Collision2D collision, Vector2 point)
    {
        Vector2 mine = collision.otherRigidbody != null ? collision.otherRigidbody.GetPointVelocity(point) : Vector2.zero;
        Vector2 theirs = collision.rigidbody != null ? collision.rigidbody.GetPointVelocity(point) : Vector2.zero;
        return (mine - theirs).magnitude;
    }
}
