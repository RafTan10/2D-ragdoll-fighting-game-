# Converts the Nuha ragdoll pack (front view, 2x PNGs, rig units) into the game's fighter parts (64 PPU pixel sprites).
param([string]$Src, [string]$Dst, [double]$S = 0.1175)  # $S = game pixels per source PNG pixel
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Dst | Out-Null

function Load($n) { [System.Drawing.Bitmap]::FromFile((Join-Path $Src "$n.png")) }

# Rows r0..r1 of a bitmap (full width), padded to the given canvas if needed.
function Crop($bmp, [int]$r0, [int]$r1) {
    $h = $r1 - $r0
    $out = New-Object System.Drawing.Bitmap $bmp.Width, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($out)
    $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle 0, 0, $bmp.Width, $h), (New-Object System.Drawing.Rectangle 0, $r0, $bmp.Width, $h), [System.Drawing.GraphicsUnit]::Pixel)
    $g.Dispose(); $out
}

# Downscale smoothly, then make alpha hard (pixel-art edges) and save.
function Save($bmp, [string]$name) {
    $w = [Math]::Max(1, [int][Math]::Round($bmp.Width * $S)); $h = [Math]::Max(1, [int][Math]::Round($bmp.Height * $S))
    $out = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($out)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $attr = New-Object System.Drawing.Imaging.ImageAttributes; $attr.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)
    $g.DrawImage($bmp, (New-Object System.Drawing.Rectangle 0, 0, $w, $h), 0, 0, $bmp.Width, $bmp.Height, [System.Drawing.GraphicsUnit]::Pixel, $attr)
    $g.Dispose()
    for ($y = 0; $y -lt $h; $y++) { for ($x = 0; $x -lt $w; $x++) {
        $c = $out.GetPixel($x, $y)
        if ($c.A -lt 110) { $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0)) }
        else { $out.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $c.R, $c.G, $c.B)) }
    } }
    $path = Join-Path $Dst "$name.png"; $out.Save($path, [System.Drawing.Imaging.ImageFormat]::Png); $out.Dispose()
    "$name ${w}x$h"
}

# Head: the hair above the neck drawn behind the face; bottom edge = the neck (rig y 0 = hair row 272).
$hair = Load "hair_back"; $head = Load "head"
$canvas = New-Object System.Drawing.Bitmap 232, 272, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.DrawImage($hair, (New-Object System.Drawing.Rectangle 0, 0, 232, 272), (New-Object System.Drawing.Rectangle 0, 0, 232, 272), [System.Drawing.GraphicsUnit]::Pixel)
# head.png: neck pivot (92,224) → its top-left sits at canvas (116-92, 272-224).
$g.DrawImage($head, 24, 48, 184, 208); $g.Dispose()
Save $canvas "head"

# Long hair below the neck → cape slot (hangs from 3 game px below the neck and swings).
Save (Crop $hair 298 500) "cape"

# Torso: neck (row 40) - 3 px to hip (row 296) + 3 px.  The dress below the hip → skirt (drawn over the legs).
$torso = Load "torso"
Save (Crop $torso 14 322) "torso"
Save (Crop $torso 296 396) "skirt"

# Limbs: from the joint row down (joint at the top edge).
Save (Crop (Load "upper_arm_R") 36 184) "upper_arm"
Save (Crop (Load "forearm_R") 24 192) "lower_arm"
Save (Crop (Load "thigh_R") 30 236) "upper_leg"
Save (Crop (Load "shin_R") 26 254) "lower_leg"

# The bag hangs from the off hand by its handles (pivot top-centre).
Save (Load "bag") "hand_prop"
