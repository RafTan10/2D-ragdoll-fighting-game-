using UnityEngine;

/// <summary>
/// Frames both fighters up close: follows their midpoint and zooms out only as far as their distance needs,
/// never showing past the edges of the arena background.
/// </summary>
public class CameraRig : MonoBehaviour
{
    public Fighter a, b;
    public ArenaDefinition arena;

    [Tooltip("Orthographic size when the fighters are close (smaller = bigger fighters).")]
    public float minSize = 1.08f;
    public float maxSize = 2.4f;
    [Tooltip("World units of floor visible below the fighters' feet.")]
    public float floorMargin = 0.45f;
    public float smoothing = 4f;

    Camera cam;
    float shake;

    public static CameraRig Current { get; private set; }

    public void Shake(float amount) => shake = Mathf.Max(shake, amount);

    void Awake() { cam = GetComponent<Camera>(); Current = this; }

    public void SnapNow() => Frame(1f);

    void LateUpdate() => Frame(1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));

    void Frame(float t)
    {
        if (cam == null || arena == null || a == null || b == null) return;
        float left = Mathf.Min(a.Torso.position.x, b.Torso.position.x);
        float right = Mathf.Max(a.Torso.position.x, b.Torso.position.x);

        float aspect = Mathf.Max(cam.aspect, 0.5f);
        float halfW = ArenaView.Width * 0.5f;
        // Keep ~1.1 units of room beside each fighter: in a clinch the fighters fill most of the screen.
        float needed = ((right - left) * 0.5f + 1.1f) / aspect;
        // Tall enough to show the floor and the highest head (a jump zooms out instead of cropping the floor).
        float highestHead = Mathf.Max(a.Head.position.y, b.Head.position.y);
        float neededTall = (highestHead + 0.35f + floorMargin) * 0.5f;
        float size = Mathf.Clamp(Mathf.Max(needed, neededTall), minSize, Mathf.Min(maxSize, halfW / aspect));

        // Keep the floor near the bottom of the screen and stay inside the background.
        float bottom = ArenaView.BottomY(arena);
        float y = Mathf.Max(size - floorMargin, bottom + size);
        y = Mathf.Min(y, bottom + ArenaView.Height - size);
        float x = (left + right) * 0.5f;
        float maxX = halfW - size * aspect;
        x = Mathf.Clamp(x, -maxX, maxX);

        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, size, t);
        Vector3 target = new Vector3(x, y, -10f);
        Vector3 pos = Vector3.Lerp(transform.position, target, t);
        transform.position = pos;

        if (shake > 0f)
        {
            transform.position += (Vector3)(Random.insideUnitCircle * shake);
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 0.6f);
        }
    }
}
