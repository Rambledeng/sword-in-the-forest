namespace SwordInTheForest.Director
{
    /// <summary>One attack aimed at the player, as evidence for the filter.</summary>
    public readonly struct ThreatEvent
    {
        public ThreatEvent(int leadMs, double? pressMs, EvidenceKind kind)
        {
            LeadMs = leadMs;
            PressMs = pressMs;
            Kind = kind;
        }

        public int LeadMs { get; }
        /// <summary>Press time after the glint (ms); required for <see cref="EvidenceKind.Timed"/>.</summary>
        public double? PressMs { get; }
        public EvidenceKind Kind { get; }

        /// <summary>The player answered in time: a timed press no later than the impact.</summary>
        public bool Success => Kind == EvidenceKind.Timed && PressMs != null && PressMs.Value <= LeadMs;

        public static ThreatEvent Timed(int leadMs, double pressMs) => new ThreatEvent(leadMs, pressMs, EvidenceKind.Timed);
        public static ThreatEvent NoResponse(int leadMs) => new ThreatEvent(leadMs, null, EvidenceKind.NoResponse);
        public static ThreatEvent Lapse(int leadMs, double? pressMs = null) => new ThreatEvent(leadMs, pressMs, EvidenceKind.Lapse);

        public override string ToString() => $"{Kind} lead {LeadMs} press {PressMs?.ToString("0") ?? "-"}";
    }
}
