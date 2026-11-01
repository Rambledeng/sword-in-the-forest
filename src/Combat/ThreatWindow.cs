using System;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// One attack aimed at the player, from its glint until its telemetry line is written.
    /// The response window is [glint, impact], both ends inclusive: the right response anywhere in it
    /// succeeds, so the lead time is exactly the player's reaction window.
    /// </summary>
    public sealed class ThreatWindow
    {
        private double? _lastGuardInWindow;
        private double? _lastRollInWindow;

        public ThreatWindow(AttackSpec attack, double glintMs, int leadMs)
        {
            if (leadMs <= 0) throw new ArgumentOutOfRangeException(nameof(leadMs));
            Attack = attack;
            GlintMs = glintMs;
            LeadMs = leadMs;
        }

        public AttackSpec Attack { get; }
        public GlintColor Glint => Attack.Glint;
        public double GlintMs { get; }
        public int LeadMs { get; }
        public double ImpactMs => GlintMs + LeadMs;

        /// <summary>Time of the first guard or roll button press after the glint, relative to the glint.</summary>
        public double? PressMs { get; private set; }
        public Resolution? Result { get; private set; }
        /// <summary>Angle between the player's facing and the attacker at the impact (0–180°).</summary>
        public int? AngleDeg { get; private set; }

        public bool InWindow(double tMs) =>
            tMs >= GlintMs - CombatTime.Epsilon && tMs <= ImpactMs + CombatTime.Epsilon;

        /// <summary>A guard or roll button went down (whether or not the player could act on it).</summary>
        public void RecordPress(double tMs)
        {
            if (PressMs == null && tMs >= GlintMs - CombatTime.Epsilon)
                PressMs = Math.Max(0, tMs - GlintMs);
        }

        /// <summary>The player performed a defensive action (entered the deflect stance or a roll).</summary>
        public void RecordAction(DefenseAction action, double tMs)
        {
            RecordPress(tMs);
            if (Result != null || !InWindow(tMs)) return;
            if (action == DefenseAction.Guard) _lastGuardInWindow = tMs;
            else _lastRollInWindow = tMs;
        }

        /// <summary>Decides the attack on its impact frame.</summary>
        public Resolution Resolve(ImpactState state)
        {
            if (Result != null) return Result.Value;
            var result = Decide(state);
            Result = result;
            AngleDeg = state.AngleDeg;
            return result;
        }

        private Resolution Decide(ImpactState state)
        {
            var rolled = _lastRollInWindow != null;
            var guarded = _lastGuardInWindow != null;
            // The latest in-window action is the one the player committed to.
            var rollIsLatest = rolled && (!guarded || _lastRollInWindow >= _lastGuardInWindow);

            // A right response in the window succeeds wherever it left the player: telemetry
            // measures the reaction, and the game applies contact effects only when in reach.
            // Rolls evade from any direction; a guard only covers its front arc.
            if (rolled && rollIsLatest) return new Resolution(Response.Roll, Outcome.Success);
            if (guarded && Glint == GlintColor.White)
            {
                if (state.GuardCovers) return new Resolution(Response.Deflect, Outcome.Success);
                return new Resolution(Response.Deflect, state.InReach ? Outcome.Hit : Outcome.Whiff);
            }
            if (rolled) return new Resolution(Response.Roll, Outcome.Success);

            var anticipation = state.Guarding || guarded ? (Glint == GlintColor.White ? Response.Block : Response.Guard) : Response.None;
            if (!state.InReach) return new Resolution(anticipation, Outcome.Whiff);
            if (state.Invulnerable) return new Resolution(Response.Roll, Outcome.Evaded);
            if (Glint == GlintColor.White && state.Guarding && state.GuardCovers) return new Resolution(Response.Block, Outcome.Blocked);
            return new Resolution(anticipation, Outcome.Hit);
        }
    }
}
