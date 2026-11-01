namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Lead times without a director: each attack's designed default, or one fixed value for every
    /// attack (used by the bot tests to sweep lead times outside the designed ranges).
    /// </summary>
    public sealed class FixedLeadTimeSource : ILeadTimeSource
    {
        public FixedLeadTimeSource(int? fixedMs = null)
        {
            FixedMs = fixedMs;
        }

        /// <summary>When set, every attack uses this lead time; otherwise its designed default.</summary>
        public int? FixedMs { get; set; }

        public int NextLeadMs(AttackSpec attack) => FixedMs ?? attack.LeadDefaultMs;
    }
}
