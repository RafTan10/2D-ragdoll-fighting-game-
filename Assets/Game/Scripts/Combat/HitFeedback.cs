using UnityEngine;

/// <summary>
/// Turns combat events into feedback, scaled by how hard the hit was: blood (flesh) or dust + sparks (armour),
/// a sound, a freeze-frame on medium/heavy hits, camera trauma. Blocks spark and clank.
/// One instance lives on the MatchManager.
/// </summary>
public class HitFeedback : MonoBehaviour
{
    [Header("Freeze-frame (hit-stop), real seconds")]
    [SerializeField] float mediumHitStop = 0.05f;
    [SerializeField] float heavyHitStop = 0.09f;

    [Header("Camera trauma added (0..1; shake = trauma²)")]
    [SerializeField] float lightTrauma = 0.05f;
    [SerializeField] float mediumTrauma = 0.2f;
    [SerializeField] float heavyTrauma = 0.4f;
    [SerializeField] float blockTrauma = 0.08f;

    void OnEnable()
    {
        HitEvents.Hit += OnHit;
        HitEvents.Blocked += OnBlocked;
        HitEvents.Bashed += OnBashed;
        HitEvents.ArmorBroken += OnArmorBroken;
        HitEvents.PowerUsed += OnPowerUsed;
    }

    void OnDisable()
    {
        HitEvents.Hit -= OnHit;
        HitEvents.Blocked -= OnBlocked;
        HitEvents.Bashed -= OnBashed;
        HitEvents.ArmorBroken -= OnArmorBroken;
        HitEvents.PowerUsed -= OnPowerUsed;
    }

    [Header("Super powers")]
    [SerializeField] float powerHitStop = 0.14f;
    [SerializeField] float powerTrauma = 0.45f;

    /// <summary>A super power firing: a burst and ring in its colour, the power-up sound, a beat of freeze, shake,
    /// and an aura for as long as it lasts (on the frozen opponent for a freeze).</summary>
    void OnPowerUsed(PowerInfo p)
    {
        var c = p.color;
        ImpactFX.Ring(p.point, 2.6f, new Color(c.r, c.g, c.b, 0.9f), 0.35f);
        ImpactFX.Hit(p.point, Vector2.up, 1.8f, new Color(Mathf.Lerp(c.r, 1f, 0.5f), Mathf.Lerp(c.g, 1f, 0.5f), Mathf.Lerp(c.b, 1f, 0.5f), 1f));
        BloodFX.Sparks(p.point, 24);
        GameFeel.Play(GameFeel.Sound.Power, 0.9f);
        var weapon = p.fighter != null ? p.fighter.MainHand.weapon : null;
        if (weapon != null && weapon.musical) GameFeel.Play(GameFeel.Sound.Twang, 1f);
        if (p.kind == PowerKind.Shockwave)
        {
            // The blast itself: a wide ring along the floor and a kick of dust.
            ImpactFX.Ring(p.point, 5.6f, new Color(c.r, c.g, c.b, 0.7f), 0.45f);
            BloodFX.Dust(p.point + Vector2.down * 0.6f, Vector2.up, 40);
            GameFeel.Play(GameFeel.Sound.Thud, 1f);
        }
        GameFeel.HitStop(powerHitStop);
        if (CameraRig.Current != null) CameraRig.Current.AddTrauma(powerTrauma);
        PowerAura.Show(p.kind == PowerKind.Freeze ? p.target : p.fighter, c, p.duration);
    }

    /// <summary>Armour falling apart: a burst of shards and sparks, a heavy clank, a short freeze.</summary>
    void OnArmorBroken(ArmorBreakInfo info)
    {
        bool metal = FighterDefinition.IsHard(info.material);
        ImpactFX.Bash(info.point, 1.3f);
        ImpactFX.Hit(info.point, Vector2.up, 1.4f, new Color(1f, 0.9f, 0.6f, 1f));
        BloodFX.Dust(info.point, Vector2.up, 30);
        if (metal) BloodFX.Sparks(info.point, 24);
        GameFeel.Play(metal ? GameFeel.Sound.Metal : GameFeel.Sound.Wood, 1f);
        GameFeel.Play(GameFeel.Sound.Thud, 0.6f);
        GameFeel.HitStop(heavyHitStop);
        if (CameraRig.Current != null) CameraRig.Current.AddTrauma(heavyTrauma);
    }

    void OnBashed(BashInfo bash)
    {
        ImpactFX.Bash(bash.point, 0.8f + Mathf.Clamp01(bash.impact / 8f));
        BloodFX.Dust(bash.point, Vector2.up, 10);
        GameFeel.Play(GameFeel.Sound.Wood, Mathf.Clamp01(0.45f + bash.impact * 0.05f));
        GameFeel.Play(GameFeel.Sound.Thud, 0.5f);
        GameFeel.HitStop(mediumHitStop);
        if (CameraRig.Current != null) CameraRig.Current.AddTrauma(mediumTrauma);
    }

    void OnHit(HitInfo hit)
    {
        float scale = hit.strength == HitStrength.Heavy ? 1f : hit.strength == HitStrength.Medium ? 0.6f : 0.3f;

        // The flash where it landed: warm white on armour, red-white on flesh; bigger for harder hits.
        var flash = hit.armoured ? new Color(1f, 0.95f, 0.75f, 0.95f) : new Color(1f, 0.55f, 0.5f, 0.95f);
        ImpactFX.Hit(hit.point, hit.direction, 0.55f + 0.75f * scale, flash);

        if (hit.armoured)
        {
            BloodFX.Dust(hit.point, hit.direction, Mathf.RoundToInt(6 + 16 * scale));
            BloodFX.Sparks(hit.point, Mathf.RoundToInt(3 + 6 * scale));
            if (hit.strength != HitStrength.Light) BloodFX.Spray(hit.point, hit.direction, Mathf.RoundToInt(hit.damage * 0.8f));
            GameFeel.Play(GameFeel.Sound.Metal, 0.25f + 0.6f * scale);
        }
        else
        {
            BloodFX.Spray(hit.point, hit.direction, Mathf.RoundToInt(6 + hit.damage * 1.6f));
            GameFeel.Play(GameFeel.Sound.Flesh, 0.3f + 0.6f * scale);
        }

        if (hit.weapon != null && hit.weapon.musical) GameFeel.Play(GameFeel.Sound.Twang, 0.5f + 0.4f * scale);

        if (hit.strength == HitStrength.Heavy) GameFeel.HitStop(heavyHitStop);
        else if (hit.strength == HitStrength.Medium) GameFeel.HitStop(mediumHitStop);

        if (CameraRig.Current != null)
        {
            CameraRig.Current.AddTrauma(hit.strength == HitStrength.Heavy ? heavyTrauma : hit.strength == HitStrength.Medium ? mediumTrauma : lightTrauma);
            CameraRig.Current.LastHitPoint = hit.point;
        }
    }

    void OnBlocked(BlockInfo block)
    {
        BloodFX.Sparks(block.point, 10);
        ImpactFX.Hit(block.point, Vector2.up, 0.6f, new Color(1f, 0.92f, 0.6f, 0.9f));
        GameFeel.Play(block.shield ? GameFeel.Sound.Wood : GameFeel.Sound.Metal, Mathf.Clamp01(0.3f + block.impact * 0.05f));
        if (block.weapon != null && block.weapon.musical) GameFeel.Play(GameFeel.Sound.Twang, 0.5f);
        if (CameraRig.Current != null) CameraRig.Current.AddTrauma(blockTrauma);
    }
}
