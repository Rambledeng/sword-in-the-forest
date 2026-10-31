using System;
using System.Collections.Generic;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Drives one enemy's attacks through windup, glint, impact and recovery. It asks an
    /// <see cref="ILeadTimeSource"/> for each lead time, opens a <see cref="ThreatWindow"/> on the
    /// player at the glint, raises <see cref="Impact"/> so the owner can resolve it, and raises
    /// <see cref="Finalized"/> once the attack's telemetry line is complete (after a late press,
    /// or after the grace period).
    /// The glint is stamped with the time of the frame it shows on, and the impact lands on the
    /// first frame at or after glint + lead, so the window is never shorter than the lead time.
    /// </summary>
    public sealed class AttackRunner
    {
        private readonly List<ThreatWindow> _pending = new List<ThreatWindow>();
        private readonly Random _rng;
        private double _impactAtMs;
        private double _extraRecoveryMs;
        private bool _swung;

        public AttackRunner(ILeadTimeSource leadTimes, int seed)
        {
            LeadTimes = leadTimes;
            _rng = new Random(seed);
        }

        public ILeadTimeSource LeadTimes { get; set; }
        public IThreatTarget? Target { get; set; }
        public double LatePressGraceMs { get; set; } = 400;

        public AttackSpec? Current { get; private set; }
        public AttackTimeline Timeline { get; private set; }
        public AttackPhase Phase { get; private set; } = AttackPhase.Done;
        public double StartMs { get; private set; }
        public ThreatWindow? Threat { get; private set; }
        public bool Running => Current != null;
        public double SwingMs { get; private set; }

        /// <summary>The glint shows this frame.</summary>
        public event Action<ThreatWindow>? Glint;
        /// <summary>The visible strike starts (SwingMs before the impact).</summary>
        public event Action<ThreatWindow>? Swing;
        /// <summary>The impact frame: the handler resolves the threat and applies the result.</summary>
        public event Action<ThreatWindow>? Impact;
        /// <summary>Recovery is over.</summary>
        public event Action<AttackSpec>? Finished;
        /// <summary>The threat's telemetry line is complete.</summary>
        public event Action<ThreatWindow>? Finalized;

        public void Start(AttackSpec attack, double nowMs, double swingMs = 0)
        {
            if (Running) Abort();
            Current = attack;
            var lead = Math.Max(1, LeadTimes.NextLeadMs(attack));
            Timeline = AttackTimeline.Create(attack, lead, _rng.NextDouble());
            StartMs = nowMs;
            SwingMs = Math.Max(0, Math.Min(swingMs, lead));
            Phase = AttackPhase.Windup;
            Threat = null;
            _extraRecoveryMs = 0;
            _swung = false;
        }

        /// <summary>Lengthens the current recovery (e.g. after being deflected).</summary>
        public void ExtendRecovery(double ms) => _extraRecoveryMs += Math.Max(0, ms);

        /// <summary>Call once per frame with the current game time.</summary>
        public void Advance(double nowMs)
        {
            FinalizeDue(nowMs);
            if (Current == null) return;

            if (Phase == AttackPhase.Windup && nowMs - StartMs >= Timeline.GlintMs - CombatTime.Epsilon)
            {
                Threat = new ThreatWindow(Current, nowMs, Timeline.LeadMs);
                Phase = AttackPhase.Lead;
                Target?.OpenThreat(Threat);
                Glint?.Invoke(Threat);
            }
            if (Phase == AttackPhase.Lead && Threat != null)
            {
                if (!_swung && nowMs >= Threat.ImpactMs - SwingMs - CombatTime.Epsilon)
                {
                    _swung = true;
                    Swing?.Invoke(Threat);
                }
                if (nowMs >= Threat.ImpactMs - CombatTime.Epsilon)
                {
                    Phase = AttackPhase.Recovery;
                    _impactAtMs = nowMs;
                    _pending.Add(Threat);
                    Impact?.Invoke(Threat);
                    FinalizeDue(nowMs);
                }
            }
            if (Phase == AttackPhase.Recovery && Current != null &&
                nowMs - _impactAtMs >= Timeline.RecoveryMs + _extraRecoveryMs - CombatTime.Epsilon)
            {
                var done = Current;
                Phase = AttackPhase.Done;
                Current = null;
                Finished?.Invoke(done);
            }
        }

        /// <summary>Stops the attack (stagger, kneel, death). A glinted threat that never landed is dropped.</summary>
        public void Abort()
        {
            if (Phase == AttackPhase.Lead && Threat != null) Target?.CloseThreat(Threat);
            Current = null;
            Phase = AttackPhase.Done;
            Threat = null;
        }

        /// <summary>Completes every pending telemetry line now (the attacker is leaving).</summary>
        public void Flush()
        {
            foreach (var t in _pending.ToArray()) Complete(t);
            Abort();
        }

        private void FinalizeDue(double nowMs)
        {
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                var t = _pending[i];
                if (t.PressMs != null || nowMs >= t.ImpactMs + LatePressGraceMs - CombatTime.Epsilon) Complete(t);
            }
        }

        private void Complete(ThreatWindow t)
        {
            _pending.Remove(t);
            Finalized?.Invoke(t);
            Target?.CloseThreat(t);
        }
    }
}
