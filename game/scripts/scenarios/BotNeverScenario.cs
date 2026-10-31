using System.Collections.Generic;
using SwordInTheForest.Bots;

namespace SwordInTheForest.Scenarios;

/// <summary>M1 "Done when": a bot that never responds is hit by every attack.</summary>
public partial class BotNeverScenario : DrillScenario
{
    protected override BotPolicy Policy => BotPolicy.Never;
    protected override IEnumerable<int> LeadTimes => new[] { 200, 420, 800 };
    protected override IEnumerable<string> AttackIds => new[] { "saber_slash_1", "saber_chop", "test_thrust_red" };

    protected override void CheckLine(Dictionary<string, object?> line, string raw)
    {
        Check(S(line, "outcome") == "hit", $"expected hit: {raw}");
        Check(S(line, "response") == "none", $"expected no response: {raw}");
        Check(line["press_ms"] == null, $"expected no press: {raw}");
    }

    protected override void CheckTotals()
    {
        Check(Lines.Count == 12, $"expected 12 attacks, got {Lines.Count}");
        Check(Player.HitsTaken == Lines.Count, $"hit {Player.HitsTaken} times by {Lines.Count} attacks");
    }
}
