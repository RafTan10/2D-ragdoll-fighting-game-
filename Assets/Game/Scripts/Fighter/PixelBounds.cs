using System.Collections.Generic;
using UnityEngine;

/// <summary>Bounds of the visible (non-transparent) pixels of a sprite, in the sprite's local units. Cached.</summary>
public static class PixelBounds
{
    static readonly Dictionary<(Sprite, int, int), Rect> cache = new Dictionary<(Sprite, int, int), Rect>();

    /// <param name="from01">Bottom of the band to measure, 0..1 of the sprite height.</param>
    /// <param name="to01">Top of the band.</param>
    public static Rect Opaque(Sprite sprite, float from01, float to01)
    {
        var key = (sprite, Mathf.RoundToInt(from01 * 1000f), Mathf.RoundToInt(to01 * 1000f));
        if (cache.TryGetValue(key, out var cached)) return cached;

        Rect r = sprite.rect;
        float ppu = sprite.pixelsPerUnit;
        Vector2 pivot = sprite.pivot;
        int y0 = Mathf.FloorToInt(r.height * from01), y1 = Mathf.CeilToInt(r.height * to01);
        Rect fallback = Rect.MinMaxRect(-pivot.x / ppu, (y0 - pivot.y) / ppu, (r.width - pivot.x) / ppu, (y1 - pivot.y) / ppu);

        Rect result = fallback;
        if (sprite.texture != null && sprite.texture.isReadable)
        {
            Color32[] px = sprite.texture.GetPixels32();
            int texW = sprite.texture.width;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = y0; y < y1; y++)
                for (int x = 0; x < (int)r.width; x++)
                {
                    if (px[((int)r.y + y) * texW + (int)r.x + x].a < 128) continue;
                    if (x < minX) minX = x; if (x > maxX) maxX = x;
                    if (y < minY) minY = y; if (y > maxY) maxY = y;
                }
            if (maxX >= 0)
                result = Rect.MinMaxRect((minX - pivot.x) / ppu, (minY - pivot.y) / ppu, (maxX + 1 - pivot.x) / ppu, (maxY + 1 - pivot.y) / ppu);
        }
        cache[key] = result;
        return result;
    }
}
