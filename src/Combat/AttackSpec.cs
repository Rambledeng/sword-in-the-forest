namespace SwordInTheForest.Combat
{
    /// <summary>
    /// The timing-relevant part of one enemy attack: a windup of random length, a glint a lead time
    /// before impact, the impact, then a recovery. All times in milliseconds.
    /// </summary>
    public sealed class AttackSpec
    {
        public AttackSpec(string id, GlintColor glint, int windupMinMs, int windupMaxMs,
            int leadDefaultMs, int leadMinMs, int leadMaxMs, int recoveryMs)
        {
            Id = id;
            Glint = glint;
            WindupMinMs = windupMinMs;
            WindupMaxMs = windupMaxMs < windupMinMs ? windupMinMs : windupMaxMs;
            LeadDefaultMs = leadDefaultMs;
            LeadMinMs = leadMinMs;
            LeadMaxMs = leadMaxMs < leadMinMs ? leadMinMs : leadMaxMs;
            RecoveryMs = recoveryMs;
        }

        public string Id { get; }
        public GlintColor Glint { get; }
        public int WindupMinMs { get; }
        public int WindupMaxMs { get; }
        /// <summary>The designed lead time, used whenever adaptive timing is off.</summary>
        public int LeadDefaultMs { get; }
        /// <summary>The designed range the director may choose a lead time from.</summary>
        public int LeadMinMs { get; }
        public int LeadMaxMs { get; }
        public int RecoveryMs { get; }
    }
}
