using UnityEngine;

/// <summary>
/// Keeps the fighter upright: a spring-and-brake torque on the torso towards a lean angle, and a
/// head-up / feet-down force couple that gives a strong righting torque without lifting the body.
/// </summary>
public class BalanceController : MonoBehaviour
{
    [Header("Torso lean")]
    [Tooltip("Degrees the torso leans towards the opponent in guard.")]
    [SerializeField] float guardLean = 7f;
    [Tooltip("Extra lean into a step, degrees.")]
    [SerializeField] float stepLean = 4f;
    [Tooltip("N·m per radian away from the lean target (scaled by body mass / 12 kg).")]
    [SerializeField] float torsoStiffness = 60f;
    [Tooltip("Brake on the torso's spin.")]
    [SerializeField] float torsoDamping = 4f;

    [Header("Righting couple")]
    [Tooltip("Head pulled up and feet pushed down with this many newtons per kg of fighter.")]
    [SerializeField] float balancePerKg = 8.5f;
    [Tooltip("Damps the head's sideways sway.")]
    [SerializeField] float headSwayDamping = 3f;

    /// <summary>The torso angle (world, degrees) the balance is aiming for this step.</summary>
    public float LeanTarget { get; private set; }

    Fighter fighter;

    public void Init(Fighter owner) => fighter = owner;

    /// <param name="stepDir">-1/+1 while stepping (world direction), 0 otherwise.</param>
    public void Tick(float fatigue, float stepDir)
    {
        LeanTarget = -fighter.Facing * guardLean - stepDir * stepLean;
        TorqueTowardLean(fatigue, 1f, 1f);
        ApplyBalanceCouple(fatigue);
    }

    /// <summary>While getting up: guard lean with a much stronger torso spring.</summary>
    public void HoldUpright(float stiffnessMultiplier)
    {
        LeanTarget = -fighter.Facing * guardLean;
        TorqueTowardLean(1f, stiffnessMultiplier, 2f);
    }

    void TorqueTowardLean(float strength, float stiffnessMultiplier, float dampingMultiplier)
    {
        var body = fighter.Body;
        var torso = body.Torso;
        float error = Mathf.DeltaAngle(torso.rotation, LeanTarget) * Mathf.Deg2Rad;
        float torque = error * torsoStiffness * stiffnessMultiplier * (body.TotalMass / 12f)
                       - torso.angularVelocity * Mathf.Deg2Rad * torsoDamping * dampingMultiplier;
        torso.AddTorque(torque * strength);
    }

    /// <summary>Head up, feet down — no net lift, but a strong righting torque. Tired fighters sag.</summary>
    public void ApplyBalanceCouple(float strength)
    {
        var body = fighter.Body;
        float force = balancePerKg * body.TotalMass * strength;
        Vector2 sway = new Vector2(body.Head.linearVelocity.x, 0f);
        body.Head.AddForce(Vector2.up * force - sway * headSwayDamping);
        body.FrontLeg.foot.AddForce(Vector2.down * force * 0.5f);
        body.BackLeg.foot.AddForce(Vector2.down * force * 0.5f);
    }
}
