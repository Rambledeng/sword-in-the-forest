using System;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// The roll: <see cref="DurationMs"/> long, invulnerable from <see cref="InvulnStartMs"/> (inclusive)
    /// to <see cref="InvulnEndMs"/> (exclusive), both measured from the frame the roll starts.
    /// </summary>
    public readonly struct RollTimeline
    {
        public RollTimeline(double durationMs, double invulnStartMs, double invulnEndMs)
        {
            if (durationMs <= 0) throw new ArgumentOutOfRangeException(nameof(durationMs));
            if (invulnStartMs < 0 || invulnEndMs < invulnStartMs) throw new ArgumentOutOfRangeException(nameof(invulnEndMs));
            DurationMs = durationMs;
            InvulnStartMs = invulnStartMs;
            InvulnEndMs = Math.Min(invulnEndMs, durationMs);
        }

        public double DurationMs { get; }
        public double InvulnStartMs { get; }
        public double InvulnEndMs { get; }

        public bool IsInvulnerable(double elapsedMs) =>
            elapsedMs >= InvulnStartMs - CombatTime.Epsilon && elapsedMs < InvulnEndMs - CombatTime.Epsilon;

        public bool IsOver(double elapsedMs) => elapsedMs >= DurationMs - CombatTime.Epsilon;
    }
}
