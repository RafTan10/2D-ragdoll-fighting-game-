using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Breakable armour. Each body region (head, torso, arms, legs) covered with leather, mail or plate has durability.
/// Every blow on it wears the armour by the blow's full damage while only part of the damage reaches the body;
/// as it wears, it protects less, and at zero it breaks off and the region is bare skin (hits hurt more and bleed).
/// Worn armour darkens; breaking raises <see cref="HitEvents.ArmorBroken"/>. Cloth and skin can't break.
/// </summary>
public class ArmorController : MonoBehaviour
{
    [Header("Durability (damage the armour can soak before it breaks)")]
    [SerializeField] float plateDurability = 80f;
    [SerializeField] float mailDurability = 55f;
    [SerializeField] float leatherDurability = 32f;
    [Tooltip("How much of its protection worn-out armour keeps just before breaking (1 = full until it breaks).")]
    [SerializeField] float protectionAtBreak = 0.5f;
    [Tooltip("How far each full durability's worth of wear dulls the armour sprite (0 = no change).")]
    [SerializeField] float wearDarken = 0.35f;

    const int Regions = 4; // PartKind values
    readonly ArmorMaterial[] material = new ArmorMaterial[Regions];
    readonly float[] max = new float[Regions], left = new float[Regions];
    readonly List<BodyPart>[] parts = new List<BodyPart>[Regions];
    Fighter fighter;

    public void Init(Fighter owner, FighterDefinition def)
    {
        fighter = owner;
        for (int i = 0; i < Regions; i++)
        {
            material[i] = def.ArmorFor((PartKind)i);
            max[i] = left[i] = Durability(material[i]);
            parts[i] = new List<BodyPart>();
        }
        foreach (var part in GetComponentsInChildren<BodyPart>()) parts[(int)part.kind].Add(part);
    }

    float Durability(ArmorMaterial m)
    {
        switch (m)
        {
            case ArmorMaterial.Plate: return plateDurability;
            case ArmorMaterial.Mail: return mailDurability;
            case ArmorMaterial.Leather: return leatherDurability;
            default: return 0f;
        }
    }

    /// <summary>The region wears breakable armour (intact or not).</summary>
    public bool Has(PartKind kind) => max[(int)kind] > 0f;
    /// <summary>1 = untouched, 0 = broken (or never had any).</summary>
    public float Integrity(PartKind kind) => max[(int)kind] > 0f ? left[(int)kind] / max[(int)kind] : 0f;
    public ArmorMaterial Material(PartKind kind) => material[(int)kind];
    public bool HasAny { get { for (int i = 0; i < Regions; i++) if (max[i] > 0f) return true; return false; } }

    /// <summary>
    /// A blow of <paramref name="raw"/> damage lands on <paramref name="part"/>. Returns the damage that reaches the
    /// body. Intact armour lets only part through (less the more worn it is) and loses durability equal to the blow.
    /// </summary>
    public float Absorb(BodyPart part, float raw, out float wear, out bool armoured)
    {
        int i = (int)part.kind;
        wear = 0f;
        armoured = false;
        if (left[i] <= 0f) return raw * FighterDefinition.DamageThrough(part.armor); // cloth, skin or broken

        armoured = FighterDefinition.IsHard(material[i]);
        float integrity = left[i] / max[i];
        float protection = 1f - FighterDefinition.DamageThrough(material[i]);
        protection *= Mathf.Lerp(protectionAtBreak, 1f, integrity);
        float body = raw * (1f - protection);

        wear = Mathf.Min(raw, left[i]);
        left[i] -= wear;
        foreach (var p in parts[i]) fighter.Body.Wear(p.GetComponent<SpriteRenderer>(), wear / max[i] * wearDarken);
        if (left[i] <= 0f) Break(i, part);
        return body;
    }

    void Break(int i, BodyPart struck)
    {
        left[i] = 0f;
        foreach (var p in parts[i]) p.armor = ArmorMaterial.Skin;
        HitEvents.RaiseArmorBroken(new ArmorBreakInfo
        {
            fighter = fighter, region = (PartKind)i, material = material[i],
            point = struck != null ? (Vector2)struck.transform.position : fighter.Torso.position
        });
    }
}
