using System;
using System.Globalization;

namespace SwordInTheForest.Director
{
    /// <summary>
    /// The cells the belief lives on: log-spaced medians × spreads × lapse rates, plus the fixed
    /// input delay d and the lead times decisions are made on (a 5 ms ladder from 200 to 800 ms).
    /// Cell index = (lapse * Spreads + spread) * Medians + median.
    /// </summary>
    public sealed class DirectorGrid
    {
        public const int LeadStepMs = 5;
        private double[]? _phiTable;

        public DirectorGrid(int medians, double medianMinMs, double medianMaxMs, double[] spreads, double[] lapses,
            double inputDelayMs, int leadMinMs = 200, int leadMaxMs = 800)
        {
            if (medians < 2) throw new ArgumentOutOfRangeException(nameof(medians));
            if (spreads.Length == 0 || lapses.Length == 0) throw new ArgumentException("Need at least one spread and lapse.");
            if (leadMinMs - inputDelayMs <= 0) throw new ArgumentOutOfRangeException(nameof(leadMinMs));
            MedianCount = medians;
            MedianMinMs = medianMinMs;
            MedianMaxMs = medianMaxMs;
            Spreads = (double[])spreads.Clone();
            Lapses = (double[])lapses.Clone();
            InputDelayMs = inputDelayMs;
            LeadMinMs = leadMinMs;
            LeadMaxMs = leadMaxMs;
            LogMedians = new double[medians];
            var lo = Math.Log(medianMinMs);
            LogStep = (Math.Log(medianMaxMs) - lo) / (medians - 1);
            for (var i = 0; i < medians; i++) LogMedians[i] = lo + i * LogStep;
        }

        /// <summary>The spec's grid: 62 medians from 150 to 900 ms, 12 spreads, 4 lapse rates (2,976 cells).</summary>
        public static DirectorGrid Default { get; } = new DirectorGrid(62, 150, 900,
            new[] { 0.08, 0.12, 0.16, 0.20, 0.24, 0.28, 0.32, 0.36, 0.40, 0.45, 0.50, 0.56 },
            new[] { 0.0, 0.03, 0.07, 0.12 },
            inputDelayMs: 25);

        public int MedianCount { get; }
        public double MedianMinMs { get; }
        public double MedianMaxMs { get; }
        public double[] LogMedians { get; }
        public double LogStep { get; }
        public double[] Spreads { get; }
        public double[] Lapses { get; }
        /// <summary>d: the press lands this long after the reaction (input and frame delay).</summary>
        public double InputDelayMs { get; }
        public int LeadMinMs { get; }
        public int LeadMaxMs { get; }

        public int SpreadCount => Spreads.Length;
        public int LapseCount => Lapses.Length;
        /// <summary>Cells per lapse slice (medians × spreads).</summary>
        public int SliceSize => MedianCount * SpreadCount;
        public int CellCount => SliceSize * LapseCount;
        public int LeadCount => (LeadMaxMs - LeadMinMs) / LeadStepMs + 1;

        public int LeadAt(int index) => LeadMinMs + index * LeadStepMs;

        /// <summary>
        /// Φ((ln(L − d) − m) / s) for every lead on the ladder (row) and every (spread, median) cell
        /// (column), computed once so a decision costs only multiply-adds.
        /// </summary>
        public double[] PhiTable
        {
            get
            {
                if (_phiTable != null) return _phiTable;
                var slice = SliceSize;
                var table = new double[LeadCount * slice];
                for (var c = 0; c < LeadCount; c++)
                {
                    var lx = Math.Log(LeadAt(c) - InputDelayMs);
                    for (var si = 0; si < SpreadCount; si++)
                        for (var mi = 0; mi < MedianCount; mi++)
                            table[c * slice + si * MedianCount + mi] = ReactionMath.Phi((lx - LogMedians[mi]) / Spreads[si]);
                }
                _phiTable = table;
                return table;
            }
        }

        /// <summary>A short text that identifies the grid in save files.</summary>
        public string Signature =>
            string.Join(" ", new[]
            {
                MedianCount.ToString(CultureInfo.InvariantCulture), F(MedianMinMs), F(MedianMaxMs),
                string.Join(";", Array.ConvertAll(Spreads, F)), string.Join(";", Array.ConvertAll(Lapses, F)), F(InputDelayMs),
            });

        private static string F(double v) => v.ToString("R", CultureInfo.InvariantCulture);

        public int NearestMedian(double medianMs)
        {
            var i = (int)Math.Round((Math.Log(Math.Max(1e-9, medianMs)) - LogMedians[0]) / LogStep);
            return Math.Max(0, Math.Min(MedianCount - 1, i));
        }

        public static int Nearest(double[] values, double v)
        {
            var best = 0;
            for (var i = 1; i < values.Length; i++)
                if (Math.Abs(values[i] - v) < Math.Abs(values[best] - v)) best = i;
            return best;
        }
    }
}
