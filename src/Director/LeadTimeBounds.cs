using System;

namespace SwordInTheForest.Director
{
    /// <summary>
    /// The range of lead times (glint to impact, in ms) the director may choose from.
    /// M0 placeholder; M2 builds the real director around it.
    /// </summary>
    public readonly struct LeadTimeBounds
    {
        public LeadTimeBounds(int minMs, int maxMs)
        {
            if (minMs <= 0) throw new ArgumentOutOfRangeException(nameof(minMs), "Must be positive.");
            if (maxMs < minMs) throw new ArgumentOutOfRangeException(nameof(maxMs), "Must be at least minMs.");
            MinMs = minMs;
            MaxMs = maxMs;
        }

        public int MinMs { get; }
        public int MaxMs { get; }

        public int Clamp(int leadMs) => Math.Min(MaxMs, Math.Max(MinMs, leadMs));
    }
}
