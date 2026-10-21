using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds a stickman ragdoll standing on a platform in a new scene.
/// Menu: Tools > Stickman > Build Stickman Scene
/// </summary>
public static class StickmanBuilder
{
    const string Root = "Assets/Stickman";
    const string ScenePath = Root + "/Stickman_Ragdoll.unity";

    static Material bodyMaterial;
    static PhysicsMaterial gripMaterial;

    [MenuItem("Tools/Stickman/Build Stickman Scene")]
    public static void BuildScene()
    {
        if (EditorSceneManager.GetActiveScene().isDirty &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        bodyMaterial = MakeMaterial("Stickman_Body", new Color(0.08f, 0.08f, 0.1f));
        var platformMaterial = MakeMaterial("Stickman_Platform", new Color(0.85f, 0.85f, 0.82f));
        gripMaterial = MakeGripMaterial();

        BuildPlatform(platformMaterial);
        BuildStickman(Vector3.zero);

        var cam = Camera.main;
        cam.transform.position = new Vector3(0f, 1.3f, -4.2f);
        cam.transform.LookAt(new Vector3(0f, 0.9f, 0f));
        cam.backgroundColor = new Color(0.55f, 0.7f, 0.85f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log("Stickman scene built: " + ScenePath);
    }

    [MenuItem("Tools/Stickman/Add Stickman To Open Scene")]
    public static void AddToOpenScene()
    {
        bodyMaterial = MakeMaterial("Stickman_Body", new Color(0.08f, 0.08f, 0.1f));
        gripMaterial = MakeGripMaterial();
        var stickman = BuildStickman(Vector3.zero);
        Undo.RegisterCreatedObjectUndo(stickman, "Add Stickman");
        Selection.activeGameObject = stickman;
    }

    static GameObject BuildPlatform(Material material)
    {
        var platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platform.name = "Platform";
        platform.transform.position = new Vector3(0f, -0.25f, 0f); // top surface at y = 0
        platform.transform.localScale = new Vector3(6f, 0.5f, 6f);
        platform.GetComponent<Renderer>().sharedMaterial = material;
        platform.GetComponent<Collider>().sharedMaterial = gripMaterial;
        return platform;
    }

    /// <summary>Builds the stickman with its feet on the ground at <paramref name="origin"/>, facing -Z.</summary>
    public static GameObject BuildStickman(Vector3 origin)
    {
        // The torso is the root body; every other part hangs off it with a ConfigurableJoint.
        var torso = Segment("Stickman", null, origin + new Vector3(0f, 1.5f, 0f), origin + new Vector3(0f, 1.0f, 0f), 0.07f, 10f);
        torso.gameObject.AddComponent<StickmanRagdoll>();

        var head = Ball("Head", torso, origin + new Vector3(0f, 1.67f, 0f), 0.14f, 3f);
        Joint(head, torso, new Vector3(0f, -0.14f, 0f), -40f, 40f, 60f, 30f);

        foreach (float side in new[] { 1f, -1f })
        {
            string s = side > 0 ? "_L" : "_R";

            var upperArm = Segment("UpperArm" + s, torso, origin + new Vector3(0.12f * side, 1.45f, 0f), origin + new Vector3(0.2f * side, 1.17f, 0f), 0.045f, 2f);
            var lowerArm = Segment("LowerArm" + s, upperArm, origin + new Vector3(0.2f * side, 1.17f, 0f), origin + new Vector3(0.25f * side, 0.9f, 0f), 0.04f, 1.5f);
            Joint(upperArm, torso, TopAnchor(upperArm), -90f, 150f, 60f, 80f);
            Joint(lowerArm, upperArm, TopAnchor(lowerArm), 0f, 140f, 10f, 10f);   // elbow: hand swings forward

            var upperLeg = Segment("UpperLeg" + s, torso, origin + new Vector3(0.09f * side, 0.98f, 0f), origin + new Vector3(0.1f * side, 0.55f, 0f), 0.055f, 5f);
            var lowerLeg = Segment("LowerLeg" + s, upperLeg, origin + new Vector3(0.1f * side, 0.55f, 0f), origin + new Vector3(0.1f * side, 0.1f, 0f), 0.05f, 3.5f);
            var foot = Foot("Foot" + s, lowerLeg, origin + new Vector3(0.1f * side, 0.03f, -0.05f));
            Joint(upperLeg, torso, TopAnchor(upperLeg), -30f, 100f, 30f, 40f);
            Joint(lowerLeg, upperLeg, TopAnchor(lowerLeg), -130f, 0f, 5f, 5f);    // knee: foot swings back
            Joint(foot, lowerLeg, foot.transform.InverseTransformPoint(origin + new Vector3(0.1f * side, 0.1f, 0f)), -30f, 30f, 10f, 10f);
        }

        return torso.gameObject;
    }

    // A limb from 'start' (joint end, nearest the torso) to 'end'. Its local +Y points from end to start.
    static Rigidbody Segment(string name, Rigidbody parent, Vector3 start, Vector3 end, float radius, float mass)
    {
        float length = Vector3.Distance(start, end);
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent.transform, false);
        go.transform.SetPositionAndRotation((start + end) * 0.5f, Quaternion.FromToRotation(Vector3.up, start - end));

        var col = go.AddComponent<CapsuleCollider>();
        col.direction = 1;
        col.radius = radius;
        col.height = length + radius * 2f;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(radius * 2f, (length + radius * 2f) * 0.5f, radius * 2f);
        visual.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

        return Body(go, mass);
    }

    static Rigidbody Ball(string name, Rigidbody parent, Vector3 center, float radius, float mass)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.SetPositionAndRotation(center, Quaternion.identity);
        go.AddComponent<SphereCollider>().radius = radius;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = Vector3.one * radius * 2f;
        visual.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

        return Body(go, mass);
    }

    static Rigidbody Foot(string name, Rigidbody parent, Vector3 center)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.SetPositionAndRotation(center, Quaternion.identity);
        var size = new Vector3(0.1f, 0.06f, 0.24f);
        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        col.sharedMaterial = gripMaterial;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = size;
        visual.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

        return Body(go, 1f);
    }

    static Rigidbody Body(GameObject go, float mass)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.angularDamping = 0.5f;
        return rb;
    }

    static Vector3 TopAnchor(Rigidbody segment)
    {
        var col = segment.GetComponent<CapsuleCollider>();
        return new Vector3(0f, col.height * 0.5f - col.radius, 0f);
    }

    // Bend (swing) happens around local X; lowX/highX are the bend range in degrees.
    static void Joint(Rigidbody body, Rigidbody parent, Vector3 anchor, float lowX, float highX, float y, float z)
    {
        var joint = body.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = parent;
        joint.anchor = anchor;
        joint.axis = Vector3.right;
        joint.secondaryAxis = Vector3.forward;
        joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Limited;
        joint.lowAngularXLimit = new SoftJointLimit { limit = lowX };
        joint.highAngularXLimit = new SoftJointLimit { limit = highX };
        joint.angularYLimit = new SoftJointLimit { limit = y };
        joint.angularZLimit = new SoftJointLimit { limit = z };
        joint.rotationDriveMode = RotationDriveMode.Slerp;
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.enablePreprocessing = false;
    }

    static Material MakeMaterial(string name, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        mat.SetFloat("_Smoothness", 0.2f);
        EnsureFolder(Root + "/Materials");
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static PhysicsMaterial MakeGripMaterial()
    {
        string path = Root + "/Materials/Stickman_Grip.physicsMaterial";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (mat != null) return mat;

        mat = new PhysicsMaterial("Stickman_Grip")
        {
            staticFriction = 1f,
            dynamicFriction = 1f,
            frictionCombine = PhysicsMaterialCombine.Maximum
        };
        EnsureFolder(Root + "/Materials");
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, slash));
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
