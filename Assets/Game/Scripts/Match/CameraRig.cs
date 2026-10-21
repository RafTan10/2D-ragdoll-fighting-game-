using UnityEngine;

/// <summary>
/// Frames both fighters like a fighting game: follows the midpoint of their torsos with smooth damping and zooms
/// only as far out as their distance (and any jump) needs, never showing past the arena background.
/// Shake is trauma-based (each hit adds trauma, shake = trauma², Perlin noise) and is applied as an offset on top
/// of the base position, so it never makes the camera drift. On a knockout it eases in towards the final blow.
/// </summary>
public class CameraRig : MonoBehaviour
{
    public Fighter a, b;
    public ArenaDefinition arena;

    [Header("Framing")]
    [Tooltip("Orthographic size when the fighters are close (smaller = bigger fighters).")]
    public float minSize = 1.08f;
    public float maxSize = 2.4f;
    [Tooltip("Room kept beside each fighter, world units.")]
    public float sidePadding = 1.1f;
    [Tooltip("World units of floor visible below the fighters' feet.")]
    public float floorMargin = 0.45f;
    [Tooltip("Seconds to catch up with the fighters (SmoothDamp).")]
    public float followSmoothTime = 0.15f;
    public float zoomSmoothTime = 0.25f;

    [Header("Shake")]
    public bool shakeEnabled = true;
    [Tooltip("Trauma lost per second.")]
    public float traumaDecay = 1.6f;
    [Tooltip("Largest shake offset, world units.")]
    public float maxShakeOffset = 0.25f;
    [Tooltip("Largest shake roll, degrees.")]
    public float maxShakeRoll = 2f;
    public float shakeFrequency = 22f;

    [Header("Knockout focus")]
    [Tooltip("Zoom-in during knockout slow motion (share of the size).")]
    public float knockoutZoom = 0.18f;
    [Tooltip("How far the view slides towards the final blow (0..1).")]
    public float knockoutPull = 0.35f;

    /// <summary>Where the last hit landed (set by the hit feedback), for the knockout zoom.</summary>
    public Vector2 LastHitPoint { get; set; }

    public static CameraRig Current { get; private set; }

    Camera cam;
    float trauma;
    Vector3 basePosition;
    Vector3 followVelocity;
    float sizeVelocity;
    float seed;

    /// <summary>Adds shake trauma (0..1). Small hits add little; trauma² makes big hits shake much more.</summary>
    public void AddTrauma(float amount) => trauma = Mathf.Clamp01(trauma + amount);

    /// <summary>Old API: a shake strength in world units, converted to trauma.</summary>
    public void Shake(float amount) => AddTrauma(amount * 4f);

    void Awake()
    {
        cam = GetComponent<Camera>();
        Current = this;
        seed = Random.value * 100f;
        basePosition = transform.position;
    }

    public void SnapNow()
    {
        if (!ComputeFrame(out Vector3 target, out float size)) return;
        basePosition = target;
        cam.orthographicSize = size;
        followVelocity = Vector3.zero;
        sizeVelocity = 0f;
        trauma = 0f;
        transform.SetPositionAndRotation(basePosition, Quaternion.identity);
    }

    void LateUpdate()
    {
        if (!ComputeFrame(out Vector3 target, out float size)) return;
        float dt = Time.unscaledDeltaTime;

        // Knockout slow motion: ease in towards the final blow.
        if (GameFeel.InSlowMotion)
        {
            size *= 1f - knockoutZoom;
            target = Vector3.Lerp(target, new Vector3(LastHitPoint.x, LastHitPoint.y, target.z), knockoutPull);
            target = ClampToArena(target, size);
        }

        basePosition = Vector3.SmoothDamp(basePosition, target, ref followVelocity, followSmoothTime, Mathf.Infinity, dt);
        cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, size, ref sizeVelocity, zoomSmoothTime, Mathf.Infinity, dt);

        // Shake on top of the base position (never fed back into it).
        Vector3 offset = Vector3.zero;
        float roll = 0f;
        if (shakeEnabled && trauma > 0f)
        {
            float s = trauma * trauma;
            float t = Time.unscaledTime * shakeFrequency;
            offset = new Vector3(Noise(t, 0f), Noise(t, 1f), 0f) * maxShakeOffset * s;
            roll = Noise(t, 2f) * maxShakeRoll * s;
            trauma = Mathf.Max(0f, trauma - traumaDecay * dt);
        }
        transform.SetPositionAndRotation(basePosition + offset, Quaternion.Euler(0f, 0f, roll));
    }

    float Noise(float t, float channel) => Mathf.PerlinNoise(seed + channel * 17.3f, t) * 2f - 1f;

    bool ComputeFrame(out Vector3 target, out float size)
    {
        target = default;
        size = minSize;
        if (cam == null || arena == null || a == null || b == null) return false;
        float left = Mathf.Min(a.Torso.position.x, b.Torso.position.x);
        float right = Mathf.Max(a.Torso.position.x, b.Torso.position.x);

        float aspect = Mathf.Max(cam.aspect, 0.5f);
        float halfW = ArenaView.Width * 0.5f;
        float needed = ((right - left) * 0.5f + sidePadding) / aspect;
        // Tall enough to show the floor and the highest head (a jump zooms out instead of cropping the floor).
        float highestHead = Mathf.Max(a.Head.position.y, b.Head.position.y);
        float neededTall = (highestHead + 0.35f + floorMargin) * 0.5f;
        size = Mathf.Clamp(Mathf.Max(needed, neededTall), minSize, Mathf.Min(maxSize, halfW / aspect));

        float y = size - floorMargin; // floor near the bottom of the screen
        target = ClampToArena(new Vector3((left + right) * 0.5f, y, -10f), size);
        return true;
    }

    Vector3 ClampToArena(Vector3 p, float size)
    {
        float aspect = Mathf.Max(cam.aspect, 0.5f);
        float halfW = ArenaView.Width * 0.5f;
        float bottom = ArenaView.BottomY(arena);
        float maxX = Mathf.Max(0f, halfW - size * aspect);
        p.x = Mathf.Clamp(p.x, -maxX, maxX);
        p.y = Mathf.Clamp(p.y, bottom + size, bottom + ArenaView.Height - size);
        return p;
    }
}
