namespace SwordInTheForest.Director
{
    /// <summary>The director's reading of the player.</summary>
    public readonly struct Estimate
    {
        public Estimate(double medianMs, double lowMs, double highMs, double spread, double lapse, double attunement, int observations)
        {
            MedianMs = medianMs;
            LowMs = lowMs;
            HighMs = highMs;
            Spread = spread;
            Lapse = lapse;
            Attunement = attunement;
            Observations = observations;
        }

        /// <summary>Posterior median of the player's median reaction time e^m (ms).</summary>
        public double MedianMs { get; }
        /// <summary>10th and 90th percentiles of the belief about the median (an 80% interval).</summary>
        public double LowMs { get; }
        public double HighMs { get; }
        /// <summary>Posterior mean log-spread s.</summary>
        public double Spread { get; }
        /// <summary>Posterior mean lapse rate λ.</summary>
        public double Lapse { get; }
        /// <summary>0..1: how sure the sword is (人剑合一), from the interval's relative width.</summary>
        public double Attunement { get; }
        public int Observations { get; }

        public override string ToString() =>
            $"median {MedianMs:0} ms [{LowMs:0}, {HighMs:0}] s {Spread:0.00} λ {Lapse:0.00} attune {Attunement:0.00} n {Observations}";
    }
}
