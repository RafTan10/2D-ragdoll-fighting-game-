using UnityEngine;

/// <summary>What a body part is covered with. Decides how much a hit hurts and what the hit looks/sounds like.</summary>
public enum ArmorMaterial { Skin, Cloth, Leather, Mail, Plate }

/// <summary>Everything that makes one playable character. Sprites are drawn facing right.</summary>
[CreateAssetMenu(menuName = "Game/Fighter", fileName = "Fighter")]
public class FighterDefinition : ScriptableObject
{
    public string displayName = "Fighter";

    [Header("Body sprites (one arm / one leg, reused for both sides)")]
    public Sprite head;
    public Sprite torso;
    public Sprite upperArm;
    public Sprite lowerArm;
    public Sprite upperLeg;
    public Sprite lowerLeg;
    [Tooltip("Optional. Hangs from the neck behind the body and flows with movement.")]
    public Sprite cape;

    [Header("Stats")]
    public float maxHealth = 100f;
    [Tooltip("Multiplies swing speed and strength.")]
    public float strength = 1f;
    public float walkSpeed = 2.2f;
    [Tooltip("Multiplies every body part's mass.")]
    public float weight = 1f;
    [Tooltip("Multiplies maximum stamina (heavy gear already lowers it).")]
    public float endurance = 1f;
    [Tooltip("Tint applied to the back arm/leg so the two sides read apart.")]
    public Color backLimbTint = new Color(0.75f, 0.75f, 0.75f);

    [Header("Armour (what each region is covered with)")]
    public ArmorMaterial headArmor = ArmorMaterial.Skin;
    public ArmorMaterial torsoArmor = ArmorMaterial.Cloth;
    public ArmorMaterial armArmor = ArmorMaterial.Cloth;
    public ArmorMaterial legArmor = ArmorMaterial.Cloth;

    [Header("Default loadout")]
    public WeaponDefinition mainHand;
    public WeaponDefinition offHand;

    public ArmorMaterial ArmorFor(PartKind kind)
    {
        switch (kind)
        {
            case PartKind.Head: return headArmor;
            case PartKind.Torso: return torsoArmor;
            case PartKind.Arm: return armArmor;
            default: return legArmor;
        }
    }

    /// <summary>Fraction of a hit's damage that gets through each material.</summary>
    public static float DamageThrough(ArmorMaterial m)
    {
        switch (m)
        {
            case ArmorMaterial.Plate: return 0.5f;
            case ArmorMaterial.Mail: return 0.7f;
            case ArmorMaterial.Leather: return 0.85f;
            case ArmorMaterial.Cloth: return 1f;
            default: return 1.15f;
        }
    }

    public static bool IsHard(ArmorMaterial m) => m == ArmorMaterial.Plate || m == ArmorMaterial.Mail;
}
