using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Fx;

namespace SwordInTheForest.Actors;

/// <summary>
/// The visible arrow or jar of a ranged attack. It flies during the attack's swing and arrives on
/// the impact frame; the attack itself is decided by the runner like any other, so the glint and
/// the response window are the same as for a cut. Deflected, it spins away.
/// </summary>
public partial class Projectile : Sprite2D
{
    private const string WeaponDir = "res://assets/third_party/ninja_adventure/weapons";
    private Vector2 _from;
    private Vector2 _to;
    private double _flightMs;
    private double _t;
    private bool _arc;
    private bool _landed;
    private Vector2 _away;
    private double _afterMs;
    private string _kind = "";

    /// <summary>It flies into a wall or obstacle and stays there.</summary>
    public bool Stuck { get; set; }

    public static Projectile Launch(string kind, Vector2 from, Vector2 to, double flightMs, bool arc)
    {
        var p = new Projectile
        {
            Texture = GD.Load<Texture2D>($"{WeaponDir}/{(kind == "jar" ? "wine_jar" : "arrow")}.png"),
            TextureFilter = TextureFilterEnum.Nearest,
            ZIndex = 14,
            _from = from,
            _to = to,
            _flightMs = Mathf.Max(1, flightMs),
            _arc = arc,
            _kind = kind,
        };
        FxLayer.Add(p, from);
        p.Rotation = kind == "arrow" ? (to - from).Angle() : 0;
        return p;
    }

    /// <summary>The impact: <paramref name="hit"/> struck the player; <paramref name="deflected"/> was batted away or blocked.</summary>
    public void Land(bool hit, bool deflected)
    {
        if (_landed) return;
        _landed = true;
        GlobalPosition = _to;
        var dir = (_to - _from).Normalized();
        if (deflected)
        {
            _away = (-dir).Rotated((float)GD.RandRange(-0.6, 0.6)) * 150f;
            return;
        }
        if (_kind == "jar")
        {
            Sfx.Play("jar_break", 0.05f);
            SheetFx.Spawn("smoke", 6, 16, _to, modulate: new Color(0.8f, 0.6f, 1f), z: 13);
            QueueFree();
            return;
        }
        if (hit) { QueueFree(); return; }
        if (Stuck)
        {
            // Into the wood: it stays a moment, then fades.
            SheetFx.Spawn("smoke", 6, 8, _to, z: 13);
            _away = Vector2.Zero;
            _afterMs = -600;
            return;
        }
        _away = dir * 220f; // a miss flies on
    }

    public override void _Process(double delta)
    {
        if (GameClock.Instance.Frozen) return;
        var dt = delta * 1000.0;
        if (!_landed)
        {
            _t = Mathf.Min(_t + dt, _flightMs);
            var f = (float)(_t / _flightMs);
            var pos = _from.Lerp(_to, f);
            if (_arc)
            {
                pos.Y -= 4 * 26 * f * (1 - f);
                Rotation += (float)(dt * 0.02);
            }
            GlobalPosition = pos;
            return;
        }
        _afterMs += dt;
        GlobalPosition += _away * (float)(dt / 1000.0);
        if (_away != Vector2.Zero && _kind == "arrow") Rotation = _away.Angle() + (float)_afterMs * 0.02f;
        Modulate = new Color(1, 1, 1, Mathf.Clamp(1f - (float)(Mathf.Max(0, _afterMs) / 400.0), 0f, 1f));
        if (_afterMs > 400) QueueFree();
    }
}
