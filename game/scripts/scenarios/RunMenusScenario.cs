using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// The run's menus with real input events: the upgrade choice by keyboard (→ then J), the pause
/// menu (Esc opens and closes it, the game time stops), and Quit to title.
/// </summary>
public partial class RunMenusScenario : Scenario
{
    private RunScene _run = null!;
    private double _pausedAt;

    private static void Key(Key k, bool down) => Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = k, Keycode = k, Pressed = down });

    protected override void Step(int frame)
    {
        switch (frame)
        {
            case 0:
                _run = new RunScene { Seed = 3, StartStep = 3, PlayMusic = false, ChangeScenes = false, Controls = new BotControls() };
                AddChild(_run);
                break;
            case 4:
                Check(_run.Upgrades != null, "the upgrade screen should show at step 3");
                Check(GetTree().Paused, "the game should pause during the upgrade choice");
                Key(Godot.Key.Right, true);
                break;
            case 5: Key(Godot.Key.Right, false); break;
            case 7: Key(Godot.Key.J, true); break;
            case 8: Key(Godot.Key.J, false); break;
            case 12:
            {
                var choices = _run.Plan.Steps[3].Choices;
                Check(_run.Stats.Upgrades.Count == 1, "no upgrade was taken");
                Check(_run.Stats.Upgrades[0] == choices[1], $"took {_run.Stats.Upgrades[0]}, expected the second card {choices[1]}");
                Check(!GetTree().Paused, "still paused after choosing");
                Check(_run.StepIndex == 4 && _run.Room != null, $"expected the first room outside the tavern, at step {_run.StepIndex}");
                Key(Godot.Key.Escape, true);
                break;
            }
            case 13: Key(Godot.Key.Escape, false); break;
            case 15:
                Check(_run.Pause.IsOpen && GetTree().Paused, "Esc should open the pause menu");
                _pausedAt = Core.GameClock.Instance.NowMs;
                break;
            case 25:
                Check(Core.GameClock.Instance.NowMs == _pausedAt, "game time ran while paused");
                Key(Godot.Key.Escape, true);
                break;
            case 26: Key(Godot.Key.Escape, false); break;
            case 28:
                Check(!_run.Pause.IsOpen && !GetTree().Paused, "Esc should close the pause menu");
                _run.Pause.Toggle(true);
                break;
            case 30:
                _run.Pause.QuitButton.EmitSignal(BaseButton.SignalName.Pressed);
                break;
            case 32:
                Check(!GetTree().Paused, "quitting to the title should unpause");
                Pass();
                break;
        }
    }
}
