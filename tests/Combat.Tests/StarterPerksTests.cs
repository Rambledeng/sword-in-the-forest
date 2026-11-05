using SwordInTheForest.Run;

namespace Combat.Tests;

public class StarterPerksTests
{
    [Fact]
    public void SameSoul_SamePerk()
    {
        for (var s = -50; s < 50; s++) Assert.Equal(StarterPerks.Pick(s), StarterPerks.Pick(s));
    }

    [Fact]
    public void EveryPerk_Comes_AboutEquallyOften()
    {
        var counts = new int[StarterPerks.All.Length];
        const int n = 20000;
        for (var s = 0; s < n; s++) counts[(int)StarterPerks.Pick(unchecked(s * 7919 + s % 3))]++;
        foreach (var c in counts) Assert.InRange(c / (double)n, 0.16, 0.24);
    }

    [Fact]
    public void ConsecutiveSouls_DoNotAllShareOnePerk()
    {
        // Souls of one run differ by the number fallen (seed * 7919 + fallen).
        var perks = new HashSet<StarterPerk>();
        for (var fallen = 0; fallen < 6; fallen++) perks.Add(StarterPerks.Pick(7 * 7919 + fallen));
        Assert.True(perks.Count > 1);
    }

    [Fact]
    public void EachPerk_EchoesADistinctUpgrade()
    {
        Assert.Equal(StarterPerks.All.Length, StarterPerks.All.Select(StarterPerks.Echoes).Distinct().Count());
    }
}
