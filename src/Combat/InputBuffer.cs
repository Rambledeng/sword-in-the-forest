using System.Collections.Generic;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Remembers button presses for a short window so a press just before a state ends still counts.
    /// Generic over the action type so the game can use its own enum.
    /// </summary>
    public sealed class InputBuffer<TAction> where TAction : notnull
    {
        private readonly Dictionary<TAction, double> _pressedAt = new Dictionary<TAction, double>();

        public InputBuffer(double windowMs)
        {
            WindowMs = windowMs;
        }

        public double WindowMs { get; set; }

        public void Press(TAction action, double tMs) => _pressedAt[action] = tMs;

        /// <summary>True if <paramref name="action"/> was pressed within the window before <paramref name="nowMs"/>.</summary>
        public bool Has(TAction action, double nowMs) =>
            _pressedAt.TryGetValue(action, out var t) && nowMs - t <= WindowMs + CombatTime.Epsilon;

        /// <summary>Like <see cref="Has"/>, and forgets the press on success.</summary>
        public bool TryConsume(TAction action, double nowMs)
        {
            if (!Has(action, nowMs)) return false;
            _pressedAt.Remove(action);
            return true;
        }

        /// <summary>The press time, if still buffered.</summary>
        public double? PressedAt(TAction action, double nowMs) => Has(action, nowMs) ? _pressedAt[action] : (double?)null;

        public void Clear() => _pressedAt.Clear();
    }
}
