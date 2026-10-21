# Ragdoll audit (before the realism upgrade)

Snapshot of how the fighter worked at commit `005e8e1` ("Baseline before realism upgrade"), and what to fix.

## 1. How a fighter is built
Everything is created at runtime by `Fighter.Build` (no prefabs). Each part = SpriteRenderer + Rigidbody2D +
collider fitted to the visible pixels (`PixelBounds`), joined to its parent with a HingeJoint2D whose pivot is the
sprite pivot. Masses are set by hand (Auto Mass is not used):

| Part | Mass (× fighter weight) | Collider | Joint limits (facing right) | Motor |
|---|---|---|---|---|
| Torso | 4.0 | box | — (root) | — |
| Head | 1.2 | circle | neck −25..25 | yes |
| Upper arm ×2 | 1.6 | box | shoulder: none | yes (aim) |
| Lower arm ×2 | 1.2 | box (+ fist box if unarmed) | elbow 0..150 | yes |
| Upper leg ×2 | 1.0 | box | hip −60..110 | yes |
| Lower leg ×2 | 0.9 | box | knee −130..0 | yes |
| Weapon | `WeaponDefinition.mass` (0.3–1.6) | handle box + strike box | FixedJoint2D to the hand | — |
| Cape | 0.15, no collider | — | −45..6 | no |

All parts: gravity scale 1, Interpolate, linear/angular damping 0 (cape 2.5 / 6). Weapons use Continuous collision,
everything else Discrete. No physics materials (default friction 0.4).

## 2. Standing, moving, swinging
- Upright: torso torque toward a lean angle + a "head up / feet down" force couple (`BalancePerKg`), all in FixedUpdate.
- Legs: hip/knee motors toward a crouched stance; steps animate the motor targets; walking = horizontal force on every body.
- Jump / lunge: impulses on every body. Turning around mirrors the whole ragdoll (the one place positions are set directly, besides spawning).
- Arms: 2-bone IK → shoulder/elbow motor targets. Speed is capped by `ArmSpeed × strength / (1 + weaponMass × 0.55)`.

## 3. Weapons and damage
Weapon = own Rigidbody2D on a FixedJoint2D. Only the striking box hurts. Damage = weapon damage × (contact speed / 5)
× body-part multiplier × armour. One cooldown per weapon. Effects are played directly from `Weapon`.

## 4. Project physics settings
Gravity −9.81, velocity/position iterations 8/3 in the project (raised to 20/8 in `MatchManager.Start`),
fixed timestep 0.02, all layers collide (self-collision is removed with `Physics2D.IgnoreCollision`).
Fighters are ≈ 1.8 units tall, so default gravity is physically right for their size.

## 5. Top 10 reasons it looks less real than it could (ranked)
1. **Balance is a cheat force**, not muscles (`Fighter.FixedUpdate`): the head-up/feet-down couple holds the body
   upright regardless of hits, so blows barely rock it. → PD "muscles" with a hip-height support force and a
   `muscleStrength` that drops when hit (stagger).
2. **Weapon weight is faked** (`Fighter.DriveArm`): arm speed is divided by weapon mass while the motors are strong
   enough to reach that speed instantly. → Limit the arm by *torque*, use real weapon masses, let physics slow heavy weapons.
3. **Damage ignores mass** (`Weapon.OnCollisionEnter2D`): a dagger and a mace at the same speed hit equally hard.
   → Damage from the collision impulse.
4. **Top-heavy body**: arms (1.6 / 1.2) are heavier than legs (1.0 / 0.9), so legs are pushed around and feet slide.
   (Arms must stay heavy relative to the weapon — lighter arms made the solver jitter — so make the legs heavier.)
5. **Soft solver**: fixed timestep 0.02 with long joint chains. → 0.01 timestep.
6. **No physics materials**: feet slide, weapons grind. → Feet friction 1.0, body 0.4, weapons 0.2, no bounce.
7. **No angular damping** on limbs: flailing never settles (very visible after a knockout).
8. **Cooldown per weapon, not per body part**: one swing grinding along a limb is handled crudely.
9. **Camera shake leaks into the camera's base position** (`CameraRig.Frame` lerps from the shaken position), so shake causes drift.
10. **No hit event**: effects, sound, camera and stagger are hard-wired inside `Weapon`, so they can't scale with hit strength consistently.

Already fine: no live limb is moved by setting transforms (except spawn and the deliberate turn-around mirror);
all forces in FixedUpdate; interpolation on; weapons Continuous; joint limits inside −180..180 and the spawn pose inside them.

## 6. Splitting `Fighter.cs`
It builds the ragdoll, balances, walks, jumps, aims arms, tracks stamina, handles knock-downs and damage. Split into:
`RagdollBody` (build, mirror), `BalanceController`, `LocomotionController`, `ArmController`, `StaminaController`,
`KnockdownController`, with `Fighter` as the thin coordinator (health, input, events) — and `HitEvents` for combat.
