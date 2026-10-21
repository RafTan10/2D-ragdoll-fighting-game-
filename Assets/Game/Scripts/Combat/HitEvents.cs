using System;
using UnityEngine;

public enum HitStrength { Light, Medium, Heavy }

/// <summary>A weapon's striking part landed on a body.</summary>
public struct HitInfo
{
    public Fighter attacker, victim;
    public BodyPart part;
    /// <summary>Damage after armour and body-part multipliers.</summary>
    public float damage;
    /// <summary>Collision impulse (N·s): speed × mass, the "how hard" of the hit.</summary>
    public float impact;
    public Vector2 point;
    /// <summary>Direction the blow travels (into the victim).</summary>
    public Vector2 direction;
    public HitStrength strength;
    /// <summary>Landed on plate or mail.</summary>
    public bool armoured;
}

/// <summary>A strike was stopped by a weapon or shield.</summary>
public struct BlockInfo
{
    public Fighter attacker, blocker;
    public Vector2 point;
    public float impact;
    public bool shield;
}

/// <summary>
/// The combat event hub. Weapons raise hits and blocks here; effects, sound, camera shake and UI listen, so
/// detection never plays effects itself and everything reacts to the same hit strength.
/// </summary>
public static class HitEvents
{
    public static event Action<HitInfo> Hit;
    public static event Action<BlockInfo> Blocked;

    /// <summary>Log every hit (impact, damage, part) to the Console for tuning. Toggle with F3 in a match.</summary>
    public static bool LogHits;

    public static void RaiseHit(HitInfo hit)
    {
        if (LogHits)
            Debug.Log($"[Hit] {hit.attacker.Definition.displayName} → {hit.victim.Definition.displayName} {hit.part.kind} " +
                      $"impact={hit.impact:F1} damage={hit.damage:F1} {hit.strength}{(hit.armoured ? " (armour)" : "")}");
        Hit?.Invoke(hit);
    }

    public static void RaiseBlocked(BlockInfo block)
    {
        if (LogHits)
            Debug.Log($"[Block] {block.attacker.Definition.displayName} blocked by {block.blocker.Definition.displayName}" +
                      $"{(block.shield ? "'s shield" : "")} impact={block.impact:F1}");
        Blocked?.Invoke(block);
    }
}
