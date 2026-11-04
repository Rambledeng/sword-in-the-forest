using System;
using Godot;
using SwordInTheForest.Run;

namespace SwordInTheForest.Fx;

/// <summary>
/// Weather for a room, drawn above the actors: bamboo leaves twirling down in the bamboo forest,
/// pink petals drifting outside the tavern, and dust motes in the lantern light of the tavern.
/// Particles wrap around the room, so the count stays fixed.
/// </summary>
public partial class Atmosphere : Node2D
{
    private const string FxDir = "res://assets/third_party/ninja_adventure/fx";
    private readonly Area _area;
    private readonly Vector2 _size;
    private readonly int _seed;
    private Texture2D? _sheet;
    private Particle[] _p = Array.Empty<Particle>();
    private double _t;

    private struct Particle
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public float Phase;
        public int Frame;
        public float Spin;
    }

    public Atmosphere(Area area, Vector2 size, int seed)
    {
        _area = area;
        _size = size;
        _seed = seed;
    }

    public Atmosphere() { }

    public int Count => _p.Length;

    public override void _Ready()
    {
        ZIndex = 30;
        var rng = new Random(_seed);
        var (count, sheet) = _area switch
        {
            Area.Outside => (26, "petal.png"),
            Area.Tavern => (40, null),
            _ => (38, "bamboo_particle.png"),
        };
        if (sheet != null) _sheet = GD.Load<Texture2D>($"{FxDir}/{sheet}");
        _p = new Particle[count];
        for (var i = 0; i < count; i++)
        {
            var p = new Particle
            {
                Pos = new Vector2((float)rng.NextDouble() * _size.X, (float)rng.NextDouble() * _size.Y),
                Phase = (float)(rng.NextDouble() * Math.PI * 2),
                Frame = rng.Next(6),
                Spin = (float)(rng.NextDouble() * 2 - 1),
            };
            p.Vel = _area switch
            {
                Area.Outside => new Vector2(12 + (float)rng.NextDouble() * 10, 8 + (float)rng.NextDouble() * 6),
                Area.Tavern => new Vector2((float)rng.NextDouble() * 4 - 2, -2 - (float)rng.NextDouble() * 3),
                _ => new Vector2(4 + (float)rng.NextDouble() * 6, 14 + (float)rng.NextDouble() * 10),
            };
            _p[i] = p;
        }
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _t += delta;
        for (var i = 0; i < _p.Length; i++)
        {
            ref var p = ref _p[i];
            var sway = Mathf.Sin((float)_t * 1.3f + p.Phase) * (_area == Area.Tavern ? 3f : 10f);
            p.Pos += (p.Vel + new Vector2(sway, 0)) * dt;
            // Wrap with a margin, so particles enter from off-screen.
            if (p.Pos.Y > _size.Y + 10) p.Pos.Y = -10;
            if (p.Pos.Y < -10) p.Pos.Y = _size.Y + 10;
            if (p.Pos.X > _size.X + 10) p.Pos.X = -10;
            if (p.Pos.X < -10) p.Pos.X = _size.X + 10;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        for (var i = 0; i < _p.Length; i++)
        {
            var p = _p[i];
            var at = p.Pos.Round();
            switch (_area)
            {
                case Area.Tavern:
                {
                    // Dust in the lantern light: a soft glow that comes and goes.
                    var a = 0.3f + 0.25f * Mathf.Sin((float)_t * 0.9f + p.Phase);
                    DrawRect(new Rect2(at, new Vector2(i % 4 == 0 ? 2 : 1, i % 4 == 0 ? 2 : 1)), new Color(1f, 0.92f, 0.7f, a));
                    break;
                }
                case Area.Outside:
                {
                    // Six frames of a petal tumbling.
                    var frame = (p.Frame + (int)(_t * 6 + p.Phase)) % 6;
                    DrawTextureRectRegion(_sheet!, new Rect2(at - new Vector2(6, 3), new Vector2(12, 7)), new Rect2(frame * 12, 0, 12, 7), new Color(1, 1, 1, 0.9f));
                    break;
                }
                default:
                {
                    // The sheet's two small leaves, turning as they fall.
                    var frame = 4 + (p.Frame % 2);
                    // Dry, yellowed bitter-bamboo leaves read against the grass.
                    DrawSetTransform(at, (float)_t * p.Spin * 2 + p.Phase, Vector2.One);
                    var tint = i % 3 == 0 ? new Color(1f, 0.85f, 0.45f) : new Color(1.25f, 1.15f, 0.55f);
                    DrawTextureRectRegion(_sheet!, new Rect2(-8, -7, 16, 15), new Rect2(frame * 16, 0, 16, 15), tint);
                    DrawSetTransform(Vector2.Zero, 0, Vector2.One);
                    break;
                }
            }
        }
    }
}
