using SwordInTheForest.Core;
using SwordInTheForest.Tuning;

namespace SwordInTheForest.Actors;

/// <summary>
/// The tutorial's weak ghost: slow white cuts at varied timings while the sword calibrates. It
/// shrugs off cuts and posture breaks until <see cref="Breakable"/> is set (calibration done).
/// It does not guard, so the player can practise cutting too.
/// </summary>
public partial class TutorialGhost : Enemy
{
    private bool _breakable;

    protected override EnemyTuning Tuning => Tunables.Enemy("ghost");
    protected override string SheetPath => "res://assets/third_party/ninja_adventure/actors/spirit.png";
    protected override string WeaponPath => "res://assets/third_party/ninja_adventure/weapons/katana_in_hand.png";
    protected override float BodyAlpha => 0.85f;

    public override bool Breakable => _breakable;
    public override bool IsBlocking => false;

    public void MakeBreakable() => _breakable = true;

    protected override AttackData ChooseAttack() => Tunables.Attack("ghost_slash");
}
