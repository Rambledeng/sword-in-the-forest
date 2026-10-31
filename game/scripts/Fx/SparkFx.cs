using Godot;
using SwordInTheForest.Core;

namespace SwordInTheForest.Fx;

/// <summary>
/// Code-drawn burst of rays and a ring: the deflect spark (gold, big), the block spark (grey,
/// small) and the hit burst. Holds its first, brightest frame through hit-stop.
/// </summary>
public partial class SparkFx : Node2D
{
    private readonly int _rays;
    private readonly float _length;
    private readonly Color _color;
    private readonly float _durationMs;
    private readonly float[] _angles;
    private readonly float[] _scales;
    private double _elapsedMs;

    public SparkFx(int rays, float length, Color color, float durationMs, int seed)
    {
        _rays = rays;
        _length = length;
        _color = color;
        _durationMs = durationMs;
        var rng = new RandomNumberGenerator { Seed = (ulong)seed };
        _angles = new float[rays];
        _scales = new float[rays];
        for (var i = 0; i < rays; i++)
        {
            _angles[i] = Mathf.Tau * i / rays + rng.RandfRange(-0.25f, 0.25f);
            _scales[i] = rng.RandfRange(0.55f, 1f);
        }
        ZIndex = 20;
    }

    public SparkFx() { _angles = _scales = System.Array.Empty<float>(); }

    private static int _seed;

    public static void Deflect(Vector2 at) => FxLayer.Add(new SparkFx(12, 19, new Color(1f, 0.92f, 0.55f), 260, ++_seed), at);
    public static void Block(Vector2 at) => FxLayer.Add(new SparkFx(6, 8, new Color(0.85f, 0.88f, 0.95f), 150, ++_seed), at);
    public static void Hit(Vector2 at) => FxLayer.Add(new SparkFx(7, 9, new Color(1f, 1f, 1f), 140, ++_seed), at);
    public static void Finisher(Vector2 at) => FxLayer.Add(new SparkFx(14, 24, new Color(1f, 0.35f, 0.3f), 360, ++_seed), at);

    public override void _Process(double delta)
    {
        if (!GameClock.Instance.Frozen) _elapsedMs += delta * 1000.0;
        if (_elapsedMs >= _durationMs) { QueueFree(); return; }
        QueueRedraw();
    }

    public override void _Draw()
    {
        var t = (float)(_elapsedMs / _durationMs);
        var ease = 1f - (1f - t) * (1f - t);
        var alpha = 1f - t * t;
        var c = new Color(_color, alpha);
        var core = new Color(1, 1, 1, alpha);
        for (var i = 0; i < _rays; i++)
        {
            var dir = Vector2.Right.Rotated(_angles[i]);
            var inner = dir * (_length * 0.25f + _length * 0.5f * ease) * _scales[i];
            var outer = dir * (_length * (0.55f + 0.45f * ease)) * _scales[i];
            DrawLine(inner.Round(), outer.Round(), c, -1f);
        }
        if (t < 0.35f)
        {
            var r = 2f + _length * 0.6f * (t / 0.35f);
            DrawArc(Vector2.Zero, r, 0, Mathf.Tau, 20, new Color(core, alpha * (1f - t / 0.35f)), -1f);
            DrawCircle(Vector2.Zero, Mathf.Max(1f, 3f * (1f - t / 0.35f)), core);
        }
    }
}
