namespace Combat.Tests;

public class FirstRedLeadTests
{
    private sealed class Fixed : ILeadTimeSource
    {
        public int NextLeadMs(AttackSpec attack) => 300;
    }

    private static AttackSpec Spec(GlintColor c) => new("a", c, 100, 200, 400, 200, 800, 300);

    [Fact]
    public void OnlyTheFirstRedAttack_GetsTheGenerousLead()
    {
        var src = new FirstRedLead(() => new Fixed(), 800);
        Assert.Equal(300, src.NextLeadMs(Spec(GlintColor.White)));
        Assert.Equal(800, src.NextLeadMs(Spec(GlintColor.Red)));
        Assert.Equal(300, src.NextLeadMs(Spec(GlintColor.Red)));
        Assert.Equal(0, src.Remaining);
    }

    [Fact]
    public void NeverShortensALongerLead()
    {
        var src = new FirstRedLead(() => new Fixed(), 200);
        Assert.Equal(300, src.NextLeadMs(Spec(GlintColor.Red)));
    }
}
