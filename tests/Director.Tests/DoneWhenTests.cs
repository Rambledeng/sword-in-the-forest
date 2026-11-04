using System.Diagnostics;
using SwordInTheForest.Director;
using SwordInTheForest.Sim;
using Xunit;
using Xunit.Abstractions;

namespace Director.Tests;

/// <summary>Runs the 1,000-player study once for every test in <see cref="DoneWhenTests"/>.</summary>
public sealed class StudyFixture
{
    public M2Study Study { get; } = new();
    private readonly Dictionary<(Challenge, bool), PlayerResult[]> _runs = new();

    public PlayerResult[] Run(Challenge challenge, bool adaptive)
    {
        lock (_runs)
        {
            if (!_runs.TryGetValue((challenge, adaptive), out var r))
                _runs[(challenge, adaptive)] = r = Study.Run(new SimConfig { Challenge = challenge, Adaptive = adaptive });
            return r;
        }
    }
}

/// <summary>M2's "Done when" list, on 1,000 simulated players (sim/).</summary>
public class DoneWhenTests : IClassFixture<StudyFixture>
{
    private readonly StudyFixture _f;
    private readonly ITestOutputHelper _out;

    public DoneWhenTests(StudyFixture f, ITestOutputHelper output)
    {
        _f = f;
        _out = output;
    }

    [Fact]
    public void MedianEstimate_Within40ms_After15Attacks_ForAtLeast80Percent()
    {
        var results = _f.Run(Challenge.Standard, adaptive: true);
        Assert.Equal(1000, results.Length);
        var share = M2Study.ShareWithin(results, 15, 40);
        _out.WriteLine($"within 40 ms after 15 attacks: {share:P1}");
        Assert.True(share >= 0.80, $"only {share:P1} within 40 ms");
    }

    [Theory]
    [InlineData(Challenge.Relaxed)]
    [InlineData(Challenge.Standard)]
    [InlineData(Challenge.Hard)]
    public void Adaptive_PutsAtLeast80PercentWithin10Points_TighterThanFixed(Challenge challenge)
    {
        var target = challenge.Target();
        var adaptive = _f.Run(challenge, adaptive: true);
        var fixedTiming = _f.Run(challenge, adaptive: false);
        Assert.All(adaptive, r => Assert.Equal(60, r.Success.Length - r.CalibrationAttacks));
        var onTarget = M2Study.ShareOnTarget(adaptive, target);
        var fixedOnTarget = M2Study.ShareOnTarget(fixedTiming, target);
        var sd = M2Study.SuccessSd(adaptive);
        var fixedSd = M2Study.SuccessSd(fixedTiming);
        _out.WriteLine($"{challenge}: adaptive {onTarget:P1} on target (sd {sd:0.000}); fixed {fixedOnTarget:P1} (sd {fixedSd:0.000})");
        Assert.True(onTarget >= 0.80, $"only {onTarget:P1} within 10 points of {target}");
        Assert.True(sd < 0.5 * fixedSd, $"spread {sd:0.000} is not clearly tighter than fixed {fixedSd:0.000}");
        Assert.True(onTarget > fixedOnTarget + 0.3);
    }

    [Theory]
    [InlineData(DirectorMode.Control)]
    [InlineData(DirectorMode.Calibrate)]
    public void OneUpdatePlusOneDecision_TakesUnderHalfAMillisecond(DirectorMode mode)
    {
        var grid = DirectorGrid.Default;
        var d = ReactionDirector.FromPrior(grid, _f.Study.Prior);
        var player = new SimPlayer(new PlayerParams(330, 0.25, 0.05));
        var rng = new Random(1);
        var bounds = new LeadTimeBounds(200, 800);
        var lead = 420;
        for (var i = 0; i < 200; i++) lead = d.NextLeadMs(bounds, mode, 0.7 + d.Observe(player.Respond(lead, rng)) * 0); // warm up
        const int n = 2000;
        var events = Enumerable.Range(0, n).Select(_ => player.Respond(lead, rng)).ToArray();
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < n; i++)
        {
            d.Observe(events[i]);
            lead = d.NextLeadMs(bounds, mode, 0.7);
        }
        var ms = sw.Elapsed.TotalMilliseconds / n;
        Console.WriteLine($"{mode}: {ms * 1000:0} µs per update + decision");
        Assert.True(ms < 0.5, $"{ms:0.000} ms per update + decision");
    }
}
