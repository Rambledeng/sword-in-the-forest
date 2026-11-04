using System;
using Godot;
using SwordInTheForest.Core;

namespace SwordInTheForest.Fx;

/// <summary>
/// Merit and karma without numbers: a short gold glitter rising around the player when merit is
/// gained, a wisp of dark smoke curling up when karma is gained. Drawn in code, follows the player
/// (it is the player's child) and runs on game time, so it freezes during hit-stop.
/// </summary>
public partial class MeritFx : Node2D
{
    private readonly bool _karma;
    private readonly double _durationMs;
    private readonly (Vector2 From, float Rise, float Delay, float Size, float Sway)[] _bits;
    private double _elapsedMs;

    public MeritFx(bool karma, int seed)
    {
        _karma = karma;
        var f = Tunables.Feel;
        _durationMs = karma ? f.KarmaFxMs : f.MeritFxMs;
        var rng = new Random(seed);
        var n = karma ? f.KarmaFxPuffs : f.MeritFxSparkles;
        _bits = new (Vector2, float, float, float, float)[n];
        for (var i = 0; i < n; i++)
        {
            var from = karma
                ? new Vector2((float)(rng.NextDouble() * 8 - 4), (float)(rng.NextDouble() * 4 - 2))
                : new Vector2((float)(rng.NextDouble() * 18 - 9), (float)(rng.NextDouble() * 14 - 7));
            _bits[i] = (from, (float)(karma ? 14 + rng.NextDouble() * 10 : 6 + rng.NextDouble() * 10),
                (float)(rng.NextDouble() * 0.45), (float)(karma ? 2.5 + rng.NextDouble() * 2 : 1 + Math.Round(rng.NextDouble())),
                (float)(rng.NextDouble() * Math.PI * 2));
        }
        ZIndex = 20;
    }

    public MeritFx() => _bits = Array.Empty<(Vector2, float, float, float, float)>();

    /// <summary>Plays on <paramref name="target"/> at <paramref name="localAt"/> (its body centre).</summary>
    public static MeritFx Play(Node2D target, Vector2 localAt, bool karma, int seed)
    {
        var fx = new MeritFx(karma, seed) { Position = localAt };
        target.AddChild(fx);
        return fx;
    }

    public override void _Process(double delta)
    {
        if (GameClock.Instance.Frozen) return;
        _elapsedMs += delta * 1000.0;
        if (_elapsedMs >= _durationMs)
        {
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        var t = (float)(_elapsedMs / _durationMs);
        foreach (var (from, rise, delay, size, sway) in _bits)
        {
            var k = Mathf.Clamp((t - delay) / (1 - delay), 0f, 1f);
            if (k <= 0 || k >= 1) continue;
            if (_karma)
            {
                // A dark wisp: puffs rise from the feet, sway, swell and thin out.
                var p = from + new Vector2(Mathf.Sin(sway + k * 5f) * 3f, 6 - rise * k);
                var a = 0.55f * Mathf.Sin(Mathf.Pi * k);
                DrawCircle(p.Round(), size * (0.6f + k), new Color(0.12f, 0.08f, 0.14f, a));
                DrawCircle((p + new Vector2(0.5f, -0.5f)).Round(), size * (0.3f + 0.6f * k), new Color(0.25f, 0.15f, 0.3f, a * 0.6f));
            }
            else
            {
                // Gold glitter: specks drift up and twinkle.
                var p = (from + new Vector2(0, -rise * k)).Round();
                var twinkle = 0.5f + 0.5f * Mathf.Sin(sway + k * 18f);
                var a = Mathf.Sin(Mathf.Pi * k) * (0.5f + 0.5f * twinkle);
                var c = new Color(1f, 0.85f, 0.35f, a);
                DrawRect(new Rect2(p, new Vector2(size, size)), c);
                if (twinkle > 0.8f)
                {
                    DrawRect(new Rect2(p + new Vector2(-1, 0), new Vector2(size + 2, 1)), new Color(1f, 0.95f, 0.7f, a * 0.7f));
                    DrawRect(new Rect2(p + new Vector2(0, -1), new Vector2(1, size + 2)), new Color(1f, 0.95f, 0.7f, a * 0.7f));
                }
            }
        }
    }
}
