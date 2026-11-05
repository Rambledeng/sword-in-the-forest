using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// 瞬步: without the upgrade the button does nothing; with it the player dashes through an enemy,
/// cutting it, and the move is unavailable until its cooldown has passed.
/// </summary>
public partial class FlashStepScenario : Scenario
{
    private readonly BotControls _controls = new();
    private RunScene _run = null!;
    private Enemy _enemy = null!;
    private int _stage;
    private int _waitFrom;
    private Vector2 _from;
    private int _health0;
    private double _posture0;

    protected override bool FailOnTimeout => true;

    private double Now => GameClock.Instance.NowMs;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _run = new RunScene { Seed = 7, StartStep = 2, PlayMusic = false, ChangeScenes = false, Controls = _controls, Invulnerable = true };
            AddChild(_run);
            return;
        }
        if (frame < 10) return;
        var p = _run.Player;
        switch (_stage)
        {
            case 0: // no upgrade: nothing happens
                _controls.Press(PlayerAction.FlashStep, Now);
                _stage = 1;
                _waitFrom = frame;
                return;
            case 1:
                if (frame - _waitFrom < 10) return;
                Check(p.FlashSteps == 0 && p.Current != Player.State.FlashStep, "flash step worked without the upgrade");
                _run.Apply(Upgrade.FlashStep);
                Check(p.Mods.FlashStep && p.FlashStepReady, "the upgrade did not enable flash step");
                // Stand a little way from the first enemy, toward the open floor the player came from.
                _enemy = _run.Room!.Spawned.First(e => IsInstanceValid(e) && !e.IsGone);
                var toward = (p.GlobalPosition - _enemy.GlobalPosition).Normalized();
                p.GlobalPosition = _enemy.GlobalPosition + toward * 30;
                _controls.AimInput = -toward;
                _from = p.GlobalPosition;
                _health0 = _enemy.Health;
                _posture0 = _enemy.Posture.Value;
                _controls.Press(PlayerAction.FlashStep, Now);
                _stage = 2;
                _waitFrom = frame;
                return;
            case 2:
                if (frame - _waitFrom < 20) return;
                var moved = p.GlobalPosition.DistanceTo(_from);
                Check(p.FlashSteps == 1, $"flash steps taken: {p.FlashSteps}");
                Check(moved >= Tunables.Run.FlashStepDistancePx * 0.6f && moved <= Tunables.Run.FlashStepDistancePx * 1.25f, $"the dash moved {moved:0} px, not about {Tunables.Run.FlashStepDistancePx}");
                // It cuts past a raised guard: health always drops.
                Check(_enemy.Health < _health0 || _enemy.IsGone, "the dash did not cut the enemy");
                Check(!p.FlashStepReady && p.FlashStepCharge < 0.2f, "flash step is ready again at once");
                GD.Print($"  frame {frame}: dashed {moved:0} px, enemy health {_health0}->{_enemy.Health}, posture {_posture0:0}->{_enemy.Posture.Value:0}");
                _controls.Press(PlayerAction.FlashStep, Now);
                _stage = 3;
                _waitFrom = frame;
                return;
            case 3: // on cooldown: a second press does nothing; then it recharges
                if (frame - _waitFrom == 10) Check(p.FlashSteps == 1, "flash step fired during its cooldown");
                if (!p.FlashStepReady) return;
                var waited = (frame - _waitFrom) * 1000.0 / 60.0;
                GD.Print($"  frame {frame}: ready again after about {waited:0} ms");
                Check(p.FlashStepCharge >= 1f, "ready but not fully charged");
                Pass();
                return;
        }
    }
}
