using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// The F3 developer menu: F3 opens it and pauses, god mode toggles, jumping reaches the boss and
/// any room, and a forced beg makes the next posture break beg (with surrender otherwise off).
/// </summary>
public partial class DevMenuScenario : Scenario
{
    private readonly BotControls _controls = new();
    private RunScene _run = null!;
    private RunBot _bot = null!;
    private int _stage;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            DirectorService.Instance?.ResetBelief();
            _run = new RunScene { Seed = 7, PlayMusic = false, ChangeScenes = false, Controls = _controls, SurrenderOverride = 0f };
            AddChild(_run);
            _bot = new RunBot(_run, _controls);
            _bot.Attach();
            return;
        }
        switch (_stage)
        {
            case 0:
                Input.ParseInputEvent(new InputEventAction { Action = "debug_dev", Pressed = true });
                _stage = 1;
                return;
            case 1:
                Check(_run.Dev.IsOpen && GetTree().Paused, "F3 did not open the menu and pause");
                _run.Player.Invulnerable = false;
                _run.Dev.Jump(_run.BossStep);
                Check(!_run.Dev.IsOpen && !GetTree().Paused, "jumping did not close the menu");
                _stage = 2;
                return;
            case 2:
                Check(_run.Room is { Plan.IsBoss: true }, "the jump did not reach the boss");
                Check(_run.Hud.Boss != null, "no boss bar after the jump");
                _run.Dev.Jump(2);
                _run.Dev.Force(BegKind.Genuine);
                _run.Player.Invulnerable = true;
                _stage = 3;
                return;
            case 3:
                Check(_run.Room is { Plan.Area: Area.Bamboo }, "the jump did not reach the bamboo room");
                _bot.Step();
                var broken = _run.Room!.Spawned.FirstOrDefault(e => IsInstanceValid(e) && e.Current is Enemy.State.Surrender or Enemy.State.Kneel);
                if (broken == null) return;
                Check(broken.Current == Enemy.State.Surrender && !broken.IsFake, $"the forced break did {broken.Current}");
                Check(_run.Mercy.ForceNext == null, "the force was not consumed");
                GD.Print($"  forced beg at frame {frame}");
                Pass();
                return;
        }
    }
}
