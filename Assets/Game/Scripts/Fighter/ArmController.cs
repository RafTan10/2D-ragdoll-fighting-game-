using UnityEngine;

/// <summary>
/// Moves both arms. An active hand reaches for its aim point (two-joint solver → shoulder/elbow motor targets);
/// a released hand relaxes into guard (weapon low and pointing at the opponent, shield across the body).
/// </summary>
public class ArmController : MonoBehaviour
{
    [Header("Speed")]
    [Tooltip("Top speed an arm can turn at, deg/s. Weapon weight isn't faked here: the shoulder's torque cap " +
             "(RagdollBody) has to accelerate the real weapon, so heavy weapons get up to speed more slowly.")]
    [SerializeField] float armSpeed = 1300f;
    [Tooltip("How hard an aimed arm chases its target (motor speed per degree of error).")]
    [SerializeField] float aimGain = 14f;
    [Tooltip("How hard a relaxed arm returns to guard.")]
    [SerializeField] float guardGain = 7f;
    [Tooltip("Share of full speed used when returning to guard.")]
    [SerializeField] float guardSpeedShare = 0.45f;

    [Header("Guard poses (shoulder, elbow degrees; 0 = hanging down, + = forward)")]
    [SerializeField] Vector2 weaponGuard = new Vector2(-10f, 22f);
    [SerializeField] Vector2 offWeaponGuard = new Vector2(5f, 35f);
    [SerializeField] Vector2 shieldGuard = new Vector2(45f, 50f);

    Fighter fighter;

    public void Init(Fighter owner) => fighter = owner;

    public void Tick(float fatigue)
    {
        Drive(fighter.MainHand, fatigue);
        Drive(fighter.OffHand, fatigue);
    }

    /// <param name="strength">Tiredness × muscle (stagger), 0..1: weaker arms are slower and easier to stop.</param>
    void Drive(Fighter.Hand hand, float strength)
    {
        float speedLimit = armSpeed * fighter.Definition.strength * strength;
        float shoulderTorque = hand.shoulder.maxTorque * strength, elbowTorque = hand.elbow.maxTorque * strength;

        float shoulder, elbow, gain, maxSpeed;
        if (hand.Active)
        {
            Solve(hand, hand.Aim, out shoulder, out elbow);
            gain = aimGain;
            maxSpeed = speedLimit;
        }
        else
        {
            Vector2 pose = hand.IsShield ? shieldGuard : hand == fighter.MainHand ? weaponGuard : offWeaponGuard;
            shoulder = pose.x;
            elbow = pose.y;
            gain = guardGain;
            maxSpeed = speedLimit * guardSpeedShare;
        }
        hand.shoulder.To(shoulder, fighter.Facing, gain, maxSpeed, shoulderTorque);
        hand.elbow.To(elbow, fighter.Facing, gain, maxSpeed, elbowTorque);
    }

    /// <summary>
    /// Two-joint arm solver. Works in the facing-right frame with angles measured from straight down
    /// (CCW positive, so 90 = pointing at the opponent). The elbow only bends forward.
    /// </summary>
    void Solve(Fighter.Hand hand, Vector2 aim, out float shoulder, out float elbow)
    {
        float facing = fighter.Facing;
        Vector2 local = new Vector2(aim.x * facing, aim.y);
        float a = hand.upperLength, b = hand.lowerLength;
        float d = Mathf.Clamp(local.magnitude, Mathf.Abs(a - b) + 0.02f, a + b - 0.001f);

        float towardTarget = Mathf.Atan2(local.x, -local.y) * Mathf.Rad2Deg;
        float elbowBend = 180f - Mathf.Acos(Mathf.Clamp((a * a + b * b - d * d) / (2f * a * b), -1f, 1f)) * Mathf.Rad2Deg;
        float upperOffset = Mathf.Acos(Mathf.Clamp((a * a + d * d - b * b) / (2f * a * d), -1f, 1f)) * Mathf.Rad2Deg;

        // Relative to the torso's *intended* lean, not its actual angle: using the actual angle feeds the
        // arm's own reaction torque back into its target and the arm spirals out of control.
        shoulder = towardTarget - upperOffset - fighter.Balance.LeanTarget * facing;
        elbow = Mathf.Clamp(elbowBend, 0f, 150f);
    }
}
