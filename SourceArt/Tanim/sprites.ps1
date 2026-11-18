# Tanim: drawn pixel by pixel from his photos (side view, facing right). Run:  powershell -File SourceArt\Tanim\sprites.ps1
# Big wavy black hair over the ears, red-tinted hexagonal sunglasses, short beard + moustache, shiny charcoal puffer
# jacket open over a navy T-shirt, dark jeans, white sneakers. The game's strongest fighter: a dark greatsword with a
# red edge (and a dagger in the off hand, from the existing weapons).
. "$PSScriptRoot\..\tools\PixelMap.ps1"
$art = "$PSScriptRoot\..\..\Assets\Game\Art"
$dst = "$art\Fighters\Tanim"

# outline | hair | skin | beard, lip | sunglasses frame, lens, lens shine | puffer light/base/seam | T-shirt | jeans |
# sneaker white/sole | watch
$pal = New-Palette ("a=#1E1418  H=#1B1A21~ h=#2E2B36~ g=#4D4858  s=#B5764F S=#88573C l=#CF9670 k=#663F2D " +
    "B=#2C201E~ m=#7E4236  F=#A7ADB8 f=#50545E r=#8A2238 L=#E35C7A  p=#7B808C P=#4D515B~ Q=#2E3038  n=#2B3350~ z=#9DA2AB " +
    "t=#2A2C36~ T=#1B1C24 y=#3E4150~  W=#ECEAE4 w=#B9B6AE  j=#D3D6DC")

Save-PixelMap $pal @(
    "5. 2a . 4a . 3a 8."
    "4. a hH a hgHH a Hh a 8."
    "3. a hHHH gHHHH hHH Hg a 5."
    "2. a hHHHH HgHHH HhHH HHg a 3."
    "1. a hHHHHH HHgHH HHhHH HHHg a 1."
    "1. a HhHHHH HHHhg HHHHH gHHH a 1."
    "a HhHHHH HHHHH hHHgH HgHHH a 1."
    "a HHhHHH HHHHH HhHH ss HHHg a 1."
    "a HhHHHHHHHHH lssss HhH s a 2."
    "a HHhHHHHHH Sssss FFFFF s a 2."
    "a HhHHHHHH ffffff F rrL F s s a 1."
    "a HHhHHHHH S sssss F rrr F ss a 1."
    "a HhHHHHHH S ssssss FFF s SS a 1."
    "a HHhHHHHH SS sssssssss S a 2."
    "a HhHHHHHH S ssssss B s BBB a 2."
    "a HHhHHHH a S ssss BBBB mm B a 2."
    "1. a HhHHH a S sss BBBBBBBBB a 2."
    "3. a HH a a S sss 8B a 3."
    "4. aa . a S 5s BB aaaa 4."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
) "$dst\head.png" 24

# Puffer jacket: quilt seams every 4 rows with a shiny band under each, open front over the navy T-shirt.
$shine = "2. a ppPPPP z nnnn z pPPPPQ a 2."
$base  = "2. a pPPPPP z nnnn z PPPPPQ a 2."
$seam  = "2. a QQQQQQ z nnnn z QQQQQQ a 2."
$jeans = "2. a yy 7t T 6t TT a 2."
Save-PixelMap $pal @(
    "8. a S ssss S a 8."
    "3. a pPPP a S ssss S a PPPQ a 3."
    "2. a pPPPP a nnnnnn a PPPPQ a 2."
    "2. a pPPPPP z nnnn z PPPPPQ a 2."
    $shine; $base; $base; $seam; $shine; $base; $base; $seam; $shine; $base; $base; $seam; $shine; $base
    $base; $seam; $shine; $base; $base; $seam; $shine; $base; $base; $seam; $shine; $base; $base; $seam
    $jeans; $jeans; $jeans
    "3. 18a 3."
) "$dst\torso.png" 24

# Puffy sleeves, full width.
$sShine = "a ppPPPPPQ a"; $sBase = "a pPPPPPPQ a"; $sSeam = "a QQQQQQQQ a"
Save-PixelMap $pal @(
    "1. 8a 1."
    $sShine; $sBase; $sBase; $sSeam; $sShine; $sBase; $sBase; $sSeam; $sShine; $sBase; $sBase; $sSeam
    $sShine; $sBase; $sBase; $sSeam; $sShine; $sBase; $sBase
) "$dst\upper_arm.png" 10

Save-PixelMap $pal @(
    $sShine; $sBase; $sBase; $sSeam; $sShine; $sBase; $sBase; $sSeam; $sShine; $sBase
    "a QQQQQQQQ a"; "a QQQQQQQQ a"
    "1. a jjjjjj a 1."
    "1. a lsssssS a"; "1. a lssssSS a"; "1. a lsSsSsS a"; "1. a lsSsSsS a"; "1. a lssssSS a"; "1. a SssssSS a"
    "2. 7a 1."
) "$dst\lower_arm.png" 10

$leg = "1. a yttttTTT a 1."; $legF = "1. a ytttTtTT a 1."
Save-PixelMap $pal @(
    $leg; $leg; $legF; $leg; $leg; $leg; $legF; $leg; $leg; $leg; $legF; $leg
    $leg; $leg; $legF; $leg; $leg; $leg; $legF; $leg; $leg; $leg; $legF; $leg
) "$dst\upper_leg.png" 12

Save-PixelMap $pal @(
    $leg; $leg; $legF; $leg; $leg; $leg; $legF; $leg; $leg; $leg; $legF; $leg
    $leg; $leg; $legF; $leg; $leg; $leg
    "1. a WWWWWWWW a 1."
    "1. a WWWWWWWW Wa"
    "1. a WWWWwWWWW a"
    "1. a wwwwwwwww a"
    "1. a WWWWWWWWW a"
    "2. 10a"
) "$dst\lower_leg.png" 12

# Greatsword, pointing up: dark steel blade with a bright left edge, a fuller and a red right edge; steel guard
# with a red gem; red and black wrapped grip; red pommel stone.
$sp = New-Palette "a=#1E1418 e=#DCE0E6 b=#3A3E48~ F=#23262D R=#C23A4E G=#8A8F99 g=#5A5F69 r=#D8304A K=#18151A D=#6E1E2A"
$blade = "4. a eFbR a 4."
$blades = @(); for ($i = 0; $i -lt 37; $i++) { $blades += $blade }
$grip = @(); for ($i = 0; $i -lt 6; $i++) { $grip += "5. a DK a 5."; $grip += "5. a KD a 5." }
Save-PixelMap $sp (@(
    "6. aa 6."
    "5. a eR a 5."
    "5. a eR a 5."
    "4. a ebR a 5."
    "4. a ebR a 5."
) + $blades + @(
    "1. 12a 1."
    "a g GGGG rr GGGG g a"
    "1. 12a 1."
) + $grip + @(
    "4. a gGGg a 4."
    "4. a grrg a 4."
    "4. a gGGg a 4."
    "5. 4a 5."
)) "$art\Weapons\greatsword.png" 14
