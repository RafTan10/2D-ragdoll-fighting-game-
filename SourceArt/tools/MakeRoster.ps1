# Builds docs/media/roster.png (every fighter's head, enlarged, with name and weapon) and docs/media/weapons.png.
#   powershell -File SourceArt\tools\MakeRoster.ps1
param([string]$Out = "$PSScriptRoot\..\..\docs\media")
Add-Type -AssemblyName System.Drawing
$art = "$PSScriptRoot\..\..\Assets\Game\Art"
New-Item -ItemType Directory -Force $Out | Out-Null

$cast = @(
    @("Knight", "sword + shield"), @("Barbarian", "axe + mace"), @("Ronin", "sword + dagger"), @("Nuha", "sword + handbag"),
    @("Tahseen", "acoustic guitar"), @("Akib", "calculator + ledger"), @("Tanim", "greatsword + dagger"),
    @("Nawfel", "hammer"), @("Alif", "keyboard + laptop"), @("Samshad", "sword + shield"), @("Ishtiaq", "two dumbbells"),
    @("Hamza", "sword + dagger"), @("Tamim", "axe + dagger"), @("Alavi", "bamboo stick + knife"), @("Tahmeed", "mace + shield"),
    @("Nameer", "two daggers")
)
$perRow = 6; $rowH = 200
$k = 5; $cell = 150; $W = $cell * $perRow; $H = $rowH * [Math]::Ceiling($cast.Count / $perRow)
$bmp = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(255, 74, 70, 84))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::SingleBitPerPixelGridFit
$name = New-Object System.Drawing.Font("Consolas", 16, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$sub = New-Object System.Drawing.Font("Consolas", 11, [System.Drawing.FontStyle]::Regular, [System.Drawing.GraphicsUnit]::Pixel)
$gold = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 232, 186, 74))
$grey = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 190, 186, 180))
$i = 0
foreach ($c in $cast) {
    $x = ($i % $perRow) * $cell; $y = [Math]::Floor($i / $perRow) * $rowH
    $head = [System.Drawing.Bitmap]::FromFile("$art\Fighters\$($c[0])\head.png")
    $w = $head.Width * $k; $h = $head.Height * $k
    $g.DrawImage($head, $x + ($cell - $w) / 2, $y + 140 - $h, $w, $h); $head.Dispose()
    $s = $g.MeasureString($c[0], $name); $g.DrawString($c[0], $name, $gold, $x + ($cell - $s.Width) / 2, $y + 150)
    $s = $g.MeasureString($c[1], $sub); $g.DrawString($c[1], $sub, $grey, $x + ($cell - $s.Width) / 2, $y + 172)
    $i++
}
$g.Dispose(); $bmp.Save("$Out\roster.png"); $bmp.Dispose()

# The new weapons, enlarged, lying side by side (rotated to horizontal).
$items = "greatsword", "guitar", "calculator", "ledger"
$k = 4
$imgs = @($items | ForEach-Object { $b = [System.Drawing.Bitmap]::FromFile("$art\Weapons\$_.png"); $b.RotateFlip([System.Drawing.RotateFlipType]::Rotate90FlipNone); $b })
$W = 40; foreach ($b in $imgs) { $W += $b.Width * $k + 40 }
$bmp = New-Object System.Drawing.Bitmap $W, 160
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(255, 74, 70, 84))
$g.InterpolationMode = 'NearestNeighbor'; $g.PixelOffsetMode = 'Half'
$x = 40
foreach ($b in $imgs) { $g.DrawImage($b, $x, (160 - $b.Height * $k) / 2, $b.Width * $k, $b.Height * $k); $x += $b.Width * $k + 40; $b.Dispose() }
$g.Dispose(); $bmp.Save("$Out\weapons.png"); $bmp.Dispose()
"roster + weapons written"
