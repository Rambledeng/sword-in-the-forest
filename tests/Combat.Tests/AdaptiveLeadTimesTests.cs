using SwordInTheForest.Director;

namespace Combat.Tests;

public class AdaptiveLeadTimesTests
{
    private static readonly AttackSpec[] Attacks =
    {
        new("saber_slash_1", GlintColor.White, 300, 700, 420, 220, 700, 500),
        new("saber_chop", GlintColor.White, 700, 1200, 520, 280, 800, 900),
        new("saber_counter", GlintColor.White, 100, 180, 380, 220, 600, 400),
        new("test_thrust_red", GlintColor.Red, 400, 800, 450, 250, 800, 600),
        new("narrow", GlintColor.White, 300, 700, 333, 333, 333, 500),
    };

    private static AdaptiveLeadTimes Make()
    {
        var grid = DirectorGrid.Default;
        return new AdaptiveLeadTimes(ReactionDirector.FromPrior(grid, Prior.FromPopulation(grid, PopulationModel.Default, 2000)));
    }

    [Fact]
    public void AdaptiveOff_EveryAttackUsesItsDesignedLeadExactly()
    {
        var leads = Make();
        leads.Adaptive = false;
        var rng = new Random(5);
        for (var i = 0; i < 200; i++)
        {
            var attack = Attacks[i % Attacks.Length];
            Assert.Equal(attack.LeadDefaultMs, leads.NextLeadMs(attack));
            // The director keeps learning (fast here) but must not change the timing.
            leads.Observe(new TelemetryRecord("r", i, attack.Id, attack.Glint, attack.LeadDefaultMs,
                150 + rng.Next(60), Response.Deflect, Outcome.Success, 0), 220);
        }
        Assert.Equal(200, leads.Director.Observations);
    }

    [Fact]
    public void AdaptiveOn_StaysInsideEachAttacksRange_AndCalibratesFirst()
    {
        var leads = Make();
        Assert.Equal(DirectorMode.Calibrate, leads.Mode);
        for (var i = 0; i < 40; i++)
        {
            var attack = Attacks[i % Attacks.Length];
            var lead = leads.NextLeadMs(attack);
            Assert.InRange(lead, Math.Max(200, attack.LeadMinMs), Math.Min(800, attack.LeadMaxMs));
            leads.Observe(new TelemetryRecord("r", i, attack.Id, attack.Glint, lead, 260, Response.Deflect, Outcome.Success, 0), 220);
        }
        Assert.Equal(DirectorMode.Control, leads.Mode);
    }

    [Fact]
    public void AFastPlayer_GetsShorterLeadsThanASlowOne()
    {
        var fast = Make();
        var slow = Make();
        var attack = Attacks[0];
        for (var i = 0; i < 20; i++)
        {
            fast.Observe(new TelemetryRecord("r", i, attack.Id, attack.Glint, 420, 230, Response.Deflect, Outcome.Success, 0), 220);
            slow.Observe(new TelemetryRecord("r", i, attack.Id, attack.Glint, 420, 520, Response.None, Outcome.Hit, 0), 220);
        }
        Assert.True(fast.NextLeadMs(attack) + 150 < slow.NextLeadMs(attack));
    }
}
