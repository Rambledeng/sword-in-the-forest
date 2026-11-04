using SwordInTheForest.Director;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Turns one telemetry record into evidence for the director, or null when the attack carries
    /// no information about the player's reaction time.
    /// <list type="bullet">
    /// <item>A press after the glint is a timed reaction, whatever became of it: a deflect or roll in
    /// the window, a late press after the impact, a press the anti-mash rule turned into a block, or a
    /// press the player could not act on.</item>
    /// <item>No press at all is a miss at the lead time, unless the player was out of reach (whiff):
    /// then not pressing was not a mistake.</item>
    /// <item>A guard or roll begun before the glint is anticipation: skipped.</item>
    /// <item>A guard against a red attack (wrong button) and a deflect that hit the player because its
    /// angle was beyond the guard arc (out of position) are lapses: misses that have nothing to do
    /// with speed.</item>
    /// </list>
    /// </summary>
    public static class ThreatEvidence
    {
        public static ThreatEvent? From(TelemetryRecord r, double guardArcDeg)
        {
            var lead = r.LeadMs;
            var press = r.PressMs;
            // Out of position: the guard was pressed in time but faced away from the blow.
            if (r.Response == Response.Deflect && r.Outcome == Outcome.Hit && r.AngleDeg != null &&
                r.AngleDeg.Value > guardArcDeg / 2)
                return ThreatEvent.Lapse(lead, press);
            // Wrong button: guarding a red attack.
            if (r.Response == Response.Guard && press != null) return ThreatEvent.Lapse(lead, press);
            if (press != null) return ThreatEvent.Timed(lead, press.Value);
            // No press after the glint from here on.
            if (r.Outcome == Outcome.Whiff) return null;
            if (r.Outcome == Outcome.Blocked || r.Outcome == Outcome.Evaded || r.Response != Response.None) return null;
            return ThreatEvent.NoResponse(lead);
        }
    }
}
