using UnityEngine;

/// <summary>Builds an arena's background layers, floor and side walls. The floor surface is at y = 0.</summary>
public static class ArenaView
{
    /// <summary>The floor collider of the current arena (fighters use it for ground checks).</summary>
    public static Collider2D Ground { get; private set; }

    /// <summary>Half the walkable width of the current arena (walls stand just beyond it).</summary>
    public static float HalfWidth { get; private set; } = 4.6f;

    /// <summary>World size of the background (640x360 px at 64 PPU).</summary>
    public const float Width = 10f, Height = 5.625f;

    public static float BottomY(ArenaDefinition arena) => -arena.floorPixels / Fighter.PixelsPerUnit;

    public static GameObject Build(ArenaDefinition arena, Material material)
    {
        var root = new GameObject("Arena_" + arena.displayName);
        float bottom = BottomY(arena);
        HalfWidth = arena.halfWidth;

        Layer(root, "Sky", arena.sky, bottom, -100, material);
        Layer(root, "Far", arena.far, bottom, -90, material);
        Layer(root, "Near", arena.near, bottom, -80, material);
        Layer(root, "Ground", arena.ground, bottom, -70, material);

        var grip = new PhysicsMaterial2D("ArenaGrip") { friction = 0.9f, bounciness = 0f };

        var floor = root.AddComponent<BoxCollider2D>();
        floor.offset = new Vector2(0f, -1f);
        floor.size = new Vector2(40f, 2f);
        floor.sharedMaterial = grip;
        Ground = floor;

        foreach (float side in new[] { -1f, 1f })
        {
            var wall = root.AddComponent<BoxCollider2D>();
            wall.offset = new Vector2(side * (arena.halfWidth + 0.5f), 5f);
            wall.size = new Vector2(1f, 14f);
        }
        return root;
    }

    static void Layer(GameObject root, string name, Sprite sprite, float bottom, int order, Material material)
    {
        if (sprite == null) return;
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(0f, bottom, 0f); // sprites pivot at bottom-centre
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (material != null) sr.sharedMaterial = material;
    }
}
