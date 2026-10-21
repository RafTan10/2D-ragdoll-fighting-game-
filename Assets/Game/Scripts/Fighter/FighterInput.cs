using UnityEngine;

/// <summary>What a brain wants its fighter to do this frame.</summary>
public struct FighterInput
{
    /// <summary>-1 = walk left, +1 = walk right (world direction).</summary>
    public float move;
    /// <summary>One-shot: lunge towards the opponent.</summary>
    public bool hop;
    /// <summary>One-shot: jump up (and along with <see cref="move"/>). High enough to leap over the opponent.</summary>
    public bool jump;

    /// <summary>The main hand (weapon) is being controlled this frame; otherwise it relaxes into guard.</summary>
    public bool mainActive;
    /// <summary>Where the main hand reaches for, as a world-space offset from its shoulder. Clamped to arm reach.</summary>
    public Vector2 mainAim;

    /// <summary>The off hand (shield or second weapon) is being controlled this frame.</summary>
    public bool offActive;
    /// <summary>Where the off hand reaches for, as a world-space offset from its shoulder.</summary>
    public Vector2 offAim;
}

/// <summary>Produces input for a fighter: a player's controls, an AI, later a network peer.</summary>
public interface IFighterBrain
{
    FighterInput Think(Fighter self, Fighter opponent);
}
