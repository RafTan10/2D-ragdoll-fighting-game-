using UnityEngine;

/// <summary>
/// The fighter's "muscles" for staying up. Three parts, all scaled by <see cref="Muscle"/> (which drops when hit,
/// so blows visibly rock the body) and by tiredness:
/// - a spring-and-brake torque on the torso towards a lean angle,
/// - a hip-height support force: the legs push the torso up towards a crouched height (and the feet down by the
///   same amount, so it's the legs doing the work, not a magic lift),
/// - a head-up / feet-down force couple that gives a righting torque without lifting the body.
/// </summary>
public class BalanceController : MonoBehaviour
{
    [Header("Torso lean")]
    [Tooltip("Degrees the torso leans towards the opponent in guard.")]
    [SerializeField] float guardLean = 7f;
    [Tooltip("Extra lean into a step, degrees.")]
    [SerializeField] float stepLean = 4f;
    [Tooltip("Extra lean towards the opponent while a strike is driving in, degrees: puts the body behind the blow.")]
    [SerializeField] float attackLean = 13f;
    [Tooltip("How fast the attack lean comes and goes (per second).")]
    [SerializeField] float attackLeanSpeed = 9f;
    [Tooltip("N·m per radian away from the lean target (scaled by body mass / 12 kg).")]
    [SerializeField] float torsoStiffness = 60f;
    [Tooltip("Brake on the torso's spin.")]
    [SerializeField] float torsoDamping = 4f;

    [Header("Hip-height support (the legs holding the body up)")]
    [Tooltip("Torso height the legs hold, as a share of the spawn (straight-legged) height.")]
    [SerializeField] float supportShare = 0.9f;
    [Tooltip("Spring: acceleration per unit of height error.")]
    [SerializeField] float supportStiffness = 60f;
    [Tooltip("Brake on vertical speed.")]
    [SerializeField] float supportDamping = 12f;
    [Tooltip("Cap, as a multiple of body weight.")]
    [SerializeField] float supportMaxWeights = 1.3f;
    [Tooltip("How far the hips dip on landing, as a share of the support height.")]
    [SerializeField] float landingSquash = 0.12f;
    [SerializeField] float landingSquashTime = 0.15f;

    [Header("Righting couple")]
    [Tooltip("Head pulled up and feet pushed down with this many newtons per kg of fighter.")]
    [SerializeField] float balancePerKg = 8.5f;
    [Tooltip("Damps the head's sideways sway.")]
    [SerializeField] float headSwayDamping = 3f;

    [Header("Stagger")]
    [Tooltip("Seconds for the muscles to recover fully from a stagger.")]
    [SerializeField] float staggerRecovery = 0.6f;

    /// <summary>The torso angle (world, degrees) the balance is aiming for this step.</summary>
    public float LeanTarget { get; private set; }
    /// <summary>0..1 muscle strength: 1 normally, drops when hit and recovers.</summary>
    public float Muscle { get; private set; } = 1f;

    Fighter fighter;
    float squashUntil;
    float attackBlend;

    /// <summary>0..1, how far into the attack lean the body is (legs use it for the lunge).</summary>
    public float AttackBlend => attackBlend;

    public void Init(Fighter owner) => fighter = owner;

    /// <summary>A hit rocks the fighter: muscles drop to <paramref name="strength"/> and recover over time.</summary>
    public void Stagger(float strength) => Muscle = Mathf.Min(Muscle, Mathf.Clamp01(strength));

    public void Land() => squashUntil = Time.time + landingSquashTime;

    [Header("Hit reactions")]
    [Tooltip("Torso spin impulse per N·s of a blade hit: the body jerks along the blow, then the muscles pull it back.")]
    [SerializeField] float flinchPerImpact = 0.09f;
    [Tooltip("Extra head snap per N·s (doubled for hits to the head).")]
    [SerializeField] float headSnapPerImpact = 0.025f;
    [SerializeField] float maxFlinch = 1.4f;
    [Tooltip("Speed (m/s) a shield bash gives the whole body, per N·s of impact.")]
    [SerializeField] float bashPushPerImpact = 0.25f;
    [Tooltip("Cap on the shove speed, m/s.")]
    [SerializeField] float maxBashPush = 1.4f;

    /// <summary>A blade landed: twist the torso along the blow and snap the head.</summary>
    public void Flinch(Vector2 blowDir, float impact, bool toHead)
    {
        var body = fighter.Body;
        float spin = -Mathf.Sign(blowDir.x); // a blow travelling +x tips the top +x, which is clockwise
        body.Torso.AddTorque(spin * Mathf.Min(impact * flinchPerImpact, maxFlinch), ForceMode2D.Impulse);
        body.Head.AddTorque(spin * Mathf.Min(impact * headSnapPerImpact * (toHead ? 2f : 1f), maxFlinch * 0.5f), ForceMode2D.Impulse);
    }

    /// <summary>A shield bash: shove the whole body back (it has to catch itself with a step) and rock it.</summary>
    public void Bashed(Vector2 pushDir, float impact)
    {
        float push = Mathf.Min(impact * bashPushPerImpact, maxBashPush);
        Vector2 dir = new Vector2(Mathf.Sign(pushDir.x), 0.25f).normalized;
        foreach (var b in fighter.Body.Bodies) b.AddForce(dir * push * b.mass, ForceMode2D.Impulse);
        Flinch(pushDir, impact * 0.6f, false);
        Stagger(0.45f);
    }

    public void RecoverMuscle(float dt) => Muscle = Mathf.MoveTowards(Muscle, 1f, dt / Mathf.Max(0.01f, staggerRecovery));

    /// <param name="strength">Tiredness × muscle, 0..1.</param>
    /// <param name="stepDir">-1/+1 while stepping (world direction), 0 otherwise.</param>
    public void Tick(float strength, float stepDir, bool grounded)
    {
        attackBlend = Mathf.MoveTowards(attackBlend, fighter.CurrentInput.attacking ? 1f : 0f, attackLeanSpeed * Time.fixedDeltaTime);
        LeanTarget = -fighter.Facing * (guardLean + attackLean * attackBlend) - stepDir * stepLean;
        TorqueTowardLean(strength, 1f, 1f);
        if (grounded) SupportHips(strength);
        ApplyBalanceCouple(strength);
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

    /// <summary>The legs push the torso towards a crouched height above the floor, and the feet into the floor.</summary>
    void SupportHips(float strength)
    {
        var body = fighter.Body;
        var torso = body.Torso;
        float target = body.StandingHeight * supportShare * (Time.time < squashUntil ? 1f - landingSquash : 1f);
        float height = torso.position.y; // the floor is at y = 0
        float accel = supportStiffness * (target - height) - supportDamping * torso.linearVelocity.y;
        float force = Mathf.Clamp(accel * body.TotalMass, -body.TotalMass * 9.81f, body.TotalMass * 9.81f * supportMaxWeights) * strength;
        if (force <= 0f) return; // legs push, they don't pull the body down
        torso.AddForce(Vector2.up * force);
        body.FrontLeg.foot.AddForce(Vector2.down * force * 0.5f);
        body.BackLeg.foot.AddForce(Vector2.down * force * 0.5f);
    }

    /// <summary>Head up, feet down — no net lift, but a strong righting torque. Tired or staggered fighters sag.</summary>
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
