using System;
using SwordInTheForest.Director;
using Xunit;

namespace Director.Tests;

public class LeadTimeBoundsTests
{
    [Theory]
    [InlineData(100, 250)]
    [InlineData(420, 420)]
    [InlineData(900, 600)]
    public void Clamp_KeepsLeadInsideBounds(int input, int expected)
    {
        var bounds = new LeadTimeBounds(250, 600);
        Assert.Equal(expected, bounds.Clamp(input));
    }

    [Fact]
    public void Constructor_RejectsInvertedRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LeadTimeBounds(600, 250));
    }
}
