# Shared helpers for turning hi-res character art into the game's 64 PPU pixel parts.
# Dot-source it from a character's convert script:   . "$PSScriptRoot\..\tools\PixelParts.ps1"
#
#   $bmp = Load-Part $src "torso"                     # a PNG from the source pack
#   $bmp = Crop-Rows $bmp 14 322                      # keep rows 14..322 (full width)
#   $bmp = Compose 232 272 @(@($hair, 0, 0), @($head, 24, 48))   # layers back to front at (x, y)
#   Save-Part $bmp "$dst\torso.png" -Scale 0.1175     # downscale + pixel-art finish (see PixelParts.Finish)
#
# Finish = hard alpha, slight mute toward the game's overcast palette, an unsharp mask (keeps eyes and small
# patterns readable), an optional limited palette per part (k-means, -Colors; off by default because it merges
# small accents like red flowers into their neighbours), and a 1 px dark inner outline on the silhouette. Image borders are treated as "inside" (they are joint cuts),
# so seams at joints don't get outlined.

Add-Type -AssemblyName System.Drawing
if (-not ("PixelParts" -as [type])) {
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

public static class PixelParts
{
    public static Bitmap Downscale(Bitmap src, double scale)
    {
        int w = Math.Max(1, (int)Math.Round(src.Width * scale)), h = Math.Max(1, (int)Math.Round(src.Height * scale));
        var dst = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(dst))
        using (var attr = new ImageAttributes())
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingMode = CompositingMode.SourceCopy;
            attr.SetWrapMode(WrapMode.TileFlipXY);
            g.DrawImage(src, new Rectangle(0, 0, w, h), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
        }
        return dst;
    }

    /// <param name="colors">Palette size for this part (0 = keep all colours).</param>
    /// <param name="mute">0..1: pull saturation down and warm the colours slightly (game look ~0.15).</param>
    /// <param name="sharpen">0..1 unsharp mask on the downscaled pixels (brings back eyes, flowers, seams).</param>
    public static void Finish(Bitmap bmp, int colors, double mute, Color outline, int alphaCut, double sharpen)
    {
        int w = bmp.Width, h = bmp.Height;
        var px = new Color[w, h];
        var solid = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color c = bmp.GetPixel(x, y);
                solid[x, y] = c.A >= alphaCut;
                px[x, y] = solid[x, y] ? Mute(c, mute) : Color.Transparent;
            }

        if (sharpen > 0) Sharpen(px, solid, w, h, sharpen);
        if (colors > 0) Quantize(px, solid, w, h, colors);

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!solid[x, y]) { bmp.SetPixel(x, y, Color.FromArgb(0, 0, 0, 0)); continue; }
                Color c = px[x, y];
                if (IsEdge(solid, x, y, w, h) && Lum(c) > 55) c = Lerp(c, outline, 0.72);
                bmp.SetPixel(x, y, Color.FromArgb(255, c.R, c.G, c.B));
            }
    }

    // Unsharp mask against the average of the solid 8-neighbours.
    static void Sharpen(Color[,] px, bool[,] solid, int w, int h, double amount)
    {
        var src = (Color[,])px.Clone();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!solid[x, y]) continue;
                double r = 0, g = 0, b = 0; int n = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if ((dx == 0 && dy == 0) || nx < 0 || ny < 0 || nx >= w || ny >= h || !solid[nx, ny]) continue;
                        r += src[nx, ny].R; g += src[nx, ny].G; b += src[nx, ny].B; n++;
                    }
                if (n == 0) continue;
                Color c = src[x, y];
                px[x, y] = Color.FromArgb(255, Clamp(c.R + (c.R - r / n) * amount), Clamp(c.G + (c.G - g / n) * amount), Clamp(c.B + (c.B - b / n) * amount));
            }
    }

    static bool IsEdge(bool[,] solid, int x, int y, int w, int h)
    {
        return Empty(solid, x - 1, y, w, h) || Empty(solid, x + 1, y, w, h) || Empty(solid, x, y - 1, w, h) || Empty(solid, x, y + 1, w, h);
    }

    // Outside the image counts as filled: borders are joint cuts, not silhouette.
    static bool Empty(bool[,] solid, int x, int y, int w, int h)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return false;
        return !solid[x, y];
    }

    static double Lum(Color c) { return 0.299 * c.R + 0.587 * c.G + 0.114 * c.B; }

    static Color Lerp(Color a, Color b, double t)
    {
        return Color.FromArgb(255, (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t));
    }

    static Color Mute(Color c, double mute)
    {
        double l = Lum(c);
        double r = c.R + (l - c.R) * mute, g = c.G + (l - c.G) * mute, b = c.B + (l - c.B) * mute;
        r *= 1 + 0.04 * mute * 4; b *= 1 - 0.05 * mute * 4; // a touch warmer, like the overcast arena light
        return Color.FromArgb(255, Clamp(r), Clamp(g), Clamp(b));
    }

    static int Clamp(double v) { return v < 0 ? 0 : v > 255 ? 255 : (int)Math.Round(v); }

    // k-means in RGB, seeded evenly along brightness so darks (outlines) and highlights both keep a colour.
    static void Quantize(Color[,] px, bool[,] solid, int w, int h, int k)
    {
        var list = new List<int[]>();
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (solid[x, y]) list.Add(new[] { px[x, y].R, px[x, y].G, px[x, y].B, x, y });
        if (list.Count <= k) return;
        list.Sort((a, b) => (a[0] * 299 + a[1] * 587 + a[2] * 114).CompareTo(b[0] * 299 + b[1] * 587 + b[2] * 114));
        var centers = new double[k, 3];
        for (int i = 0; i < k; i++)
        {
            var s = list[(int)((i + 0.5) * list.Count / k)];
            centers[i, 0] = s[0]; centers[i, 1] = s[1]; centers[i, 2] = s[2];
        }
        var owner = new int[list.Count];
        for (int iter = 0; iter < 12; iter++)
        {
            var sum = new double[k, 4];
            for (int p = 0; p < list.Count; p++)
            {
                int best = 0; double bestD = double.MaxValue;
                for (int i = 0; i < k; i++)
                {
                    double dr = list[p][0] - centers[i, 0], dg = list[p][1] - centers[i, 1], db = list[p][2] - centers[i, 2];
                    double d = dr * dr * 0.3 + dg * dg * 0.59 + db * db * 0.11;
                    if (d < bestD) { bestD = d; best = i; }
                }
                owner[p] = best;
                sum[best, 0] += list[p][0]; sum[best, 1] += list[p][1]; sum[best, 2] += list[p][2]; sum[best, 3]++;
            }
            for (int i = 0; i < k; i++)
                if (sum[i, 3] > 0) { centers[i, 0] = sum[i, 0] / sum[i, 3]; centers[i, 1] = sum[i, 1] / sum[i, 3]; centers[i, 2] = sum[i, 2] / sum[i, 3]; }
        }
        for (int p = 0; p < list.Count; p++)
        {
            int i = owner[p];
            px[list[p][3], list[p][4]] = Color.FromArgb(255, Clamp(centers[i, 0]), Clamp(centers[i, 1]), Clamp(centers[i, 2]));
        }
    }
}
'@
}

# The game's outline colour (dark warm brown-purple, as on the Knight / Barbarian).
$PixelOutline = [System.Drawing.Color]::FromArgb(255, 36, 26, 30)

function Load-Part([string]$dir, [string]$name) { [System.Drawing.Bitmap]::FromFile((Join-Path $dir "$name.png")) }

function Crop-Rows($bmp, [int]$r0, [int]$r1) {
    $h = $r1 - $r0
    $out = New-Object System.Drawing.Bitmap $bmp.Width, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($out)
    $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle 0, 0, $bmp.Width, $h), (New-Object System.Drawing.Rectangle 0, $r0, $bmp.Width, $h), [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose(); $out
}

# Layers back to front: each item is @(bitmap, x, y) on a w x h transparent canvas.
function Compose([int]$w, [int]$h, $layers) {
    $out = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($out)
    foreach ($l in $layers) { $g.DrawImage($l[0], [int]$l[1], [int]$l[2], $l[0].Width, $l[0].Height) }
    $g.Dispose(); $out
}

# Downscale, finish as pixel art and save. Returns "name WxH".
function Save-Part($bmp, [string]$path, [double]$Scale, [int]$Colors = 0, [double]$Mute = 0.08, [double]$Sharpen = 0.45, [int]$AlphaCut = 110) {
    $small = [PixelParts]::Downscale($bmp, $Scale)
    [PixelParts]::Finish($small, $Colors, $Mute, $PixelOutline, $AlphaCut, $Sharpen)
    $small.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $r = "{0} {1}x{2}" -f [IO.Path]::GetFileNameWithoutExtension($path), $small.Width, $small.Height
    $small.Dispose(); $r
}
