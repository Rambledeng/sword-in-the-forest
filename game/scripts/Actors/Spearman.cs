using SwordInTheForest.Core;
using SwordInTheForest.Tuning;

namespace SwordInTheForest.Actors;

/// <summary>
/// The spearman (枪客): teaches reading red warnings. A long red thrust (roll, don't guard) and a
/// wide white sweep. Keeps a little more distance than the bandit, and guards while circling.
/// </summary>
public partial class Spearman : Enemy
{
    protected override EnemyTuning Tuning => Tunables.Enemy("spearman");
    protected override string SheetPath => "res://assets/third_party/ninja_adventure/actors/samurai_blue.png";
    protected override string WeaponPath => "res://assets/third_party/ninja_adventure/weapons/lance_in_hand.png";

    protected override AttackData? CounterAttack => Tunables.Attack("spear_sweep");

    protected override AttackData ChooseAttack() =>
        Roll() < Tuning.ChopChance ? Tunables.Attack("spear_thrust") : Tunables.Attack("spear_sweep");
}
