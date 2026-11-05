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
    private Color _firstTint;

    protected override bool FailOnTimeout => true;

    /// <summary>The soul has its seeded perk, applied, recorded and shown under the area title.</summary>
    private void CheckPerk(string who)
    {
        var soulSeed = unchecked(_run.Plan.Seed * 7919 + (DirectorService.Instance?.SoulsFallen ?? 0));
        Check(_run.Perk == StarterPerks.Pick(soulSeed), $"{who}'s perk is not the seeded one");
        Check(_run.Stats.Perk == _run.Perk, $"{who}'s perk is not in the run's stats");
        Check(_run.Hud.PerkShown && _run.Hud.PerkLine.Contains(Ui.PerkText.Name(_run.Perk)), $"{who}'s perk is not shown");
        var m = _run.Player.Mods;
        var applied = _run.Perk switch
        {
            StarterPerk.Hale => m.HealthBonus >= Tunables.Run.PerkHealth,
            StarterPerk.Sturdy => m.PostureBonus >= Tunables.Run.PerkPosture,
            StarterPerk.EvenBreath => m.PostureDrainFactor >= Tunables.Run.PerkBreathFactor - 0.001f,
            StarterPerk.SpareSip => m.SipsBonus >= Tunables.Run.PerkSips,
            _ => m.DeflectFactor >= Tunables.Run.PerkDeflectFactor - 0.001f,
        };
        Check(applied, $"{who}'s perk {_run.Perk} was not applied");
        GD.Print($"  {who}: {_run.Hud.PerkLine}");
    }

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
                _firstTint = _run.Player.Tint;
                Check(_firstTint != Colors.White, "the soul has no tint");
                CheckPerk("the first soul");
                _run.Player.Kill();
                _stage = 2;
                return;
            case 2:
                if (_run.Summary == null) return;
                Check(!_run.Stats.Won, "a death ended in a win");
                Check(_run.Summary.Lines.Any(l => l.Contains(Tr("ui.area.outside"))), "the summary does not say where the next soul starts");
                Check(_run.Summary.Lines.Any(l => l.Contains(Ui.PerkText.Name(_run.Perk))), "the summary does not list the soul's perk");
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
                var perkHealth = _run.Perk == StarterPerk.Hale ? Tunables.Run.PerkHealth : 0;
                Check(_run.Player.MaxHealth == Tunables.Player.MaxHealth + Tunables.Run.VitalityHealth + perkHealth, "Vitality's health is missing");
                CheckPerk("the new soul");
                Check(RunScene.NextArea == null, "the continuation was not consumed");
                Check(_run.Player.Tint != _firstTint, "the new soul has the same tint as the last");
                Check(Mathf.Min(_run.Player.Tint.R, Mathf.Min(_run.Player.Tint.G, _run.Player.Tint.B)) >= 1f - Tunables.Player.SoulTintStrength - 0.01f, "the tint is more than slight");
                GD.Print($"  new soul at step {_run.StepIndex} ({_run.Room!.Plan.Template.Id}), upgrades {string.Join(",", _run.Stats.Upgrades)}, perk {_run.Perk}");
                Pass();
                return;
        }
    }
}
