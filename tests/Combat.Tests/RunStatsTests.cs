using SwordInTheForest.Run;

namespace Combat.Tests;

public class RunStatsTests
{
    [Fact]
    public void MeritKarmaAndKills_FollowTheSpec()
    {
        var s = new RunStats();
        s.Finished(begging: false); // a kneeling enemy: merit
        s.Finished(begging: true);  // one that begged: karma
        s.Spare();                  // spared: merit
        s.Killed();
        Assert.Equal(2, s.Merit);
        Assert.Equal(1, s.Karma);
        Assert.Equal(3, s.Kills);
        Assert.Equal(1, s.Spared);
    }

    [Theory]
    [InlineData(3, 0, true, "sword.verdict.win_merit")]
    [InlineData(1, 1, true, "sword.verdict.win_balanced")]
    [InlineData(0, 2, false, "sword.verdict.loss_karma")]
    [InlineData(2, 1, false, "sword.verdict.loss_balanced")]
    public void Verdict_LeansWithMeritAndKarma(int spared, int begging, bool won, string key)
    {
        var s = new RunStats();
        for (var i = 0; i < spared; i++) s.Spare();
        for (var i = 0; i < begging; i++) s.Finished(begging: true);
        s.End(won);
        s.End(!won); // the first end sticks
        Assert.Equal(key, s.VerdictKey);
    }

    [Fact]
    public void ReadingWatcher_SpeaksOnAttunementThenOnShifts()
    {
        var w = new ReadingWatcher(0.5, 0.12, 4);
        Assert.Equal(ReadingChange.None, w.Observe(400, 0.2, 3));
        Assert.Equal(ReadingChange.Attuned, w.Observe(300, 0.55, 10));
        Assert.Equal(ReadingChange.None, w.Observe(250, 0.6, 12)); // too soon
        Assert.Equal(ReadingChange.Quicker, w.Observe(250, 0.6, 14));
        Assert.Equal(ReadingChange.None, w.Observe(270, 0.6, 20)); // under 12%
        Assert.Equal(ReadingChange.Slower, w.Observe(300, 0.6, 24));
        var primed = new ReadingWatcher(0.5, 0.12, 4);
        primed.Prime(300, 50);
        Assert.Equal(ReadingChange.None, primed.Observe(305, 0.9, 60));
    }
}
