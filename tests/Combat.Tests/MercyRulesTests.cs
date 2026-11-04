using SwordInTheForest.Run;

namespace Combat.Tests;

public class MercyRulesTests
{
    [Fact]
    public void FirstBeggar_IsAlwaysGenuine()
    {
        var m = new MercyRules(fakeChance: 1.0);
        Assert.Equal(BegKind.Genuine, m.Decide(1.0, () => 0.0));
        Assert.Equal(BegKind.Fake, m.Decide(1.0, () => 0.0));
        Assert.Equal(2, m.Begs);
        Assert.Equal(1, m.Fakes);
    }

    [Fact]
    public void NoSurrender_Kneels()
    {
        var m = new MercyRules(0.33);
        Assert.Equal(BegKind.None, m.Decide(0.25, () => 0.5));
        Assert.Equal(BegKind.None, m.Decide(0.0, () => 0.0));
        Assert.Equal(0, m.Begs);
    }

    [Fact]
    public void AboutOneInThree_LaterBeggarsAreFake()
    {
        var rng = new System.Random(3);
        var m = new MercyRules(1.0 / 3);
        m.Decide(1.0, rng.NextDouble);
        var fakes = 0;
        for (var i = 0; i < 3000; i++) if (m.Decide(1.0, rng.NextDouble) == BegKind.Fake) fakes++;
        Assert.InRange(fakes / 3000.0, 0.30, 0.37);
    }

    [Fact]
    public void ForceNext_OverridesOnce()
    {
        var m = new MercyRules(0.0) { ForceNext = BegKind.Fake };
        Assert.Equal(BegKind.Fake, m.Decide(0.0, () => 0.9));
        Assert.Null(m.ForceNext);
        Assert.Equal(BegKind.None, m.Decide(0.0, () => 0.9));
    }
}
