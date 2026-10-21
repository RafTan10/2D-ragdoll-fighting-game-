using UnityEngine;

/// <summary>Light diagonal rain streaks in front of the background (arenas with rain = true).</summary>
public class RainFX : MonoBehaviour
{
    const int Count = 140;
    static readonly Vector2 Velocity = new Vector2(-1.2f, -7f);

    Transform[] drops;
    Camera cam;

    public static RainFX Create(Material material)
    {
        var fx = new GameObject("Rain").AddComponent<RainFX>();
        fx.Build(material);
        return fx;
    }

    void Build(Material material)
    {
        cam = Camera.main;
        var tex = new Texture2D(1, 6) { filterMode = FilterMode.Point };
        for (int y = 0; y < 6; y++) tex.SetPixel(0, y, new Color(0.85f, 0.88f, 0.9f, 0.15f + y * 0.06f));
        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 6), new Vector2(0.5f, 0.5f), Fighter.PixelsPerUnit);

        drops = new Transform[Count];
        float angle = Mathf.Atan2(Velocity.x, -Velocity.y) * Mathf.Rad2Deg;
        for (int i = 0; i < Count; i++)
        {
            var go = new GameObject("Drop");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = i % 3 == 0 ? 60 : -65; // some in front of the fighters, most behind
            if (material != null) sr.sharedMaterial = material;
            go.transform.rotation = Quaternion.Euler(0f, 0f, -angle);
            go.transform.localScale = new Vector3(1f, Random.Range(1.5f, 3f), 1f);
            drops[i] = go.transform;
            drops[i].position = RandomPoint(Random.Range(-1f, 1f));
        }
    }

    Vector3 RandomPoint(float yFraction)
    {
        float h = cam != null ? cam.orthographicSize : 2.8f;
        float w = h * (cam != null ? cam.aspect : 1.78f);
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        return new Vector3(c.x + Random.Range(-w - 1f, w + 1f), c.y + yFraction * h, 0f);
    }

    void Update()
    {
        if (cam == null) cam = Camera.main;
        float bottom = (cam != null ? cam.transform.position.y - cam.orthographicSize : -1f) - 0.2f;
        foreach (var d in drops)
        {
            d.position += (Vector3)(Velocity * Time.deltaTime);
            if (d.position.y < bottom || d.position.y < -0.05f && Random.value < 0.08f) d.position = RandomPoint(1.1f);
        }
    }
}
