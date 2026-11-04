using SwordInTheForest.Director;

namespace SwordInTheForest.Sim;

/// <summary>
/// A model the director does not assume: RT = Normal(μ, σ) + Exponential(τ), the ex-Gaussian
/// that fits human choice reaction times well. Sharper on the fast side and heavier on the slow
/// side than a log-normal with the same median.
/// </summary>
public sealed class ExGaussianTime : IReactionTime
{
    public ExGaussianTime(double muMs, double sigmaMs, double tauMs)
    {
        if (sigmaMs <= 0 || tauMs <= 0) throw new ArgumentException("σ and τ must be positive.");
        MuMs = muMs;
        SigmaMs = sigmaMs;
        TauMs = tauMs;
        MedianMs = Quantile(0.5);
    }

    public double MuMs { get; }
    public double SigmaMs { get; }
    public double TauMs { get; }
    public double MedianMs { get; }

    /// <summary>The ex-Gaussian with this σ and τ whose median is <paramref name="medianMs"/>.</summary>
    public static ExGaussianTime WithMedian(double medianMs, double sigmaMs, double tauMs)
    {
        // The median is μ plus a constant that depends only on σ and τ.
        var offset = new ExGaussianTime(0, sigmaMs, tauMs).MedianMs;
        return new ExGaussianTime(medianMs - offset, sigmaMs, tauMs);
    }

    /// <summary>
    /// A player whose median, and lapse, come from a log-normal player; σ is 6–15% of the median
    /// and τ 20–50% of it (typical ex-Gaussian fits put τ at a quarter to a half of the mean).
    /// </summary>
    public static ExGaussianTime Like(PlayerParams p, Random rng) =>
        WithMedian(p.MedianMs, p.MedianMs * (0.06 + 0.09 * rng.NextDouble()), p.MedianMs * (0.20 + 0.30 * rng.NextDouble()));

    public double SampleMs(Random rng) =>
        Math.Max(1, MuMs + SigmaMs * PopulationModel.Gaussian(rng) - TauMs * Math.Log(1.0 - rng.NextDouble()));

    public double Cdf(double x)
    {
        var z = (x - MuMs) / SigmaMs;
        var tail = Math.Exp(-(x - MuMs) / TauMs + SigmaMs * SigmaMs / (2 * TauMs * TauMs) + LogPhi(z - SigmaMs / TauMs));
        return ReactionMath.Phi(z) - tail;
    }

    private double Quantile(double p)
    {
        double lo = MuMs - 10 * SigmaMs, hi = MuMs + 10 * SigmaMs + 20 * TauMs;
        for (var i = 0; i < 100; i++)
        {
            var mid = 0.5 * (lo + hi);
            if (Cdf(mid) < p) lo = mid; else hi = mid;
        }
        return 0.5 * (lo + hi);
    }

    private static double LogPhi(double z) => z > -30 ? Math.Log(Math.Max(1e-300, ReactionMath.Phi(z))) : -0.5 * z * z - Math.Log(-z) - 0.9189385332;
}
