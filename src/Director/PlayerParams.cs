namespace SwordInTheForest.Director
{
    /// <summary>One player's reaction model: median e^m (ms), log-spread s and lapse rate λ.</summary>
    public readonly struct PlayerParams
    {
        public PlayerParams(double medianMs, double spread, double lapse)
        {
            MedianMs = medianMs;
            Spread = spread;
            Lapse = lapse;
        }

        public double MedianMs { get; }
        public double Spread { get; }
        public double Lapse { get; }

        public override string ToString() => $"median {MedianMs:0} ms s {Spread:0.00} λ {Lapse:0.00}";
    }
}
