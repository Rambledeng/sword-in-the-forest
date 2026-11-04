using System.Linq;
using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Death does not send the next soul back to the tutorial: a soul with one upgrade falls in the
/// second room outside the tavern; the summary appears and names the area; New soul starts the
/// next run at the first room outside the tavern, with the upgrade kept.
/// </summary>
public partial class RunContinueScenario : Scenario
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
                _run = new RunScene { Seed = 7, StartStep = 5, PlayMusic = false, ChangeScenes = false };
                AddChild(_run);
                _run.Apply(Upgrade.Vitality);
                _run.Stats.Take(Upgrade.Vitality);
                _stage = 1;
                return;
            case 1:
                Check(_run.Room is { Plan.Area: Area.Outside }, "did not start outside the tavern");
                _run.Player.Kill();
                _stage = 2;
                return;
            case 2:
                if (_run.Summary == null) return;
                Check(!_run.Stats.Won, "a death ended in a win");
                Check(_run.Summary.Lines.Any(l => l.Contains(Tr("ui.area.outside"))), "the summary does not say where the next soul starts");
                _run.Retry();
                Check(RunScene.NextArea == Area.Outside, $"next area is {RunScene.NextArea}");
                _run.QueueFree();
                _stage = 3;
                return;
            case 3:
                if (++_wait < 3) return;
                GetTree().Paused = false;
                _run = new RunScene { PlayMusic = false, ChangeScenes = false };
                AddChild(_run);
                _stage = 4;
                return;
            case 4:
                var outsideStart = _run.Plan.AreaStart(Area.Outside);
                Check(_run.StepIndex == outsideStart, $"the new soul started at step {_run.StepIndex}, not {outsideStart}");
                Check(_run.ContinuedFrom == Area.Outside, "the new soul does not know it continues");
                Check(_run.Stats.Upgrades.SequenceEqual(new[] { Upgrade.Vitality }), "the upgrade was not kept");
                Check(_run.Player.MaxHealth == Tunables.Player.MaxHealth + Tunables.Run.VitalityHealth, "Vitality's health is missing");
                Check(RunScene.NextArea == null, "the continuation was not consumed");
                GD.Print($"  new soul at step {_run.StepIndex} ({_run.Room!.Plan.Template.Id}), upgrades {string.Join(",", _run.Stats.Upgrades)}");
                Pass();
                return;
        }
    }
}
