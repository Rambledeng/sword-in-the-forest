using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// A release build (developer tools off): F1, F2 and F3 open nothing, and the run's hint does not
/// mention F2. Then, with the tools on again, F2 opens the overlay (the gate is the only change).
/// </summary>
public partial class DevGateScenario : Scenario
{
    private readonly BotControls _controls = new();
    private RunScene _run = null!;

    protected override bool FailOnTimeout => true;

    private static void Press(string action)
    {
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
        Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
    }

    protected override void Step(int frame)
    {
        switch (frame)
        {
            case 0:
                DevTools.Enabled = false;
                _run = new RunScene { Seed = 7, PlayMusic = false, ChangeScenes = false, Controls = _controls };
                AddChild(_run);
                return;
            case 30:
                Check(_run.Hud.HintKey == "ui.run.hint_release", $"the release hint is {_run.Hud.HintKey}");
                Press("debug_tuning");
                Press("debug_overlay");
                Press("debug_dev");
                return;
            case 33:
                Check(!_run.Tuning.IsOpen && !_run.Overlay.IsOpen && !_run.Dev.IsOpen && !GetTree().Paused,
                    "a developer tool opened in a release build");
                DevTools.Enabled = true;
                Press("debug_overlay");
                return;
            case 36:
                Check(_run.Overlay.IsOpen, "F2 did not open the overlay with the tools on");
                Pass();
                return;
        }
    }
}
