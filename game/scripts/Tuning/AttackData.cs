using Godot;
using SwordInTheForest.Combat;

namespace SwordInTheForest.Tuning;

/// <summary>
/// One enemy attack as data: windup range, glint colour, designed lead time and range, damage,
/// posture damage and hit area. Files live in res://tuning/attacks/.
/// </summary>
[GlobalClass]
public partial class AttackData : Resource
{
    [Export] public string Id { get; set; } = "attack";
    /// <summary>Red glint (杀招): unblockable, roll only.</summary>
    [Export] public bool Red { get; set; }

    [ExportGroup("Timing")]
    [Export(PropertyHint.Range, "0,4000,10")] public int WindupMinMs { get; set; }
    [Export(PropertyHint.Range, "0,4000,10")] public int WindupMaxMs { get; set; }
    [Export(PropertyHint.Range, "50,1500,10")] public int LeadDefaultMs { get; set; }
    [Export(PropertyHint.Range, "50,1500,10")] public int LeadMinMs { get; set; }
    [Export(PropertyHint.Range, "50,1500,10")] public int LeadMaxMs { get; set; }
    /// <summary>The visible strike starts this long before the impact; keep it under a reaction time.</summary>
    [Export(PropertyHint.Range, "0,300,5")] public int SwingMs { get; set; }
    [Export(PropertyHint.Range, "0,4000,10")] public int RecoveryMs { get; set; }

    [ExportGroup("Effect")]
    [Export(PropertyHint.Range, "0,10,1")] public int Damage { get; set; }
    [Export(PropertyHint.Range, "0,100,1")] public float PostureDamage { get; set; }
    [Export(PropertyHint.Range, "0,100,1")] public float DeflectPostureDamage { get; set; }

    [ExportGroup("Hit area")]
    [Export(PropertyHint.Range, "4,120,1")] public float ReachPx { get; set; }
    [Export(PropertyHint.Range, "10,360,5")] public float ArcDeg { get; set; }
    [Export(PropertyHint.Range, "0,60,1")] public float LungePx { get; set; }

    /// <summary>The next attack of a combo, started right after this one's recovery.</summary>
    [Export] public AttackData? Next { get; set; }

    public AttackSpec ToSpec() => new(Id, Red ? GlintColor.Red : GlintColor.White,
        WindupMinMs, WindupMaxMs, LeadDefaultMs, LeadMinMs, LeadMaxMs, RecoveryMs);
}
