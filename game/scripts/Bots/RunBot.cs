using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Bots;

/// <summary>
/// Plays a run for the scenarios: walks (around obstacles) to the nearest enemy, deflects every
/// white glint and rolls from red ones (through <see cref="GlintBot"/>), cuts when the enemy is
/// open, spares or finishes beggars, takes the first upgrade, and walks out through the exit.
/// </summary>
public sealed class RunBot
{
    private readonly RunScene _run;
    private readonly BotControls _controls;
    private readonly GlintBot _glints;
    private readonly System.Collections.Generic.Dictionary<Enemy, bool> _decisions = new();

    public RunBot(RunScene run, BotControls controls)
    {
        _run = run;
        _controls = controls;
        _glints = new GlintBot(controls, BotPolicy.Right, 150);
    }

    /// <summary>Decides for each beggar: true spares it, false finishes it.</summary>
    public System.Func<Enemy, bool> SparePolicy { get; set; } = _ => true;
    public int UpgradesChosen { get; private set; }

    public void Attach() => _glints.Attach();
    public void Detach() => _glints.Detach();

    /// <summary>The bot's decision for a beggar, asked of <see cref="SparePolicy"/> once.</summary>
    private bool Decide(Enemy e)
    {
        if (!_decisions.TryGetValue(e, out var spare)) _decisions[e] = spare = SparePolicy(e);
        return spare;
    }

    public void Step()
    {
        if (_run.Upgrades != null)
        {
            _run.Upgrades.Choose(0);
            UpgradesChosen++;
            return;
        }
        var p = _run.Player;
        var room = _run.Room;
        _controls.MoveInput = Vector2.Zero;
        if (room == null || p.IsDead || _run.Summary != null) return;
        var now = GameClock.Instance.NowMs;

        var target = room.Alive.Where(e => GodotObject.IsInstanceValid(e) && !e.IsGone)
            .OrderBy(e => e.BodyGlobal.DistanceSquaredTo(p.BodyGlobal)).FirstOrDefault();
        if (target == null)
        {
            if (room.IsClear) _controls.MoveInput = room.Steer(p.GlobalPosition, room.ExitGlobal);
            return;
        }
        // Spare first: a cut to finish one beggar could also catch one it means to spare.
        if (target.IsBegging && !Decide(target))
        {
            var spare = room.Alive.FirstOrDefault(e => GodotObject.IsInstanceValid(e) && e.IsBegging && Decide(e));
            if (spare != null) target = spare;
        }
        var to = target.BodyGlobal - p.BodyGlobal;
        var dist = to.Length();
        _controls.AimInput = to.Normalized();
        var free = p.Current is Player.State.Free or Player.State.Attack;
        if (target.IsBegging)
        {
            // Spare only acts on the nearest beggar: get closer until that is this one.
            var wrongOne = Decide(target) && p.SpareCandidate() != target;
            if (dist > Tunables.Run.SpareRangePx - 12 || (wrongOne && dist > 8)) _controls.MoveInput = room.Steer(p.GlobalPosition, target.GlobalPosition);
            else if (p.Current == Player.State.Free && _controls.Pending == 0)
            {
                _controls.Press(Decide(target) ? PlayerAction.Interact : PlayerAction.Attack, now, 30);
            }
            return;
        }
        var threatened = p.OpenThreats.Count > 0 || (target.Current == Enemy.State.Attack && target is not Archer && dist < 60);
        if (!threatened && dist > 20) _controls.MoveInput = room.Steer(p.GlobalPosition, target.GlobalPosition);
        // Never cut while a beggar it means to spare stands within reach (the cut would finish it).
        var shielding = room.Alive.Any(e => GodotObject.IsInstanceValid(e) && e.IsBegging && Decide(e) &&
                                            e.BodyGlobal.DistanceTo(p.BodyGlobal) < Tunables.Player.AttackReachPx + Enemy.Radius + 6);
        if (shielding) return;
        var open = target.Current is Enemy.State.Recover or Enemy.State.Stagger or Enemy.State.Kneel ||
                   (!target.IsBlocking && target.Current != Enemy.State.Attack);
        if (open && dist < 30 && free && p.OpenThreats.Count == 0 && _controls.Pending == 0)
            _controls.Press(PlayerAction.Attack, now, 30);
    }
}
