namespace Combat.Tests;

public class PostureAndBufferTests
{
    [Fact]
    public void Posture_BreaksWhenFull_AndDrainsAfterTheDelay()
    {
        var p = new PostureMeter(100);
        Assert.False(p.Add(60));
        p.Tick(500, drainPerSecond: 20, drainDelayMs: 600);
        Assert.Equal(60, p.Value);          // still inside the delay
        p.Tick(500, 20, 600);
        Assert.Equal(50, p.Value, 6);       // past the delay: this tick drains 0.5 s at 20/s
        Assert.True(p.Add(80));
        Assert.Equal(100, p.Value);
    }

    [Fact]
    public void Posture_HoldDrain_StopsDraining()
    {
        var p = new PostureMeter(100);
        p.Add(50);
        p.Tick(5000, 20, 0, holdDrain: true);
        Assert.Equal(50, p.Value);
        p.Tick(1000, 20, 0);
        Assert.Equal(30, p.Value, 6);
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1100, true)]   // exactly 100 ms later still counts
    [InlineData(1100.5, false)]
    public void InputBuffer_KeepsPressesForItsWindow(double now, bool expected)
    {
        var b = new InputBuffer<string>(100);
        b.Press("attack", 1000);
        Assert.Equal(expected, b.Has("attack", now));
    }

    [Fact]
    public void InputBuffer_ConsumeForgetsThePress()
    {
        var b = new InputBuffer<string>(100);
        b.Press("roll", 0);
        Assert.True(b.TryConsume("roll", 50));
        Assert.False(b.TryConsume("roll", 60));
    }

    [Fact]
    public void InputBuffer_PressSixFramesBeforeAStateEnds_Counts()
    {
        var b = new InputBuffer<string>(100);
        b.Press("attack", Frames.Ms(100));
        Assert.True(b.Has("attack", Frames.Ms(106)));  // 100 ms
        Assert.False(b.Has("attack", Frames.Ms(107)));
    }
}
