using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple "active ragdoll" for the stickman. Each limb's ConfigurableJoint holds its start pose
/// with a slerp drive, and an upright torque on the torso keeps the body standing.
///
/// Controls (Play mode):
///   Space       - toggle limp (pure ragdoll) / standing
///   Left click  - shove the body part under the mouse
///   R           - reset to the starting pose
/// </summary>
public class StickmanRagdoll : MonoBehaviour
{
    [Header("Standing")]
    public bool limp = false;
    [Tooltip("Spring strength of every joint drive holding the pose.")]
    public float jointStrength = 1500f;
    public float jointDamping = 40f;
    [Tooltip("Torque that rotates the torso back to upright.")]
    public float uprightStrength = 300f;
    public float uprightDamping = 30f;
    [Tooltip("Upward pull on the head and equal downward push on the feet; this couple keeps the body balanced.")]
    public float balanceForce = 600f;
    [Tooltip("Damps the head's sideways sway.")]
    public float balanceDamping = 60f;

    [Header("Parts (found by name if left empty)")]
    public Rigidbody head;
    public Rigidbody[] feet;

    [Header("Interaction")]
    public float shoveForce = 25f;

    Rigidbody torso;
    Rigidbody[] bodies;
    ConfigurableJoint[] joints;
    Vector3[] startPositions;
    Quaternion[] startRotations;
    bool appliedLimp;

    void Awake()
    {
        torso = GetComponent<Rigidbody>();
        bodies = GetComponentsInChildren<Rigidbody>();
        joints = GetComponentsInChildren<ConfigurableJoint>();
        if (head == null) head = FindBody("Head");
        if (feet == null || feet.Length == 0) feet = new[] { FindBody("Foot_L"), FindBody("Foot_R") };

        startPositions = new Vector3[bodies.Length];
        startRotations = new Quaternion[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].solverIterations = 20;
            startPositions[i] = bodies[i].position;
            startRotations[i] = bodies[i].rotation;
        }

        ApplyDrives();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.spaceKey.wasPressedThisFrame) limp = !limp;
            if (keyboard.rKey.wasPressedThisFrame) ResetPose();
        }

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.rigidbody != null
                && hit.rigidbody.transform.IsChildOf(transform))
            {
                hit.rigidbody.AddForceAtPosition(ray.direction * shoveForce, hit.point, ForceMode.Impulse);
            }
        }

        if (limp != appliedLimp) ApplyDrives();
    }

    void FixedUpdate()
    {
        if (limp) return;

        // Rotate the torso's up axis back towards world up, damped by its spin.
        Vector3 axis = Vector3.Cross(torso.transform.up, Vector3.up);
        torso.AddTorque(axis * uprightStrength - torso.angularVelocity * uprightDamping, ForceMode.Acceleration);

        // Pull the head up and push the feet down: an upright couple that doesn't lift the body overall.
        if (head != null)
        {
            Vector3 sway = Vector3.ProjectOnPlane(head.linearVelocity, Vector3.up);
            head.AddForce(Vector3.up * balanceForce - sway * balanceDamping);
            foreach (var foot in feet)
                if (foot != null) foot.AddForce(Vector3.down * balanceForce / feet.Length);
        }
    }

    Rigidbody FindBody(string partName)
    {
        foreach (var body in GetComponentsInChildren<Rigidbody>())
            if (body.name == partName) return body;
        return null;
    }

    void ApplyDrives()
    {
        appliedLimp = limp;
        var drive = new JointDrive
        {
            positionSpring = limp ? 0f : jointStrength,
            positionDamper = limp ? 2f : jointDamping,
            maximumForce = float.MaxValue
        };
        foreach (var joint in joints)
        {
            joint.slerpDrive = drive;
            joint.targetRotation = Quaternion.identity; // identity = the pose the joint started in
        }
    }

    public void ResetPose()
    {
        for (int i = 0; i < bodies.Length; i++)
        {
            bodies[i].linearVelocity = Vector3.zero;
            bodies[i].angularVelocity = Vector3.zero;
            bodies[i].position = startPositions[i];
            bodies[i].rotation = startRotations[i];
            bodies[i].transform.SetPositionAndRotation(startPositions[i], startRotations[i]);
        }
        limp = false;
        ApplyDrives();
    }
}
