using System;
using SwordInTheForest.Director;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Lead times from the director. With <see cref="Adaptive"/> off every attack uses its designed
    /// default exactly; the director keeps observing, so its estimate is ready when it is turned on.
    /// The first <see cref="CalibrationAttacks"/> observations calibrate; after that it controls.
    /// </summary>
    public sealed class AdaptiveLeadTimes : ILeadTimeSource
    {
        public AdaptiveLeadTimes(ReactionDirector director)
        {
            Director = director;
        }

        public ReactionDirector Director { get; set; }
        public bool Adaptive { get; set; } = true;
        public Challenge Challenge { get; set; } = Challenge.Standard;
        public int CalibrationAttacks { get; set; } = 12;
        /// <summary>The spec's limits for any director decision.</summary>
        public LeadTimeBounds DirectorBounds { get; set; } = new LeadTimeBounds(200, 800);

        public DirectorMode Mode => Director.Observations < CalibrationAttacks ? DirectorMode.Calibrate : DirectorMode.Control;
        public int LastLeadMs { get; private set; }
        public string LastAttack { get; private set; } = "";
        /// <summary>Success the belief predicted for the last lead when it was chosen.</summary>
        public double LastPredicted { get; private set; } = double.NaN;

        public int NextLeadMs(AttackSpec attack)
        {
            int lead;
            if (!Adaptive)
            {
                lead = attack.LeadDefaultMs;
            }
            else
            {
                var min = Math.Max(DirectorBounds.MinMs, attack.LeadMinMs);
                var max = Math.Min(DirectorBounds.MaxMs, attack.LeadMaxMs);
                lead = min <= max
                    ? Director.NextLeadMs(new LeadTimeBounds(min, max), Mode, Challenge.Target())
                    : attack.LeadDefaultMs;
            }
            LastLeadMs = lead;
            LastAttack = attack.Id;
            LastPredicted = Director.PredictSuccess(lead);
            return lead;
        }

        /// <summary>Feeds one finished attack to the director. Returns false when it carried no evidence.</summary>
        public bool Observe(TelemetryRecord record, double guardArcDeg)
        {
            var e = ThreatEvidence.From(record, guardArcDeg);
            if (e == null) return false;
            Director.Observe(e.Value);
            return true;
        }
    }
}
