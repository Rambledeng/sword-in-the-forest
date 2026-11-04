using System;
using Godot;

namespace SwordInTheForest.Intro;

/// <summary>
/// The title's close-up, drawn in code at 480×270: the sword 鸣不平 stabbed in the ground of the
/// bitter-bamboo grove, a peeling seal on its blade and a skeleton slumped beside it. The start
/// sequence drives <see cref="Hum"/>, <see cref="SealFall"/> and <see cref="Grip"/> (0 to 1).
/// </summary>
public partial class TitleArt : Node2D
{
    public static readonly Vector2 SwordBase = new(335, 240);
    private const float Ground = 232;
    private static readonly Vector2 SwordScale = new(1.55f, 1.55f);
    private const float Tilt = -0.07f;
    private Texture2D _skeleton = null!;
    private readonly (float X, float W, float Shade, float Seg)[] _stalks = new (float, float, float, float)[22];
    private readonly (Vector2 At, float Len, float Angle)[] _leaves = new (Vector2, float, float)[40];
    private double _t;

    /// <summary>How hard the sword hums (shakes and glows).</summary>
    public float Hum { get; set; }
    /// <summary>The seal peeling off and falling.</summary>
    public float SealFall { get; set; }
    /// <summary>The soul's hand reaching in and closing on the grip.</summary>
    public float Grip { get; set; }

    public override void _Ready()
    {
        _skeleton = GD.Load<Texture2D>("res://assets/third_party/ninja_adventure/actors/skeleton.png");
        var rng = new Random(9);
        for (var i = 0; i < _stalks.Length; i++)
        {
            var near = i >= 14;
            _stalks[i] = ((float)rng.NextDouble() * 500 - 10, near ? 9 + rng.Next(4) : 4 + rng.Next(3), near ? 1f : 0.45f + (float)rng.NextDouble() * 0.3f, 26 + rng.Next(18));
        }
        for (var i = 0; i < _leaves.Length; i++)
            _leaves[i] = (new Vector2((float)rng.NextDouble() * 480, (float)rng.NextDouble() * 150), 5 + (float)rng.NextDouble() * 5, (float)(rng.NextDouble() * 0.8 + 0.3) * (rng.Next(2) == 0 ? 1 : -1));
    }

    public override void _Process(double delta)
    {
        _t += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawGrove();
        DrawSkeleton();
        DrawSword();
        DrawHand();
        // Mist over the ground.
        for (var i = 0; i < 4; i++)
        {
            var y = Ground - 18 + i * 6 + Mathf.Sin((float)_t * 0.4f + i) * 2;
            DrawRect(new Rect2(0, y, 480, 5), new Color(0.7f, 0.85f, 0.75f, 0.04f));
        }
    }

    private void DrawGrove()
    {
        for (var y = 0; y < Ground + 2; y += 6)
        {
            var k = y / Ground;
            DrawRect(new Rect2(0, y, 480, 6), new Color(0.04f + 0.05f * k, 0.08f + 0.07f * k, 0.06f + 0.04f * k));
        }
        foreach (var (x, w, shade, seg) in _stalks)
        {
            var sway = Mathf.Sin((float)_t * 0.5f + x * 0.05f) * (shade < 1 ? 0.6f : 1.2f);
            var body = new Color(0.12f, 0.3f, 0.16f).Lerp(new Color(0.3f, 0.5f, 0.22f), shade - 0.45f) * new Color(shade, shade, shade, 1f);
            body.A = 1;
            var px = Mathf.Round(x + sway);
            DrawRect(new Rect2(px, 0, w, Ground + 4), body);
            DrawRect(new Rect2(px + 1, 0, Mathf.Max(1, w * 0.25f), Ground + 4), body.Lightened(0.15f));
            for (var y = (int)(seg * 0.6f); y < Ground; y += (int)seg)
                DrawRect(new Rect2(px - 1, y, w + 2, 2), body.Darkened(0.35f));
        }
        foreach (var (at, len, angle) in _leaves)
        {
            var a = at + new Vector2(Mathf.Sin((float)_t * 0.7f + at.X) * 1.5f, 0);
            DrawLine(a, a + Vector2.Right.Rotated(angle) * len, new Color(0.25f, 0.45f, 0.2f, 0.8f), 2);
        }
        // The ground, a little mound at the sword's foot, tufts of grass.
        DrawRect(new Rect2(0, Ground, 480, 270 - Ground), new Color(0.13f, 0.17f, 0.09f));
        DrawRect(new Rect2(0, Ground, 480, 2), new Color(0.2f, 0.27f, 0.12f));
        var foot = new Vector2(SwordBase.X - 6, Ground + 2);
        DrawColoredPolygon(new[] { foot + new Vector2(-34, 2), foot + new Vector2(-14, -4), foot + new Vector2(14, -4), foot + new Vector2(34, 2) }, new Color(0.18f, 0.15f, 0.09f));
        var rng = new Random(4);
        for (var i = 0; i < 70; i++)
        {
            var g = new Vector2(rng.Next(480), Ground + 2 + rng.Next(36));
            var h = 2 + rng.Next(4);
            DrawLine(g, g + new Vector2(rng.Next(3) - 1, -h), new Color(0.24f, 0.36f, 0.15f), 1);
        }
    }

    private void DrawSkeleton()
    {
        // Slumped against nothing, long dead: the crouching frame, greyed, three times the size.
        // Lying on its side at the sword's foot, its hand still reaching for the hilt.
        var src = new Rect2(0, 5 * 16, 16, 16);
        DrawSetTransform(new Vector2(392, Ground + 4), -1.45f, new Vector2(3.2f, 3.2f));
        DrawTextureRectRegion(_skeleton, new Rect2(0, -16, 16, 16), src, new Color(0.55f, 0.57f, 0.53f));
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        var bone = new Color(0.7f, 0.7f, 0.63f);
        DrawLine(new Vector2(372, Ground - 6), new Vector2(352, Ground - 14), bone, 2);
        DrawLine(new Vector2(352, Ground - 14), new Vector2(346, Ground - 12), bone, 1);
        DrawLine(new Vector2(446, Ground + 6), new Vector2(460, Ground + 9), bone, 2);
        // Its old straw hat.
        DrawColoredPolygon(new[] { new Vector2(420, Ground + 10), new Vector2(438, Ground - 2), new Vector2(456, Ground + 10) }, new Color(0.5f, 0.41f, 0.22f));
    }

    private void DrawSword()
    {
        var shake = Hum > 0 ? new Vector2(Mathf.Sin((float)_t * 90f), Mathf.Cos((float)_t * 70f)) * Hum * 1.2f : Vector2.Zero;
        DrawSetTransform(SwordBase + shake, Tilt, SwordScale);
        if (Hum > 0)
        {
            // A cold glow around the blade.
            for (var i = 3; i >= 1; i--)
                DrawRect(new Rect2(-4 - i * 3, -150 - i * 3, 8 + i * 6, 152 + i * 3), new Color(0.6f, 0.85f, 1f, 0.06f * Hum));
        }
        // Blade (its tip is in the ground), spine, edge, rust.
        DrawColoredPolygon(new[] { new Vector2(-4, 4), new Vector2(-4, -96), new Vector2(4, -96), new Vector2(4, 4) }, new Color(0.62f, 0.66f, 0.7f));
        DrawRect(new Rect2(-1, -96, 1, 100), new Color(0.45f, 0.48f, 0.52f));
        DrawRect(new Rect2(3, -96, 1, 100), new Color(0.9f, 0.93f, 0.96f));
        foreach (var (y, w) in new[] { (-30f, 2f), (-58f, 3f), (-12f, 2f) })
            DrawRect(new Rect2(-3, y, w, 2), new Color(0.45f, 0.28f, 0.16f, 0.8f));
        // Guard (镡), grip wrapped in faded cord, pommel ring, red tassel.
        DrawRect(new Rect2(-13, -102, 26, 6), new Color(0.42f, 0.32f, 0.16f));
        DrawRect(new Rect2(-13, -102, 26, 1), new Color(0.65f, 0.52f, 0.28f));
        DrawRect(new Rect2(-3, -140, 6, 38), new Color(0.18f, 0.1f, 0.08f));
        for (var y = -138; y < -104; y += 5)
            DrawColoredPolygon(new[] { new Vector2(-3, y), new Vector2(0, y + 2), new Vector2(3, y), new Vector2(0, y - 2) }, new Color(0.42f, 0.18f, 0.14f));
        DrawArc(new Vector2(0, -145), 4, 0, Mathf.Tau, 10, new Color(0.6f, 0.48f, 0.24f), 2);
        var sway = Mathf.Sin((float)_t * 1.6f) * 3 + Hum * Mathf.Sin((float)_t * 30f) * 2;
        for (var i = -1; i <= 1; i++)
            DrawPolyline(new[] { new Vector2(i, -141), new Vector2(6 + i + sway * 0.5f, -128), new Vector2(9 + i + sway, -112) }, new Color(0.7f, 0.12f, 0.1f), 1);
        DrawSeal();
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private void DrawSeal()
    {
        // A yellow paper talisman (符) pasted across the blade, its top corner peeling in the wind.
        var fall = SealFall;
        var drop = new Vector2(fall * 18, fall * fall * 110);
        var alpha = 1f - Mathf.Clamp((fall - 0.6f) / 0.4f, 0f, 1f);
        if (alpha <= 0) return;
        var paper = new Color(0.9f, 0.8f, 0.42f, alpha);
        var ink = new Color(0.75f, 0.1f, 0.08f, alpha);
        var peel = 0.25f + Mathf.Sin((float)_t * 2.2f) * 0.12f + fall * 1.2f;
        var o = new Vector2(-6, -84) + drop;
        DrawSetTransform(SwordBase + (o * SwordScale).Rotated(Tilt), Tilt + fall * 0.9f, SwordScale);
        DrawRect(new Rect2(0, 10, 12, 36), paper);
        // The peeling top flap.
        var hinge = new Vector2(0, 10);
        var flap = new[] { hinge, hinge + new Vector2(12, 0), hinge + new Vector2(12, -10).Rotated(-peel * 0.6f), hinge + new Vector2(0, -10).Rotated(-peel) };
        DrawColoredPolygon(flap, paper.Darkened(0.12f));
        // Red strokes, like 敕令 written fast.
        DrawRect(new Rect2(5, 14, 2, 26), ink);
        DrawRect(new Rect2(2, 18, 8, 2), ink);
        DrawRect(new Rect2(3, 26, 6, 1), ink);
        DrawRect(new Rect2(2, 33, 8, 2), ink);
        DrawArc(new Vector2(6, 42), 2.5f, 0, Mathf.Tau, 8, ink, 1);
    }

    private void DrawHand()
    {
        if (Grip <= 0) return;
        // The wandering soul's pale hand and grey sleeve reach in from the right and close on the grip.
        var reach = Mathf.Clamp(Grip * 1.25f, 0f, 1f);
        var close = Mathf.Clamp((Grip - 0.8f) / 0.2f, 0f, 1f);
        var grip = SwordBase + (new Vector2(0, -122) * SwordScale).Rotated(Tilt);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        var at = new Vector2(Mathf.Lerp(560, grip.X + 4, reach * reach * (3 - 2 * reach)), grip.Y + 3);
        DrawSetTransform(at, 0, SwordScale);
        var hand = Vector2.Zero;
        var sleeve = new Color(0.32f, 0.33f, 0.36f);
        var skin = new Color(0.82f, 0.86f, 0.88f, 0.92f);
        DrawColoredPolygon(new[] { hand + new Vector2(10, -9), hand + new Vector2(200, -18), hand + new Vector2(200, 22), hand + new Vector2(10, 11) }, sleeve);
        DrawRect(new Rect2(hand + new Vector2(8, -10), new Vector2(4, 22)), sleeve.Darkened(0.3f));
        DrawRect(new Rect2(hand + new Vector2(-2, -8), new Vector2(11, 17)), skin);
        // Fingers: open while reaching, wrapped around the grip once it closes.
        for (var i = 0; i < 4; i++)
        {
            var y = -7 + i * 4;
            var len = Mathf.Lerp(6, 11, close);
            DrawRect(new Rect2(hand + new Vector2(-2 - len, y), new Vector2(len, 3)), skin.Darkened(0.06f * i));
        }
        DrawRect(new Rect2(hand + new Vector2(-6, -11), new Vector2(7, 3)), skin);
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
