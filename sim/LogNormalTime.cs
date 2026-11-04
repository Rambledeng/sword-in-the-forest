using SwordInTheForest.Director;

namespace SwordInTheForest.Sim;

/// <summary>The director's own model: ln(RT) ~ Normal(ln median, spread).</summary>
public sealed class LogNormalTime : IReactionTime
{
    public LogNormalTime(double medianMs, double spread)
    {
        MedianMs = medianMs;
        Spread = spread;
    }

    public double MedianMs { get; }
    public double Spread { get; }

    public double SampleMs(Random rng) => Math.Exp(Math.Log(MedianMs) + Spread * PopulationModel.Gaussian(rng));
}
