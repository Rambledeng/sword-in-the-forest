using System;
using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Fx;
using SwordInTheForest.Tuning;

namespace SwordInTheForest.Actors;

/// <summary>
/// The tavern keeper (placeholder name), with twin cleavers. Phase 1: a two-hit cleaver combo and
/// a red charge. Phase 2, at half health or after her first finisher: shorter windups, a three-hit
/// flurry ending in red, and a thrown jar of drugged wine (蒙汗药) that slows you.
/// </summary>
public partial class TavernBoss : Enemy
{
    public int Phase { get; private set; } = 1;
    public event Action<TavernBoss>? PhaseChanged;

    protected override EnemyTuning Tuning => Tunables.Enemy("boss");
    protected override string SheetPath => "res://assets/third_party/ninja_adventure/actors/sorcerer_black.png";
    protected override string WeaponPath => "res://assets/third_party/ninja_adventure/weapons/cleaver_in_hand.png";
    protected override string? OffhandWeaponPath => WeaponPath;
    protected override float SpriteScale => 1.35f;
    protected override bool FinisherKills => Phase >= 2;
    protected override float WindupFactor => Phase >= 2 ? Tunables.Run.BossPhase2WindupFactor : 1f;

    protected override AttackData? CounterAttack => Tunables.Attack("boss_cleave_1");

    protected override AttackData ChooseAttack()
    {
        var run = Tunables.Run;
        var far = Target != null && IsInstanceValid(Target) && Target.BodyGlobal.DistanceTo(BodyGlobal) > 60;
        if (Phase >= 2)
        {
            if (far && Roll() < run.BossJarChance * 2) return Tunables.Attack("boss_jar");
            if (Roll() < run.BossJarChance * 0.5f) return Tunables.Attack("boss_jar");
            if (Roll() < run.BossFlurryChance) return Tunables.Attack("boss_flurry_1");
        }
        if (far || Roll() < run.BossChargeChance) return Tunables.Attack("boss_charge");
        return Tunables.Attack("boss_cleave_1");
    }

    protected override void OnHurt()
    {
        if (Phase == 1 && Health <= MaxHealth * Tunables.Run.BossPhase2At) EnterPhase2();
    }

    protected override void OnPhaseFinisher(Vector2 dir)
    {
        // Her first finisher costs her half her life and wakes her second phase.
        SetHealth(Mathf.Min(Health, Mathf.RoundToInt((float)(MaxHealth * Tunables.Run.BossPhase2At))));
        SparkFx.Finisher(BodyGlobal);
        Sfx.Play("finisher", 0.03f);
        EnterPhase2();
    }

    private void EnterPhase2()
    {
        if (Phase >= 2) return;
        Phase = 2;
        Recover(0);
        Sfx.Play("posture_break", 0.02f, 1.2f);
        ShakeCamera.Shake(Tunables.Feel.ShakeFinisher);
        SheetFx.Spawn("smoke", 6, 14, BodyGlobal, modulate: new Color(1f, 0.5f, 0.4f), z: 10);
        PhaseChanged?.Invoke(this);
    }
}
