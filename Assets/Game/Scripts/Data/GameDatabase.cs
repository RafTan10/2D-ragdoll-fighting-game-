using System.Collections.Generic;
using UnityEngine;

/// <summary>The list of all content in the game. Editor tools register new definitions here.</summary>
[CreateAssetMenu(menuName = "Game/Game Database", fileName = "GameDatabase")]
public class GameDatabase : ScriptableObject
{
    public List<FighterDefinition> fighters = new List<FighterDefinition>();
    public List<WeaponDefinition> weapons = new List<WeaponDefinition>();
    public List<ArenaDefinition> arenas = new List<ArenaDefinition>();
    public List<AIProfile> aiProfiles = new List<AIProfile>();

    [Tooltip("Unlit sprite material used for fighters, weapons, arenas and blood.")]
    public Material spriteMaterial;
}
