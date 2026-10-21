using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pixel particles: blood that flies, falls and stays on the floor as stains; sparks off steel; dust puffs off armour.
/// Simulated by hand (no physics bodies) so hundreds of them cost almost nothing.
/// </summary>
public class BloodFX : MonoBehaviour
{
    public static Material Material;

    const int MaxDrops = 700;
    const float Gravity = 14f;

    static BloodFX instance;
    static Sprite pixel;

    enum Kind { Blood, Spark, Dust }

    class Drop
    {
        public Transform transform; public SpriteRenderer renderer;
        public Vector2 velocity; public bool landed; public Kind kind;
        public float life, maxLife; public Color color;
    }
    readonly List<Drop> drops = new List<Drop>();
    int nextRecycle;

    static readonly Color[] BloodColors =
    {
        new Color(0.62f, 0.02f, 0.03f), new Color(0.78f, 0.05f, 0.05f), new Color(0.45f, 0.0f, 0.02f)
    };
    static readonly Color[] SparkColors = { new Color(1f, 0.95f, 0.6f), new Color(1f, 0.75f, 0.2f), Color.white };
    static readonly Color[] DustColors = { new Color(0.72f, 0.7f, 0.64f), new Color(0.6f, 0.58f, 0.53f), new Color(0.82f, 0.8f, 0.74f) };

    public static void Spray(Vector2 at, Vector2 direction, int count) => Emit(Kind.Blood, at, direction, count, 1.6f);

    public static void Sparks(Vector2 at, int count) => Emit(Kind.Spark, at, Vector2.up, count, 2.8f);

    /// <summary>A grey burst off armour: drifts out, rises a little and fades.</summary>
    public static void Dust(Vector2 at, Vector2 direction, int count) => Emit(Kind.Dust, at, direction, count, 0.9f);

    public static void Clear()
    {
        if (instance == null) return;
        foreach (var d in instance.drops) Destroy(d.transform.gameObject);
        instance.drops.Clear();
        instance.nextRecycle = 0;
    }

    static void Emit(Kind kind, Vector2 at, Vector2 direction, int count, float speed)
    {
        if (instance == null) instance = new GameObject("BloodFX").AddComponent<BloodFX>();
        if (pixel == null)
        {
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), Fighter.PixelsPerUnit);
        }

        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        var palette = kind == Kind.Blood ? BloodColors : kind == Kind.Spark ? SparkColors : DustColors;
        for (int i = 0; i < count; i++)
        {
            Drop d = instance.GetDrop();
            d.kind = kind;
            d.transform.position = at + Random.insideUnitCircle * (kind == Kind.Dust ? 0.05f : 0.01f);
            d.transform.localScale = Vector3.one * (kind == Kind.Dust ? Random.Range(3, 6) : Random.Range(2, 4));
            d.color = palette[Random.Range(0, palette.Length)];
            d.renderer.color = d.color;
            d.renderer.sortingOrder = 40;
            Vector2 v = (direction + Random.insideUnitCircle * 0.9f + Vector2.up * (kind == Kind.Dust ? 0.2f : 0.6f)).normalized;
            d.velocity = v * speed * Random.Range(0.6f, 1.6f);
            d.landed = false;
            d.maxLife = d.life = kind == Kind.Blood ? 999f : kind == Kind.Spark ? Random.Range(0.18f, 0.35f) : Random.Range(0.35f, 0.6f);
            d.renderer.enabled = true;
        }
    }

    Drop GetDrop()
    {
        // Prefer a finished spark/dust particle, then a free slot, then recycle the oldest.
        foreach (var d in drops) if (!d.renderer.enabled) return d;
        if (drops.Count < MaxDrops)
        {
            var go = new GameObject("FX");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            if (Material != null) sr.sharedMaterial = Material;
            var d = new Drop { transform = go.transform, renderer = sr };
            drops.Add(d);
            return d;
        }
        var recycled = drops[nextRecycle];
        nextRecycle = (nextRecycle + 1) % drops.Count;
        return recycled;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        foreach (var d in drops)
        {
            if (d.landed || !d.renderer.enabled) continue;

            if (d.kind != Kind.Blood)
            {
                d.life -= dt;
                if (d.life <= 0f) { d.renderer.enabled = false; continue; }
                var c = d.color;
                c.a = Mathf.Clamp01(d.life / d.maxLife) * (d.kind == Kind.Dust ? 0.8f : 1f);
                d.renderer.color = c;
            }

            if (d.kind == Kind.Dust) d.velocity *= 1f - 3.5f * dt;         // puffs slow down quickly
            else d.velocity.y -= Gravity * dt;

            Vector3 p = d.transform.position + (Vector3)(d.velocity * dt);
            if (p.y <= 0f && d.kind == Kind.Blood)
            {
                p.y = Random.Range(-0.12f, 0f); // stain on the floor, behind the fighters' feet
                d.landed = true;
                d.renderer.sortingOrder = -60;
                d.transform.localScale = new Vector3(Random.Range(2, 5), 1f, 1f); // splat flat
                d.renderer.color = d.color * 0.75f;
            }
            else if (p.y <= 0f) { d.renderer.enabled = false; continue; }
            d.transform.position = p;
        }
    }
}
