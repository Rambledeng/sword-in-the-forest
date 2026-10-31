using Godot;

namespace SwordInTheForest.Tuning;

/// <summary>Game feel: hit-stop, shake, flash, knockback.</summary>
[GlobalClass]
public partial class FeelTuning : Resource
{
    [ExportGroup("Hit-stop")]
    [Export(PropertyHint.Range, "0,300,5")] public int HitStopHitMs { get; set; }
    [Export(PropertyHint.Range, "0,300,5")] public int HitStopDeflectMs { get; set; }
    [Export(PropertyHint.Range, "0,300,5")] public int HitStopBlockMs { get; set; }
    [Export(PropertyHint.Range, "0,300,5")] public int HitStopPlayerHurtMs { get; set; }
    [Export(PropertyHint.Range, "0,500,5")] public int HitStopFinisherMs { get; set; }

    [ExportGroup("Camera shake")]
    [Export(PropertyHint.Range, "0,1,0.05")] public float ShakeHit { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float ShakeDeflect { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float ShakeBlock { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float ShakePlayerHurt { get; set; }
    [Export(PropertyHint.Range, "0,1,0.05")] public float ShakeFinisher { get; set; }
    [Export(PropertyHint.Range, "0,16,1")] public float ShakeMaxPx { get; set; }
    [Export(PropertyHint.Range, "0.1,10,0.1")] public float ShakeDecayPerSec { get; set; }

    [ExportGroup("Knockback and flash")]
    [Export(PropertyHint.Range, "0,600,5")] public float KnockbackEnemyHit { get; set; }
    [Export(PropertyHint.Range, "0,600,5")] public float KnockbackDeflect { get; set; }
    [Export(PropertyHint.Range, "0,600,5")] public float KnockbackBlock { get; set; }
    [Export(PropertyHint.Range, "0,600,5")] public float KnockbackPlayerHurt { get; set; }
    [Export(PropertyHint.Range, "100,5000,10")] public float KnockbackDecay { get; set; }
    [Export(PropertyHint.Range, "0,500,5")] public int FlashMs { get; set; }

    [ExportGroup("Glint")]
    [Export(PropertyHint.Range, "20,600,5")] public int GlintVisibleMs { get; set; }
    [Export(PropertyHint.Range, "0,2,0.05")] public float SfxVolume { get; set; }
    [Export(PropertyHint.Range, "0,2,0.05")] public float MusicVolume { get; set; }
}
