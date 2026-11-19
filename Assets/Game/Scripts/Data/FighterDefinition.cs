using UnityEngine;

/// <summary>What a body part is covered with. Decides how much a hit hurts and what the hit looks/sounds like.</summary>
public enum ArmorMaterial { Skin, Cloth, Leather, Mail, Plate }

/// <summary>
/// What a fighter's super power does in Power Mode (see <see cref="PowerController"/>). Several fighters can share a
/// kind under their own name and colour.
/// </summary>
public enum PowerKind
{
    None,
    /// <summary>Hits harder, swings faster, moves faster for a while.</summary>
    Rage,
    /// <summary>A blast from the body that throws a nearby opponent off their feet.</summary>
    Shockwave,
    /// <summary>The opponent is frozen in place for a moment: no attacks, no guard.</summary>
    Freeze,
    /// <summary>Rushes at the opponent and lands one blow that can't be blocked.</summary>
    Dash,
    /// <summary>Takes little damage, can't be knocked down, and blows bounce the attacker back.</summary>
    Fortify,
    /// <summary>The main weapon grows huge and heavy for a while.</summary>
    GiantWeapon,
    /// <summary>Both hands swing nonstop at high speed, without tiring.</summary>
    Flurry,
    /// <summary>Full stamina, some health back, and quick tireless feet for a while.</summary>
    SecondWind
}

/// <summary>Everything that makes one playable character. Sprites are drawn facing right.</summary>
[CreateAssetMenu(menuName = "Game/Fighter", fileName = "Fighter")]
public class FighterDefinition : ScriptableObject
{
    public string displayName = "Fighter";
    [Tooltip("One of the game's stock characters (Knight, Barbarian, Ronin). Custom fighters are listed before them.")]
    public bool stock;
    [Tooltip("Short line shown under the name in the menu (who they are / how they fight).")]
    public string tagline;

    [Header("Body sprites (one arm / one leg, reused for both sides)")]
    public Sprite head;
    public Sprite torso;
    public Sprite upperArm;
    public Sprite lowerArm;
    public Sprite upperLeg;
    public Sprite lowerLeg;
    [Tooltip("Optional. Hangs from the neck behind the body and flows with movement.")]
    public Sprite cape;
    [Tooltip("Optional. A skirt / tunic hem fixed to the hips and drawn over both legs (pivot top-centre = the hip).")]
    public Sprite skirt;
    [Tooltip("Optional. Hangs from the off hand on a free hinge and swings (a bag, a lantern). Visual only, no hitbox.")]
    public Sprite handProp;

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

    [Header("Super power (Power Mode only)")]
    public PowerKind power = PowerKind.Rage;
    [Tooltip("Shown in the menu and shouted on screen when it fires, e.g. \"Biker Rage\".")]
    public string powerName = "Rage";
    public Color powerColor = new Color(1f, 0.75f, 0.25f);

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
