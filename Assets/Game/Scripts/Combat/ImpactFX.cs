using UnityEngine;

/// <summary>
/// Short pixel-art impact flashes: a bright star burst where a blow lands (bigger for harder hits),
/// a streak along the blow, and a ring for shield bashes. Each lives a few frames, grows and fades.
/// </summary>
public class ImpactFX : MonoBehaviour
{
    public static Material Material;

    static Sprite star, streak, ring;

    float life, maxLife, startScale, endScale;
    SpriteRenderer sr;
    Color color;

    /// <param name="size">1 = a medium hit.</param>
    public static void Hit(Vector2 point, Vector2 direction, float size, Color color)
    {
        EnsureSprites();
        Spawn(star, point, Random.Range(0f, 90f), size * 0.9f, size * 1.5f, 0.1f, color);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Spawn(streak, point, angle, size * 1.1f, size * 1.6f, 0.08f, new Color(1f, 1f, 1f, color.a * 0.8f));
    }

    public static void Bash(Vector2 point, float size)
    {
        EnsureSprites();
        Spawn(ring, point, 0f, size * 0.35f, size * 1.05f, 0.14f, new Color(0.95f, 0.9f, 0.75f, 0.6f));
    }

    static void Spawn(Sprite sprite, Vector2 point, float angle, float from, float to, float seconds, Color color)
    {
        var go = new GameObject("Impact");
        go.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, 0f, angle));
        var fx = go.AddComponent<ImpactFX>();
        fx.sr = go.AddComponent<SpriteRenderer>();
        fx.sr.sprite = sprite;
        fx.sr.sortingOrder = 50;
        if (Material != null) fx.sr.sharedMaterial = Material;
        fx.color = color;
        fx.life = fx.maxLife = seconds;
        fx.startScale = from;
        fx.endScale = to;
        fx.Update();
    }

    void Update()
    {
        // Real time, so flashes still play during hit-stop.
        life -= Time.unscaledDeltaTime;
        if (life <= 0f) { Destroy(gameObject); return; }
        float t = 1f - life / maxLife;
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
        var c = color;
        c.a *= 1f - t * t;
        sr.color = c;
    }

    // ---- procedural pixel sprites (32 px per unit so they read as chunky pixels at 64 PPU art)

    static void EnsureSprites()
    {
        if (star != null) return;
        star = Make(15, (x, y, c) =>
        {
            int dx = Mathf.Abs(x - c), dy = Mathf.Abs(y - c);
            if (dx == 0 && dy <= c) return 1f;                       // vertical spike
            if (dy == 0 && dx <= c) return 1f;                       // horizontal spike
            if (dx == dy && dx <= c / 2) return 0.85f;               // short diagonals
            if (dx + dy <= 2) return 1f;                             // bright core
            return 0f;
        });
        streak = Make(15, (x, y, c) =>
        {
            int dy = Mathf.Abs(y - c);
            float along = x / (float)(2 * c);                        // thin at the back, thick at the front
            return dy <= Mathf.RoundToInt(along * 1.6f) ? 0.4f + along * 0.6f : 0f;
        });
        ring = Make(17, (x, y, c) =>
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
            return Mathf.Abs(d - (c - 1)) < 0.75f ? 1f : 0f;
        });
    }

    static Sprite Make(int size, System.Func<int, int, int, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        int c = size / 2;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(x, y, c)));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
    }
}
