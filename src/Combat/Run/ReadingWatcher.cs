using System;

namespace SwordInTheForest.Run
{
    /// <summary>
    /// Decides when the sword comments on its reading of the player: once when attunement first
    /// reaches <see cref="AttunedAt"/>, then whenever the median has moved by at least
    /// <see cref="ShiftFraction"/> from the value it last spoke about, at most once per
    /// <see cref="MinObservationsBetween"/> observations.
    /// </summary>
    public sealed class ReadingWatcher
    {
        private double _spokenMedian = double.NaN;
        private int _spokenAt = int.MinValue / 2;

        public ReadingWatcher(double attunedAt, double shiftFraction, int minObservationsBetween)
        {
            AttunedAt = attunedAt;
            ShiftFraction = shiftFraction;
            MinObservationsBetween = minObservationsBetween;
        }

        public double AttunedAt { get; }
        public double ShiftFraction { get; }
        public int MinObservationsBetween { get; }
        public bool HasSpoken => !double.IsNaN(_spokenMedian);

        /// <summary>Starts from an existing reading (a returning player): no "attuned" line, only shifts.</summary>
        public void Prime(double medianMs, int observations)
        {
            _spokenMedian = medianMs;
            _spokenAt = observations;
        }

        public ReadingChange Observe(double medianMs, double attunement, int observations)
        {
            if (!HasSpoken)
            {
                if (attunement < AttunedAt) return ReadingChange.None;
                Prime(medianMs, observations);
                return ReadingChange.Attuned;
            }
            if (observations - _spokenAt < MinObservationsBetween) return ReadingChange.None;
            var shift = (medianMs - _spokenMedian) / _spokenMedian;
            if (Math.Abs(shift) < ShiftFraction) return ReadingChange.None;
            Prime(medianMs, observations);
            return shift < 0 ? ReadingChange.Quicker : ReadingChange.Slower;
        }
    }
}
