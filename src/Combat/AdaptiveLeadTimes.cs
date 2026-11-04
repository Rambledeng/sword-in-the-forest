using System;
using SwordInTheForest.Director;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Lead times from the director. With <see cref="Adaptive"/> off every attack uses its designed
    /// default exactly; the director keeps observing, so its estimate is ready when it is turned on.
    /// It controls by default; <see cref="BeginCalibration"/> (the tutorial ghost) switches it to
    /// calibration for a number of observations, after which it controls again.
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
        /// <summary>Calibration lasts until the director has this many observations.</summary>
        public int CalibrateUntil { get; private set; }
        /// <summary>The spec's limits for any director decision.</summary>
        public LeadTimeBounds DirectorBounds { get; set; } = new LeadTimeBounds(200, 800);

        public DirectorMode Mode => Director.Observations < CalibrateUntil ? DirectorMode.Calibrate : DirectorMode.Control;
        /// <summary>Observations left in the current calibration (0 when controlling).</summary>
        public int CalibrationLeft => Math.Max(0, CalibrateUntil - Director.Observations);

        /// <summary>Calibrates (picks the most informative leads) for the next <paramref name="attacks"/> observations.</summary>
        public void BeginCalibration(int attacks) => CalibrateUntil = Director.Observations + Math.Max(0, attacks);

        /// <summary>Back to control now, even if calibration attacks remain.</summary>
        public void EndCalibration() => CalibrateUntil = 0;
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
