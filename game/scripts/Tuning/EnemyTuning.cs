using Godot;

namespace SwordInTheForest.Tuning;

/// <summary>One enemy type's numbers (the saber bandit in M1). Attacks are separate AttackData files.</summary>
[GlobalClass]
public partial class EnemyTuning : Resource
{
    [ExportGroup("Body")]
    [Export(PropertyHint.Range, "1,100,1")] public int MaxHealth { get; set; }
    [Export(PropertyHint.Range, "10,400,5")] public float MaxPosture { get; set; }
    [Export(PropertyHint.Range, "0,200,1")] public float PostureDrainPerSec { get; set; }
    [Export(PropertyHint.Range, "0,5000,10")] public float PostureDrainDelayMs { get; set; }

    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "0,300,1")] public float MoveSpeed { get; set; }
    [Export(PropertyHint.Range, "0,300,1")] public float CircleSpeed { get; set; }
    [Export(PropertyHint.Range, "4,200,1")] public float AttackRangePx { get; set; }
    [Export(PropertyHint.Range, "4,200,1")] public float CircleDistancePx { get; set; }
    [Export(PropertyHint.Range, "0,5000,10")] public int CircleMinMs { get; set; }
    [Export(PropertyHint.Range, "0,5000,10")] public int CircleMaxMs { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float ChopChance { get; set; }
    [Export(PropertyHint.Range, "0,1000,10")] public float TurnSpeedDeg { get; set; }

    [ExportGroup("Reactions")]
    [Export(PropertyHint.Range, "0,2000,10")] public int StaggerMs { get; set; }
    [Export(PropertyHint.Range, "1,10,1")] public int MaxStaggerChain { get; set; }
    [Export(PropertyHint.Range, "0,2000,10")] public int DeflectedRecoilMs { get; set; }
    [Export(PropertyHint.Range, "0,5000,10")] public int KneelMs { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float PostureAfterKneel { get; set; }
}
