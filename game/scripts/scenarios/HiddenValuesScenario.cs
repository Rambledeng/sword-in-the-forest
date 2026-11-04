using System.Linq;
using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Fx;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Before the first ending the HUD and summary hide merit, karma, reaction times and attunement;
/// merit plays a gold glitter and karma dark smoke on the player instead. Beating the boss marks the
/// first ending and unlocks Detail mode in the settings, which shows the numbers again.
/// </summary>
public partial class HiddenValuesScenario : Scenario
{
    private RunScene _run = null!;
    private int _stage;
    private int _wait;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        switch (_stage)
        {
            case 0:
                RunScene.NextArea = null;
                UserSettings.SetProgressForTest(endingSeen: false, detailMode: false);
                _run = new RunScene { Seed = 7, StartStep = 1, PlayMusic = false, ChangeScenes = false };
                AddChild(_run);
                _stage = 1;
                return;
            case 1:
                if (++_wait < 3) return;
                _wait = 0;
                Check(!_run.Hud.AttunementShown, "the attunement meter shows before the first ending");
                _run.Stats.Spare();
                Check(_run.Player.GetChildren().OfType<MeritFx>().Count() == 1, "no glitter for merit");
                _run.Stats.Finished(begging: true);
                Check(_run.ValueFxPlayed == 2 && _run.Player.GetChildren().OfType<MeritFx>().Count() == 2, "no smoke for karma");
                _run.Pause.Toggle(true);
                _run.Pause.Settings.Open();
                Check(!_run.Pause.Settings.DetailButton.Visible, "Detail mode is offered before the first ending");
                _run.Pause.Settings.Close();
                _run.Pause.Toggle(false);
                _run.EndForShot(won: true);
                _stage = 2;
                return;
            case 2:
                if (_run.Summary == null) return;
                Check(UserSettings.EndingSeen && _run.FirstEnding, "the win did not mark the first ending");
                var lines = _run.Summary.Lines;
                Check(!lines.Any(l => l.Contains(" ms") || l.Contains("%")), "the summary shows numbers before Detail mode");
                Check(!lines.Any(l => l.StartsWith(Tr("summary.merit_karma").Split(':')[0])), "the summary shows merit and karma");
                Check(lines.Contains(Tr("summary.detail_unlocked")), "the summary does not announce Detail mode");
                Check(new[] { "unread", "quick", "learning", "slow" }.Any(k => lines.Contains(Tr($"summary.reading.{k}"))), "the summary has no worded reading");
                GetTree().Paused = false;
                _run.QueueFree();
                _stage = 3;
                return;
            case 3:
                if (++_wait < 3) return;
                _wait = 0;
                _run = new RunScene { Seed = 8, StartStep = 1, PlayMusic = false, ChangeScenes = false };
                AddChild(_run);
                _stage = 4;
                return;
            case 4:
                if (++_wait < 2) return;
                _wait = 0;
                _run.Pause.Toggle(true);
                _run.Pause.Settings.Open();
                Check(_run.Pause.Settings.DetailButton.Visible, "Detail mode is not offered after the first ending");
                _run.Pause.Settings.DetailButton.EmitSignal(BaseButton.SignalName.Pressed);
                Check(UserSettings.ShowDetails, "the Detail mode button did not turn it on");
                _run.Pause.Settings.Close();
                _run.Pause.Toggle(false);
                _stage = 5;
                return;
            case 5:
                if (++_wait < 3) return;
                Check(_run.Hud.AttunementShown, "the attunement meter is hidden in Detail mode");
                _run.EndForShot(won: false);
                _stage = 6;
                return;
            case 6:
                if (_run.Summary == null) return;
                Check(!_run.FirstEnding, "a loss counted as an ending");
                Check(_run.Summary.Lines.Any(l => l.Contains(" ms")), "Detail mode does not show the reading");
                Check(!_run.Summary.Lines.Contains(Tr("summary.detail_unlocked")), "Detail mode announced twice");
                Pass();
                return;
        }
    }
}
