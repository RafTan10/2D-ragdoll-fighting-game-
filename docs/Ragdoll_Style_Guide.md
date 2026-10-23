# Ragdoll Fighter Style Guide

How to turn photos of a real person into a fighter for this game. It is written for Claude (or any artist), and it
works on its own: paste the whole file into a Claude chat together with the photos. In this project the command
`/fighter-from-pics` follows it and also installs the fighter.

The game: a 2D side-view ragdoll duel in pixel art. Every fighter is built at runtime from **separate body-part
images** joined at the joints, so each part has to be drawn on its own, at the right size, with its joint in the
right place.

---

## 1. Ground rules

- **Consent.** Only make fighters of people who have agreed to it. Use the name they want shown, nothing more.
- **Original work.** Draw everything new. No logos, no brand prints, no art taken from other games.
- **Readable at a glance.** The whole fighter is about 110 pixels tall on screen. People recognise someone by
  their **silhouette, hair and colours** long before their face. Put the effort there.

## 2. Likeness: what to take from the photos

Pick these out of the photos first, and write them down before drawing:

1. **Hair**: shape and length (the most important cue), parting, fringe, buns or ponytails, colour (2–3 tones).
2. **Skin tone**: matched from a well-lit photo, with one shadow tone and one highlight tone.
3. **One or two signature items**: glasses, beard, hijab, earrings, a bag, a cap, a favourite jacket.
4. **Outfit colours**: the two or three main colours of what they usually wear, plus the pattern if it's bold
   (big flowers, stripes). Simplify a pattern to 2–3 colours of blobs at least 1 game pixel each.
5. **Build**: tall or short, slim or broad. The rig has fixed joint spacing, so express build through **width**
   (torso and limb thickness) and the stats (section 8), not through limb length.

Keep their real colours. Tone down anything neon about 10–20% so it sits in the game's muted, overcast world.

## 3. View and pose

- **Side view, facing right.** Profile or three-quarter face looking right, torso turned toward the right.
  (Front-facing art does work — the fighter "Nuha" is front-facing — but side view reads better in a side-view
  fight.)
- **Standing straight, every limb hanging straight down**, as if dropped on a hanger: no bent elbows or knees,
  no angled feet. The game bends the joints itself.
- **Draw one arm and one leg**: the near (front) side. The game reuses them for the far side and darkens that copy.
- Hands are **closed fists** (they grip weapons), knuckles toward the right. Feet and shoes point right.

## 4. The parts

Final sizes at **64 pixels per game unit**, in pixels, width × height. Small differences are fine; the rig adapts to
the image size. What matters is **where the joint is**.

| File | Size (W×H) | Where the joint is | Notes |
|---|---|---|---|
| `head.png` | 24×24 (up to 28×32 with big hair) | **bottom edge, centre** = the neck | Face, hair on top and behind. No neck below the chin line. |
| `torso.png` | 24×36 | neck **3 px below the top**, hips **3 px above the bottom** | Shoulders to hips. The neck/collar shows in the top 3 px. |
| `upper_arm.png` | 10×20 | **top edge, centre** = shoulder | Elbow ends 2 px above the bottom (overlap). |
| `lower_arm.png` | 8–10×20 | **top edge** = elbow | Fist in the bottom ~5 px; the weapon is gripped 4 px above the bottom. |
| `upper_leg.png` | 10–12×24 | **top edge** = hip | Knee 2 px above the bottom. |
| `lower_leg.png` | 12×24–27 | **top edge** = knee | Shoe at the bottom, toe pointing right. The bottom edge is the floor. |
| `cape.png` *(optional)* | 24×24–44 | **top edge** = 3 px below the neck | A cape, or **long hair below the neck**. It swings, so draw it hanging straight down. |
| `skirt.png` *(optional)* | 24×10–16 | **top edge** = the hips | Anything that hangs below the hips over both legs: skirt, dress hem, tunic, coat tails. Fixed to the hips. |
| `hand_prop.png` *(optional)* | ≤ 16×18 | **top edge** = held in the off hand | Something carried that swings (a bag, a lantern). For looks only. |

Joint overlap: leave about **2 extra pixels** of the part beyond each joint (the next part covers it), so seams never
show when the ragdoll bends.

Whole figure, feet to top of head: about **105–115 px**. Neck about 78 px above the floor, hips about 46–53 px.

Weapons are separate files (`Assets/Game/Art/Weapons/<name>.png`): drawn **pointing straight up**, with the grip near
the bottom. The game already has dagger, sword, axe, mace, hammer and shield; only draw a new one if the person has
a signature weapon in mind.

## 5. Pixel-art style

- **Transparent background**, PNG, no anti-aliased (semi-transparent) edges at the final size.
- **1 px dark outline** around every part's silhouette, in a warm near-black: **`#241A1E`** (not pure black).
  Inner lines (belt, collar, between fingers) use a dark tone of the local colour instead.
- **Three tones per material**: shadow, base, highlight. Light comes from the **upper left**. No gradients, no
  airbrush, no texture noise: flat clusters of colour.
- **Palette**: muted and warm. Skin, cloth and leather slightly desaturated; metal grey-blue with one bright
  highlight pixel. About 6–16 colours per part.
- **Faces at this size**: eyes 1–2 px each (dark, with the white only if there's room), brows 1 px, mouth 1–2 px,
  rosy cheek 1 px. Glasses = a 1 px frame line. Don't try to draw a nose in front view; in profile, 1 px bump.
- **Contrast between parts**: the arm must stand out against the torso (different tone or an outline), or arm swings
  become unreadable.

## 6. If drawing big (vector or high-res) instead

Drawing at full size first (SVG, or PNGs at 10× the final size) is fine; it gets converted here with
`SourceArt/tools/PixelParts.ps1` (downscale, sharpen, hard edges, 1 px outline). To survive shrinking:

- Draw at **exactly 10× the final sizes** above (head 240×240, torso 240×360, …) with the joints in the same
  places. Then the conversion is a plain resize, with no cropping guesswork.
- **Outlines at least 10 px thick**, and every detail you want to keep (eyes, earrings, flowers, buttons) **at least
  10 px across**. Anything thinner vanishes.
- **Flat colours, no gradients or soft shadows**: they turn into mud when shrunk.
- Also deliver `_assembled.png` (the whole character put together) and a `rig.json` or notes giving each part's
  joint position in pixels.

## 7. Armour (what the game needs to know)

Each body region is covered by one material. It changes how much damage gets through, the hit effect (blood or
sparks) and how many hits it takes before it breaks.

| Region | Choose from |
|---|---|
| head, torso, arms, legs | `Skin` (bare), `Cloth` (shirt, dress, trousers), `Leather` (jacket, corset, boots), `Mail` (chainmail), `Plate` (steel) |

Pick what the art shows. Bare arms = Skin, a hoodie = Cloth, a leather jacket = Leather.

## 8. Stats and loadout

Defaults are 100 health, strength 1, weight 1, walk speed 2.2, endurance 1. Nudge them **±10–20%** to match the
person: big and strong → more health and strength, slower walk; small and quick → faster walk, more endurance, a bit
less health. Pick a main-hand weapon and an off-hand item (shield, a second weapon, or nothing / a `hand_prop`).

## 9. What to deliver

A folder named after the fighter, containing:

```
head.png  torso.png  upper_arm.png  lower_arm.png  upper_leg.png  lower_leg.png
cape.png  skirt.png  hand_prop.png        (only the ones that apply)
_assembled.png                            (preview of the whole character)
notes.md                                  (name, armour per region, stats, weapons, anything unusual)
```

## 10. Final check

- [ ] Facing right; limbs straight down; one arm and one leg; fists closed.
- [ ] Each file has the joint on the right edge (table in section 4) and about 2 px of overlap.
- [ ] Transparent background, hard edges, 1 px `#241A1E` outline, no gradients.
- [ ] The hair silhouette and one signature item are recognisable in a 110 px tall preview.
- [ ] Arm reads clearly against the torso; shoes point right.
- [ ] `notes.md` lists armour, stats and weapons.

---

### Prompt to paste into Claude with the photos

> Make a fighter for my 2D ragdoll game from these photos of **[name]**, who agreed to be in the game. Follow the
> style guide below exactly. First list the likeness notes (section 2) and the armour/stats/weapon you suggest,
> then draw each part as a separate transparent PNG at the sizes in section 4 (or at 10× size per section 6), and
> show the assembled preview. **[Anything special: "keep her bag", "give him a hammer", "she always wears red".]**
>
> *(paste this whole guide here)*
