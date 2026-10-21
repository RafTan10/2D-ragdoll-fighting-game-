using UnityEngine;

public enum WeaponType { Dagger, Sword, Axe, Mace, Hammer, Shield }

/// <summary>A hand-held item. The sprite is drawn pointing up with the grip at the bottom (shields are centred).</summary>
[CreateAssetMenu(menuName = "Game/Weapon", fileName = "Weapon")]
public class WeaponDefinition : ScriptableObject
{
    public string displayName = "Weapon";
    public WeaponType type = WeaponType.Sword;
    public Sprite sprite;

    [Header("Combat")]
    [Tooltip("Damage of a hit at the reference impact speed (6 m/s).")]
    public float damage = 12f;
    public float mass = 0.6f;
    [Tooltip("Hits slower than this (m/s) do nothing.")]
    public float minImpactSpeed = 2.5f;
    public bool twoHanded = false;

    [Header("Holding")]
    [Tooltip("Angle of the weapon relative to the forearm (0 = continues the forearm, 90 = points forward).")]
    public float gripAngle = 80f;
    [Tooltip("Pixels from the bottom of the sprite where the hand grips it.")]
    public float gripPixels = 2f;

    [Tooltip("Where the striking part starts, as a fraction of the sprite height from the bottom (grip end). " +
             "Only that part deals damage; the handle below it just bumps. -1 = a default for the weapon type.")]
    [Range(-1f, 1f)] public float strikeFrom = -1f;

    public bool IsShield => type == WeaponType.Shield;

    /// <summary>Start of the damaging part (blade / head), 0..1 from the grip end.</summary>
    public float StrikeStart
    {
        get
        {
            if (strikeFrom >= 0f) return strikeFrom;
            switch (type)
            {
                case WeaponType.Dagger: return 0.33f;
                case WeaponType.Sword: return 0.22f;
                case WeaponType.Axe: return 0.58f;
                case WeaponType.Mace: return 0.62f;
                case WeaponType.Hammer: return 0.74f;
                default: return 0f;
            }
        }
    }
}
