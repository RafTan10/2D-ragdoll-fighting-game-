# Character props used as weapons, drawn pixel by pixel. Run:  powershell -File SourceArt\Weapons\props.ps1
# Weapons are drawn pointing up with the grip near the bottom; shields are centred (see CLAUDE.md).
#   keyboard  — Alif's mechanical keyboard (blunt, held by its end)
#   laptop    — Alif's laptop, lid towards the opponent (shield)
#   dumbbell  — Ishtiaq's dumbbell, gripped on the bar; the top plates strike
#   bamboo    — Alavi's hiking stick: long, light, a spiral pattern, hollow top
. "$PSScriptRoot\..\tools\PixelMap.ps1"
$dst = "$PSScriptRoot\..\..\Assets\Game\Art\Weapons"

# Mechanical keyboard: dark case, light keycaps in rows, one accent key, a cable stub at the grip end.
$kp = New-Palette "a=#1E1418 K=#2E3038~ k=#45474F D=#1C1D22 U=#C9CCD2 u=#8E9199 R=#D8573A C=#3A3C44"
$keys1 = "a k K UU K UU K UU K D a"; $keys2 = "a k K uu K uu K uu K D a"; $gap = "a k 10K D a"
Save-PixelMap $kp @(
    "1. 12a 1."
    $gap
    "a k K RR K UU K UU K D a"; "a k K uu K uu K uu K D a"; $gap
    $keys1; $keys2; $gap; $keys1; $keys2; $gap; $keys1; $keys2; $gap; $keys1; $keys2; $gap
    $keys1; $keys2; $gap; $keys1; $keys2; $gap; $keys1; $keys2; $gap
    "a k K 8U K D a"; "a k K 8u K D a"; $gap
    $gap; $gap; $gap; $gap
    "1. 12a 1."
    "6. CC 6."; "6. CC 6."; "6. CC 6."
) "$dst\keyboard.png" 14

# Laptop lid as a shield: brushed silver with a darker hinge edge and a sticker.
$lp = New-Palette "a=#1E1418 S=#B9BDC6~ s=#D3D6DC H=#7E838D G=#2E3038 g=#7ED39A"
$lid = "a s 18S H a"
Save-PixelMap $lp @(
    "1. 20a 1."
    "a 20s a"
    $lid; $lid; $lid; $lid; $lid; $lid; $lid
    "a s 6S 6G 6S H a"
    "a s 6S G g G g gG 6S H a"
    "a s 6S G gg G g G 6S H a"
    "a s 6S 6G 6S H a"
    $lid; $lid; $lid; $lid; $lid; $lid; $lid; $lid; $lid
    "a 20H a"
    "a 20G a"
    "1. 20a 1."
) "$dst\laptop.png" 22

# Dumbbell: iron plates at both ends of a knurled bar; held in the middle, the top plates hit.
$dp = New-Palette "a=#1E1418 P=#3A3C44~ p=#5A5D66 Q=#24262C B=#9DA2AB b=#6E737C"
$plate = "a p 12P Q a"; $plateN = "1. a p 10P Q a 1."; $bar = "6. a Bb a 6."
Save-PixelMap $dp @(
    "1. 14a 1."
    $plate; $plate; $plate; $plate; $plate
    "1. 14a 1."
    "1. a p 10P Q a 1."; $plateN; $plateN; $plateN
    "2. 12a 2."
    $bar; $bar; $bar; $bar; $bar; $bar; $bar; $bar; $bar; $bar; $bar; $bar; $bar; $bar
    "2. 12a 2."
    $plateN; $plateN; $plateN; $plateN
    "1. 14a 1."
    $plate; $plate; $plate
    "1. 14a 1."
) "$dst\dumbbell.png" 16

# Bamboo hiking stick: brown cane with a light spiral wrap, node rings, a hollow top.
$bp = New-Palette "a=#1E1418 w=#6E3E1E~ d=#4A2614 y=#D8B070 Q=#2A1A10"
$cane = @()
for ($r = 0; $r -lt 54; $r++) {
    if ($r % 14 -eq 13) { $cane += "2. a dddd a 2."; continue }
    switch ($r % 6) { 0 { $cane += "2. a ywwd a 2." } 1 { $cane += "2. a wywd a 2." } 2 { $cane += "2. a wwyd a 2." } default { $cane += "2. a wwwd a 2." } }
}
Save-PixelMap $bp (@("2. 6a 2.", "2. a dQQd a 2.") + $cane + @("2. 6a 2.")) "$dst\bamboo.png" 10
