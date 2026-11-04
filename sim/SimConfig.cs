using SwordInTheForest.Director;

namespace SwordInTheForest.Sim;

/// <summary>One simulated session: calibration attacks (the tutorial), then the run's attacks.</summary>
public sealed class SimConfig
{
    public int CalibrationAttacks { get; init; } = 12;
    public int ControlAttacks { get; init; } = 60;
    public Challenge Challenge { get; init; } = Challenge.Standard;
    /// <summary>Off: every attack uses <see cref="FixedLeadMs"/> (the designed default).</summary>
    public bool Adaptive { get; init; } = true;
    public int FixedLeadMs { get; init; } = 420;
    /// <summary>The attack's designed range; the director also stays within 200–800 ms.</summary>
    public LeadTimeBounds AttackBounds { get; init; } = new(200, 800);
}
