using Godot;

namespace SwordInTheForest.Tuning;

/// <summary>Rules shared by every fight and the test arena.</summary>
[GlobalClass]
public partial class CombatTuning : Resource
{
    /// <summary>How long after an impact a late guard or roll press is still timed for telemetry.</summary>
    [Export(PropertyHint.Range, "0,2000,10")] public int LatePressGraceMs { get; set; }
    /// <summary>0 = each attack's designed lead time; otherwise every attack uses this lead (ms).</summary>
    [Export(PropertyHint.Range, "0,1500,10")] public int FixedLeadOverrideMs { get; set; }
    /// <summary>The director calibrates (picks the most informative lead) for this many attacks, then controls.</summary>
    [Export(PropertyHint.Range, "0,40,1")] public int DirectorCalibrationAttacks { get; set; }
    [Export(PropertyHint.Range, "1,6,1")] public int ArenaEnemies { get; set; }
    [Export(PropertyHint.Range, "0,10000,100")] public int RespawnDelayMs { get; set; }
}
