using System;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Wraps the lead-time source so the first red (unblockable) attacks of a run get a generous
    /// lead: at least <see cref="LeadMs"/>, while the player is still learning what red means.
    /// Every other attack, and every red one after the first <see cref="Remaining"/>, is unchanged.
    /// </summary>
    public sealed class FirstRedLead : ILeadTimeSource
    {
        private readonly Func<ILeadTimeSource> _inner;

        public FirstRedLead(Func<ILeadTimeSource> inner, int leadMs, int count = 1)
        {
            _inner = inner;
            LeadMs = leadMs;
            Remaining = count;
        }

        public int LeadMs { get; set; }
        /// <summary>Red attacks that will still get the generous lead.</summary>
        public int Remaining { get; private set; }

        public int NextLeadMs(AttackSpec attack)
        {
            var lead = _inner().NextLeadMs(attack);
            if (attack.Glint != GlintColor.Red || Remaining <= 0) return lead;
            Remaining--;
            return Math.Max(lead, LeadMs);
        }
    }
}
