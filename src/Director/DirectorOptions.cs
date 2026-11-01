namespace SwordInTheForest.Director
{
    /// <summary>Filter and display settings. The defaults are the tuned values the tests hold to.</summary>
    public sealed class DirectorOptions
    {
        /// <summary>
        /// Before each update, this share of every cell's weight moves to each neighbouring median
        /// (a random walk on m), so the belief can follow a player who improves or tires.
        /// </summary>
        public double BlurPerAttack { get; set; } = 0.03;
        /// <summary>Density (1/ms) of a press made during a lapse: a flat guess over 1.5 s.</summary>
        public double LapsePressDensity { get; set; } = 1.0 / 1500;
        /// <summary>Relative 80% interval width (high − low) / median at which attunement reads 0…</summary>
        public double UnsureWidth { get; set; } = 0.65;
        /// <summary>…and at which it reads 1.</summary>
        public double AttunedWidth { get; set; } = 0.12;
        /// <summary>Calibration tries every this many ms of lead (a multiple of the 5 ms ladder).</summary>
        public int CalibrationStepMs { get; set; } = 10;
        /// <summary>Observations kept for predicted-against-observed displays.</summary>
        public int HistoryLimit { get; set; } = 200;
    }
}
