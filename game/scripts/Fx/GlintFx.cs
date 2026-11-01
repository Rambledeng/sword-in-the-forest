using Godot;
using SwordInTheForest.Core;

namespace SwordInTheForest.Fx;

/// <summary>
/// The glint (剑光): a four-point star at the weapon tip, brightest on the frame it appears, with a
/// ring that snaps outward. White glints are pale gold; red ones (杀招) are red with a second ring.
/// </summary>
public partial class GlintFx : Node2D
{
    private readonly bool _red;
    private readonly float _durationMs;
    private readonly System.Func<Vector2>? _anchor;
    private double _elapsedMs;

    public GlintFx(bool red, float durationMs, System.Func<Vector2> anchor)
    {
        _red = red;
        _durationMs = durationMs;
        _anchor = anchor;
        ZIndex = 30;
    }

    public GlintFx() { }

    public static void Spawn(bool red, System.Func<Vector2> anchor) =>
        FxLayer.Add(new GlintFx(red, Tunables.Feel.GlintVisibleMs, anchor), anchor());

    public override void _Process(double delta)
    {
        _elapsedMs += delta * 1000.0;
        if (_elapsedMs >= _durationMs) { QueueFree(); return; }
        if (_anchor != null) GlobalPosition = _anchor().Round();
        QueueRedraw();
    }

    public override void _Draw()
    {
        var t = (float)(_elapsedMs / _durationMs);
        var main = _red ? new Color(1f, 0.18f, 0.15f) : new Color(1f, 0.97f, 0.8f);
        var core = _red ? new Color(1f, 0.85f, 0.8f) : new Color(1f, 1f, 1f);
        // Pop to full size immediately, then shrink.
        var size = t < 0.15f ? 1f : 1f - (t - 0.15f) / 0.85f;
        var alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
        var lon = 13f * size;
        var lat = 2.2f * size;
        Star(lon, lat, new Color(main, alpha));
        Star(lon * 0.55f, lat * 0.7f, new Color(main, alpha), Mathf.Pi / 4);
        DrawCircle(Vector2.Zero, 2.2f * size, new Color(core, alpha));
        var ring = 3f + 16f * Mathf.Min(1f, t / 0.5f);
        var ringAlpha = Mathf.Max(0f, 1f - t / 0.5f);
        DrawArc(Vector2.Zero, ring, 0, Mathf.Tau, 24, new Color(main, ringAlpha), -1f);
        if (_red) DrawArc(Vector2.Zero, ring * 0.6f, 0, Mathf.Tau, 20, new Color(main, ringAlpha), -1f);
    }

    private void Star(float lon, float lat, Color c, float rot = 0)
    {
        for (var i = 0; i < 2; i++)
        {
            var a = Vector2.Right.Rotated(rot + i * Mathf.Pi / 2) * lon;
            var b = Vector2.Right.Rotated(rot + i * Mathf.Pi / 2 + Mathf.Pi / 2) * lat;
            DrawColoredPolygon(new[] { a, b, -a, -b }, c);
        }
    }
}
