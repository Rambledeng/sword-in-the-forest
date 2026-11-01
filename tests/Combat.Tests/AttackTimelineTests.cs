namespace Combat.Tests;

public class AttackTimelineTests
{
    private static readonly AttackSpec Slash = new("saber_slash", GlintColor.White, 400, 800, 420, 350, 550, 600);

    [Fact]
    public void Phases_MeetAtTheGlintAndImpact()
    {
        var t = new AttackTimeline(500, 420, 600);
        Assert.Equal(500, t.GlintMs);
        Assert.Equal(920, t.ImpactMs);
        Assert.Equal(1520, t.EndMs);
        Assert.Equal(AttackPhase.Windup, t.PhaseAt(499.999));
        Assert.Equal(AttackPhase.Lead, t.PhaseAt(500));      // the glint frame opens the window
        Assert.Equal(AttackPhase.Lead, t.PhaseAt(920));      // the impact frame is still in it
        Assert.Equal(AttackPhase.Recovery, t.PhaseAt(920.001));
        Assert.Equal(AttackPhase.Done, t.PhaseAt(1520));
    }

    [Theory]
    [InlineData(0.0, 400)]
    [InlineData(0.5, 600)]
    [InlineData(0.99999999, 800)]
    [InlineData(1.0, 800)]
    public void Create_PicksWindupInsideTheRange(double unit, int expected)
    {
        Assert.Equal(expected, AttackTimeline.Create(Slash, 420, unit).WindupMs);
    }

    [Fact]
    public void Create_UsesTheGivenLeadAndRecovery()
    {
        var t = AttackTimeline.Create(Slash, 250, 0.3);
        Assert.Equal(250, t.LeadMs);
        Assert.Equal(600, t.RecoveryMs);
    }

    [Fact]
    public void FixedLeadTimeSource_UsesDefaultOrOverride()
    {
        Assert.Equal(420, new FixedLeadTimeSource().NextLeadMs(Slash));
        Assert.Equal(200, new FixedLeadTimeSource(200).NextLeadMs(Slash));
    }
}
