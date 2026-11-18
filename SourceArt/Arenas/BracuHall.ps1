# BRACU Hall: the big multipurpose hall — raw concrete ceiling with round ducts, giant diagonal concrete beams,
# warm wood-panelled walls and grey perforated block screens, an audience at round tables, tan lounge chairs,
# and the brick floor at the edge of the stage.
# Run:  powershell -File SourceArt\Arenas\BracuHall.ps1   (writes Assets\Game\Art\Arenas\BracuHall\*.png)
. "$PSScriptRoot\common.ps1"
$dst = "$PSScriptRoot\..\..\Assets\Game\Art\Arenas\BracuHall"

$concrete = Col '#A9A69F'; $concreteD = Col '#83807A'; $concreteS = Col '#5F5D58'
$wood = Col '#8E4A28'; $woodL = Col '#A65A32'; $woodD = Col '#6E3820'
$block = Col '#B8B4AA'; $blockD = Col '#86827A'; $carpet = Col '#8E8F92'
$brick = Col '#A35B40'; $mortar = Col '#7A4232'; $yellow = Col '#D8A63A'

# ---------------------------------------------------------------- sky: ceiling, walls, windows
$c = New-Canvas 31
$c.Rect(0, 0, 640, 304, $concreteD, 4)
# raw concrete ceiling with round duct vents
$c.Rect(0, 0, 640, 52, $concrete, 6); $c.Rect(0, 52, 640, 3, $concreteS, 0)
for ($x = 20; $x -lt 640; $x += 54) { $c.Poly(@(($x + 4), 30, ($x + 10), 30, ($x + 14), 34, ($x + 14), 40, ($x + 10), 44, ($x + 4), 44, $x, 40, $x, 34), $concreteS, 0); $c.Poly(@(($x + 5), 32, ($x + 9), 32, ($x + 12), 35, ($x + 12), 39, ($x + 9), 42, ($x + 5), 42, ($x + 2), 39, ($x + 2), 35), (Col '#C9C6BE'), 2); $c.Rect($x + 5, 35, 4, 4, (Col '#3E3C38'), 0) }
for ($x = 0; $x -lt 640; $x += 160) { $c.Rect($x, 52, 30, 26, $concrete, 5); $c.Rect($x, 76, 30, 2, $concreteS, 0) }   # ceiling beams
# tall windows on the left
$c.Rect(0, 78, 110, 190, (Col '#3A3E44'), 2)
for ($y = 80; $y -lt 266; $y += 24) { for ($x = 4; $x -lt 106; $x += 26) { $c.Rect($x, $y, 22, 20, (Col '#8FA3AE'), 4) } }
# wood-panelled walls (centre and right) with grey perforated block panels between
$c.Rect(110, 78, 530, 226, $wood, 4)
for ($x = 110; $x -lt 640; $x += 5) { $c.Rect($x, 78, 1, 226, $woodD, 0) }
for ($y = 130; $y -lt 304; $y += 52) { $c.Rect(110, $y, 530, 2, $woodL, 0) }
foreach ($bx in @(@(250, 90, 120, 150), @(500, 120, 140, 120))) {
    $c.Rect($bx[0], $bx[1], $bx[2], $bx[3], $blockD, 2)
    for ($y = $bx[1] + 2; $y -lt $bx[1] + $bx[3] - 4; $y += 7) { for ($x = $bx[0] + 2; $x -lt $bx[0] + $bx[2] - 4; $x += 7) { $c.Rect($x, $y, 5, 5, $block, 3) } }
}
# balcony slot with a glimpse of people upstairs
$c.Rect(500, 104, 140, 12, (Col '#2E2E30'), 2)
foreach ($x in 520, 548, 600) { $c.Rect($x, 106, 5, 10, (Col '#6E8EB0'), 2) }
$c.Rect(0, 268, 640, 36, $carpet, 4)
$c.Save("$dst\sky.png")

# ---------------------------------------------------------------- far: the giant diagonal concrete beams
$c = New-Canvas 32
foreach ($b in @(@(168, 62, 206, 62, 302, 268, 266, 268), @(430, 62, 468, 62, 336, 268, 300, 268), @(560, 62, 598, 62, 478, 268, 442, 268))) {
    $c.Poly($b, $concrete, 5)
    $c.Line($b[0], $b[1], $b[6], $b[7], (Col '#C9C6BE')); $c.Line($b[2], $b[3], $b[4], $b[5], $concreteS)
}
$c.Rect(0, 52, 640, 10, $concrete, 5); $c.Rect(0, 62, 640, 2, $concreteS, 0)
# people standing at the back by the windows
Person $c 30 268 40 (Col '#E6E2D8') (Col '#2A2A30') (Col '#A8714E') (Col '#1C1A1E')
Person $c 52 268 42 (Col '#4F6E8E') (Col '#2A2A30') (Col '#B07A55') (Col '#1C1A1E')
Person $c 84 268 38 (Col '#2B2B31') (Col '#2A2A30') (Col '#A06A48') (Col '#1C1A1E')
$c.Save("$dst\far.png")

# ---------------------------------------------------------------- near: audience at round tables, lounge chairs
$c = New-Canvas 33
$cols = '#E6E2D8','#2B2B31','#8DB8DA','#C04A6A','#4F6E8E','#F0ECE2','#6A8F5A','#2C2440','#D8B23A','#7A4A8E'
$i = 0
foreach ($t in @(@(140, 276), @(250, 270), @(360, 276), @(470, 270), @(580, 276))) {
    $tx = $t[0]; $ty = $t[1]
    # seated people around the table (behind it), then the table with a white cloth
    foreach ($o in -24, -10, 6, 20) { Person $c ($tx + $o) ($ty + 6) 30 (Col $cols[($i++) % $cols.Count]) (Col '#2A2A30') (Col '#A8714E') (Col '#1C1A1E') }
    $c.Poly(@(($tx - 30), ($ty - 2), ($tx + 30), ($ty - 2), ($tx + 34), ($ty + 4), ($tx + 30), ($ty + 14), ($tx - 30), ($ty + 14), ($tx - 34), ($ty + 4)), (Col '#EEEAE0'), 3)
    $c.Rect($tx - 30, ($ty + 14), 60, 2, (Col '#B9B4A8'), 0)
    $c.Rect($tx - 4, ($ty - 8), 8, 6, (Col '#4F7A3E'), 8); $c.Rect($tx - 2, ($ty - 2), 4, 2, (Col '#8E5A3A'), 0)   # plant
}
# tan lounge chairs in the front row
foreach ($x in 20, 70, 600) {
    $c.Box($x, 278, 34, 22, (Col '#B07A48'), (Col '#5E3A20'), 4); $c.Rect($x + 2, 284, 30, 2, (Col '#8E5A30'), 0)
    $c.Rect($x, 300, 2, 4, (Col '#5E3A20'), 0); $c.Rect($x + 32, 300, 2, 4, (Col '#5E3A20'), 0)
}
Person $c 80 288 40 (Col '#E6E2D8') (Col '#2A2A30') (Col '#A06A48') (Col '#9A9A9A')            # a guest in a lounge chair
$c.Save("$dst\near.png")

# ---------------------------------------------------------------- ground: brick floor with the yellow edge line
$c = New-Canvas 34
$c.Rect(0, 304, 640, 56, $brick, 6)
$c.Rect(0, 304, 640, 2, $yellow, 0)
$row = 0
for ($y = 306; $y -lt 360; $y += 6) {
    $c.Rect(0, $y, 640, 1, $mortar, 0)
    for ($x = (($row % 2) * 9); $x -lt 640; $x += 18) { $c.Rect($x, $y, 1, 6, $mortar, 0) }
    $row++
}
$c.Save("$dst\ground.png")
"BracuHall done"
