using UnityEngine;

/// <summary>
/// A soft, pulsing glow in a power's colour behind a fighter while their super power lasts (or behind the opponent
/// it froze). Pixelated radial gradient, drawn behind both fighters and in front of the arena. Visual only.
/// </summary>
public class PowerAura : MonoBehaviour
{
    public static Material Material;
    static Sprite glow;

    Fighter follow;
    SpriteRenderer sr;
    Color color;
    float until, start;

    public static void Show(Fighter fighter, Color color, float seconds)
    {
        if (fighter == null || seconds <= 0f) return;
        var go = new GameObject("PowerAura");
        var aura = go.AddComponent<PowerAura>();
        aura.follow = fighter;
        aura.color = color;
        aura.start = Time.time;
        aura.until = Time.time + seconds;
        aura.sr = go.AddComponent<SpriteRenderer>();
        aura.sr.sprite = Glow();
        aura.sr.sortingOrder = -20;
        if (Material != null) aura.sr.sharedMaterial = Material;
        aura.Update();
    }

    void Update()
    {
        if (follow == null || follow.IsKnockedOut || Time.time > until) { Destroy(gameObject); return; }
        float t = Time.time - start, left = until - Time.time;
        // Torso-centred, a little taller than wide; breathes, and fades in / out at the ends.
        transform.position = (Vector3)follow.Torso.position + new Vector3(0f, -0.05f, 0f);
        float pulse = 1f + 0.08f * Mathf.Sin(t * 9f);
        transform.localScale = new Vector3(2.3f, 3.1f, 1f) * pulse;
        var c = color;
        c.a = 0.55f * Mathf.Clamp01(t / 0.15f) * Mathf.Clamp01(left / 0.3f) * (0.8f + 0.2f * Mathf.Sin(t * 13f));
        sr.color = c;
    }

    /// <summary>A 32 px radial gradient in 4 bands (reads as pixel art), white so it takes the power's colour.</summary>
    static Sprite Glow()
    {
        if (glow != null) return glow;
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                a = Mathf.Floor(a * a * 4f + 0.35f) / 4f; // banded
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        glow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return glow;
    }
}
