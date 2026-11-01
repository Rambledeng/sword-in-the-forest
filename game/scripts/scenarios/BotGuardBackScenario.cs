using System.Collections.Generic;
using Godot;
using SwordInTheForest.Bots;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Directional guard: with the bot facing away from the bandit, a perfectly timed deflect
/// against a white attack is still hit (logged as a positioning miss, angle_deg near 180),
/// while a roll against a red attack evades from behind.
/// </summary>
public partial class BotGuardBackScenario : DrillScenario
{
    protected override BotPolicy Policy => BotPolicy.Right;
    protected override Vector2 PlayerAim => Vector2.Down; // the bandit is above: back turned
    protected override IEnumerable<int> LeadTimes => new[] { 250, 500 };
    protected override IEnumerable<string> AttackIds => new[] { "saber_chop", "test_thrust_red" };

    protected override void CheckLine(Dictionary<string, object?> line, string raw)
    {
        var angle = (double)line["angle_deg"]!;
        Check(angle >= 150, $"the bot should face away: {raw}");
        if (S(line, "glint") == "red")
            Check(S(line, "outcome") == "success" && S(line, "response") == "roll", $"a roll evades from behind: {raw}");
        else
            Check(S(line, "outcome") == "hit" && S(line, "response") == "deflect", $"a deflect from behind is hit: {raw}");
    }

    protected override void CheckTotals()
    {
        Check(Lines.Count == 4, $"expected 4 attacks, got {Lines.Count}");
        Check(Player.Deflects == 0, $"no deflect should land from behind, got {Player.Deflects}");
        Check(Player.HitsTaken == 2, $"expected 2 hits (the white attacks), got {Player.HitsTaken}");
    }
}
