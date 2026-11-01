using Godot;

namespace SwordInTheForest.Tuning;

/// <summary>Player numbers, edited live in the F1 panel. Saved to res://tuning/player.tres.</summary>
[GlobalClass]
public partial class PlayerTuning : Resource
{
    [ExportGroup("Health and gourd")]
    [Export(PropertyHint.Range, "1,40,1")] public int MaxHealth { get; set; }
    [Export(PropertyHint.Range, "0,9,1")] public int GourdSips { get; set; }
    [Export(PropertyHint.Range, "0,10,1")] public int GourdHeal { get; set; }
    [Export(PropertyHint.Range, "100,3000,10")] public int DrinkMs { get; set; }
    [Export(PropertyHint.Range, "0,3000,10")] public int DrinkHealAtMs { get; set; }
    [Export(PropertyHint.Range, "0,200,1")] public float DrinkMoveSpeed { get; set; }

    [ExportGroup("Movement")]
    [Export(PropertyHint.Range, "10,300,1")] public float MoveSpeed { get; set; }
    [Export(PropertyHint.Range, "10,300,1")] public float GuardMoveSpeed { get; set; }
    [Export(PropertyHint.Range, "100,5000,10")] public float Acceleration { get; set; }
    [Export(PropertyHint.Range, "100,5000,10")] public float Friction { get; set; }

    [ExportGroup("Combo")]
    [Export(PropertyHint.Range, "0,500,5")] public int Attack1WindupMs { get; set; }
    [Export(PropertyHint.Range, "0,1000,5")] public int Attack1RecoveryMs { get; set; }
    [Export(PropertyHint.Range, "0,10,1")] public int Attack1Damage { get; set; }
    [Export(PropertyHint.Range, "0,100,1")] public int Attack1Posture { get; set; }
    [Export(PropertyHint.Range, "0,500,5")] public int Attack2WindupMs { get; set; }
    [Export(PropertyHint.Range, "0,1000,5")] public int Attack2RecoveryMs { get; set; }
    [Export(PropertyHint.Range, "0,10,1")] public int Attack2Damage { get; set; }
    [Export(PropertyHint.Range, "0,100,1")] public int Attack2Posture { get; set; }
    [Export(PropertyHint.Range, "0,500,5")] public int Attack3WindupMs { get; set; }
    [Export(PropertyHint.Range, "0,1000,5")] public int Attack3RecoveryMs { get; set; }
    [Export(PropertyHint.Range, "0,10,1")] public int Attack3Damage { get; set; }
    [Export(PropertyHint.Range, "0,100,1")] public int Attack3Posture { get; set; }
    [Export(PropertyHint.Range, "0,1000,5")] public int ComboCancelFromMs { get; set; }
    [Export(PropertyHint.Range, "4,80,1")] public float AttackReachPx { get; set; }
    [Export(PropertyHint.Range, "10,360,5")] public float AttackArcDeg { get; set; }
    [Export(PropertyHint.Range, "0,40,1")] public float AttackLungePx { get; set; }
    [Export(PropertyHint.Range, "0,40,1")] public float FinisherLungePx { get; set; }
    /// <summary>Posture each of your own swings costs (stamina is posture): spam breaks your guard.</summary>
    [Export(PropertyHint.Range, "0,50,1")] public float AttackPostureCost { get; set; }

    [ExportGroup("Aim")]
    /// <summary>The mouse aims only while it moved or clicked this recently; otherwise movement/facing aims.</summary>
    [Export(PropertyHint.Range, "0,10000,100")] public int MouseAimIdleMs { get; set; }
    /// <summary>Aim assist cone (full angle) when aiming by movement or facing.</summary>
    [Export(PropertyHint.Range, "0,180,5")] public float AimAssistDeg { get; set; }
    /// <summary>Aim assist cone (full angle) for mouse or right-stick aim.</summary>
    [Export(PropertyHint.Range, "0,180,5")] public float AimAssistExplicitDeg { get; set; }
    [Export(PropertyHint.Range, "0,200,1")] public float AimAssistRangePx { get; set; }

    [ExportGroup("Roll")]
    [Export(PropertyHint.Range, "100,1000,10")] public int RollDurationMs { get; set; }
    [Export(PropertyHint.Range, "0,1000,10")] public int RollInvulnStartMs { get; set; }
    [Export(PropertyHint.Range, "0,1000,10")] public int RollInvulnEndMs { get; set; }
    [Export(PropertyHint.Range, "0,200,1")] public float RollDistancePx { get; set; }

    [ExportGroup("Guard and posture")]
    [Export(PropertyHint.Range, "10,400,5")] public float MaxPosture { get; set; }
    [Export(PropertyHint.Range, "0,200,1")] public float PostureDrainPerSec { get; set; }
    [Export(PropertyHint.Range, "0,3000,10")] public float PostureDrainDelayMs { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float DeflectPostureFactor { get; set; }
    /// <summary>Guard and deflect only catch attacks from within this arc around your facing (full angle). Rolls evade from anywhere.</summary>
    [Export(PropertyHint.Range, "30,360,5")] public float GuardArcDeg { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float HitPostureFactor { get; set; }
    [Export(PropertyHint.Range, "100,3000,10")] public int BrokenMs { get; set; }
    [Export(PropertyHint.Range, "0,1000,10")] public int HurtMs { get; set; }
    [Export(PropertyHint.Range, "0,1000,10")] public int InputBufferMs { get; set; }
    /// <summary>A guard press this soon after the previous one only blocks: mashing guard does not deflect.</summary>
    [Export(PropertyHint.Range, "0,1000,10")] public int DeflectMashLockoutMs { get; set; }
}
