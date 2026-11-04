using System;
using System.Collections.Generic;

namespace SwordInTheForest.Director
{
    /// <summary>Priors over the grid: flat, or fitted to a training set of players (empirical Bayes).</summary>
    public static class Prior
    {
        /// <summary>Every cell equally likely ("player data only").</summary>
        public static double[] Uniform(DirectorGrid grid)
        {
            var w = new double[grid.CellCount];
            for (var i = 0; i < w.Length; i++) w[i] = 1.0 / w.Length;
            return w;
        }

        /// <summary>
        /// Fits a prior to training players: a normal over the log-median (mean and sd of the sample),
        /// times smoothed histograms over spread and lapse. A small floor keeps every cell possible, so
        /// a player unlike the training set can still be learned.
        /// </summary>
        public static double[] Fit(DirectorGrid grid, IReadOnlyList<PlayerParams> training, double floor = 0.02)
        {
            if (training.Count < 2) throw new ArgumentException("Need at least two training players.", nameof(training));
            double sum = 0, sumSq = 0;
            var spreadHist = new double[grid.SpreadCount];
            var lapseHist = new double[grid.LapseCount];
            foreach (var p in training)
            {
                var lm = Math.Log(p.MedianMs);
                sum += lm;
                sumSq += lm * lm;
                spreadHist[DirectorGrid.Nearest(grid.Spreads, p.Spread)]++;
                lapseHist[DirectorGrid.Nearest(grid.Lapses, p.Lapse)]++;
            }
            var n = training.Count;
            var mean = sum / n;
            var sd = Math.Sqrt(Math.Max(1e-6, sumSq / n - mean * mean));
            Smooth(spreadHist);
            Normalize(spreadHist, floor);
            Normalize(lapseHist, floor);
            var medianPdf = new double[grid.MedianCount];
            for (var i = 0; i < medianPdf.Length; i++)
            {
                var z = (grid.LogMedians[i] - mean) / sd;
                medianPdf[i] = Math.Exp(-0.5 * z * z);
            }
            Normalize(medianPdf, floor);

            var w = new double[grid.CellCount];
            for (var li = 0; li < grid.LapseCount; li++)
                for (var si = 0; si < grid.SpreadCount; si++)
                    for (var mi = 0; mi < grid.MedianCount; mi++)
                        w[(li * grid.SpreadCount + si) * grid.MedianCount + mi] = lapseHist[li] * spreadHist[si] * medianPdf[mi];
            Normalize(w, 0);
            return w;
        }

        /// <summary>Fits the prior to <paramref name="count"/> players drawn from a population.</summary>
        public static double[] FromPopulation(DirectorGrid grid, PopulationModel population, int count = 20000, int seed = 2026)
        {
            var rng = new Random(seed);
            var players = new PlayerParams[count];
            for (var i = 0; i < count; i++) players[i] = population.Sample(rng);
            return Fit(grid, players);
        }

        private static void Smooth(double[] h)
        {
            if (h.Length < 3) return;
            var copy = (double[])h.Clone();
            for (var i = 0; i < h.Length; i++)
            {
                var l = copy[Math.Max(0, i - 1)];
                var r = copy[Math.Min(h.Length - 1, i + 1)];
                h[i] = 0.25 * l + 0.5 * copy[i] + 0.25 * r;
            }
        }

        /// <summary>Scales to sum 1 after mixing in <paramref name="floor"/> of a flat distribution.</summary>
        private static void Normalize(double[] h, double floor)
        {
            double total = 0;
            foreach (var v in h) total += v;
            for (var i = 0; i < h.Length; i++)
                h[i] = (1 - floor) * (total > 0 ? h[i] / total : 1.0 / h.Length) + floor / h.Length;
        }
    }
}
