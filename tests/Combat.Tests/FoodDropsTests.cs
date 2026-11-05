using SwordInTheForest.Run;

namespace Combat.Tests;

public class FoodDropsTests
{
    [Fact]
    public void NoChance_NeverDrops()
    {
        var f = new FoodDrops(0, 1, 1, 1);
        for (var i = 0; i < 100; i++) Assert.Null(f.Roll(() => 0.0));
        Assert.Equal(0, f.Dropped);
    }

    [Fact]
    public void DropRate_AndWeights_Hold()
    {
        var rng = new System.Random(5);
        var f = new FoodDrops(0.12, 0.6, 0.3, 0.1);
        var counts = new int[3];
        const int n = 40000;
        for (var i = 0; i < n; i++) if (f.Roll(rng.NextDouble) is Food food) counts[(int)food]++;
        Assert.InRange(f.Dropped / (double)n, 0.11, 0.13);
        Assert.InRange(counts[0] / (double)f.Dropped, 0.56, 0.64);
        Assert.InRange(counts[1] / (double)f.Dropped, 0.26, 0.34);
        Assert.InRange(counts[2] / (double)f.Dropped, 0.07, 0.13);
    }

    [Fact]
    public void ForceNext_OverridesOnce()
    {
        var f = new FoodDrops(0, 1, 0, 0) { ForceNext = Food.RoastChicken };
        Assert.Equal(Food.RoastChicken, f.Roll(() => 0.99));
        Assert.Null(f.Roll(() => 0.0));
        Assert.Equal(1, f.Dropped);
    }
}
