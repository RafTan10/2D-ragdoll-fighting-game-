# Akib: drawn pixel by pixel from his photos (side view, facing right). Run:  powershell -File SourceArt\Akib\sprites.ps1
# Navy fedora with a black band, thin rimless glasses, short beard + pointed chin beard, cream shirt (sleeves rolled)
# with an open collar and a silver chain, navy waistcoat, navy trousers, a ring. He studies accounting, so he fights
# with a giant calculator and blocks with a ledger.
. "$PSScriptRoot\..\tools\PixelMap.ps1"
$art = "$PSScriptRoot\..\..\Assets\Game\Art"
$dst = "$art\Fighters\Akib"

# outline | hat light/base/shadow, band | hair | skin base/shadow/light/deep | beard, brow, lip, eye white, glasses |
# shirt light/shadow | waistcoat light/base/dark, button | chain | trousers | shoe | ring
$pal = New-Palette ("a=#1E1418  n=#4A5E96 N=#2E4073~ M=#1F2C50 q=#141418  H=#1C1719~ " +
    "s=#A86A4A S=#7C4A33 l=#C58762 k=#5E3626  B=#2A1F1D~ b=#241A1D m=#74392F w=#ECE4D8 G=#A3ACB8 Q=#626873 " +
    "W=#E9E3D3~ X=#C2B9A4  v=#3A4262~ V=#252B40~ D=#171B29 u=#0B0B10  c=#D3D6DC " +
    "t=#222840~ T=#151A2A y=#353D5E~  o=#1A1414 O=#3D3434  r=#C9A24A")

Save-PixelMap $pal @(
    "8. 7a 9."
    "7. a nnNNNN a 9."
    "6. a nNNNNNNN a 8."
    "5. a nNNNNNNNNM a 7."
    "4. a nNNNNNNNNNNM a 6."
    "4. a 12q a 6."
    "a n 20N M a"
    "1. a 20M a 1."
    "3. a HHH Ssssssssssss S a 3."
    "3. a HHH Sss sssss bbbb s a 3."
    "3. a HH a k S QQQQQ s GGGGG s a 2."
    "3. a HH a k S ssssss G w a s G s s a 1."
    "3. a HH a SS ssssss GGGGG s S a 1."
    "3. a HHH SS ssssssss sss S a 2."
    "3. a HHH S ssssss B ss BBBB a 2."
    "3. a HH a S ssss BBBBBB mm B a 2."
    "6. a S ssss BBBBBBBBB a 2."
    "7. a S sss BBBB BBBBB a 2."
    "7. a S sssss a BBBBB a 3."
    "7. a S ssssss S a BB a 4."
    "7. a S ssssss S a a 6."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
    "7. a S ssssss S a 7."
) "$dst\head.png" 24

$vest   = "2. a v 7V D V 7V D a 2."
$button = "2. a v 7V D u 7V D a 2."
$pocket = "2. a v V 5D V D V V 5D V D a 2."
$pants  = "2. a yy 7t T 6t TT a 2."
Save-PixelMap $pal @(
    "8. a S ssss S a 8."
    "4. aaaa W Sssss S W aaaa 4."
    "3. a vVV WW W ssss WW VVVD a 3."
    "2. a vVVVV WW S ss S WW VVVVD a 2."
    "2. a vVVVV WW c ss c WW VVVVD a 2."
    "2. a vVVVVV WW cc WW VVVVVD a 2."
    "2. a vVVVVVV X ss X VVVVVVD a 2."
    "2. a vVVVVVV W WW W VVVVVVD a 2."
    "2. a vVVVVVVV WW VVVVVVVD a 2."
    $button; $vest; $vest; $vest; $button; $vest; $vest; $vest; $button; $pocket; $vest; $vest; $button
    $vest; $vest; $vest; $vest; $vest
    "2. a v 7V DD 7V D a 2."
    "2. a 18D a 2."
    $pants; $pants; $pants; $pants; $pants; $pants
    "3. 18a 3."
) "$dst\torso.png" 24

# Cream sleeve; rolled cuff at the top of the forearm; bare forearm; a ring on the fist.
$sleeve = "1. a WWWWWX a 1."; $crease = "1. a WWXWWX a 1."
Save-PixelMap $pal @(
    "2. aaaaaa 2."
    $sleeve; $sleeve; $crease; $sleeve; $sleeve; $sleeve; $crease; $sleeve; $sleeve; $sleeve; $crease
    $sleeve; $sleeve; $sleeve; $crease; $sleeve; $sleeve; $sleeve; $sleeve
) "$dst\upper_arm.png" 10

Save-PixelMap $pal @(
    "1. a WWWWWX a 1."; "1. a XXXXXX a 1."; "1. a WWWWWX a 1."; "1. a XWXWXX a 1."
    "1. a lsssSS a 1."; "1. a lskssS a 1."; "1. a lsssSS a 1."; "1. a lssskS a 1."; "1. a lsssSS a 1."
    "1. a lsksSS a 1."; "1. a lsssSS a 1."; "1. a lsssSS a 1."; "1. a lsssSS a 1."
    "1. a lsssssS a"; "1. a lssssSS a"; "1. a lsSsSsS a"; "1. a lsSrrsS a"; "1. a lssssSS a"; "1. a SssssSS a"
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

# Giant calculator, drawn pointing up (held by its bottom end): LCD, a red C key, grey keys, an orange "=" key.
$cp = New-Palette "a=#1E1418 k=#5A5F6C K=#3A3E48~ D=#24272F E=#A3BC90 e=#2F3D2C U=#CDD1D8 u=#8E939C R=#C0473D O=#E08A2E"
$plain = "a k 10K D a"; $gap = $plain
Save-PixelMap $cp @(
    "1. 12a 1."
    $plain
    "a k K 8E K D a"
    "a k K EeEeeEeE K D a"
    "a k K EeEeeEeE K D a"
    "a k K 8E K D a"
    $plain; $plain
    "a k K RR K UU K UU K D a"; "a k K RR K uu K uu K D a"; $gap
    "a k K UU K UU K UU K D a"; "a k K uu K uu K uu K D a"; $gap
    "a k K UU K UU K UU K D a"; "a k K uu K uu K uu K D a"; $gap
    "a k K UU K UU K UU K D a"; "a k K uu K uu K uu K D a"; $gap
    "a k K UU K UU K OO K D a"; "a k K uu K uu K OO K D a"
    $plain; $plain; $plain; $plain; $plain; $plain; $plain; $plain; $plain; $plain; $plain; $plain; $plain
    "1. 12a 1."
) "$art\Weapons\calculator.png" 14

# Ledger book as a shield: leather cover, gold corners and bands, a label, page edges on the right.
$lp = New-Palette "a=#1E1418 C=#6E3B25~ D=#4A2618 G=#C9A24A g=#8A6A2A L=#E9DFC6 e=#3A2A20 p=#DDD0B2 P=#B8A888"
$cover = "a DD 16C pp a"; $cover2 = "a DD 16C PP a"
Save-PixelMap $lp @(
    "1. 20a 1."
    "a DD GG 12C GG pp a"
    "a DD G 14C G PP a"
    $cover; $cover2; $cover; $cover2
    "a DD 16G pp a"; "a DD 16g PP a"
    $cover; $cover2
    "a DD 3C 10g 3C pp a"
    "a DD 3C g 8L g 3C PP a"
    "a DD 3C g LeeLeLLe g 3C pp a"
    "a DD 3C g 8L g 3C PP a"
    "a DD 3C g LeLeeLeL g 3C pp a"
    "a DD 3C g 8L g 3C PP a"
    "a DD 3C 10g 3C pp a"
    $cover2; $cover
    "a DD 16G PP a"; "a DD 16g pp a"
    $cover2; $cover; $cover2
    "a DD G 14C G pp a"
    "a DD GG 12C GG PP a"
    "1. 20a 1."
) "$art\Weapons\ledger.png" 22
