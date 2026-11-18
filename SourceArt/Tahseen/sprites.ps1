# Tahseen: drawn pixel by pixel from his photos (side view, facing right). Run:  powershell -File SourceArt\Tahseen\sprites.ps1
# Wavy black hair swept forward with curls over the forehead, light stubble, black shirt (sleeves rolled), silver buckle,
# wristwatch. Weapon: an acoustic guitar, held by the neck.
. "$PSScriptRoot\..\tools\PixelMap.ps1"
$art = "$PSScriptRoot\..\..\Assets\Game\Art"
$dst = "$art\Fighters\Tahseen"

# outline | hair dark/mid/highlight | skin base/shadow/light/deep | eye white, brow, stubble, lip |
# shirt base/shadow/light/button | trousers | belt, buckle | shoe | watch strap/face
$pal = New-Palette ("a=#1E1418  H=#1B1A21~ h=#2E2B36~ g=#4D4858  s=#B97A58 S=#8E5A40 l=#D39A74 k=#6E4332 " +
    "w=#EEE6DA b=#241A1D B=#5A3E34~ m=#84473B  c=#32323A~ C=#202027~ x=#5A5A66~ n=#7A7A86 " +
    "t=#26262E~ T=#18181D y=#474755~  q=#141416 v=#D3D6DC V=#8D9199  o=#1A1414 O=#3D3434  J=#3D3F46 j=#D3D6DC")

Save-PixelMap $pal @(
    "7. 2a . 3a . 2a 8."
    "6. a hH a gh a Hh a 8."
    "5. a hHH gHH hHH Hg a 6."
    "4. a hHHH gHHH hHHH gH a 4."
    "3. a hHHHH hgHHH HhHHH gH a 2."
    "2. a hHHHHH HhgHH HHHgh HHH a 1."
    "2. a HhHHHH HHhHH gHHHH HgH a 1."
    "2. a HHhHHH HHHH lls HHgH s a 2."
    "2. a HHhHH HHHH llsss Hh ls a 2."
    "2. a HHhHH HH SSs lss bbb ls a 2."
    "2. a HhHHH a kS sslss w a ssS a 2."
    "2. a HHhHH a kS sssssssss s s a 1."
    "2. a HhHHH a Sk ssssssss sS S a 1."
    "2. a HHhHH a SSssssss sss S a 2."
    "2. a HhHHH S ssssss B ss BBB a 2."
    "2. a HhHH a S ssss BBBBB mm B a 2."
    "3. a HhH a S sss BBBBBBBBB a 2."
    "7. a S sss BBBB BBBB a 3."
    "7. a S ssss sBB aaaa 4."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
) "$dst\head.png" 24

$shirt  = "2. a xx cccccc C c cccccc CC a 2."
$button = "2. a xx cccccc C n cccccc CC a 2."
$fold   = "2. a xx ccCccc C c cccCcc CC a 2."
$pants  = "2. a yy ttttttt T tttttt TT a 2."
Save-PixelMap $pal @(
    "8. a S sss s S a 8."
    "4. aaaa C S ssss S C aaaa 4."
    "3. a xxcc c C ssss C c ccCC a 3."
    "2. a xxccc cc C ss C cc cccCC a 2."
    "2. a xxccc ccc CC ccc ccCCC a 2."
    $shirt; $button; $shirt; $fold; $shirt; $button; $shirt; $shirt; $fold; $button; $shirt; $shirt
    $fold; $button; $shirt; $shirt; $fold; $button; $shirt; $shirt; $shirt
    "2. a qqqqqqq vVv qqqqqqqq a 2."
    "2. a qqqqqqq vqv qqqqqqqq a 2."
    "2. a qqqqqqq VvV qqqqqqqq a 2."
    $pants; $pants; $pants; $pants; $pants; $pants
    "3. 18a 3."
) "$dst\torso.png" 24

# Black sleeve all the way down (rolled at the forearm).
Save-PixelMap $pal @(
    "2. aaaaaa 2."
    "1. a xccccC a 1."
    "1. a xcccCC a 1."; "1. a xcccCC a 1."; "1. a xcCccC a 1."; "1. a xcccCC a 1."; "1. a xcccCC a 1."
    "1. a xccCcC a 1."; "1. a xcccCC a 1."; "1. a xcccCC a 1."; "1. a xcCccC a 1."; "1. a xcccCC a 1."
    "1. a xcccCC a 1."; "1. a xccCcC a 1."; "1. a xcccCC a 1."; "1. a xcccCC a 1."; "1. a xcCccC a 1."
    "1. a xcccCC a 1."; "1. a xcccCC a 1."; "1. a xcccCC a 1."
) "$dst\upper_arm.png" 10

# Rolled cuff, bare forearm, watch, fist (the hand sticks out one pixel forward).
Save-PixelMap $pal @(
    "1. a xxxxxC a 1."; "1. a xnxxxC a 1."; "1. a CCCCCC a 1."; "1. a xxxxxC a 1."
    "1. a lsssSS a 1."; "1. a lsssSS a 1."; "1. a lsssSS a 1."; "1. a lssSSS a 1."; "1. a lsssSS a 1."
    "1. a lsssSS a 1."; "1. a lsssSS a 1."
    "1. a JJJJJJ a 1."; "1. a JjjJJJ a 1."
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
    "1. a oooooooo a 1."
    "1. a oOoooooo oa"
    "1. a oOooooooo a"
    "1. a OOOOOOOOO a"
    "1. a ooooooooo a"
    "2. 10a"
) "$dst\lower_leg.png" 12

# Acoustic guitar, drawn pointing up: wide lower bout at the top, sound hole, then the neck down to the headstock.
$gp = New-Palette "a=#1E1418 W=#D49C55~ w=#B07636~ d=#7A4C22 p=#4A2D18 i=#CFC4A8 z=#1E1418 f=#3B2516 P=#D3D6DC"
$neck = "6. a ff a 6."; $fret = "6. a ii a 6."
Save-PixelMap $gp @(
    "4. 8a 4."
    "2. aa wwWWWWww aa 2."
    "1. a dwWWWWWWWWwd a 1."
    "1. a dwWWWWWWWWwd a 1."
    "a dwWWWWWWWWWWwd a"
    "a dwWWW pppp WWWwd a"
    "a dwWWW pipp WWWwd a"
    "a dwWWWW iW WWWWwd a"
    "a dwWWWW iW WWWWwd a"
    "a dwWWWW iW WWWWwd a"
    "1. a dwWWW iW WWWwd a 1."
    "1. a dwWWW iW WWWwd a 1."
    "2. a dwWW iW WWwd a 2."
    "2. a dwWW iW WWwd a 2."
    "2. a dwWW iW WWwd a 2."
    "1. a dwWWW iW WWWwd a 1."
    "1. a dwWW ziWz WWwd a 1."
    "1. a dwW zziWzz Wwd a 1."
    "1. a dwW zziWzz Wwd a 1."
    "1. a dwWW ziWz WWwd a 1."
    "1. a dwWWW iW WWWwd a 1."
    "2. a dwWW iW WWwd a 2."
    "3. a dwW iW Wwd a 3."
    "4. a dw iW wd a 4."
    "5. a dffd a 5."
    $neck; $neck; $fret; $neck; $neck; $fret; $neck; $neck; $fret; $neck; $neck; $fret
    $neck; $neck; $fret; $neck; $neck; $fret
    "5. a dddd a 5."; "5. a dddd a 5."; "5. a dddd a 5."
    "4. P a dddd a P 4."; "5. a dddd a 5."; "4. P a dddd a P 4."; "5. a dddd a 5."; "4. P a dddd a P 4."
    "5. 6a 5."
) "$art\Weapons\guitar.png" 16
