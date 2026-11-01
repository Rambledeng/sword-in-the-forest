namespace Combat.Tests;

/// <summary>
/// The M1 bot rules, run frame by frame at 60 Hz through the real AttackRunner and ThreatWindow.
/// The headless scenarios repeat them inside the game with the real player and enemy.
/// </summary>
public class AttackRunnerTests
{
    private enum Policy { Right, Never, GuardAll }

    private static readonly AttackSpec White = new("saber_slash_1", GlintColor.White, 450, 900, 420, 350, 550, 300);
    private static readonly AttackSpec Red = new("test_thrust_red", GlintColor.Red, 500, 800, 450, 350, 550, 300);

    /// <summary>A bot that responds a fixed delay after each glint, and stands in reach.</summary>
    private sealed class Bot : IThreatTarget
    {
        private readonly Policy _policy;
        private readonly double _delayMs;
        private readonly List<(ThreatWindow Threat, double At)> _scheduled = new();
        public readonly List<ThreatWindow> Open = new();
        public readonly List<ThreatWindow> Closed = new();

        public Bot(Policy policy, double delayMs) { _policy = policy; _delayMs = delayMs; }

        public void OpenThreat(ThreatWindow t)
        {
            Open.Add(t);
            if (_policy != Policy.Never) _scheduled.Add((t, t.GlintMs + _delayMs));
        }

        public void CloseThreat(ThreatWindow t) { Open.Remove(t); Closed.Add(t); }

        public void Tick(double now)
        {
            foreach (var s in _scheduled.Where(s => now >= s.At - 1e-6).ToList())
            {
                _scheduled.Remove(s);
                var action = _policy == Policy.Right && s.Threat.Glint == GlintColor.Red ? DefenseAction.Roll : DefenseAction.Guard;
                s.Threat.RecordAction(action, now);
            }
        }

        public ImpactState State() => new(inReach: true, invulnerable: false, guarding: false);
    }

    private static List<ThreatWindow> Run(Policy policy, AttackSpec attack, int leadMs, int attacks = 5, double delayMs = 150)
    {
        var bot = new Bot(policy, delayMs);
        var runner = new AttackRunner(new FixedLeadTimeSource(leadMs), seed: leadMs) { Target = bot };
        runner.Impact += t => t.Resolve(bot.State());
        var started = 0;
        for (var frame = 0; frame < 60 * 60 && bot.Closed.Count < attacks; frame++)
        {
            var now = Frames.Ms(frame);
            bot.Tick(now);                       // the player acts first in a tick
            if (!runner.Running && started < attacks) { runner.Start(attack, now); started++; }
            runner.Advance(now);
        }
        Assert.Equal(attacks, bot.Closed.Count);
        Assert.Empty(bot.Open);
        return bot.Closed;
    }

    public static IEnumerable<object[]> Leads() =>
        new[] { 200, 210, 233, 250, 300, 350, 420, 500, 550, 650, 800 }.Select(l => new object[] { l });

    [Theory]
    [MemberData(nameof(Leads))]
    public void RightResponseAt150Ms_IsNeverHit(int lead)
    {
        foreach (var attack in new[] { White, Red })
        foreach (var t in Run(Policy.Right, attack, lead))
        {
            Assert.Equal(Outcome.Success, t.Result!.Value.Outcome);
            Assert.InRange(t.PressMs!.Value, 150 - 1e-6, 150 + 1000.0 / 60);
        }
    }

    [Theory]
    [MemberData(nameof(Leads))]
    public void NeverResponding_IsHitByEveryAttack(int lead)
    {
        foreach (var attack in new[] { White, Red })
        foreach (var t in Run(Policy.Never, attack, lead))
        {
            Assert.True(t.Result!.Value.PlayerHit);
            Assert.Null(t.PressMs);
        }
    }

    [Theory]
    [MemberData(nameof(Leads))]
    public void GuardingAgainstRed_IsHit(int lead)
    {
        foreach (var t in Run(Policy.GuardAll, Red, lead))
        {
            Assert.Equal(Outcome.Hit, t.Result!.Value.Outcome);
            Assert.Equal(Response.Guard, t.Result!.Value.Response);
        }
        foreach (var t in Run(Policy.GuardAll, White, lead))
            Assert.Equal(Response.Deflect, t.Result!.Value.Response);
    }

    [Fact]
    public void TooSlowForTheLead_IsHit_AndTheLatePressIsTimed()
    {
        foreach (var t in Run(Policy.Right, White, 120))
        {
            Assert.Equal(Outcome.Hit, t.Result!.Value.Outcome);
            Assert.True(t.PressMs > 120);
        }
    }

    [Fact]
    public void GlintAndImpact_LandOnFrames_WindowNeverShorterThanLead()
    {
        var bot = new Bot(Policy.Never, 0);
        var runner = new AttackRunner(new FixedLeadTimeSource(205), seed: 3) { Target = bot };
        double? glintFrame = null, impactFrame = null;
        runner.Glint += t => glintFrame = t.GlintMs;
        runner.Impact += t => { impactFrame = Frames.Ms(_frame); t.Resolve(bot.State()); };
        runner.Start(White, 0);
        for (_frame = 0; _frame < 200; _frame++) runner.Advance(Frames.Ms(_frame));
        Assert.NotNull(glintFrame);
        Assert.True(impactFrame - glintFrame >= 205 - 1e-6);
        Assert.True(impactFrame - glintFrame < 205 + 1000.0 / 60);
    }

    private static int _frame;

    [Fact]
    public void ScaleRecovery_ShortensTheRecovery()
    {
        var bot = new Bot(Policy.Never, 0);
        var runner = new AttackRunner(new FixedLeadTimeSource(300), seed: 2) { Target = bot };
        double impact = -1, finished = -1;
        var f = 0;
        runner.Impact += t => { impact = Frames.Ms(f); t.Resolve(bot.State()); runner.ScaleRecovery(0.5); };
        runner.Finished += _ => finished = Frames.Ms(f);
        runner.Start(White, 0); // recovery 300 ms
        for (; f < 300 && finished < 0; f++) runner.Advance(Frames.Ms(f));
        Assert.InRange(finished - impact, 150 - 1e-6, 150 + 1000.0 / 60);
    }

    [Fact]
    public void SkipToGlint_GlintsNextFrame_WithTheSameLead()
    {
        var bot = new Bot(Policy.Never, 0);
        var runner = new AttackRunner(new FixedLeadTimeSource(400), seed: 2) { Target = bot };
        double? glint = null;
        runner.Glint += t => glint = t.GlintMs;
        runner.Start(White, 0); // windup 450-900 ms
        for (var f = 0; f <= 6; f++) runner.Advance(Frames.Ms(f));
        Assert.Null(glint);
        Assert.True(runner.SkipToGlint(Frames.Ms(6)));
        runner.Advance(Frames.Ms(7));
        Assert.Equal(Frames.Ms(7), glint!.Value, 6);
        Assert.Equal(400, runner.Threat!.LeadMs);
        Assert.False(runner.SkipToGlint(Frames.Ms(8)));
    }

    [Fact]
    public void Abort_BeforeImpact_ClosesWithoutTelemetry()
    {
        var bot = new Bot(Policy.Never, 0);
        var runner = new AttackRunner(new FixedLeadTimeSource(400), seed: 1) { Target = bot };
        var finalized = 0;
        runner.Finalized += _ => finalized++;
        runner.Glint += _ => runner.Abort();
        runner.Start(White, 0);
        for (var f = 0; f < 200; f++) runner.Advance(Frames.Ms(f));
        Assert.Equal(0, finalized);
        Assert.Empty(bot.Open);
        Assert.False(runner.Running);
    }

    [Fact]
    public void Finalize_WaitsForTheGraceOrALatePress()
    {
        var bot = new Bot(Policy.Never, 0);
        var runner = new AttackRunner(new FixedLeadTimeSource(300), seed: 1) { Target = bot, LatePressGraceMs = 400 };
        ThreatWindow? done = null;
        double doneAt = 0;
        runner.Finalized += t => { done = t; doneAt = runner.Threat!.ImpactMs; };
        runner.Impact += t => t.Resolve(bot.State());
        runner.Start(White, 0);
        var f = 0;
        for (; done == null && f < 300; f++) runner.Advance(Frames.Ms(f));
        Assert.NotNull(done);
        Assert.True(Frames.Ms(f - 1) >= done!.ImpactMs + 400 - 1e-6);
        Assert.Null(done.PressMs);
    }
}
