namespace SwordInTheForest.Combat
{
    /// <summary>One attack aimed at the player: one JSON line in the telemetry file.</summary>
    public sealed class TelemetryRecord
    {
        public TelemetryRecord(string run, double tSeconds, string attack, GlintColor glint, int leadMs,
            int? pressMs, Response response, Outcome outcome)
        {
            Run = run;
            TSeconds = tSeconds;
            Attack = attack;
            Glint = glint;
            LeadMs = leadMs;
            PressMs = pressMs;
            Response = response;
            Outcome = outcome;
        }

        public string Run { get; }
        /// <summary>Game time of the glint, in seconds since the run started.</summary>
        public double TSeconds { get; }
        public string Attack { get; }
        public GlintColor Glint { get; }
        public int LeadMs { get; }
        /// <summary>First guard or roll press after the glint, in ms; null when there was none.</summary>
        public int? PressMs { get; }
        public Response Response { get; }
        public Outcome Outcome { get; }

        public static TelemetryRecord From(ThreatWindow threat, string run, double runStartMs)
        {
            var result = threat.Result ?? new Resolution(Response.None, Outcome.Hit);
            int? press = threat.PressMs == null ? (int?)null : (int)System.Math.Round(threat.PressMs.Value);
            return new TelemetryRecord(run, (threat.GlintMs - runStartMs) / 1000.0, threat.Attack.Id, threat.Glint,
                threat.LeadMs, press, result.Response, result.Outcome);
        }
    }
}
