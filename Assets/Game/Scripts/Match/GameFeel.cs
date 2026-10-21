using UnityEngine;

/// <summary>
/// Hit feedback that isn't a particle: freeze-frames (hit-stop), knockout slow motion, and generated hit sounds.
/// One place owns <see cref="Time.timeScale"/> so the effects don't fight each other. Call <see cref="Tick"/> every frame.
/// </summary>
public static class GameFeel
{
    static float hitStopUntil, slowMoUntil;

    /// <summary>Briefly freeze the action (a heavy hit lands).</summary>
    public static void HitStop(float seconds) => hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + seconds);

    /// <summary>Slow motion, e.g. on a knockout.</summary>
    public static void SlowMo(float seconds) => slowMoUntil = Mathf.Max(slowMoUntil, Time.unscaledTime + seconds);

    public static void Tick()
    {
        float now = Time.unscaledTime;
        float scale = now < hitStopUntil ? 0.06f : now < slowMoUntil ? 0.35f : 1f;
        if (!Mathf.Approximately(Time.timeScale, scale))
        {
            Time.timeScale = scale;
            Time.fixedDeltaTime = 0.02f * Mathf.Max(scale, 0.35f);
        }
    }

    public static void Reset()
    {
        hitStopUntil = slowMoUntil = 0f;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    // ================================================================ sound

    public enum Sound { Flesh, Metal, Wood, Whoosh }

    static AudioSource source;
    static AudioClip flesh, metal, wood, whoosh;

    public static void Play(Sound sound, float volume)
    {
        if (source == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            source = cam.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            BuildClips();
        }
        var clip = sound == Sound.Flesh ? flesh : sound == Sound.Metal ? metal : sound == Sound.Wood ? wood : whoosh;
        source.pitch = Random.Range(0.88f, 1.12f);
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // Small synthesized clips so the project needs no audio assets.
    static void BuildClips()
    {
        const int rate = 22050;
        var rng = new System.Random(3);
        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        flesh = Make("HitFlesh", rate, 0.22f, (t, i) =>
        {
            float env = Mathf.Exp(-t * 22f);
            return (Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.8f + Noise() * 0.5f) * env;
        });
        metal = Make("HitMetal", rate, 0.45f, (t, i) =>
        {
            float env = Mathf.Exp(-t * 9f);
            float ring = Mathf.Sin(2f * Mathf.PI * 1180f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 1730f * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * 2950f * t);
            return (ring * 0.35f + Noise() * 0.4f * Mathf.Exp(-t * 60f)) * env;
        });
        wood = Make("HitWood", rate, 0.25f, (t, i) =>
        {
            float env = Mathf.Exp(-t * 18f);
            return (Mathf.Sin(2f * Mathf.PI * 180f * t) * 0.7f + Mathf.Sin(2f * Mathf.PI * 410f * t) * 0.3f + Noise() * 0.35f) * env;
        });
        float lp = 0f;
        whoosh = Make("Whoosh", rate, 0.3f, (t, i) =>
        {
            lp += (Noise() - lp) * 0.08f; // low-passed noise
            return lp * Mathf.Sin(Mathf.PI * t / 0.3f) * 1.6f;
        });
    }

    static AudioClip Make(string name, int rate, float seconds, System.Func<float, int, float> sample)
    {
        int n = Mathf.CeilToInt(rate * seconds);
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(sample(i / (float)rate, i), -1f, 1f);
        var clip = AudioClip.Create(name, n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
