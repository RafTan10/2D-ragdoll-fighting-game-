using UnityEngine;

/// <summary>How a computer-controlled fighter behaves.</summary>
[CreateAssetMenu(menuName = "Game/AI Profile", fileName = "AIProfile")]
public class AIProfile : ScriptableObject
{
    public string displayName = "Normal";
    [Tooltip("Seconds between decisions.")]
    public float reactionTime = 0.35f;
    [Tooltip("How long it holds a wind-up before striking (min, max seconds).")]
    public Vector2 windUp = new Vector2(0.3f, 0.6f);
    [Tooltip("Seconds to wait after a strike (min, max).")]
    public Vector2 recovery = new Vector2(0.4f, 0.9f);
    [Range(0f, 1f)] public float aggression = 0.6f;
    [Range(0f, 1f)] public float blockChance = 0.4f;
    [Range(0f, 1f)] public float hopChance = 0.1f;
    [Tooltip("Multiplier on damage this fighter deals.")]
    public float damageMultiplier = 1f;
}
