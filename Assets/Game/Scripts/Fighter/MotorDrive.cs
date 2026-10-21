using UnityEngine;

/// <summary>
/// Drives one HingeJoint2D towards a target angle with its motor (a speed command, solved by the physics engine,
/// so it stays stable even on light parts).
/// Angles are "CCW degrees for a fighter facing right" (for a limb hanging down, positive = swings forward);
/// <c>facing</c> mirrors them.
/// </summary>
public class MotorDrive
{
    /// <summary>Maps our CCW angle convention onto HingeJoint2D motor speed / limits / jointAngle.</summary>
    public const float JointSign = -1f;

    public HingeJoint2D joint;
    public Rigidbody2D body, parent;
    /// <summary>Default torque the motor may use.</summary>
    public float maxTorque;

    /// <param name="gain">Degrees/second of motor speed per degree of error.</param>
    /// <param name="torque">Torque cap for this call (≤ 0 = <see cref="maxTorque"/>).</param>
    public void To(float target, float facing, float gain, float maxSpeed, float torque = -1f)
    {
        float current = Mathf.DeltaAngle(parent.rotation, body.rotation);
        float error = Mathf.DeltaAngle(current, target * facing);
        var motor = joint.motor;
        motor.motorSpeed = Mathf.Clamp(error * gain, -maxSpeed, maxSpeed) * JointSign;
        motor.maxMotorTorque = torque > 0f ? torque : maxTorque;
        joint.motor = motor;
        joint.useMotor = true;
    }

    /// <summary>Relative angle to the parent in our convention (for a fighter facing <paramref name="facing"/>).</summary>
    public float Angle(float facing) => Mathf.DeltaAngle(parent.rotation, body.rotation) * facing;
}
