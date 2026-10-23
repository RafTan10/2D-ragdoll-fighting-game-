# Nuha: front-view ragdoll pack (png/ at 2x, rig.json in source units, neck at y 0) -> the game's fighter parts.
#   powershell -File SourceArt\Nuha\convert.ps1            (writes Assets\Game\Art\Fighters\Nuha\*.png)
# Then in Unity: ContentTools.CreateFighter("Assets/Game/Art/Fighters/Nuha", "Sword", null) keeps her stats.
param([string]$Src, [string]$Dst = "$PSScriptRoot\..\..\Assets\Game\Art\Fighters\Nuha", [double]$S = 0.1175)
. "$PSScriptRoot\..\tools\PixelParts.ps1"

if (-not $Src) {
    $Src = "$env:TEMP\nuha_src"
    if (-not (Test-Path "$Src\Nuha Ragdoll\png")) { Expand-Archive "$PSScriptRoot\Nuha Ragdoll.zip" $Src -Force }
    $Src = "$Src\Nuha Ragdoll\png"
}
New-Item -ItemType Directory -Force $Dst | Out-Null

# Head: the hair above the neck behind the face; bottom edge = the neck (hair_back row 272 = rig y 0).
# head.png's neck pivot is (92, 224), so its top-left sits at (116 - 92, 272 - 224) on the 232-wide canvas.
$hair = Load-Part $Src "hair_back"; $head = Load-Part $Src "head"
Save-Part (Compose 232 272 @(@((Crop-Rows $hair 0 272), 0, 0), @($head, 24, 48))) "$Dst\head.png" $S

# Long hair below the neck -> cape (the rig hangs capes 3 game px below the neck; 3 px = 25.5 source px).
Save-Part (Crop-Rows $hair 298 500) "$Dst\cape.png" $S

# Torso spans neck - 3 px .. hip + 3 px (neck row 40, hip row 296); the dress below the hip -> skirt.
$torso = Load-Part $Src "torso"
Save-Part (Crop-Rows $torso 14 322) "$Dst\torso.png" $S
Save-Part (Crop-Rows $torso 296 396) "$Dst\skirt.png" $S

# Limbs: from the joint row down (the joint is the top edge). One arm and one leg serve both sides.
Save-Part (Crop-Rows (Load-Part $Src "upper_arm_R") 36 184) "$Dst\upper_arm.png" $S
Save-Part (Crop-Rows (Load-Part $Src "forearm_R") 24 192) "$Dst\lower_arm.png" $S
Save-Part (Crop-Rows (Load-Part $Src "thigh_R") 30 236) "$Dst\upper_leg.png" $S
Save-Part (Crop-Rows (Load-Part $Src "shin_R") 26 254) "$Dst\lower_leg.png" $S

# The bag hangs from the off hand by its handles (pivot top-centre).
Save-Part (Load-Part $Src "bag") "$Dst\hand_prop.png" $S
