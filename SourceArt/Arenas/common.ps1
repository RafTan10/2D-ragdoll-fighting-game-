# Shared drawing pieces for the BRACU arenas (built on tools\PixelScene.ps1).
. "$PSScriptRoot\..\tools\PixelScene.ps1"

$Outline = Col '#1E1418'

# A small background person, feet at (x, feetY), h px tall. Colours: shirt, trousers, skin, hair.
function Person($c, [int]$x, [int]$feetY, [int]$h, $shirt, $pants, $skin, $hair, [switch]$Walking) {
    $headH = [Math]::Max(3, [int]($h * 0.15)); $bodyH = [Math]::Max(4, [int]($h * 0.36)); $legH = $h - $headH - $bodyH
    $w = [Math]::Max(3, [int]($h * 0.22)); $top = $feetY - $h
    $c.Rect($x - [int]($w / 2) + 1, $top, $w - 2, $headH, $skin, 3)               # head
    $c.Rect($x - [int]($w / 2) + 1, $top, $w - 2, [Math]::Max(1, [int]($headH / 3)), $hair, 2)   # hair
    $c.Rect($x - [int]($w / 2), $top + $headH, $w, $bodyH, $shirt, 4)            # torso
    $legW = [Math]::Max(1, [int]($w / 2) - 1)
    if ($Walking) {
        $c.Poly(@(($x - 1), ($top + $headH + $bodyH), ($x - 1 - $legW), ($top + $headH + $bodyH), ($x - $legW - 3), $feetY, ($x - 3), $feetY), $pants, 3)
        $c.Poly(@(($x), ($top + $headH + $bodyH), ($x + $legW), ($top + $headH + $bodyH), ($x + $legW + 2), $feetY, ($x + 2), $feetY), $pants, 3)
    } else {
        $c.Rect($x - [int]($w / 2), $top + $headH + $bodyH, $legW, $legH, $pants, 3)
        $c.Rect($x + [int]($w / 2) - $legW, $top + $headH + $bodyH, $legW, $legH, $pants, 3)
    }
}

# Steel slat railing: top rail + vertical slats every `gap` px (gaps stay transparent on overlay layers).
function Slats($c, [int]$x, [int]$y, [int]$w, [int]$h, $col, [int]$gap = 3) {
    $c.Rect($x, $y, $w, 2, $col, 0)
    for ($i = $x; $i -lt $x + $w; $i += $gap) { $c.Rect($i, $y, 1, $h, $col, 0) }
}

# Polished stone floor strip for the ground layer: surface edge at y 304, receding tiles, a dark grate band.
function TileFloor($c, $tile, $grout, $shine, [int]$grateY = 0) {
    $c.Rect(0, 304, 640, 56, $tile, 5)
    $c.Rect(0, 304, 640, 1, $shine, 0)
    foreach ($y in 312, 324, 340) { $c.Rect(0, $y, 640, 1, $grout, 0) }
    for ($k = -10; $k -le 10; $k++) {           # tile joints fanning out from a vanishing point above the floor
        $x0 = 320 + $k * 40; $x1 = 320 + $k * 64
        $c.Line($x0, 305, $x1, 359, $grout)
    }
    for ($x = 0; $x -lt 640; $x += 7) { $c.Rect($x, 330, 3, 1, $shine, 0) }   # soft reflections
    if ($grateY -gt 0) { $c.Rect(0, $grateY, 640, 3, (Col '#2A2A2C'), 2); for ($x = 0; $x -lt 640; $x += 3) { $c.Rect($x, $grateY + 1, 1, 1, (Col '#4A4A4E'), 0) } }
}
