using UnityEngine;

/// <summary>
/// Falling and getting up. A fighter tipped far over (or with the head near the ground), hit very hard, or out of
/// stamina goes limp for a moment, then gets back up with its muscles plus a helping lift — never by teleporting.
/// </summary>
public class KnockdownController : MonoBehaviour
{
    [Header("Knock-down")]
    [Tooltip("Torso tilt (degrees from upright) that counts as falling over.")]
    [SerializeField] float tipAngle = 62f;
    [Tooltip("Head below this share of standing height counts as falling over.")]
    [SerializeField] float headLowShare = 0.5f;
    [Tooltip("Seconds tipped before going down.")]
    [SerializeField] float tipTime = 0.25f;
    [Tooltip("A single hit at least this big knocks the fighter down.")]
    [SerializeField] float knockdownDamage = 26f;
    [Tooltip("Seconds lying down before getting up.")]
    [SerializeField] float downTime = 0.9f;
    [Tooltip("Seconds limp after fainting from exhaustion.")]
    [SerializeField] float faintTime = 2.2f;

    [Header("Getting up")]
    [Tooltip("Share of body weight lifted at the start of getting up (fades to the end value).")]
    [SerializeField] float liftStart = 0.95f;
    [SerializeField] float liftEnd = 0.6f;
    [Tooltip("Seconds after standing during which nothing can tip the fighter again.")]
    [SerializeField] float steadyTime = 1.2f;

    public Fighter.Condition State { get; private set; }

    Fighter fighter;
    float timer, getUpTime, tippedTime, steadyUntil;

    public void Init(Fighter owner) => fighter = owner;

    public bool Steady => Time.time < steadyUntil;

    public void Faint()
    {
        if (State == Fighter.Condition.Standing) GoDown(Fighter.Condition.Fainted, faintTime);
    }

    /// <summary>Called for every hit that got through.</summary>
    public void OnDamaged(float amount)
    {
        if (amount >= knockdownDamage && State == Fighter.Condition.Standing && !Steady)
            GoDown(Fighter.Condition.Down, downTime * 0.8f);
    }

    /// <summary>Standing: check whether the body has toppled.</summary>
    public void CheckTipped(float dt)
    {
        var body = fighter.Body;
        float tilt = Mathf.Abs(Mathf.DeltaAngle(body.Torso.rotation, 0f));
        bool tipped = (tilt > tipAngle || body.Head.position.y < body.StandingHeight * headLowShare) && !Steady;
        tippedTime = tipped ? tippedTime + dt : 0f;
        if (tippedTime > tipTime) GoDown(Fighter.Condition.Down, downTime);
    }

    void GoDown(Fighter.Condition condition, float seconds)
    {
        if (fighter.IsKnockedOut) return;
        State = condition;
        timer = seconds;
        getUpTime = 0f;
        tippedTime = 0f;
        fighter.OnWentDown();
        fighter.Body.RelaxAllMotors();
    }

    /// <summary>Down or fainted: lie limp, then get up.</summary>
    public void TickHelpless(float dt)
    {
        timer -= dt;
        if (timer > 0f) return;
        GetUp(dt);
    }

    void GetUp(float dt)
    {
        var body = fighter.Body;
        getUpTime += dt;

        // Muscles back on, back into guard, with a helping lift while the legs find the floor.
        fighter.Arms.Tick(1f);
        body.Neck.To(0f, body.Facing, 8f, 300f);
        fighter.Locomotion.DriveLegs(dt, 1f);
        fighter.Balance.HoldUpright(3f);

        var torso = body.Torso;
        // Lift only until the torso is back near standing height, so getting up never turns into a hop.
        if (torso.position.y < body.StandingHeight * 0.85f)
        {
            float lift = Mathf.Lerp(liftStart, liftEnd, Mathf.Clamp01(getUpTime / 1.2f));
            torso.AddForce(Vector2.up * body.TotalMass * 9.81f * lift - new Vector2(0f, torso.linearVelocity.y * body.TotalMass * 2f));
        }
        fighter.Balance.ApplyBalanceCouple(1f);
        foreach (var b in body.Bodies) b.AddForce(new Vector2(-b.linearVelocity.x * 4f * b.mass, 0f)); // don't slide while rising

        bool upright = Mathf.Abs(Mathf.DeltaAngle(torso.rotation, 0f)) < 25f && torso.position.y > body.StandingHeight * 0.75f;
        if (upright && getUpTime > 0.3f)
        {
            if (State == Fighter.Condition.Fainted) fighter.StaminaSystem.RefillTo(0.4f);
            State = Fighter.Condition.Standing;
            tippedTime = 0f;
            steadyUntil = Time.time + steadyTime; // a moment to find balance before anything can tip us again
        }
        else if (getUpTime > 3f) getUpTime = 0f; // keep trying
    }
}
