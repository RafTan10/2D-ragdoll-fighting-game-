using UnityEngine;

/// <summary>
/// IMGUI drawing helpers for the menu: a fighter standing in full (assembled from its part sprites, with the same
/// joint spacing as <see cref="RagdollBody"/>), a head icon, and an arena as a full-screen backdrop.
/// Call only from OnGUI.
/// </summary>
public static class MenuArt
{
    /// <summary>Draws a sprite so its pivot lands on <paramref name="at"/> (GUI coordinates), <paramref name="scale"/> screen units per pixel.</summary>
    public static void Sprite(Sprite s, Vector2 at, float scale, bool flip, Color tint, float angle = 0f)
    {
        if (s == null) return;
        if (angle != 0f)
        {
            // Rotate about the pivot (clockwise on screen); composed onto GUI.matrix so the menu's UI scale still applies.
            var saved = GUI.matrix;
            GUI.matrix = saved * Matrix4x4.Translate(at) * Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, angle)) * Matrix4x4.Translate(-at);
            Sprite(s, at, scale, flip, tint);
            GUI.matrix = saved;
            return;
        }
        Rect tr = s.textureRect;
        var tex = s.texture;
        float w = tr.width * scale, h = tr.height * scale;
        float px = s.pivot.x * scale, py = s.pivot.y * scale;
        var r = new Rect(flip ? at.x - (w - px) : at.x - px, at.y - (h - py), w, h);
        var uv = new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height);
        if (flip) uv = new Rect(uv.xMax, uv.y, -uv.width, uv.height);
        var old = GUI.color;
        GUI.color = tint * old;
        GUI.DrawTextureWithTexCoords(r, tex, uv);
        GUI.color = old;
    }

    /// <summary>
    /// The fighter standing on the bottom-centre of <paramref name="area"/>, facing right (or left when flipped),
    /// holding its weapons. Scaled to fit the area's height.
    /// </summary>
    public static void Fighter(Rect area, FighterDefinition def, bool faceLeft, float alpha = 1f)
    {
        if (def == null || def.torso == null) return;
        float H(Sprite s) => s != null ? s.rect.height : 0f;

        // Pixel layout (y up from the feet), as RagdollBody.Build.
        float hip = H(def.lowerLeg) + H(def.upperLeg) - 2f;
        float torsoC = hip + H(def.torso) * 0.5f - 3f;
        float neck = torsoC + H(def.torso) * 0.5f - 3f;
        float shoulder = neck - 5f;
        float knee = hip - (H(def.upperLeg) - 2f);
        float elbow = shoulder - (H(def.upperArm) - 2f);
        float hand = elbow - (H(def.lowerArm) - 4f);

        float dir = faceLeft ? -1f : 1f;
        Color front = new Color(1f, 1f, 1f, alpha);
        Color back = def.backLimbTint; back.a = alpha;
        const float backArmX = 2f, frontArmX = -1f, backLegX = -3f, frontLegX = 3f;

        // Guard pose (angles forward from hanging straight down): the weapon arm bent in front with the blade tilted
        // out, so it isn't hidden behind the torso; a second weapon raised the same way, a shield held across the body.
        var mainArm = new Arm(new Vector2(backArmX, shoulder), H(def.upperArm) - 2f, H(def.lowerArm) - 4f, 30f, 85f, 30f);
        bool offShield = def.offHand != null && def.offHand.IsShield, offWeapon = def.offHand != null && !offShield;
        var offArm = offShield ? new Arm(new Vector2(frontArmX, shoulder), H(def.upperArm) - 2f, H(def.lowerArm) - 4f, 20f, 60f, 0f)
                   : offWeapon ? new Arm(new Vector2(frontArmX, shoulder), H(def.upperArm) - 2f, H(def.lowerArm) - 4f, 15f, 55f, 55f)
                   : new Arm(new Vector2(frontArmX, shoulder), H(def.upperArm) - 2f, H(def.lowerArm) - 4f, 4f, 8f, 0f);

        float top = Mathf.Max(neck + H(def.head), mainArm.WeaponTop(def.mainHand), offArm.WeaponTop(def.offHand));
        float scale = area.height / (top + 4f);
        Vector2 feet = new Vector2(area.center.x, area.yMax);
        Vector2 P(Vector2 p) => feet + new Vector2(p.x * dir * scale, -p.y * scale);
        float Turn(float forward) => -forward * dir; // GUI angle (clockwise) that swings a hanging part forward

        // A soft shadow on the floor.
        GUI.color = new Color(0f, 0f, 0f, 0.35f * alpha);
        GUI.DrawTexture(new Rect(feet.x - 15f * scale, feet.y - 2f * scale, 30f * scale, 4f * scale), Texture2D.whiteTexture);
        GUI.color = Color.white;

        void DrawArm(Arm a, Color tint)
        {
            Sprite(def.upperArm, P(a.shoulder), scale, faceLeft, tint, Turn(a.upper));
            Sprite(def.lowerArm, P(a.Elbow), scale, faceLeft, tint, Turn(a.lower));
        }
        void DrawWeapon(WeaponDefinition w, Arm a, Color tint)
        {
            if (w == null || w.sprite == null) return;
            if (w.IsShield) { Sprite(w.sprite, P(a.Hand + new Vector2(3f, 0f)), scale, faceLeft, tint); return; }
            Sprite(w.sprite, P(a.WeaponPivot(w)), scale, faceLeft, tint, a.weapon * dir);
        }

        // Back to front, like the rig's sorting: cape, main weapon, back arm, back leg, torso, head, front leg,
        // skirt, front arm, hand prop, off-hand item.
        Sprite(def.cape, P(new Vector2(0f, neck - 3f)), scale, faceLeft, front);
        DrawWeapon(def.mainHand, mainArm, back);
        DrawArm(mainArm, back);
        Sprite(def.upperLeg, P(new Vector2(backLegX, hip)), scale, faceLeft, back);
        Sprite(def.lowerLeg, P(new Vector2(backLegX, knee)), scale, faceLeft, back);
        Sprite(def.torso, P(new Vector2(0f, torsoC)), scale, faceLeft, front);
        Sprite(def.head, P(new Vector2(0f, neck)), scale, faceLeft, front);
        Sprite(def.upperLeg, P(new Vector2(frontLegX, hip)), scale, faceLeft, front);
        Sprite(def.lowerLeg, P(new Vector2(frontLegX, knee)), scale, faceLeft, front);
        Sprite(def.skirt, P(new Vector2(0f, hip)), scale, faceLeft, front);
        DrawArm(offArm, front);
        if (def.handProp != null) Sprite(def.handProp, P(offArm.Hand + new Vector2(0f, 2f)), scale, faceLeft, front);
        DrawWeapon(def.offHand, offArm, front);
    }

    /// <summary>A posed arm in pixel space (y up, facing right): angles are degrees forward from hanging down.</summary>
    struct Arm
    {
        public Vector2 shoulder; public float upperLen, lowerLen, upper, lower, weapon;
        public Arm(Vector2 shoulder, float upperLen, float lowerLen, float upper, float lower, float weapon)
        { this.shoulder = shoulder; this.upperLen = upperLen; this.lowerLen = lowerLen; this.upper = upper; this.lower = lower; this.weapon = weapon; }
        static Vector2 Down(float deg) => new Vector2(Mathf.Sin(deg * Mathf.Deg2Rad), -Mathf.Cos(deg * Mathf.Deg2Rad));
        public Vector2 Elbow => shoulder + Down(upper) * upperLen;
        public Vector2 Hand => Elbow + Down(lower) * lowerLen;
        Vector2 Up => new Vector2(Mathf.Sin(weapon * Mathf.Deg2Rad), Mathf.Cos(weapon * Mathf.Deg2Rad)); // blade direction
        /// <summary>Where the weapon sprite's pivot (bottom of the grip) goes so the grip sits in the hand.</summary>
        public Vector2 WeaponPivot(WeaponDefinition w) => Hand - Up * w.gripPixels;
        public float WeaponTop(WeaponDefinition w) =>
            w == null || w.sprite == null || w.IsShield ? 0f : (WeaponPivot(w) + Up * w.sprite.rect.height).y;
    }

    /// <summary>The fighter's head, fitted into <paramref name="box"/>.</summary>
    public static void Head(Rect box, FighterDefinition def, float alpha = 1f)
    {
        if (def == null || def.head == null) return;
        Rect tr = def.head.rect;
        float scale = Mathf.Min(box.width / tr.width, box.height / tr.height);
        Vector2 bottom = new Vector2(box.center.x, box.center.y + tr.height * scale * 0.5f);
        // Head pivot is the neck (bottom-centre); shift so the sprite's middle sits in the box.
        Vector2 at = bottom + new Vector2((def.head.pivot.x - tr.width * 0.5f) * scale, -def.head.pivot.y * scale);
        Sprite(def.head, at, scale, false, new Color(1f, 1f, 1f, alpha));
    }

    /// <summary>All four arena layers stretched to cover <paramref name="screen"/> (keeping 16:9, cropped).</summary>
    public static void Arena(Rect screen, ArenaDefinition arena)
    {
        if (arena == null) return;
        float aspect = 16f / 9f;
        float w = Mathf.Max(screen.width, screen.height * aspect), h = w / aspect;
        var r = new Rect(screen.center.x - w * 0.5f, screen.yMax - h, w, h); // keep the floor in view
        foreach (var layer in new[] { arena.sky, arena.far, arena.near, arena.ground })
        {
            if (layer == null) continue;
            Rect tr = layer.textureRect; var tex = layer.texture;
            GUI.DrawTextureWithTexCoords(r, tex, new Rect(tr.x / tex.width, tr.y / tex.height, tr.width / tex.width, tr.height / tex.height));
        }
    }

    public static void Fill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    public static void Frame(Rect r, Color c, float t = 2f)
    {
        Fill(new Rect(r.x, r.y, r.width, t), c);
        Fill(new Rect(r.x, r.yMax - t, r.width, t), c);
        Fill(new Rect(r.x, r.y, t, r.height), c);
        Fill(new Rect(r.xMax - t, r.y, t, r.height), c);
    }
}
