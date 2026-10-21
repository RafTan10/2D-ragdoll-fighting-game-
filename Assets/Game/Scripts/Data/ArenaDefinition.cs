using UnityEngine;

/// <summary>A background scene to fight in. Layers are 640x360 px; the ground layer's top edge is the floor.</summary>
[CreateAssetMenu(menuName = "Game/Arena", fileName = "Arena")]
public class ArenaDefinition : ScriptableObject
{
    public string displayName = "Arena";
    public Sprite sky;
    public Sprite far;
    public Sprite near;
    public Sprite ground;

    [Tooltip("Height of the floor surface, in pixels from the bottom of the ground sprite.")]
    public float floorPixels = 56f;
    [Tooltip("Half the width of the walkable area, in world units.")]
    public float halfWidth = 4.6f;
    public Color fogColor = new Color(0.55f, 0.55f, 0.42f);
    public bool rain = true;
}
