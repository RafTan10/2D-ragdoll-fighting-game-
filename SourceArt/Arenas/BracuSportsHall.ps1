# BRACU Sports Hall: the indoor hall during an inter-university cricket match — green court, a red pitch mat
# (the fighters duel on it), dark stage curtains with a glowing screen, a perforated concrete wall and a crowd.
# No sponsor names or logos: the screen just says CRICKET.
# Run:  powershell -File SourceArt\Arenas\BracuSportsHall.ps1   (writes Assets\Game\Art\Arenas\BracuSportsHall\*.png)
. "$PSScriptRoot\common.ps1"
$dst = "$PSScriptRoot\..\..\Assets\Game\Art\Arenas\BracuSportsHall"

$court = Col '#2E8A6A'; $courtD = Col '#24735A'; $line = Col '#E8E8E0'; $yellow = Col '#D8B23A'
$mat = Col '#C8322E'; $matD = Col '#A0261F'; $jali = Col '#C9C4BA'; $jaliD = Col '#8E897F'

# 5x7 pixel font for the screen
$font = @{
    'C' = @('01110','10001','10000','10000','10000','10001','01110'); 'R' = @('11110','10001','10001','11110','10100','10010','10001')
    'I' = @('01110','00100','00100','00100','00100','00100','01110'); 'K' = @('10001','10010','10100','11000','10100','10010','10001')
    'E' = @('11111','10000','10000','11110','10000','10000','11111'); 'T' = @('11111','00100','00100','00100','00100','00100','00100')
}
function Text($c, [string]$s, [int]$x, [int]$y, $col, [int]$k = 2) {
    foreach ($ch in $s.ToCharArray()) {
        $g = $font[[string]$ch]
        for ($r = 0; $r -lt 7; $r++) { for ($q = 0; $q -lt 5; $q++) { if ($g[$r][$q] -eq '1') { $c.Rect($x + $q * $k, $y + $r * $k, $k, $k, $col, 0) } } }
        $x += 6 * $k
    }
}

# ---------------------------------------------------------------- sky: dark stage, curtains, screen, jali wall
$c = New-Canvas 21
$c.Gradient(0, 0, 640, 304, (Col '#2B2626'), (Col '#3C3632'))
for ($x = 0; $x -lt 470; $x += 6) { $c.Rect($x, 0, 3, 70, (Col '#221E1E'), 2); $c.Rect($x + 3, 0, 1, 70, (Col '#3A3434'), 0) }   # curtain folds
for ($x = 6; $x -lt 470; $x += 22) { $c.Rect($x, 4, 1, 1, (Col '#6AB0E8'), 0) }                                           # stage lights
# perforated concrete (jali) wall on the right
$c.Rect(470, 0, 170, 270, $jaliD, 3)
for ($y = 4; $y -lt 266; $y += 9) { for ($x = 474; $x -lt 640; $x += 9) { $c.Rect($x, $y, 6, 6, $jali, 3); $c.Rect($x + 2, $y + 2, 2, 3, (Col '#5E5952'), 0) } }
# the big screen, glowing blue-purple, with a trophy
$c.Rect(140, 92, 250, 120, (Col '#141218'), 0)
$c.Gradient(144, 96, 242, 112, (Col '#3A2E9A'), (Col '#7A2E8E'))
for ($k = 0; $k -lt 10; $k++) { $c.Line(150 + $k * 24, 205, 260, 120, (Col '#8E7AE0')) }                                   # light rays
$c.Rect(252, 122, 18, 16, (Col '#E3B54A'), 4); $c.Rect(258, 138, 6, 10, (Col '#C8962E'), 0); $c.Rect(252, 148, 18, 4, (Col '#C8962E'), 0)   # trophy
Text $c "CRICKET" 196 160 (Col '#F4F2EC') 3
# side posters
foreach ($x in 92, 396) { $c.Box($x, 102, 42, 96, (Col '#2C2440'), (Col '#141218'), 3); $c.Rect($x + 14, 130, 14, 24, (Col '#C8962E'), 4) }
$c.Rect(0, 262, 640, 42, (Col '#2E2A2A'), 3)
$c.Save("$dst\sky.png")

# ---------------------------------------------------------------- far: perimeter banner and the crowd
$c = New-Canvas 22
# crowd behind the barrier on the right, standing
$cols = '#E6E2D8','#2B2B31','#8DB8DA','#C8322E','#4F6E8E','#F0ECE2','#6A8F5A','#2C2440','#D8B23A'
$i = 0
for ($x = 476; $x -lt 640; $x += 9) {
    $h = 34 + (($i * 7) % 9)
    Person $c $x 262 $h (Col $cols[$i % $cols.Count]) (Col '#22222A') (Col '#A8714E') (Col '#1C1A1E')
    $i++
}
for ($x = 482; $x -lt 640; $x += 13) { Person $c $x 266 30 (Col $cols[($i++) % $cols.Count]) (Col '#22222A') (Col '#B07A55') (Col '#1C1A1E') }
# low perimeter banner with plain stripes
$c.Rect(0, 252, 640, 16, (Col '#3E2A5E'), 3)
for ($x = 0; $x -lt 640; $x += 80) { $c.Rect($x + 6, 256, 40, 2, (Col '#E8E8E0'), 0); $c.Rect($x + 6, 261, 26, 2, (Col '#C8322E'), 0) }
$c.Rect(0, 268, 640, 3, (Col '#C8322E'), 2); $c.Rect(0, 271, 640, 2, (Col '#E8E8E0'), 0)
$c.Save("$dst\far.png")

# ---------------------------------------------------------------- near: far court, distant players, stumps
$c = New-Canvas 23
$c.Rect(0, 273, 640, 31, $courtD, 4)
$c.Rect(0, 286, 640, 1, $line, 0); $c.Rect(0, 296, 640, 1, $yellow, 0)
foreach ($x in 60, 250, 430) { $c.Rect($x, 273, 1, 31, $line, 0) }
# the far half of the pitch mat, so the fighters stand on red
$c.Rect(0, 293, 640, 11, $mat, 4); $c.Rect(0, 293, 640, 1, (Col '#E05550'), 0)
foreach ($x in 64, 576) { $c.Rect($x, 293, 2, 11, $line, 0) }
Person $c 214 290 44 (Col '#E8742A') (Col '#1C1D24') (Col '#A8714E') (Col '#1C1A1E')       # fielder, orange jersey
Person $c 338 288 42 (Col '#E46AA8') (Col '#1C1D24') (Col '#B07A55') (Col '#1C1A1E')       # batter, pink jersey
$c.Rect(346, 270, 2, 14, (Col '#D8C8A0'), 0)                                                   # bat
Person $c 600 292 52 (Col '#1C1D24') (Col '#1C1D24') (Col '#8E5A3E') (Col '#141414')       # umpire in black
# stumps at both ends of the pitch
foreach ($x in 40, 590) {
    foreach ($o in 0, 4, 8) { $c.Rect($x + $o, 276, 2, 28, (Col '#E8E4DA'), 0); $c.Rect($x + $o, 284, 2, 3, (Col '#3E2A5E'), 0) }
    $c.Rect($x, 275, 10, 1, (Col '#C8962E'), 0)
}
$c.Save("$dst\near.png")

# ---------------------------------------------------------------- ground: the red pitch mat on the green court
$c = New-Canvas 24
$c.Rect(0, 304, 640, 56, $court, 4)
$c.Rect(0, 304, 640, 20, $mat, 4); $c.Rect(0, 304, 640, 1, (Col '#E05550'), 0); $c.Rect(0, 323, 640, 1, $matD, 0)
foreach ($x in 64, 576) { $c.Rect($x, 304, 2, 20, $line, 0) }                 # creases
$c.Rect(0, 324, 640, 2, $line, 0)
$c.Rect(0, 342, 640, 1, $yellow, 0); $c.Rect(300, 326, 1, 34, $line, 0)
$c.Save("$dst\ground.png")
"BracuSportsHall done"
