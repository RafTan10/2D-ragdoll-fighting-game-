# Builds docs/media/gameplay.gif (title card + captioned clips) and still images from frames recorded with
# FrameRecorder (Play mode, see CLAUDE.md "Recording"). Usage:
#   powershell -File SourceArt\tools\MakeGameplayGif.ps1 -Frames %TEMP%\stickman_cc\frames
param([string]$Frames = "$env:TEMP\stickman_cc\frames", [string]$Out = "$PSScriptRoot\..\..\docs\media")
. "$PSScriptRoot\GifWriter.ps1"
New-Item -ItemType Directory -Force $Out | Out-Null

$W = 480; $H = 270; $delay = 7   # ~14 fps
$clips = @(
    # still = which of the clip's 60 frames becomes the README still (pick one with both heads in view)
    @{ dir = "clip1"; caption = "TANIM  vs  TAHSEEN"; place = "BRACU ATRIUM"; still = 26 }
    @{ dir = "clip2"; caption = "AKIB  vs  KNIGHT"; place = "BRACU SPORTS HALL"; still = 20 }
    @{ dir = "clip3"; caption = "NUHA  vs  BARBARIAN"; place = "BRACU HALL"; still = 26 }
)
$white = [System.Drawing.Brushes]::White
$gold = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 232, 186, 74))
$shadow = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 20, 16, 20))
$bar = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 24, 20, 24))
$big = New-Object System.Drawing.Font("Consolas", 30, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$mid = New-Object System.Drawing.Font("Consolas", 14, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$small = New-Object System.Drawing.Font("Consolas", 11, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)

function Crisp($g) { $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit }
function Shadowed($g, [string]$s, $font, $brush, [float]$x, [float]$y) { $g.DrawString($s, $font, $shadow, $x + 1, $y + 1); $g.DrawString($s, $font, $brush, $x, $y) }

$gif = New-Object GifWriter ((Join-Path $Out "gameplay.gif")), $W, $H
$gif.MaxColours = 96

# Title card over a dimmed frame of the first clip
$first = [System.Drawing.Bitmap]::FromFile((Join-Path $Frames "clip1\0040.png"))
$card = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($card); Crisp $g
$g.DrawImage($first, 0, 0, $W, $H); $first.Dispose()
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(185, 16, 12, 16))), 0, 0, $W, $H)
$t = "RAGDOLL DUEL"; $sz = $g.MeasureString($t, $big); Shadowed $g $t $big $gold (($W - $sz.Width) / 2) 92
$t = "physics duels starring real people"; $sz = $g.MeasureString($t, $mid); Shadowed $g $t $mid $white (($W - $sz.Width) / 2) 136
$t = "made in Unity  -  every hit is real physics"; $sz = $g.MeasureString($t, $small); Shadowed $g $t $small $white (($W - $sz.Width) / 2) 160
$g.Dispose()
for ($i = 0; $i -lt 22; $i++) { $gif.AddFrame($card, $delay) }
$card.Dispose()

foreach ($clip in $clips) {
    $files = Get-ChildItem (Join-Path $Frames $clip.dir) -Filter *.png | Sort-Object Name | Select-Object -Skip 8 -First 60
    $n = 0
    foreach ($file in $files) {
        $b = [System.Drawing.Bitmap]::FromFile($file.FullName)
        $f = New-Object System.Drawing.Bitmap $W, $H
        $g = [System.Drawing.Graphics]::FromImage($f); Crisp $g
        $g.DrawImage($b, 0, 0, $W, $H); $b.Dispose()
        $g.FillRectangle($bar, 0, 0, $W, 20)
        Shadowed $g $clip.caption $mid $white 8 2
        $sz = $g.MeasureString($clip.place, $small); Shadowed $g $clip.place $small $gold ($W - $sz.Width - 8) 4
        $g.Dispose()
        $gif.AddFrame($f, $delay)
        if ($n -eq $clip.still) { $f.Save((Join-Path $Out ($clip.dir + ".png"))) }   # a still for the README
        $f.Dispose(); $n++
    }
}
$gif.Close()
"{0:N1} MB" -f ((Get-Item (Join-Path $Out "gameplay.gif")).Length / 1MB)
