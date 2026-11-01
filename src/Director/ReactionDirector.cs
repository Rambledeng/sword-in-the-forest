using System;
using System.Collections.Generic;

namespace SwordInTheForest.Director
{
    /// <summary>
    /// The adaptive director: a histogram filter over (median, spread, lapse) of the player's
    /// reaction time, a calibration picker (Psi method) and a control law.
    /// <code>
    /// var director = ReactionDirector.FromPrior(DirectorGrid.Default, prior);
    /// director.Observe(ThreatEvent.Timed(leadMs: 420, pressMs: 365));
    /// int lead = director.NextLeadMs(bounds, DirectorMode.Control, Challenge.Standard.Target());
    /// Estimate e = director.Estimate();
    /// </code>
    /// Model: P(success | L) = (1 − λ) Φ((ln(L − d) − m) / s). Not thread-safe.
    /// </summary>
    public sealed class ReactionDirector
    {
        private readonly double[] _w;
        private readonly double[] _backup;
        private readonly double[] _slice;   // per (spread, median): scratch for likelihoods and Σλ w(1 − λ)
        private readonly double[] _margM;
        private readonly double[] _a;
        private readonly List<ObservationRecord> _history = new List<ObservationRecord>();

        public ReactionDirector(DirectorGrid grid, double[] weights, int observations = 0, DirectorOptions? options = null)
        {
            if (weights.Length != grid.CellCount) throw new ArgumentException("Weights do not match the grid.", nameof(weights));
            Grid = grid;
            Options = options ?? new DirectorOptions();
            _w = (double[])weights.Clone();
            _backup = new double[_w.Length];
            _slice = new double[grid.SliceSize];
            _margM = new double[grid.MedianCount];
            _a = new double[grid.MedianCount];
            Observations = observations;
            if (!Normalize()) throw new ArgumentException("Weights must have a positive, finite sum.", nameof(weights));
        }

        public static ReactionDirector FromPrior(DirectorGrid grid, double[] prior, DirectorOptions? options = null) =>
            new ReactionDirector(grid, prior, 0, options);

        public DirectorGrid Grid { get; }
        public DirectorOptions Options { get; }
        /// <summary>Attacks observed (timed, no response or lapse) since the prior.</summary>
        public int Observations { get; private set; }
        /// <summary>Recent observations with the success predicted just before each.</summary>
        public IReadOnlyList<ObservationRecord> History => _history;
        /// <summary>The belief, normalized; index as in <see cref="DirectorGrid"/>.</summary>
        public IReadOnlyList<double> Weights => _w;

        public ReactionDirector Clone()
        {
            var c = new ReactionDirector(Grid, _w, Observations, Options);
            c._history.AddRange(_history);
            return c;
        }

        /// <summary>Updates the belief with one attack. Returns the success the belief predicted for it.</summary>
        public double Observe(ThreatEvent e)
        {
            if (e.Kind == EvidenceKind.Timed && e.PressMs == null)
                throw new ArgumentException("A timed observation needs a press time.", nameof(e));
            var predicted = PredictSuccess(e.LeadMs);
            Array.Copy(_w, _backup, _w.Length);
            Blur();
            switch (e.Kind)
            {
                case EvidenceKind.Timed: ApplyTimed(e.PressMs!.Value); break;
                case EvidenceKind.NoResponse: ApplyNoResponse(e.LeadMs); break;
                default: ApplyLapse(); break;
            }
            // Evidence the model cannot explain at all (underflow) leaves the belief as it was.
            if (!Normalize()) Array.Copy(_backup, _w, _w.Length);
            Observations++;
            _history.Add(new ObservationRecord(e, predicted));
            if (_history.Count > Options.HistoryLimit) _history.RemoveAt(0);
            return predicted;
        }

        /// <summary>Success at this lead time, averaged over the belief.</summary>
        public double PredictSuccess(int leadMs)
        {
            FillLapseWeighted();
            var x = leadMs - Grid.InputDelayMs;
            if (x <= 0) return 0;
            var row = LadderIndex(leadMs);
            var M = Grid.MedianCount;
            double p = 0;
            if (row >= 0)
            {
                var table = Grid.PhiTable;
                var off = row * _slice.Length;
                for (var k = 0; k < _slice.Length; k++) p += _slice[k] * table[off + k];
                return p;
            }
            var lx = Math.Log(x);
            for (var si = 0; si < Grid.SpreadCount; si++)
            {
                var s = Grid.Spreads[si];
                for (var mi = 0; mi < M; mi++)
                    p += _slice[si * M + mi] * ReactionMath.Phi((lx - Grid.LogMedians[mi]) / s);
            }
            return p;
        }

        /// <summary>
        /// The next lead time within <paramref name="bounds"/> (and the grid's 200–800 ms): in
        /// <see cref="DirectorMode.Control"/> the shortest one whose predicted success reaches
        /// <paramref name="target"/>; in <see cref="DirectorMode.Calibrate"/> the one whose outcome is
        /// expected to shrink the uncertainty about the median the most.
        /// </summary>
        public int NextLeadMs(LeadTimeBounds bounds, DirectorMode mode, double target)
        {
            var lo = Math.Max(bounds.MinMs, Grid.LeadMinMs);
            var hi = Math.Min(bounds.MaxMs, Grid.LeadMaxMs);
            if (lo > hi) return bounds.MaxMs < Grid.LeadMinMs ? bounds.MaxMs : bounds.MinMs;
            var step = DirectorGrid.LeadStepMs;
            var first = (lo - Grid.LeadMinMs + step - 1) / step;
            var last = (hi - Grid.LeadMinMs) / step;
            if (first > last) return lo;
            FillLapseWeighted();
            return mode == DirectorMode.Calibrate ? Calibrate(first, last) : Control(first, last, target);
        }

        public Estimate Estimate()
        {
            var M = Grid.MedianCount;
            Array.Clear(_margM, 0, M);
            double spread = 0, lapse = 0;
            for (var li = 0; li < Grid.LapseCount; li++)
                for (var si = 0; si < Grid.SpreadCount; si++)
                {
                    var off = (li * Grid.SpreadCount + si) * M;
                    double rowSum = 0;
                    for (var mi = 0; mi < M; mi++)
                    {
                        _margM[mi] += _w[off + mi];
                        rowSum += _w[off + mi];
                    }
                    spread += rowSum * Grid.Spreads[si];
                    lapse += rowSum * Grid.Lapses[li];
                }
            var low = Math.Exp(Quantile(_margM, 0.1));
            var median = Math.Exp(Quantile(_margM, 0.5));
            var high = Math.Exp(Quantile(_margM, 0.9));
            var width = (high - low) / median;
            var att = (Options.UnsureWidth - width) / (Options.UnsureWidth - Options.AttunedWidth);
            return new Estimate(median, low, high, spread, lapse, Math.Max(0, Math.Min(1, att)), Observations);
        }

        /// <summary>The belief summed over lapse rates, per (spread, median) cell: the overlay's heat map.</summary>
        public void MedianSpreadMarginal(double[] into)
        {
            if (into.Length != Grid.SliceSize) throw new ArgumentException("Wrong length.", nameof(into));
            Array.Clear(into, 0, into.Length);
            for (var li = 0; li < Grid.LapseCount; li++)
            {
                var off = li * Grid.SliceSize;
                for (var k = 0; k < into.Length; k++) into[k] += _w[off + k];
            }
        }

        private int Control(int first, int last, double target)
        {
            if (Predict(last) < target) return Grid.LeadAt(last);
            // Predicted success rises with the lead: binary search for the first rung that reaches it.
            int a = first, b = last;
            while (a < b)
            {
                var mid = (a + b) / 2;
                if (Predict(mid) >= target) b = mid;
                else a = mid + 1;
            }
            return Grid.LeadAt(a);
        }

        private int Calibrate(int first, int last)
        {
            var M = Grid.MedianCount;
            var S = Grid.SpreadCount;
            Array.Clear(_margM, 0, M);
            for (var k = 0; k < _w.Length; k++) _margM[k % M] += _w[k];
            // Medians the belief has (practically) ruled out cannot change the entropy: skip them.
            int m0 = 0, m1 = M - 1;
            while (m0 < m1 && _margM[m0] < 1e-9) m0++;
            while (m1 > m0 && _margM[m1] < 1e-9) m1--;
            var table = Grid.PhiTable;
            var stride = Math.Max(1, Options.CalibrationStepMs / DirectorGrid.LeadStepMs);
            var best = first;
            var bestH = double.MaxValue;
            for (var c = first; c <= last; c += stride)
            {
                var off = c * _slice.Length;
                Array.Clear(_a, 0, M);
                for (var si = 0; si < S; si++)
                {
                    var o = si * M;
                    for (var mi = m0; mi <= m1; mi++) _a[mi] += _slice[o + mi] * table[off + o + mi];
                }
                // Expected entropy of the median after a success or a failure at this lead.
                double ps = 0, h = 0;
                for (var mi = m0; mi <= m1; mi++)
                {
                    var a = _a[mi];
                    var f = _margM[mi] - a;
                    ps += a;
                    if (a > 0) h -= a * Math.Log(a);
                    if (f > 0) h -= f * Math.Log(f);
                }
                var pf = 1 - ps;
                if (ps > 0) h += ps * Math.Log(ps);
                if (pf > 0) h += pf * Math.Log(pf);
                if (h < bestH - 1e-12)
                {
                    bestH = h;
                    best = c;
                }
            }
            return Grid.LeadAt(best);
        }

        private double Predict(int row)
        {
            var table = Grid.PhiTable;
            var off = row * _slice.Length;
            double p = 0;
            for (var k = 0; k < _slice.Length; k++) p += _slice[k] * table[off + k];
            return p;
        }

        private int LadderIndex(int leadMs)
        {
            var d = leadMs - Grid.LeadMinMs;
            if (d < 0 || leadMs > Grid.LeadMaxMs || d % DirectorGrid.LeadStepMs != 0) return -1;
            return d / DirectorGrid.LeadStepMs;
        }

        /// <summary>_slice[k] = Σ over lapse rates of w · (1 − λ).</summary>
        private void FillLapseWeighted()
        {
            Array.Clear(_slice, 0, _slice.Length);
            for (var li = 0; li < Grid.LapseCount; li++)
            {
                var keep = 1 - Grid.Lapses[li];
                var off = li * _slice.Length;
                for (var k = 0; k < _slice.Length; k++) _slice[k] += _w[off + k] * keep;
            }
        }

        private void Blur()
        {
            var b = Options.BlurPerAttack;
            if (b <= 0) return;
            var M = Grid.MedianCount;
            for (var row = 0; row < _w.Length; row += M)
            {
                // Reflecting edges keep the mass inside the grid.
                var prev = _w[row];
                for (var mi = 0; mi < M; mi++)
                {
                    var cur = _w[row + mi];
                    var next = mi + 1 < M ? _w[row + mi + 1] : cur;
                    _w[row + mi] = (1 - 2 * b) * cur + b * (prev + next);
                    prev = cur;
                }
            }
        }

        private void ApplyTimed(double pressMs)
        {
            var x = pressMs - Grid.InputDelayMs;
            var M = Grid.MedianCount;
            for (var si = 0; si < Grid.SpreadCount; si++)
                for (var mi = 0; mi < M; mi++)
                    _slice[si * M + mi] = ReactionMath.LogNormalPdf(x, Grid.LogMedians[mi], Grid.Spreads[si]);
            var u = Options.LapsePressDensity;
            for (var li = 0; li < Grid.LapseCount; li++)
            {
                var lapse = Grid.Lapses[li];
                var off = li * _slice.Length;
                for (var k = 0; k < _slice.Length; k++) _w[off + k] *= (1 - lapse) * _slice[k] + lapse * u;
            }
        }

        private void ApplyNoResponse(int leadMs)
        {
            var x = leadMs - Grid.InputDelayMs;
            var M = Grid.MedianCount;
            var lx = x > 0 ? Math.Log(x) : double.NegativeInfinity;
            for (var si = 0; si < Grid.SpreadCount; si++)
                for (var mi = 0; mi < M; mi++)
                    _slice[si * M + mi] = x > 0 ? ReactionMath.Phi((lx - Grid.LogMedians[mi]) / Grid.Spreads[si]) : 0;
            for (var li = 0; li < Grid.LapseCount; li++)
            {
                var lapse = Grid.Lapses[li];
                var off = li * _slice.Length;
                for (var k = 0; k < _slice.Length; k++) _w[off + k] *= (1 - lapse) * (1 - _slice[k]) + lapse;
            }
        }

        private void ApplyLapse()
        {
            for (var li = 0; li < Grid.LapseCount; li++)
            {
                var lapse = Grid.Lapses[li];
                var off = li * _slice.Length;
                for (var k = 0; k < _slice.Length; k++) _w[off + k] *= lapse;
            }
        }

        private bool Normalize()
        {
            double sum = 0;
            foreach (var v in _w) sum += v;
            if (!(sum > 0) || double.IsInfinity(sum) || double.IsNaN(sum)) return false;
            var inv = 1.0 / sum;
            for (var i = 0; i < _w.Length; i++) _w[i] *= inv;
            return true;
        }

        /// <summary>Quantile of a mass function over the log-medians, interpolated between cell centres.</summary>
        private double Quantile(double[] mass, double q)
        {
            var lm = Grid.LogMedians;
            double cum = 0;
            for (var i = 0; i < mass.Length; i++)
            {
                var centre = cum + mass[i] / 2;
                if (centre >= q)
                {
                    if (i == 0) return lm[0];
                    var prevCentre = cum - mass[i - 1] / 2;
                    var t = centre > prevCentre ? (q - prevCentre) / (centre - prevCentre) : 1;
                    return lm[i - 1] + t * (lm[i] - lm[i - 1]);
                }
                cum += mass[i];
            }
            return lm[lm.Length - 1];
        }
    }
}
