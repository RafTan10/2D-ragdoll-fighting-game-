# Draws pixel-art sprites from text: one string per row, one letter per pixel, looked up in a palette.
# Dot-source it from a character's sprite script:   . "$PSScriptRoot\..\tools\PixelMap.ps1"
#
#   $pal = New-Palette "a=#1E1418 s=#B97A58 H=#1B1A21~"       # case-sensitive; '~' = add a little texture noise
#   Save-PixelMap $pal @(
#       "7. 2a . 3a . 2a 8."      # tokens: "<count><letter>" repeats, anything else is literal letters
#       "6. a hH a gh a Hh a 8."  # '.' = transparent; spaces only separate tokens
#   ) "Assets\Game\Art\Fighters\X\head.png" 24
#
# Every row must come out exactly <width> pixels wide; a wrong row stops the script and says which one.

Add-Type -AssemblyName System.Drawing

# Palette from "letter=#rrggbb[~] ..." (PowerShell hashtables ignore case, so h and H would collide).
function New-Palette([string]$spec) {
    $d = New-Object 'System.Collections.Generic.Dictionary[string,string]' ([StringComparer]::Ordinal)
    foreach ($pair in ($spec -split '\s+' | Where-Object { $_ -ne '' })) {
        $k, $v = $pair -split '=', 2
        $d[$k] = $v
    }
    $d
}

function Expand-PixelRow([string]$row) {
    $out = New-Object System.Text.StringBuilder
    foreach ($tok in ($row -split '\s+' | Where-Object { $_ -ne '' })) {
        if ($tok -match '^(\d+)(.)$') { [void]$out.Append([string]$Matches[2] * [int]$Matches[1]) }
        else { [void]$out.Append($tok) }
    }
    $out.ToString()
}

function Save-PixelMap($palette, [string[]]$rows, [string]$path, [int]$width, [int]$seed = 7, [int]$noise = 9) {
    $lines = @($rows | ForEach-Object { Expand-PixelRow $_ })
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Length -ne $width) { throw "$([IO.Path]::GetFileName($path)) row $i is $($lines[$i].Length) px, expected ${width}: '$($rows[$i])'" }
    }
    $rand = New-Object System.Random $seed
    $bmp = New-Object System.Drawing.Bitmap $width, $lines.Count, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt $lines.Count; $y++) {
        for ($x = 0; $x -lt $width; $x++) {
            $ch = [string]$lines[$y][$x]
            if ($ch -eq '.') { $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0)); continue }
            $hex = $null
            if (-not $palette.TryGetValue($ch, [ref]$hex)) { throw "$([IO.Path]::GetFileName($path)) row $y uses '$ch', which isn't in the palette" }
            $textured = $hex.EndsWith('~')
            $c = [System.Drawing.ColorTranslator]::FromHtml($hex.TrimEnd('~'))
            $d = if ($textured) { $rand.Next(-$noise, $noise + 1) } else { 0 }
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, [Math]::Max(0, [Math]::Min(255, $c.R + $d)),
                [Math]::Max(0, [Math]::Min(255, $c.G + $d)), [Math]::Max(0, [Math]::Min(255, $c.B + $d))))
        }
    }
    New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    "{0} {1}x{2}" -f [IO.Path]::GetFileNameWithoutExtension($path), $width, $lines.Count
    $bmp.Dispose()
}

# Enlarged contact sheet of PNGs side by side, for checking art (nearest-neighbour, k x).
function Save-ContactSheet([string[]]$pngs, [string]$out, [int]$k = 10, [int]$cell = 30) {
    $imgs = @($pngs | ForEach-Object { [System.Drawing.Bitmap]::FromFile($_) })
    $h = ($imgs | Measure-Object -Property Height -Maximum).Maximum
    $sheet = New-Object System.Drawing.Bitmap ($imgs.Count * $cell * $k), (($h + 2) * $k)
    $g = [System.Drawing.Graphics]::FromImage($sheet)
    $g.Clear([System.Drawing.Color]::FromArgb(255, 90, 110, 80))
    $g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
    $x = 0
    foreach ($b in $imgs) { $g.DrawImage($b, $x, $k, $b.Width * $k, $b.Height * $k); $x += $cell * $k; $b.Dispose() }
    $g.Dispose(); $sheet.Save($out); $sheet.Dispose()
}
