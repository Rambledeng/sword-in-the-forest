using Godot;
using SwordInTheForest.Combat;

namespace SwordInTheForest.Actors;

/// <summary>
/// Drawn warnings beside the glint: the archer's aim line (faint while it aims, bright once it
/// glints), a red line along a thrust or charge after its glint, and the landing ring of a thrown jar.
/// </summary>
public partial class Telegraph : Node2D
{
    private readonly Enemy _enemy;

    public Telegraph(Enemy enemy) => _enemy = enemy;

    public Telegraph() => _enemy = null!;

    public override void _Draw()
    {
        var e = _enemy;
        var a = e.CurrentAttack;
        if (a == null || e.Current != Enemy.State.Attack) return;
        var phase = e.Runner.Phase;
        var lead = phase == AttackPhase.Lead;
        var from = Enemy.BodyCenter;
        if (a.Projectile == "arrow")
        {
            var dir = lead ? e.AimDir : e.Facing;
            var len = e.ShotRangeFor(dir, a.ReachPx);
            var c = lead ? new Color(1f, 0.95f, 0.8f, 0.75f) : new Color(1f, 0.9f, 0.7f, 0.28f);
            DrawDashed(from + dir * 6, from + dir * len, c, lead ? 1.5f : 1f);
            return;
        }
        if (!lead || e.InSwing) return;
        if (a.IsArea)
        {
            var at = ToLocal(e.AreaTarget);
            var c = a.Red ? new Color(1f, 0.35f, 0.3f, 0.6f) : new Color(0.75f, 0.5f, 1f, 0.6f);
            DrawArc(at, a.WidthPx, 0, Mathf.Tau, 24, c, 1f);
            DrawCircle(at, a.WidthPx, new Color(c.R, c.G, c.B, 0.15f));
        }
        else if (a.IsLine && a.Red)
        {
            DrawLine(from + e.AimDir * 6, from + e.AimDir * a.ReachPx, new Color(1f, 0.3f, 0.25f, 0.45f), a.WidthPx * 0.6f);
        }
    }

    private void DrawDashed(Vector2 a, Vector2 b, Color c, float width)
    {
        var len = a.DistanceTo(b);
        var dir = (b - a) / Mathf.Max(len, 0.01f);
        for (var d = 0f; d < len; d += 6) DrawLine(a + dir * d, a + dir * Mathf.Min(d + 3.5f, len), c, width);
    }
}
