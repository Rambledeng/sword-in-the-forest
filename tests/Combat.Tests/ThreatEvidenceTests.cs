using SwordInTheForest.Director;

namespace Combat.Tests;

/// <summary>How telemetry becomes director evidence (M2 decisions in PROGRESS.md).</summary>
public class ThreatEvidenceTests
{
    private const double Arc = 220;

    private static ThreatEvent? Map(Response response, Outcome outcome, int? press, int? angle = 10, GlintColor glint = GlintColor.White) =>
        ThreatEvidence.From(new TelemetryRecord("r", 1, "a", glint, 420, press, response, outcome, angle), Arc);

    [Fact]
    public void DeflectAndRollInTheWindow_AreTimedSuccesses()
    {
        var e = Map(Response.Deflect, Outcome.Success, 300)!.Value;
        Assert.Equal(EvidenceKind.Timed, e.Kind);
        Assert.Equal(300, e.PressMs);
        Assert.True(e.Success);
        Assert.Equal(EvidenceKind.Timed, Map(Response.Roll, Outcome.Success, 250, glint: GlintColor.Red)!.Value.Kind);
    }

    [Fact]
    public void LatePress_IsTimed_AndAFailure()
    {
        var e = Map(Response.None, Outcome.Hit, 520)!.Value;
        Assert.Equal(EvidenceKind.Timed, e.Kind);
        Assert.False(e.Success);
    }

    [Fact]
    public void AntiMashBlock_UsesThePressTime()
    {
        // A guard press inside the window that the mash lockout turned into a block.
        var e = Map(Response.Block, Outcome.Blocked, 310)!.Value;
        Assert.Equal(EvidenceKind.Timed, e.Kind);
        Assert.Equal(310, e.PressMs);
        Assert.True(e.Success);
    }

    [Fact]
    public void NoPress_IsAMissAtTheLeadTime()
    {
        var e = Map(Response.None, Outcome.Hit, null)!.Value;
        Assert.Equal(EvidenceKind.NoResponse, e.Kind);
        Assert.Equal(420, e.LeadMs);
    }

    [Theory]
    [InlineData(Response.Block, Outcome.Blocked)]  // guard raised before the glint
    [InlineData(Response.Roll, Outcome.Evaded)]    // roll started before the glint
    [InlineData(Response.Guard, Outcome.Hit)]      // guard held before a red glint
    [InlineData(Response.None, Outcome.Whiff)]     // out of reach and no press
    public void Anticipation_AndHarmlessNonResponses_AreSkipped(Response response, Outcome outcome) =>
        Assert.Null(Map(response, outcome, null));

    [Fact]
    public void DeflectBeyondTheGuardArc_IsALapse_NotTiming()
    {
        var back = Map(Response.Deflect, Outcome.Hit, 200, angle: 175)!.Value;
        Assert.Equal(EvidenceKind.Lapse, back.Kind);
        Assert.False(back.Success);
        // Inside the arc a failed deflect is still timing evidence.
        Assert.Equal(EvidenceKind.Timed, Map(Response.Deflect, Outcome.Hit, 200, angle: 100)!.Value.Kind);
    }

    [Fact]
    public void GuardPressedAgainstRed_IsALapse() =>
        Assert.Equal(EvidenceKind.Lapse, Map(Response.Guard, Outcome.Hit, 200, glint: GlintColor.Red)!.Value.Kind);
}
