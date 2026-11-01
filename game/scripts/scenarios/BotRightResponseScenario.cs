using System.Collections.Generic;
using SwordInTheForest.Bots;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// M1 "Done when": a bot giving the right response 150 ms after each glint is never hit when the
/// lead time is 200 ms or more. White glints are deflected, red ones rolled.
/// </summary>
public partial class BotRightResponseScenario : DrillScenario
{
    protected override BotPolicy Policy => BotPolicy.Right;
    protected override IEnumerable<int> LeadTimes => new[] { 200, 217, 250, 300, 350, 420, 550, 650, 800 };
    protected override IEnumerable<string> AttackIds => new[] { "saber_slash_1", "saber_chop", "test_thrust_red" };

    protected override void CheckLine(Dictionary<string, object?> line, string raw)
    {
        Check(S(line, "outcome") == "success", $"expected success: {raw}");
        var expected = S(line, "glint") == "red" ? "roll" : "deflect";
        Check(S(line, "response") == expected, $"expected {expected}: {raw}");
        if (S(line, "glint") == "white") Check((double)line["angle_deg"]! <= 30, $"the bot faces the bandit: {raw}");
        var press = (double)line["press_ms"]!;
        Check(press >= 150 - 0.5 && press <= 167, $"press should be ~150 ms after the glint: {raw}");
    }

    protected override void CheckTotals()
    {
        Check(Player.HitsTaken == 0, $"the right-response bot was hit {Player.HitsTaken} times");
        // 9 leads x (slash_1 + slash_2 combo, chop, thrust) = 36 attacks.
        Check(Lines.Count == 36, $"expected 36 attacks, got {Lines.Count}");
        Check(Player.Deflects == 27, $"expected 27 deflects, got {Player.Deflects}");
    }
}
