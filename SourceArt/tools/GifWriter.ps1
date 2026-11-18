# Animated GIF encoder (no external tools): each frame stores only the box of pixels that changed since the last one
# (unchanged pixels transparent), with its own palette (exact when <= 255 colours, else the most common + nearest),
# LZW-compressed, looping forever.
#   . "$PSScriptRoot\GifWriter.ps1"
#   $gif = New-Object GifWriter "docs\media\gameplay.gif", 480, 270
#   $gif.AddFrame($bitmap, 7)      # delay in 1/100 s
#   $gif.Close()

Add-Type -AssemblyName System.Drawing
if (-not ("GifWriter" -as [type])) {
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

public class GifWriter
{
    readonly FileStream fs;
    readonly int W, H;

    public GifWriter(string path, int width, int height)
    {
        W = width; H = height;
        fs = new FileStream(path, FileMode.Create);
        Bytes("GIF89a"); Short(W); Short(H);
        fs.WriteByte(0x00); fs.WriteByte(0); fs.WriteByte(0);            // no global colour table
        fs.Write(new byte[] { 0x21, 0xFF, 0x0B }, 0, 3); Bytes("NETSCAPE2.0");
        fs.Write(new byte[] { 3, 1, 0, 0, 0 }, 0, 5);                     // loop forever
    }

    /// <summary>Colours per frame (2..255). Fewer = smaller files; pixel art survives ~96 well.</summary>
    public int MaxColours = 255;

    int[] prev;   // the previous frame's pixels: unchanged pixels are sent as transparent, and only the changed box

    public void AddFrame(Bitmap src, int delayCs)
    {
        var data = src.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        var px = new int[W * H];
        Marshal.Copy(data.Scan0, px, 0, px.Length);
        src.UnlockBits(data);
        for (int i = 0; i < px.Length; i++) px[i] &= 0xFFFFFF;

        // box around the pixels that changed since the last frame
        int x0 = 0, y0 = 0, x1 = W - 1, y1 = H - 1;
        if (prev != null)
        {
            x0 = W; y0 = H; x1 = -1; y1 = -1;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (px[y * W + x] != prev[y * W + x]) { if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y; }
            if (x1 < 0) { x0 = 0; y0 = 0; x1 = 0; y1 = 0; }   // nothing changed: a 1-pixel frame
        }
        int bw = x1 - x0 + 1, bh = y1 - y0 + 1;
        bool diff = prev != null;
        const int Transparent = 255;

        // palette (255 colours + the transparent index): exact if possible, else the most common
        var counts = new Dictionary<int, int>();
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x < x0 + bw; x++)
            {
                int i = y * W + x;
                if (diff && px[i] == prev[i]) continue;
                int n; counts.TryGetValue(px[i], out n); counts[px[i]] = n + 1;
            }
        var colours = new List<KeyValuePair<int, int>>(counts);
        colours.Sort((a, b) => b.Value.CompareTo(a.Value));
        int size = Math.Min(Math.Max(2, Math.Min(255, MaxColours)), colours.Count);
        var pal = new int[256];
        var index = new Dictionary<int, byte>();
        for (int i = 0; i < size; i++) { pal[i] = colours[i].Key; index[colours[i].Key] = (byte)i; }
        var indices = new byte[bw * bh];
        for (int y = 0; y < bh; y++)
            for (int x = 0; x < bw; x++)
            {
                int i = (y0 + y) * W + x0 + x;
                if (diff && px[i] == prev[i]) { indices[y * bw + x] = Transparent; continue; }
                byte k;
                if (!index.TryGetValue(px[i], out k)) { k = Nearest(pal, Math.Max(1, size), px[i]); index[px[i]] = k; }
                indices[y * bw + x] = k;
            }
        prev = px;

        // graphic control: keep the previous frame underneath (disposal 1), index 255 transparent
        fs.Write(new byte[] { 0x21, 0xF9, 4, (byte)(diff ? 0x05 : 0x04) }, 0, 4); Short(delayCs);
        fs.WriteByte(Transparent); fs.WriteByte(0);
        fs.WriteByte(0x2C); Short(x0); Short(y0); Short(bw); Short(bh);
        fs.WriteByte(0x87);                                                  // local colour table, 256 entries
        for (int i = 0; i < 256; i++) { fs.WriteByte((byte)(pal[i] >> 16)); fs.WriteByte((byte)(pal[i] >> 8)); fs.WriteByte((byte)pal[i]); }
        Lzw(indices);
    }
    static byte Nearest(int[] pal, int size, int c)
    {
        int r = (c >> 16) & 255, g = (c >> 8) & 255, b = c & 255, best = 0, bestD = int.MaxValue;
        for (int i = 0; i < size; i++)
        {
            int dr = ((pal[i] >> 16) & 255) - r, dg = ((pal[i] >> 8) & 255) - g, db = (pal[i] & 255) - b;
            int d = dr * dr * 3 + dg * dg * 4 + db * db * 2;
            if (d < bestD) { bestD = d; best = i; }
        }
        return (byte)best;
    }

    // ---- LZW (min code size 8), written in 255-byte sub-blocks. Code-size switching follows the reference encoder:
    // after writing a code, widen when the next free code no longer fits.
    List<byte> block; int bitBuf, bitCount, codeSize, next, maxCode;

    void Out(int code)
    {
        Emit(code, codeSize);
        if (next > maxCode && codeSize < 12) { codeSize++; maxCode = codeSize == 12 ? 4096 : (1 << codeSize) - 1; }
    }

    void Lzw(byte[] data)
    {
        fs.WriteByte(8);
        block = new List<byte>(); bitBuf = 0; bitCount = 0;
        const int clear = 256, end = 257;
        var dict = new Dictionary<int, int>();
        codeSize = 9; next = 258; maxCode = 511;
        Emit(clear, codeSize);
        int prefix = data[0];
        for (int i = 1; i < data.Length; i++)
        {
            int key = (prefix << 8) | data[i], code;
            if (dict.TryGetValue(key, out code)) { prefix = code; continue; }
            Out(prefix);
            if (next < 4096) dict[key] = next++;
            else { Emit(clear, codeSize); dict.Clear(); codeSize = 9; next = 258; maxCode = 511; }
            prefix = data[i];
        }
        Out(prefix);
        Out(end);
        if (bitCount > 0) Push((byte)bitBuf);
        if (block.Count > 0) { fs.WriteByte((byte)block.Count); fs.Write(block.ToArray(), 0, block.Count); }
        fs.WriteByte(0);
    }

    void Emit(int code, int size)
    {
        bitBuf |= code << bitCount; bitCount += size;
        while (bitCount >= 8) { Push((byte)(bitBuf & 255)); bitBuf >>= 8; bitCount -= 8; }
    }

    void Push(byte b)
    {
        block.Add(b);
        if (block.Count == 255) { fs.WriteByte(255); fs.Write(block.ToArray(), 0, 255); block.Clear(); }
    }

    void Short(int v) { fs.WriteByte((byte)(v & 255)); fs.WriteByte((byte)(v >> 8)); }
    void Bytes(string s) { foreach (char ch in s) fs.WriteByte((byte)ch); }

    public void Close() { fs.WriteByte(0x3B); fs.Close(); }
}
'@
}
