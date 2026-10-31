using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Tuning;

namespace SwordInTheForest.Actors;

/// <summary>
/// The saber bandit (刀客): teaches blocking and deflecting. A two-hit combo and a slow overhead
/// chop, all white. Approaches, circles at a distance, then attacks.
/// </summary>
public partial class SaberBandit : Enemy
{
    protected override EnemyTuning Tuning => Tunables.SaberBandit;
    protected override string SheetPath => "res://assets/third_party/ninja_adventure/actors/camouflage_red.png";
    protected override string WeaponPath => "res://assets/third_party/ninja_adventure/weapons/big_sword_in_hand.png";

    protected override AttackData ChooseAttack() =>
        GD.Randf() < Tuning.ChopChance ? Tunables.Attack("saber_chop") : Tunables.Attack("saber_slash_1");
}
