using System;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// One concrete attack: windup, glint, lead, impact, recovery, measured in ms from the attack's start.
    /// Boundaries: the glint belongs to the lead phase, the impact to the lead phase (a press on the
    /// impact frame still counts), and recovery starts right after.
    /// </summary>
    public readonly struct AttackTimeline
    {
        public AttackTimeline(int windupMs, int leadMs, int recoveryMs)
        {
            if (windupMs < 0) throw new ArgumentOutOfRangeException(nameof(windupMs));
            if (leadMs <= 0) throw new ArgumentOutOfRangeException(nameof(leadMs));
            if (recoveryMs < 0) throw new ArgumentOutOfRangeException(nameof(recoveryMs));
            WindupMs = windupMs;
            LeadMs = leadMs;
            RecoveryMs = recoveryMs;
        }

        public int WindupMs { get; }
        public int LeadMs { get; }
        public int RecoveryMs { get; }

        public int GlintMs => WindupMs;
        public int ImpactMs => WindupMs + LeadMs;
        public int EndMs => ImpactMs + RecoveryMs;

        /// <summary>Picks a windup in [min, max] from a unit random number in [0, 1).</summary>
        public static AttackTimeline Create(AttackSpec attack, int leadMs, double unitRandom)
        {
            var span = attack.WindupMaxMs - attack.WindupMinMs;
            var windup = attack.WindupMinMs + (int)Math.Floor(span * Math.Max(0, Math.Min(0.999999, unitRandom)) + 0.5);
            return new AttackTimeline(windup, leadMs, attack.RecoveryMs);
        }

        public AttackPhase PhaseAt(double elapsedMs)
        {
            if (elapsedMs < GlintMs - CombatTime.Epsilon) return AttackPhase.Windup;
            if (elapsedMs <= ImpactMs + CombatTime.Epsilon) return AttackPhase.Lead;
            if (elapsedMs < EndMs - CombatTime.Epsilon) return AttackPhase.Recovery;
            return AttackPhase.Done;
        }
    }
}
