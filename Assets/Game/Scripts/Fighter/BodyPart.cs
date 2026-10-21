using UnityEngine;

public enum PartKind { Head, Torso, Arm, Leg }

/// <summary>Marks a collider as part of a fighter so weapons know whom they hit, how hard it counts and what it's wearing.</summary>
public class BodyPart : MonoBehaviour
{
    public Fighter fighter;
    public PartKind kind;
    public float damageMultiplier = 1f;
    public ArmorMaterial armor;
}
