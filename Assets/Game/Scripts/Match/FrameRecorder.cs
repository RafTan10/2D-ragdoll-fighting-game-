using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Dev tool for trailers and README GIFs: renders the main camera (no IMGUI HUD) to numbered PNGs while the game
/// runs at a fixed capture frame rate, so the footage is smooth even when the Editor is in the background.
/// Start it from an eval in Play mode:  FrameRecorder.Begin(@"C:\...\frames\clip1", 90, 2, 480, 270);
/// </summary>
public class FrameRecorder : MonoBehaviour
{
    static FrameRecorder current;

    /// <summary>A recording is still running.</summary>
    public static bool Busy => current != null;

    string folder;
    int frames, every, rendered, saved;
    RenderTexture target;
    Texture2D readback;

    /// <param name="frames">PNGs to save.</param>
    /// <param name="every">Save one of every N rendered frames (game time runs at <paramref name="fps"/>).</param>
    public static void Begin(string folder, int frames, int every, int width, int height, int fps = 30)
    {
        if (current != null) Destroy(current.gameObject);
        Directory.CreateDirectory(folder);
        var rec = new GameObject("FrameRecorder").AddComponent<FrameRecorder>();
        rec.folder = folder;
        rec.frames = frames;
        rec.every = Mathf.Max(1, every);
        rec.target = new RenderTexture(width, height, 24) { filterMode = FilterMode.Point };
        rec.readback = new Texture2D(width, height, TextureFormat.RGB24, false);
        Time.captureFramerate = fps;
        current = rec;
    }

    IEnumerator Start()
    {
        while (saved < frames)
        {
            yield return new WaitForEndOfFrame();
            if (rendered++ % every == 0) Capture();
        }
        Destroy(gameObject);
    }

    void Capture()
    {
        var cam = Camera.main;
        if (cam == null) return;
        var previous = cam.targetTexture;
        cam.targetTexture = target;
        cam.Render();
        cam.targetTexture = previous;

        var active = RenderTexture.active;
        RenderTexture.active = target;
        readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        readback.Apply();
        RenderTexture.active = active;
        File.WriteAllBytes(Path.Combine(folder, saved.ToString("D4") + ".png"), readback.EncodeToPNG());
        saved++;
    }

    void OnDestroy()
    {
        if (current == this) current = null;
        Time.captureFramerate = 0;
        if (target != null) target.Release();
    }
}
