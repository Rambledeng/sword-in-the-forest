namespace Combat.Tests;

public class RollTimelineTests
{
    private static readonly RollTimeline Roll = new(400, 40, 340);

    [Theory]
    [InlineData(39.999, false)]
    [InlineData(40.0, true)]   // start is inclusive
    [InlineData(200.0, true)]
    [InlineData(339.999, true)]
    [InlineData(340.0, false)] // end is exclusive
    public void Invulnerability_ExactBoundaries(double elapsedMs, bool expected)
    {
        Assert.Equal(expected, Roll.IsInvulnerable(elapsedMs));
    }

    [Theory]
    [InlineData(0, false)]  // 0 ms: the roll's first frame
    [InlineData(2, false)]  // 33.3 ms
    [InlineData(3, true)]   // 50 ms: first invulnerable frame
    [InlineData(20, true)]  // 333.3 ms: last invulnerable frame
    [InlineData(21, false)] // 350 ms
    [InlineData(23, false)] // 383.3 ms: still rolling, vulnerable again
    public void Invulnerability_At60HzFrames(int frame, bool expected)
    {
        Assert.Equal(expected, Roll.IsInvulnerable(Frames.Ms(frame)));
    }

    [Fact]
    public void Invulnerability_At60Hz_CoversEighteenFrames()
    {
        var count = Enumerable.Range(0, 30).Count(f => Roll.IsInvulnerable(Frames.Ms(f)));
        Assert.Equal(18, count); // frames 3..20 = 300 ms
    }

    [Theory]
    [InlineData(23, false)] // 383.3 ms
    [InlineData(24, true)]  // 400 ms: the roll ends on this frame
    public void IsOver_AtFrames(int frame, bool expected)
    {
        Assert.Equal(expected, Roll.IsOver(Frames.Ms(frame)));
    }

    [Fact]
    public void InvulnEnd_IsClampedToDuration()
    {
        var roll = new RollTimeline(300, 40, 340);
        Assert.False(roll.IsInvulnerable(300));
        Assert.True(roll.IsInvulnerable(299));
    }

    [Fact]
    public void Constructor_RejectsBadRanges()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RollTimeline(0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RollTimeline(400, 300, 100));
    }
}
