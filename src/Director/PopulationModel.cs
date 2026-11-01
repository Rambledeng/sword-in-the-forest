using System;

namespace SwordInTheForest.Director
{
    /// <summary>
    /// A population of players: log-median ~ Normal, spread and lapse ~ Uniform. Simulated players
    /// are drawn from it, and the director's prior is fitted to a training sample of it.
    /// </summary>
    public sealed class PopulationModel
    {
        public PopulationModel(double medianOfMediansMs, double logMedianSd, double spreadMin, double spreadMax,
            double lapseMin, double lapseMax, double medianMinMs = 160, double medianMaxMs = 850)
        {
            MedianOfMediansMs = medianOfMediansMs;
            LogMedianSd = logMedianSd;
            SpreadMin = spreadMin;
            SpreadMax = spreadMax;
            LapseMin = lapseMin;
            LapseMax = lapseMax;
            MedianMinMs = medianMinMs;
            MedianMaxMs = medianMaxMs;
        }

        /// <summary>
        /// Players reacting to a glint and choosing deflect or roll: median of medians 330 ms
        /// (log-sd 0.25, so 80% between about 240 and 450 ms), spread 0.12–0.38, lapses 0–10%.
        /// </summary>
        public static PopulationModel Default { get; } = new PopulationModel(330, 0.25, 0.12, 0.38, 0.0, 0.10);

        public double MedianOfMediansMs { get; }
        public double LogMedianSd { get; }
        public double SpreadMin { get; }
        public double SpreadMax { get; }
        public double LapseMin { get; }
        public double LapseMax { get; }
        public double MedianMinMs { get; }
        public double MedianMaxMs { get; }

        /// <summary>The same population, slower (factor &gt; 1) or faster (&lt; 1).</summary>
        public PopulationModel Shifted(double medianFactor) => new PopulationModel(MedianOfMediansMs * medianFactor,
            LogMedianSd, SpreadMin, SpreadMax, LapseMin, LapseMax, MedianMinMs, MedianMaxMs);

        public PlayerParams Sample(Random rng)
        {
            var median = Math.Exp(Math.Log(MedianOfMediansMs) + LogMedianSd * Gaussian(rng));
            median = Math.Max(MedianMinMs, Math.Min(MedianMaxMs, median));
            var spread = SpreadMin + (SpreadMax - SpreadMin) * rng.NextDouble();
            var lapse = LapseMin + (LapseMax - LapseMin) * rng.NextDouble();
            return new PlayerParams(median, spread, lapse);
        }

        /// <summary>Standard normal draw (Box–Muller).</summary>
        public static double Gaussian(Random rng)
        {
            var u1 = 1.0 - rng.NextDouble();
            var u2 = rng.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }
    }
}
