using System.Collections.Generic;
using System.Linq;
using SwordInTheForest.Bots;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// M1 "Done when": a bot that guards against red attacks is hit by those. Run twice over: tapping
/// guard after the glint (a deflect attempt), then holding guard throughout. White attacks are
/// mixed in as a control: the tap deflects them, the held guard blocks them.
/// </summary>
public partial class BotGuardRedScenario : DrillScenario
{
    private int _frame;

    protected override BotPolicy Policy => BotPolicy.GuardTap;
    protected override IEnumerable<int> LeadTimes => new[] { 200, 450, 800 };
    protected override IEnumerable<string> AttackIds => new[] { "test_thrust_red", "saber_chop", "test_thrust_red" };

    protected override void Step(int frame)
    {
        _frame = frame;
        // Halfway through, switch from tapping to holding guard.
        if (Bot != null && Lines.Count == 0 && Telemetry() >= 6 && Bot.Policy == BotPolicy.GuardTap)
        {
            Bot.Policy = BotPolicy.GuardHold;
            Controls.HoldForever(Actors.PlayerAction.Guard, true);
        }
        base.Step(frame);
    }

    private int Telemetry() => Core.Telemetry.Instance.Lines.Count;

    protected override void CheckLine(Dictionary<string, object?> line, string raw)
    {
        if (S(line, "glint") == "red")
        {
            Check(S(line, "outcome") == "hit", $"guarding a red attack must be hit: {raw}");
            Check(S(line, "response") == "guard", $"expected response guard: {raw}");
        }
        else
        {
            Check(S(line, "outcome") is "success" or "blocked", $"white attack should be deflected or blocked: {raw}");
        }
    }

    protected override void CheckTotals()
    {
        var reds = Lines.Count(l => S(l, "glint") == "red");
        Check(reds == 6, $"expected 6 red attacks, got {reds}");
        Check(Player.HitsTaken == reds, $"hit {Player.HitsTaken} times by {reds} red attacks");
        Check(Lines.Any(l => S(l, "response") == "deflect"), "the guard tap never deflected a white attack");
        Check(Lines.Any(l => S(l, "response") == "block"), "the held guard never blocked a white attack");
    }
}
