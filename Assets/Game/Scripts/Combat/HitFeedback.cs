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
    }

    void OnDisable()
    {
        HitEvents.Hit -= OnHit;
        HitEvents.Blocked -= OnBlocked;
    }

    void OnHit(HitInfo hit)
    {
        float scale = hit.strength == HitStrength.Heavy ? 1f : hit.strength == HitStrength.Medium ? 0.6f : 0.3f;

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
        GameFeel.Play(block.shield ? GameFeel.Sound.Wood : GameFeel.Sound.Metal, Mathf.Clamp01(0.3f + block.impact * 0.05f));
        if (CameraRig.Current != null) CameraRig.Current.AddTrauma(blockTrauma);
    }
}
