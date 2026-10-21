using UnityEngine;

/// <summary>
/// Feet and moving: discrete steps (holding a direction chains them), planted feet in between, automatic recovery
/// steps when the body leans past the feet, jumps and lunges, and turning round when the opponent ends up behind.
/// </summary>
public class LocomotionController : MonoBehaviour
{
    [Header("Steps")]
    [Tooltip("Seconds per step; holding a direction chains steps.")]
    [SerializeField] float stepDuration = 0.3f;
    [Tooltip("Peak step speed as a multiple of the fighter's walk speed.")]
    [SerializeField] float stepSpeed = 1.45f;
    [Tooltip("How hard the body is pushed to the step speed (and braked between steps).")]
    [SerializeField] float walkAcceleration = 10f;
    [Tooltip("Braking between steps, as a share of walkAcceleration.")]
    [SerializeField] float plantedBrake = 0.7f;
    [Tooltip("Torso this far past the feet (units) triggers a recovery step.")]
    [SerializeField] float recoveryLean = 0.32f;
    [SerializeField] float stepStaminaCost = 0.4f;

    [Header("Stance (degrees; + = forward)")]
    [SerializeField] float frontHip = 26f;
    [SerializeField] float backHip = -16f;
    [SerializeField] float frontKnee = -32f;
    [SerializeField] float backKnee = -24f;
    [Tooltip("Extra knee bend when exhausted.")]
    [SerializeField] float tiredKneeSag = 50f;

    [Header("Jump / lunge")]
    [SerializeField] float jumpUp = 4.6f;
    [SerializeField] float jumpForward = 2.4f;
    [SerializeField] float jumpStaminaCost = 9f;
    [SerializeField] float lungeUp = 3f;
    [SerializeField] float lungeForward = 3.4f;
    [SerializeField] float lungeStaminaCost = 7f;
    [Tooltip("Share of the ground push available for steering in the air.")]
    [SerializeField] float airControl = 0.15f;

    [Header("Turning round")]
    [Tooltip("Seconds the opponent must be behind before the fighter turns.")]
    [SerializeField] float turnDelay = 0.12f;

    /// <summary>-1/+1 (world) while a step is under way, else 0.</summary>
    public float SteppingDir => stepTimer > 0f ? stepDir : 0f;

    Fighter fighter;
    float stepTimer, stepDir;
    int stepCount;
    float hopCooldown, crossedTime, airTime;
    bool hopQueued, jumpQueued;

    public void Init(Fighter owner) => fighter = owner;

    public void QueueHop() => hopQueued = true;
    public void QueueJump() => jumpQueued = true;
    public void CancelStep() => stepTimer = 0f;

    /// <summary>When the opponent ends up behind us (jumped over, pushed past), turn round.</summary>
    public void FaceOpponent(float dt)
    {
        var opponent = fighter.Opponent;
        if (opponent == null) return;
        float dx = opponent.Torso.position.x - fighter.Torso.position.x;
        bool behind = Mathf.Sign(dx) != fighter.Facing && Mathf.Abs(dx) > 0.3f;
        crossedTime = behind ? crossedTime + dt : 0f;
        if (crossedTime > turnDelay) { crossedTime = 0f; fighter.TurnAround(); }
    }

    public void TickLegs(float dt, float fatigue, bool grounded)
    {
        // Landing after a real flight: dip the hips for a moment.
        if (grounded && airTime > 0.2f) fighter.Balance.Land();
        airTime = grounded ? 0f : airTime + dt;

        if (grounded || hopCooldown < 0.35f) DriveLegs(dt, fatigue);
        else TuckLegs(); // just after take-off: knees up
    }

    /// <summary>A little steering in the air, a fraction of the ground push.</summary>
    public void AirControl()
    {
        var body = fighter.Body;
        float target = fighter.CurrentInput.move * fighter.Definition.walkSpeed;
        float accel = (target - body.Torso.linearVelocity.x) * walkAcceleration * airControl;
        if (Mathf.Abs(fighter.CurrentInput.move) < 0.1f) return;
        foreach (var b in body.Bodies) b.AddForce(new Vector2(accel * b.mass, 0f));
    }

    /// <summary>Discrete steps: start one when asked (or when the body leans past the feet), brake in between.</summary>
    public void Footwork(float dt)
    {
        var body = fighter.Body;
        float vx = body.Torso.linearVelocity.x;
        if (stepTimer <= 0f)
        {
            float want = fighter.CurrentInput.move;
            float support = (body.FrontLeg.foot.position.x + body.BackLeg.foot.position.x) * 0.5f;
            float lead = body.Torso.position.x - support;
            if (Mathf.Abs(want) < 0.1f && Mathf.Abs(lead) > recoveryLean) want = Mathf.Sign(lead); // recovery step
            if (Mathf.Abs(want) > 0.1f)
            {
                stepDir = Mathf.Sign(want);
                stepTimer = stepDuration;
                stepCount++;
                fighter.SpendStamina(stepStaminaCost);
            }
        }

        float targetVx = 0f;
        if (stepTimer > 0f)
        {
            stepTimer -= dt;
            float progress = 1f - Mathf.Clamp01(stepTimer / stepDuration);
            targetVx = stepDir * fighter.Definition.walkSpeed * stepSpeed * Mathf.Sin(progress * Mathf.PI);
        }
        // Planted between steps: brake hard.
        float accel = (targetVx - vx) * walkAcceleration * (stepTimer > 0f ? 1f : plantedBrake);
        foreach (var b in body.Bodies) b.AddForce(new Vector2(accel * b.mass, 0f));
    }

    public void DriveLegs(float dt, float fatigue)
    {
        var body = fighter.Body;
        float facing = fighter.Facing;
        // Crouched guard: front leg forward, back leg behind, knees bent (more when tired).
        float sag = (1f - fatigue) * tiredKneeSag;
        float fHip = frontHip, bHip = backHip, fKnee = frontKnee - sag, bKnee = backKnee - sag;

        if (stepTimer > 0f)
        {
            // One leg swings (lifted, from behind to in front of the other), the other pushes; legs alternate.
            float progress = 1f - Mathf.Clamp01(stepTimer / stepDuration);
            float lift = Mathf.Sin(progress * Mathf.PI);
            float sweep = stepDir * facing * (progress - 0.5f) * 34f;
            if (stepCount % 2 == 0) { fHip += sweep; fKnee -= 40f * lift; bHip -= sweep * 0.5f; }
            else { bHip += sweep; bKnee -= 40f * lift; fHip -= sweep * 0.5f; }
        }

        float torque = body.LegTorque * fatigue;
        body.FrontLeg.hip.To(fHip, facing, 8f, 420f, torque);
        body.BackLeg.hip.To(bHip, facing, 8f, 420f, torque);
        body.FrontLeg.knee.To(fKnee, facing, 8f, 420f, torque);
        body.BackLeg.knee.To(bKnee, facing, 8f, 420f, torque);
    }

    void TuckLegs()
    {
        var body = fighter.Body;
        float facing = fighter.Facing;
        body.FrontLeg.hip.To(80f, facing, 12f, 700f);
        body.BackLeg.hip.To(50f, facing, 12f, 700f);
        body.FrontLeg.knee.To(-105f, facing, 12f, 700f);
        body.BackLeg.knee.To(-100f, facing, 12f, 700f);
    }

    public void TickJumps(float dt, bool grounded)
    {
        var body = fighter.Body;
        hopCooldown -= dt;
        if (hopQueued)
        {
            hopQueued = false;
            if (grounded && hopCooldown <= 0f && fighter.Stamina > lungeStaminaCost + 1f)
            {
                // A low lunge towards the opponent.
                hopCooldown = 0.7f;
                fighter.SpendStamina(lungeStaminaCost);
                var opponent = fighter.Opponent;
                float dir = opponent != null ? Mathf.Sign(opponent.Torso.position.x - body.Torso.position.x) : fighter.Facing;
                foreach (var b in body.Bodies) b.AddForce(new Vector2(dir * lungeForward, lungeUp) * b.mass, ForceMode2D.Impulse);
            }
        }
        if (jumpQueued)
        {
            jumpQueued = false;
            if (grounded && hopCooldown <= 0f && fighter.Stamina > jumpStaminaCost + 1f)
            {
                hopCooldown = 0.75f;
                fighter.SpendStamina(jumpStaminaCost);
                float move = fighter.CurrentInput.move;
                foreach (var b in body.Bodies) b.AddForce(new Vector2(move * jumpForward, jumpUp) * b.mass, ForceMode2D.Impulse);
            }
        }
    }
}
