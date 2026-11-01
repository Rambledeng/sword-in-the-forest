using SwordInTheForest.Director;
using Xunit;

namespace Director.Tests;

public class ReactionMathTests
{
    [Theory]
    [InlineData(0.0, 0.5)]
    [InlineData(1.0, 0.8413447)]
    [InlineData(-1.96, 0.0249979)]
    [InlineData(3.0, 0.9986501)]
    public void Phi_MatchesTable(double z, double expected) => Assert.Equal(expected, ReactionMath.Phi(z), 6);

    [Fact]
    public void LogNormalPdf_IntegratesToOne()
    {
        double sum = 0;
        for (var x = 0.5; x < 5000; x += 1) sum += ReactionMath.LogNormalPdf(x, Math.Log(330), 0.25);
        Assert.Equal(1.0, sum, 3);
    }

    [Fact]
    public void Success_IsHalfAtMedianPlusDelay_AndScaledByLapse()
    {
        Assert.Equal(0.5, ReactionMath.SuccessProbability(355, 25, Math.Log(330), 0.25, 0), 6);
        Assert.Equal(0.45, ReactionMath.SuccessProbability(355, 25, Math.Log(330), 0.25, 0.1), 6);
        Assert.Equal(0, ReactionMath.SuccessProbability(20, 25, Math.Log(330), 0.25, 0));
    }
}
