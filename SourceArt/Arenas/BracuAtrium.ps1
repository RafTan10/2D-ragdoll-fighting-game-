# BRACU Atrium: the central atrium of BRAC University's campus — stacked concrete floors with steel slat railings,
# criss-crossing escalators under an arched opening, students on the far side of a polished stone floor.
# Run:  powershell -File SourceArt\Arenas\BracuAtrium.ps1   (writes Assets\Game\Art\Arenas\BracuAtrium\*.png)
. "$PSScriptRoot\common.ps1"
$dst = "$PSScriptRoot\..\..\Assets\Game\Art\Arenas\BracuAtrium"

$concrete = Col '#9C9890'; $concreteD = Col '#75726B'; $concreteS = Col '#55534E'; $ceiling = Col '#3E3D3A'
$slat = Col '#3B3D42'; $glass = Col '#6F8088'; $warm = Col '#D8B97E'; $wood = Col '#8C5B3A'; $floorFar = Col '#86847E'

# ---------------------------------------------------------------- sky: the far side of the atrium
$c = New-Canvas 11
$c.Gradient(0, 0, 640, 304, (Col '#D4D2C8'), (Col '#8E8B84'))
# roof structure with a bright skylight in the middle
$c.Rect(0, 0, 640, 14, $ceiling, 4)
$c.Rect(220, 0, 200, 14, (Col '#F2F0E6'), 3)
for ($x = 0; $x -lt 640; $x += 32) { $c.Rect($x, 0, 4, 24, $ceiling, 3) }
# stacked floors: alternating steel louvre screens and open bays with a few lit offices
$fi = 0
foreach ($y in 58, 118, 178, 238) {
    $c.Gradient(0, ($y - 44), 640, 44, (Col '#6F6C66'), (Col '#8A867E'))      # deep interior in shadow
    for ($x = 0; $x -lt 640; $x += 80) {
        $seg = (($x / 80) + $fi) % 4
        if ($seg -eq 0 -or $seg -eq 2) {                                       # louvre screen
            $c.Rect($x, $y - 44, 80, 44, (Col '#A9A59C'), 3)
            for ($i = $x; $i -lt $x + 80; $i += 2) { $c.Rect($i, $y - 44, 1, 44, (Col '#7D7A73'), 0) }
        } elseif ($seg -eq 1) {                                                # an office with warm light
            $c.Box($x + 18, $y - 34, 44, 24, $warm, $concreteS, 5)
            $c.Rect($x + 30, $y - 24, 3, 14, $concreteS, 0); $c.Rect($x + 44, $y - 22, 3, 12, $concreteS, 0)
        }
    }
    $c.Rect(0, $y, 640, 7, $concrete, 5)                                  # slab
    $c.Rect(0, $y + 7, 640, 2, $concreteS, 0)
    Slats $c 0 ($y - 12) 640 12 (Col '#55575C') 3
    $fi++
}
# ground-floor lobby behind the far floor: darker, glass doors
$c.Rect(0, 247, 640, 19, (Col '#5E5B55'), 4)
for ($x = 20; $x -lt 640; $x += 90) { $c.Box($x, 249, 36, 17, (Col '#7E939A'), (Col '#3A3C40'), 3); $c.Rect($x + 17, 249, 1, 17, (Col '#3A3C40'), 0) }
# floor of the far side
$c.Rect(0, 266, 640, 38, $floorFar, 4)
$c.Rect(0, 266, 640, 1, (Col '#A9A69E'), 0)
$c.Save("$dst\sky.png")

# ---------------------------------------------------------------- far: arch, columns and the escalators
$c = New-Canvas 12
# the big arch over the escalators
foreach ($x in 132, 470) { $c.Rect($x, 20, 20, 284, $concreteD, 4); $c.Rect($x, 20, 3, 284, $concrete, 3); $c.Rect($x + 17, 20, 3, 284, $concreteS, 0) }
# two escalators climbing to the right, side by side
foreach ($off in 0, 30) {
    $x0 = 200 + $off; $y0 = 300; $x1 = 360 + $off; $y1 = 110
    $c.Poly(@($x0, $y0, ($x0 + 22), $y0, ($x1 + 22), $y1, $x1, $y1), (Col '#4C4F55'), 3)               # side panel
    $c.Poly(@(($x0 + 3), ($y0 - 3), ($x0 + 19), ($y0 - 3), ($x1 + 19), ($y1 - 3), ($x1 + 3), ($y1 - 3)), (Col '#BDBCB6'), 4)   # steps
    for ($t = 0; $t -lt 1; $t += 0.05) {                                     # step lines
        $sx = [int]($x0 + 3 + ($x1 - $x0) * $t); $sy = [int]($y0 - 3 + ($y1 - $y0) * $t); $c.Rect($sx, $sy, 16, 1, (Col '#8E8D88'), 0)
    }
    $c.Line($x0, ($y0 - 12), $x1, ($y1 - 12), (Col '#151518')); $c.Line($x0, ($y0 - 11), $x1, ($y1 - 11), (Col '#151518'))   # handrails
    $c.Poly(@($x0, ($y0 - 10), ($x0 + 22), ($y0 - 10), ($x1 + 22), ($y1 - 10), $x1, ($y1 - 10), $x1, ($y1 - 3), $x0, ($y0 - 3)), (Col '#7E939A'), 0)
}
# riders on the escalators
$riders = @(@(0, 0.30, '#3E5C8A'), @(30, 0.55, '#E6E2D8'), @(0, 0.72, '#2B2B31'), @(30, 0.22, '#6A8F5A'))
foreach ($r in $riders) {
    $t = $r[1]; $x = [int](211 + $r[0] + 160 * $t); $y = [int](297 - 190 * $t)
    Person $c $x $y 26 (Col $r[2]) (Col '#24242B') (Col '#A8714E') (Col '#1C1A1E')
}
# reception counter at the foot of the escalators
$c.Box(60, 270, 64, 30, $wood, (Col '#4A2E1E'), 5); $c.Rect(60, 268, 64, 3, (Col '#C9B8A0'), 2)
$c.Save("$dst\far.png")

# ---------------------------------------------------------------- near: the close balconies on the right, people
$c = New-Canvas 13
foreach ($y in 74, 170) {
    $c.Rect(470, $y, 170, 9, $concrete, 5); $c.Rect(470, $y + 9, 170, 3, $concreteS, 0)
    Slats $c 470 ($y - 26) 170 26 $slat 2
}
$c.Rect(462, 0, 12, 304, $concreteD, 4); $c.Rect(462, 0, 2, 304, $concrete, 0)
# a planter on the near balcony
$c.Box(500, 154, 40, 16, (Col '#6E6A62'), $concreteS, 4)
foreach ($p in @(@(506, 146), @(516, 141), @(527, 145))) { $c.Rect($p[0], $p[1], 9, 10, (Col '#4F7A3E'), 10) }
# students on the far side of the floor
Person $c 36 286 58 (Col '#8DB8DA') (Col '#1F1F25') (Col '#A8714E') (Col '#1C1A1E') -Walking
Person $c 404 288 56 (Col '#2C8C86') (Col '#2C8C86') (Col '#B07A55') (Col '#1C1A1E') -Walking
Person $c 588 287 60 (Col '#F0ECE2') (Col '#1C1D2A') (Col '#A06A48') (Col '#1C1A1E') -Walking
Person $c 616 289 52 (Col '#3E5C8A') (Col '#2A2A30') (Col '#B07A55') (Col '#1C1A1E')
$c.Save("$dst\near.png")

# ---------------------------------------------------------------- ground: polished stone tiles and the drain grate
$c = New-Canvas 14
TileFloor $c (Col '#7E7D78') (Col '#5F5E5A') (Col '#A3A29C') 316
$c.Save("$dst\ground.png")
"BracuAtrium done"
