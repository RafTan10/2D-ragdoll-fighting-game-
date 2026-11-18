# Pixel-art drawing for arena layers (640x360): hard-edged rectangles, lines, filled polygons, ordered-dither
# gradients and texture noise, written to a transparent PNG. Dot-source it from an arena's script:
#
#   . "$PSScriptRoot\..\tools\PixelScene.ps1"
#   $c = New-Canvas                       # 640x360, transparent
#   $c.Rect(0, 0, 640, 360, (Col '#5A5048'), 6)            # x, y, w, h, colour, noise (0 = flat)
#   $c.Gradient(0, 0, 640, 200, (Col '#8A8A80'), (Col '#4A4A44'))   # vertical, Bayer-dithered
#   $c.Line(10, 10, 100, 60, (Col '#1E1418'))
#   $c.Poly(@(0,0, 50,0, 30,40), (Col '#7A6A5A'), 4)       # x0,y0, x1,y1, ... filled
#   $c.Save("Assets\Game\Art\Arenas\X\far.png")
#
# y grows downwards (image coordinates). The arena floor line is y = 304 (56 px up from the bottom).

Add-Type -AssemblyName System.Drawing
if (-not ("PixelCanvas" -as [type])) {
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public class PixelCanvas
{
    public readonly int W, H;
    readonly int[] px; // ARGB
    readonly Random rand;
    static readonly int[,] Bayer = { { 0, 8, 2, 10 }, { 12, 4, 14, 6 }, { 3, 11, 1, 9 }, { 15, 7, 13, 5 } };

    public PixelCanvas(int w, int h, int seed) { W = w; H = h; px = new int[w * h]; rand = new Random(seed); }

    public void Set(int x, int y, Color c, int noise)
    {
        if (x < 0 || y < 0 || x >= W || y >= H || c.A == 0) return;
        int d = noise > 0 ? rand.Next(-noise, noise + 1) : 0;
        px[y * W + x] = (255 << 24) | (Cl(c.R + d) << 16) | (Cl(c.G + d) << 8) | Cl(c.B + d);
    }

    public void Clear(int x, int y) { if (x >= 0 && y >= 0 && x < W && y < H) px[y * W + x] = 0; }

    public void Rect(int x, int y, int w, int h, Color c, int noise)
    {
        for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Set(i, j, c, noise);
    }

    /// <summary>Two colours mixed by a 4x4 Bayer pattern; amount 0..1 = share of b.</summary>
    public void Dither(int x, int y, int w, int h, Color a, Color b, double amount, int noise)
    {
        for (int j = y; j < y + h; j++)
            for (int i = x; i < x + w; i++)
                Set(i, j, (Bayer[((j % 4) + 4) % 4, ((i % 4) + 4) % 4] + 0.5) / 16.0 < amount ? b : a, noise);
    }

    /// <summary>Vertical gradient top→bottom, dithered between 6 bands so it stays pixel-art.</summary>
    public void Gradient(int x, int y, int w, int h, Color top, Color bottom)
    {
        for (int j = y; j < y + h; j++)
        {
            double t = h <= 1 ? 0 : (j - y) / (double)(h - 1) * 6.0;
            int band = (int)Math.Floor(t); double f = t - band;
            Color a = Mix(top, bottom, band / 6.0), b = Mix(top, bottom, Math.Min(6, band + 1) / 6.0);
            for (int i = x; i < x + w; i++) Set(i, j, (Bayer[j & 3, i & 3] + 0.5) / 16.0 < f ? b : a, 0);
        }
    }

    public void Line(int x0, int y0, int x1, int y1, Color c)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            Set(x0, y0, c, 0);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    /// <summary>Filled polygon (even-odd scanline), coordinates as x0,y0,x1,y1,...</summary>
    public void Poly(int[] xy, Color c, int noise)
    {
        int n = xy.Length / 2, minY = int.MaxValue, maxY = int.MinValue;
        for (int k = 0; k < n; k++) { minY = Math.Min(minY, xy[2 * k + 1]); maxY = Math.Max(maxY, xy[2 * k + 1]); }
        var xs = new System.Collections.Generic.List<double>();
        for (int y = minY; y <= maxY; y++)
        {
            xs.Clear(); double sy = y + 0.5;
            for (int k = 0; k < n; k++)
            {
                double ax = xy[2 * k], ay = xy[2 * k + 1], bx = xy[2 * ((k + 1) % n)], by = xy[2 * ((k + 1) % n) + 1];
                if ((ay <= sy && by > sy) || (by <= sy && ay > sy)) xs.Add(ax + (sy - ay) / (by - ay) * (bx - ax));
            }
            xs.Sort();
            for (int k = 0; k + 1 < xs.Count; k += 2)
                for (int x = (int)Math.Round(xs[k]); x < (int)Math.Round(xs[k + 1]); x++) Set(x, y, c, noise);
        }
    }

    /// <summary>Outlined box: fill plus a 1 px darker border.</summary>
    public void Box(int x, int y, int w, int h, Color fill, Color edge, int noise)
    {
        Rect(x, y, w, h, edge, 0); Rect(x + 1, y + 1, w - 2, h - 2, fill, noise);
    }

    public void Save(string path)
    {
        var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        var data = bmp.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        Marshal.Copy(px, 0, data.Scan0, px.Length);
        bmp.UnlockBits(data);
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
        bmp.Save(path, ImageFormat.Png); bmp.Dispose();
    }

    public static Color Mix(Color a, Color b, double t)
    {
        return Color.FromArgb(255, Cl(a.R + (b.R - a.R) * t), Cl(a.G + (b.G - a.G) * t), Cl(a.B + (b.B - a.B) * t));
    }
    static int Cl(double v) { return v < 0 ? 0 : v > 255 ? 255 : (int)Math.Round(v); }
}
'@
}

function Col([string]$hex) { [System.Drawing.ColorTranslator]::FromHtml($hex) }
function Mix($a, $b, [double]$t) { [PixelCanvas]::Mix($a, $b, $t) }
function New-Canvas([int]$seed = 1) { New-Object PixelCanvas 640, 360, $seed }

# The four layers stacked into one preview, enlarged 2x.
function Save-ArenaPreview([string]$dir, [string]$out) {
    $all = New-Object System.Drawing.Bitmap 1280, 720
    $g = [System.Drawing.Graphics]::FromImage($all)
    $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    foreach ($n in "sky", "far", "near", "ground") {
        if (Test-Path "$dir\$n.png") { $b = [System.Drawing.Bitmap]::FromFile("$dir\$n.png"); $g.DrawImage($b, 0, 0, 1280, 720); $b.Dispose() }
    }
    $g.Dispose(); $all.Save($out); $all.Dispose()
}
