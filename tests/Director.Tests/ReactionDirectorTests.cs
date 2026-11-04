using SwordInTheForest.Director;
using SwordInTheForest.Sim;
using Xunit;

namespace Director.Tests;

public class ReactionDirectorTests
{
    private static readonly DirectorGrid Grid = DirectorGrid.Default;
    private static readonly double[] PriorWeights = Simulator.DefaultPrior(Grid);
    private static readonly LeadTimeBounds Wide = new(200, 800);

    private static ReactionDirector Fresh() => ReactionDirector.FromPrior(Grid, PriorWeights);

    [Fact]
    public void Grid_HasAboutThreeThousandCells_From150To900()
    {
        Assert.InRange(Grid.CellCount, 2800, 3200);
        Assert.Equal(150, Math.Exp(Grid.LogMedians[0]), 6);
        Assert.Equal(900, Math.Exp(Grid.LogMedians[^1]), 6);
        Assert.Equal(12, Grid.SpreadCount);
        Assert.Equal(4, Grid.LapseCount);
    }

    [Fact]
    public void Prior_CentresOnThePopulation()
    {
        var e = Fresh().Estimate();
        Assert.InRange(e.MedianMs, 300, 360);
        Assert.True(e.LowMs < 260 && e.HighMs > 420, e.ToString());
        Assert.InRange(e.Attunement, 0, 0.1);
    }

    [Theory]
    [InlineData(230)]
    [InlineData(480)]
    [InlineData(700)]
    public void TimedPresses_PullTheEstimateToThePlayer(double median)
    {
        var d = Fresh();
        var rng = new Random(3);
        var player = new SimPlayer(new PlayerParams(median, 0.2, 0));
        for (var i = 0; i < 30; i++) d.Observe(player.Respond(500, rng));
        var e = d.Estimate();
        Assert.InRange(e.MedianMs, median * 0.9, median * 1.1);
        Assert.True(e.Attunement > 0.7, e.ToString());
    }

    [Fact]
    public void Misses_MakeItSlower_AndLapsesLeaveTheMedianAlone()
    {
        var misses = Fresh();
        for (var i = 0; i < 5; i++) misses.Observe(ThreatEvent.NoResponse(400));
        Assert.True(misses.Estimate().MedianMs > Fresh().Estimate().MedianMs + 20);

        var lapses = Fresh();
        for (var i = 0; i < 5; i++) lapses.Observe(ThreatEvent.Lapse(400, 200));
        Assert.Equal(Fresh().Estimate().MedianMs, lapses.Estimate().MedianMs, 0);
        Assert.True(lapses.Estimate().Lapse > Fresh().Estimate().Lapse);
    }

    [Fact]
    public void Control_ReturnsTheShortestLeadReachingTheTarget()
    {
        var d = Fresh();
        var standard = d.NextLeadMs(Wide, DirectorMode.Control, 0.70);
        Assert.True(d.PredictSuccess(standard) >= 0.70);
        Assert.True(d.PredictSuccess(standard - DirectorGrid.LeadStepMs) < 0.70);
        Assert.True(d.NextLeadMs(Wide, DirectorMode.Control, 0.85) > standard);
        Assert.True(d.NextLeadMs(Wide, DirectorMode.Control, 0.55) < standard);
    }

    [Theory]
    [InlineData(350, 480)]
    [InlineData(400, 600)]
    [InlineData(150, 900)]
    [InlineData(203, 207)]
    public void Decisions_StayInsideTheAttackRangeAnd200To800(int min, int max)
    {
        var d = Fresh();
        foreach (var mode in new[] { DirectorMode.Calibrate, DirectorMode.Control })
            foreach (var target in new[] { 0.01, 0.55, 0.70, 0.85, 0.999 })
            {
                var lead = d.NextLeadMs(new LeadTimeBounds(min, max), mode, target);
                Assert.InRange(lead, Math.Max(min, 200), Math.Min(max, 800));
            }
    }

    [Fact]
    public void Calibration_ProbesNearThePlayersMedian()
    {
        var fast = Fresh();
        var slow = Fresh();
        for (var i = 0; i < 8; i++)
        {
            fast.Observe(ThreatEvent.Timed(400, 250));
            slow.Observe(ThreatEvent.Timed(400, 600));
        }
        Assert.InRange(fast.NextLeadMs(Wide, DirectorMode.Calibrate, 0.7), 200, 330);
        Assert.InRange(slow.NextLeadMs(Wide, DirectorMode.Calibrate, 0.7), 520, 800);
    }

    [Fact]
    public void ImpossibleEvidence_LeavesTheBeliefUsable()
    {
        var d = Fresh();
        d.Observe(ThreatEvent.Timed(400, 1e9));
        d.Observe(ThreatEvent.Timed(400, 0));
        var e = d.Estimate();
        Assert.False(double.IsNaN(e.MedianMs));
        Assert.Equal(2, d.Observations);
    }

    [Fact]
    public void Observe_ReturnsThePredictedSuccess_AndKeepsHistory()
    {
        var d = Fresh();
        var expected = d.PredictSuccess(420);
        Assert.Equal(expected, d.Observe(ThreatEvent.Timed(420, 300)), 9);
        Assert.Single(d.History);
        Assert.True(d.History[0].Success);
        Assert.False(ThreatEvent.Timed(420, 430).Success);
    }

    [Fact]
    public void Save_RoundTrips_AndRejectsOtherGrids()
    {
        var d = Fresh();
        d.Observe(ThreatEvent.Timed(420, 280));
        d.Observe(ThreatEvent.NoResponse(300));
        var text = DirectorSave.ToText(d);
        var back = DirectorSave.FromText(Grid, text);
        Assert.NotNull(back);
        Assert.Equal(2, back!.Observations);
        Assert.Equal(d.Estimate().MedianMs, back.Estimate().MedianMs, 3);
        var other = new DirectorGrid(30, 150, 900, new[] { 0.2, 0.3 }, new[] { 0.0, 0.1 }, 25);
        Assert.Null(DirectorSave.FromText(other, text));
        Assert.Null(DirectorSave.FromText(Grid, "garbage"));
        Assert.Null(DirectorSave.FromText(Grid, text.Replace("weights", "weightz")));
    }
}
