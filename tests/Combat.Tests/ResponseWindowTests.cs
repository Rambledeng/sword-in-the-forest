namespace Combat.Tests;

/// <summary>The response window is [glint, impact]: the right response anywhere in it succeeds.</summary>
public class ResponseWindowTests
{
    private static readonly AttackSpec White = new("saber_slash", GlintColor.White, 400, 800, 420, 350, 550, 600);
    private static readonly AttackSpec Red = new("spear_thrust", GlintColor.Red, 400, 800, 450, 350, 550, 600);
    private static readonly ImpactState Open = new(inReach: true, invulnerable: false, guarding: false);
    private static readonly ImpactState Guarding = new(inReach: true, invulnerable: false, guarding: true);

    [Fact]
    public void GuardOnTheGlintFrame_Deflects()
    {
        var w = new ThreatWindow(White, 1000, 420);
        w.RecordAction(DefenseAction.Guard, 1000);
        Assert.Equal(new Resolution(Response.Deflect, Outcome.Success).ToString(), w.Resolve(Guarding).ToString());
        Assert.Equal(0, w.PressMs);
    }

    [Fact]
    public void GuardOnTheImpactFrame_Deflects()
    {
        var w = new ThreatWindow(White, 1000, 420);
        w.RecordAction(DefenseAction.Guard, 1420);
        Assert.Equal(Response.Deflect, w.Resolve(Guarding).Response);
        Assert.Equal(420, w.PressMs);
    }

    [Fact]
    public void GuardOneFrameBeforeTheGlint_OnlyBlocks()
    {
        var glint = Frames.Ms(60);
        var w = new ThreatWindow(White, glint, 420);
        w.RecordAction(DefenseAction.Guard, Frames.Ms(59)); // anticipation: no timing information
        var r = w.Resolve(Guarding);
        Assert.Equal(Response.Block, r.Response);
        Assert.Equal(Outcome.Blocked, r.Outcome);
        Assert.Null(w.PressMs);
    }

    [Fact]
    public void GuardOneFrameAfterTheImpact_IsHit_ButStillTimed()
    {
        var glint = Frames.Ms(60);
        var lead = 200;
        var w = new ThreatWindow(White, glint, lead);
        var impactFrame = Frames.AtOrAfter(glint + lead);
        Assert.Equal(Outcome.Hit, w.Resolve(Open).Outcome);
        w.RecordPress(Frames.Ms(impactFrame + 1)); // a late press after the impact
        Assert.Equal(Response.None, w.Result!.Value.Response);
        Assert.True(w.PressMs > lead, $"late press should be timed after the lead, got {w.PressMs}");
    }

    [Fact]
    public void NoResponse_IsHit_WithNoPress()
    {
        var w = new ThreatWindow(White, 1000, 420);
        var r = w.Resolve(Open);
        Assert.Equal(Response.None, r.Response);
        Assert.True(r.PlayerHit);
        Assert.Null(w.PressMs);
    }

    [Fact]
    public void Red_GuardHeld_IsHit()
    {
        var w = new ThreatWindow(Red, 1000, 450);
        var r = w.Resolve(Guarding);
        Assert.Equal(Response.Guard, r.Response);
        Assert.Equal(Outcome.Hit, r.Outcome);
    }

    [Fact]
    public void Red_GuardTappedInWindow_IsHit()
    {
        var w = new ThreatWindow(Red, 1000, 450);
        w.RecordAction(DefenseAction.Guard, 1150);
        Assert.Equal(Outcome.Hit, w.Resolve(Guarding).Outcome);
        Assert.Equal(150, w.PressMs);
    }

    [Theory]
    [InlineData(GlintColor.White)]
    [InlineData(GlintColor.Red)]
    public void RollInWindow_Succeeds_EvenAfterTheInvulnerabilityEnds(GlintColor color)
    {
        var w = new ThreatWindow(color == GlintColor.Red ? Red : White, 1000, 800);
        w.RecordAction(DefenseAction.Roll, 1150);
        Assert.Equal(new Resolution(Response.Roll, Outcome.Success).ToString(), w.Resolve(Open).ToString());
    }

    [Fact]
    public void RollBeforeTheGlint_StillInvulnerable_Evades()
    {
        var w = new ThreatWindow(Red, 1000, 450);
        w.RecordAction(DefenseAction.Roll, 900);
        var r = w.Resolve(new ImpactState(inReach: true, invulnerable: true, guarding: false));
        Assert.Equal(Outcome.Evaded, r.Outcome);
        Assert.Null(w.PressMs);
    }

    [Fact]
    public void OutOfReach_IsWhiff()
    {
        var w = new ThreatWindow(White, 1000, 420);
        Assert.Equal(Outcome.Whiff, w.Resolve(new ImpactState(false, false, false)).Outcome);
    }

    [Theory]
    [InlineData(DefenseAction.Roll, "Roll/Success")]
    [InlineData(DefenseAction.Guard, "Deflect/Success")]
    public void RightResponse_OutOfReach_StillSucceeds(DefenseAction action, string expected)
    {
        var w = new ThreatWindow(White, 1000, 420);
        w.RecordAction(action, 1200);
        Assert.Equal(expected, w.Resolve(new ImpactState(false, false, false)).ToString());
    }

    [Fact]
    public void LatestActionInWindow_Wins()
    {
        var w = new ThreatWindow(White, 1000, 420);
        w.RecordAction(DefenseAction.Roll, 1050);
        w.RecordAction(DefenseAction.Guard, 1300);
        Assert.Equal(Response.Deflect, w.Resolve(Guarding).Response);
        Assert.Equal(50, w.PressMs); // timing uses the first press
    }

    [Fact]
    public void ResultIsDecidedOnce()
    {
        var w = new ThreatWindow(White, 1000, 420);
        w.Resolve(Open);
        w.RecordAction(DefenseAction.Guard, 1400);
        Assert.Equal(Outcome.Hit, w.Resolve(Guarding).Outcome);
    }

    /// <summary>
    /// The M1 bot rule at frame granularity: a correct response 150 ms after the glint (rounded up to
    /// the next 60 Hz frame) succeeds for every lead time from 200 ms up, for both colours.
    /// </summary>
    [Fact]
    public void BotAt150Ms_NeverHit_ForLeadsFrom200()
    {
        foreach (var lead in Enumerable.Range(200, 601))
        foreach (var color in new[] { GlintColor.White, GlintColor.Red })
        foreach (var glintFrame in new[] { 37, 60, 61 })
        {
            var glint = Frames.Ms(glintFrame);
            var w = new ThreatWindow(color == GlintColor.Red ? Red : White, glint, lead);
            var press = Frames.Ms(Frames.AtOrAfter(glint + 150));
            w.RecordAction(color == GlintColor.Red ? DefenseAction.Roll : DefenseAction.Guard, press);
            var r = w.Resolve(Open);
            Assert.False(r.PlayerHit, $"lead {lead} {color} glint frame {glintFrame}: {r}");
            Assert.Equal(Outcome.Success, r.Outcome);
        }
    }
}
