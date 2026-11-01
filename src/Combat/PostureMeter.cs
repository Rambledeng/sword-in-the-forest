using System;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Posture (架势): fills when blocking or being hit, drains after a pause in pressure.
    /// Reaching the maximum breaks it; the owner decides what a break means (stagger or kneel).
    /// </summary>
    public sealed class PostureMeter
    {
        private double _sinceLastFillMs;

        public PostureMeter(double max)
        {
            Max = max;
        }

        public double Max { get; set; }
        public double Value { get; private set; }
        public double Fraction => Max <= 0 ? 0 : Value / Max;

        /// <summary>Adds posture damage. Returns true if this fill broke the posture.</summary>
        public bool Add(double amount)
        {
            if (amount <= 0) return false;
            _sinceLastFillMs = 0;
            Value = Math.Min(Max, Value + amount);
            return Value >= Max - CombatTime.Epsilon;
        }

        /// <summary>Advances time; drains at <paramref name="drainPerSecond"/> once <paramref name="drainDelayMs"/> passed since the last fill.</summary>
        public void Tick(double dtMs, double drainPerSecond, double drainDelayMs, bool holdDrain = false)
        {
            _sinceLastFillMs += dtMs;
            if (holdDrain || _sinceLastFillMs < drainDelayMs) return;
            Value = Math.Max(0, Value - drainPerSecond * dtMs / 1000.0);
        }

        public void Reset(double value = 0)
        {
            Value = Math.Max(0, Math.Min(Max, value));
            _sinceLastFillMs = 0;
        }
    }
}
