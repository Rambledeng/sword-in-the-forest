using SwordInTheForest.Director;
using SwordInTheForest.Sim;

namespace Director.Tests;

public class ExGaussianTimeTests
{
    [Theory]
    [InlineData(330, 25, 100)]
    [InlineData(200, 12, 40)]
    [InlineData(600, 90, 300)]
    public void WithMedian_HasThatMedian_AndSamplesAgree(double median, double sigma, double tau)
    {
        var eg = ExGaussianTime.WithMedian(median, sigma, tau);
        Assert.Equal(median, eg.MedianMs, 3);
        Assert.Equal(0.5, eg.Cdf(median), 6);
        var rng = new Random(3);
        var samples = Enumerable.Range(0, 40000).Select(_ => eg.SampleMs(rng)).OrderBy(x => x).ToArray();
        Assert.InRange(samples[samples.Length / 2], median * 0.98, median * 1.02);
        // Mean of an ex-Gaussian is μ + τ.
        Assert.InRange(samples.Average(), (eg.MuMs + tau) * 0.98, (eg.MuMs + tau) * 1.02);
    }

    [Fact]
    public void LogNormalPlayer_IsUnchangedByTheReactionModelRefactor()
    {
        var truth = new PlayerParams(330, 0.25, 0.0);
        var a = new SimPlayer(truth);
        var b = new SimPlayer(truth, new LogNormalTime(330, 0.25));
        var r1 = new Random(5);
        var r2 = new Random(5);
        for (var i = 0; i < 200; i++) Assert.Equal(a.Respond(400, r1).PressMs, b.Respond(400, r2).PressMs);
    }
}
