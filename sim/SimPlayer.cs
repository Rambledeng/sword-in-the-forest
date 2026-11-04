using SwordInTheForest.Director;

namespace SwordInTheForest.Sim;

/// <summary>
/// A simulated player with known reaction parameters. Each attack: with probability λ a lapse
/// (no press); otherwise a log-normal reaction time, plus the true input delay, landing on the
/// next 60 Hz frame. A press later than the impact is still timed up to the grace period, as in
/// the game; after that it counts as no press.
/// </summary>
public sealed class SimPlayer
{
    public const double FrameMs = 1000.0 / 60;

    public SimPlayer(PlayerParams truth, double inputDelayMs = 17, double latePressGraceMs = 400)
        : this(truth, new LogNormalTime(truth.MedianMs, truth.Spread), inputDelayMs, latePressGraceMs)
    {
    }

    /// <summary>A player whose reaction times come from <paramref name="reaction"/>; only the lapse rate of <paramref name="truth"/> is used.</summary>
    public SimPlayer(PlayerParams truth, IReactionTime reaction, double inputDelayMs = 17, double latePressGraceMs = 400)
    {
        Truth = truth;
        Reaction = reaction;
        InputDelayMs = inputDelayMs;
        LatePressGraceMs = latePressGraceMs;
    }

    public PlayerParams Truth { get; }
    public IReactionTime Reaction { get; }
    public double InputDelayMs { get; }
    public double LatePressGraceMs { get; }

    public ThreatEvent Respond(int leadMs, Random rng)
    {
        if (rng.NextDouble() < Truth.Lapse) return ThreatEvent.NoResponse(leadMs);
        var rt = Reaction.SampleMs(rng);
        var press = Math.Ceiling((InputDelayMs + rt) / FrameMs) * FrameMs;
        press = Math.Round(press);
        return press <= leadMs + LatePressGraceMs ? ThreatEvent.Timed(leadMs, press) : ThreatEvent.NoResponse(leadMs);
    }
}
