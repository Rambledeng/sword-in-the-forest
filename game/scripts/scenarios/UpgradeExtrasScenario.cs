using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// 余威 Dread: a finisher shakes the posture of the other enemies near the player.
/// 止水 Still Water: deflects cost the player less posture (the multiplier is applied).
/// </summary>
public partial class UpgradeExtrasScenario : Scenario
{
    private readonly BotControls _controls = new();
    private RunScene _run = null!;
    private Enemy _kneeler = null!;
    private Enemy _other = null!;
    private double _otherPosture0;
    private int _stage;
    private int _waitFrom;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            // Bamboo room 2 has two bandits; no begging so the broken one kneels.
            _run = new RunScene { Seed = 7, StartStep = 2, PlayMusic = false, ChangeScenes = false, Controls = _controls, Invulnerable = true, SurrenderOverride = 0f };
            AddChild(_run);
            return;
        }
        if (frame < 10) return;
        var p = _run.Player;
        switch (_stage)
        {
            case 0:
                _run.Apply(Upgrade.StillWater);
                Check(Mathf.IsEqualApprox(p.Mods.DeflectSelfPostureFactor, Tunables.Run.StillWaterFactor), "Still Water did not lower the deflect's posture cost");
                _run.Apply(Upgrade.Dread);
                Check(Mathf.IsEqualApprox(p.Mods.DreadPosture, Tunables.Run.DreadPosture), "Dread was not applied");
                var alive = _run.Room!.Spawned.Where(e => IsInstanceValid(e) && !e.IsGone).ToList();
                if (alive.Count < 2)
                {
                    Fail($"need two enemies, room has {alive.Count}");
                    return;
                }
                _kneeler = alive[0];
                _other = alive[1];
                // Gather them by the player, break one, cut it down.
                var toward = (p.GlobalPosition - _kneeler.GlobalPosition).Normalized();
                p.GlobalPosition = _kneeler.GlobalPosition + toward * 14;
                _other.GlobalPosition = _kneeler.GlobalPosition + toward.Orthogonal() * 24;
                _kneeler.Shake(1000);
                Check(_kneeler.IsKneeling, $"the shaken enemy is {_kneeler.Current}, not kneeling");
                _otherPosture0 = _other.Posture.Value;
                _controls.AimInput = -toward;
                _controls.Press(PlayerAction.Attack, GameClock.Instance.NowMs);
                _stage = 1;
                _waitFrom = frame;
                return;
            case 1:
                if (!_kneeler.IsGone)
                {
                    if (frame - _waitFrom > 60) Fail("the finisher did not land");
                    return;
                }
                Check(p.DreadShaken == 1, $"Dread shook {p.DreadShaken} enemies, not 1");
                Check(_other.IsKneeling || _other.Posture.Value >= _otherPosture0 + Tunables.Run.DreadPosture - 5,
                    $"the other enemy's posture went {_otherPosture0:0} -> {_other.Posture.Value:0}");
                GD.Print($"  frame {frame}: finisher shook the other bandit, posture {_otherPosture0:0} -> {_other.Posture.Value:0}");
                Pass();
                return;
        }
    }
}
