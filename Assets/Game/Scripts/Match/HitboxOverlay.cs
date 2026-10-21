using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Debug view of every fighter's hitboxes, toggled with H during a match:
/// red = striking part of a weapon (deals damage), grey = handle (only bumps), blue = shield,
/// green = body, yellow = head.
/// </summary>
public class HitboxOverlay : MonoBehaviour
{
    static Material lineMaterial;

    static readonly Color Strike = new Color(1f, 0.2f, 0.15f), Handle = new Color(0.7f, 0.7f, 0.7f),
                          Shield = new Color(0.3f, 0.6f, 1f), Body = new Color(0.3f, 1f, 0.35f), HeadColor = new Color(1f, 0.9f, 0.2f);

    void OnEnable() => RenderPipelineManager.endCameraRendering += Draw;
    void OnDisable() => RenderPipelineManager.endCameraRendering -= Draw;

    void Draw(ScriptableRenderContext context, Camera cam)
    {
        if (cam != Camera.main) return;
        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
            lineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
            lineMaterial.SetInt("_ZWrite", 0);
        }

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.modelview = cam.worldToCameraMatrix;
        GL.Begin(GL.LINES);
        foreach (var fighter in FindObjectsByType<Fighter>())
            foreach (var col in fighter.GetComponentsInChildren<Collider2D>())
            {
                GL.Color(ColorFor(col));
                if (col is BoxCollider2D box) DrawBox(box);
                else if (col is CircleCollider2D circle) DrawCircle(circle);
            }
        GL.End();
        GL.PopMatrix();
    }

    static Color ColorFor(Collider2D col)
    {
        var weapon = col.GetComponent<Weapon>();
        if (weapon != null && weapon.IsShield) return Shield;
        if (weapon != null && weapon.strikeCollider == col) return Strike;
        if (weapon != null && weapon.definition != null) return Handle;
        var part = col.GetComponent<BodyPart>();
        return part != null && part.kind == PartKind.Head ? HeadColor : Body;
    }

    static void DrawBox(BoxCollider2D box)
    {
        var t = box.transform;
        Vector2 h = box.size * 0.5f, o = box.offset;
        Vector3 a = t.TransformPoint(o + new Vector2(-h.x, -h.y)), b = t.TransformPoint(o + new Vector2(h.x, -h.y));
        Vector3 c = t.TransformPoint(o + new Vector2(h.x, h.y)), d = t.TransformPoint(o + new Vector2(-h.x, h.y));
        Line(a, b); Line(b, c); Line(c, d); Line(d, a);
    }

    static void DrawCircle(CircleCollider2D circle)
    {
        var t = circle.transform;
        const int segments = 20;
        Vector3 prev = t.TransformPoint(circle.offset + new Vector2(circle.radius, 0f));
        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 next = t.TransformPoint(circle.offset + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * circle.radius);
            Line(prev, next);
            prev = next;
        }
    }

    static void Line(Vector3 a, Vector3 b) { a.z = b.z = 0f; GL.Vertex(a); GL.Vertex(b); }
}
