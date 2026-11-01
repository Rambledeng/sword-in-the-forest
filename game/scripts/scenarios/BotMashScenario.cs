using System.Collections.Generic;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;
using SwordInTheForest.Core;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Mashing guard every 100 ms never deflects: each press comes too soon after the last one
/// (DeflectMashLockoutMs), so white attacks are only blocked.
/// </summary>
public partial class BotMashScenario : DrillScenario
{
    private double _nextPressMs;

    protected override BotPolicy Policy => BotPolicy.Never;
    protected override bool IgnorePendingPresses => true;
    protected override IEnumerable<int> LeadTimes => new[] { 250, 450, 700 };
    protected override IEnumerable<string> AttackIds => new[] { "saber_chop", "saber_slash_1" };

    protected override void Step(int frame)
    {
        var now = GameClock.Instance?.NowMs ?? 0;
        if (frame > 0 && now >= _nextPressMs)
        {
            Controls.Press(PlayerAction.Guard, now, 50);
            _nextPressMs = now + 100;
        }
        base.Step(frame);
    }

    protected override void CheckLine(Dictionary<string, object?> line, string raw)
    {
        Check(S(line, "response") != "deflect", $"mashing must not deflect: {raw}");
    }

    protected override void CheckTotals()
    {
        Check(Player.Deflects == 0, $"mashing deflected {Player.Deflects} times");
        Check(Player.Blocks > 0, "mashing should still block");
    }
}
