using Godot;

namespace SwordInTheForest.Tuning;

/// <summary>The run's numbers: tutorial, mercy, upgrades, the sword's lines, the boss and its wine.</summary>
[GlobalClass]
public partial class RunTuning : Resource
{
    [ExportGroup("Tutorial")]
    /// <summary>Attacks the tutorial ghost makes (calibrating) before it can be broken.</summary>
    [Export(PropertyHint.Range, "0,40,1")] public int TutorialAttacks { get; set; }
    /// <summary>Fewer for a returning player whose reading already has this many observations.</summary>
    [Export(PropertyHint.Range, "0,40,1")] public int TutorialAttacksReturning { get; set; }
    [Export(PropertyHint.Range, "0,1000,5")] public int ReturningObservations { get; set; }
    /// <summary>The run's first red attack (the spearman's thrust) gets at least this lead.</summary>
    [Export(PropertyHint.Range, "200,1500,10")] public int FirstRedLeadMs { get; set; }

    [ExportGroup("Mercy")]
    /// <summary>Chance that a posture-broken enemy drops its weapon and begs instead of kneeling.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float SurrenderChance { get; set; }
    [Export(PropertyHint.Range, "8,120,1")] public float SpareRangePx { get; set; }
    /// <summary>After the first beggar of a run (always genuine), the chance that a beggar is a fake.</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float FakeBegChance { get; set; }
    /// <summary>A spared enemy takes this long to stand up before it walks out.</summary>
    [Export(PropertyHint.Range, "0,3000,50")] public int SpareStandMs { get; set; }
    /// <summary>Its walking speed out of the room, as a fraction of its move speed.</summary>
    [Export(PropertyHint.Range, "0.1,2,0.05")] public float SpareLeaveSpeedFactor { get; set; }
    /// <summary>It fades out wherever it is after this long (stuck on a corner).</summary>
    [Export(PropertyHint.Range, "1000,30000,500")] public int SpareLeaveMaxMs { get; set; }

    [ExportGroup("Upgrades")]
    [Export(PropertyHint.Range, "0,10,1")] public int VitalityHealth { get; set; }
    [Export(PropertyHint.Range, "0,200,5")] public float BroadStancePosture { get; set; }
    [Export(PropertyHint.Range, "1,4,0.05")] public float SteadyBreathFactor { get; set; }
    [Export(PropertyHint.Range, "0,5,1")] public int KeenEdgeDamage { get; set; }
    [Export(PropertyHint.Range, "0,5,1")] public int DeepGourdSips { get; set; }
    [Export(PropertyHint.Range, "1,4,0.05")] public float IronDeflectFactor { get; set; }
    [Export(PropertyHint.Range, "0,10,1")] public int MercifulCutHeal { get; set; }

    [ExportGroup("Sword's lines")]
    [Export(PropertyHint.Range, "500,10000,100")] public int SwordLineMs { get; set; }
    /// <summary>The sword names your speed once attunement reaches this.</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float ReadingAttunedAt { get; set; }
    /// <summary>It comments again when its median has moved this much (fraction).</summary>
    [Export(PropertyHint.Range, "0,1,0.01")] public float ReadingShift { get; set; }
    [Export(PropertyHint.Range, "1,100,1")] public int ReadingMinGap { get; set; }

    [ExportGroup("Rooms")]
    [Export(PropertyHint.Range, "0,5000,50")] public int DeathToSummaryMs { get; set; }
    [Export(PropertyHint.Range, "0,5000,50")] public int AreaTitleMs { get; set; }

    [ExportGroup("Boss")]
    /// <summary>Phase 2 starts at this fraction of health (or at the first finisher).</summary>
    [Export(PropertyHint.Range, "0,1,0.05")] public float BossPhase2At { get; set; }
    /// <summary>Phase 2 windups are multiplied by this.</summary>
    [Export(PropertyHint.Range, "0.2,1,0.05")] public float BossPhase2WindupFactor { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float BossChargeChance { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float BossJarChance { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float BossFlurryChance { get; set; }
    /// <summary>Drugged wine: your speed is multiplied by this while it lasts.</summary>
    [Export(PropertyHint.Range, "0.1,1,0.05")] public float DrugSlowFactor { get; set; }
    [Export(PropertyHint.Range, "0,10000,100")] public int DrugSlowMs { get; set; }

    [ExportGroup("Archer")]
    /// <summary>The archer backs off when you are closer than this.</summary>
    [Export(PropertyHint.Range, "0,200,1")] public float ArcherFleePx { get; set; }
    /// <summary>The distance it likes to shoot from.</summary>
    [Export(PropertyHint.Range, "0,300,1")] public float ArcherKeepPx { get; set; }
}
