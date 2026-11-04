using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Tuning;

namespace SwordInTheForest.Actors;

/// <summary>
/// The archer (弓手): teaches spacing and closing in. Aims with a visible line, then shoots a
/// white arrow that can be deflected. Backs off when you close in, and does not guard.
/// </summary>
public partial class Archer : Enemy
{
    protected override EnemyTuning Tuning => Tunables.Enemy("archer");
    protected override string SheetPath => "res://assets/third_party/ninja_adventure/actors/hunter.png";
    protected override string WeaponPath => "res://assets/third_party/ninja_adventure/weapons/bow.png";

    public override bool IsBlocking => false;

    protected override AttackData ChooseAttack() => Tunables.Attack("archer_shot");

    protected override Vector2 UpdateApproach(Vector2 toTarget, float dist, bool targetAlive)
    {
        if (!targetAlive) return Vector2.Zero;
        Facing = toTarget.Normalized();
        if (dist <= Tunables.Run.ArcherKeepPx + 20)
        {
            BeginCircle();
            return Vector2.Zero;
        }
        AddWalk();
        return SteerTo(Target!.GlobalPosition) * Tuning.MoveSpeed;
    }

    protected override Vector2 UpdateCircle(Vector2 toTarget, float dist, bool targetAlive)
    {
        if (!targetAlive) return Vector2.Zero;
        Facing = toTarget.Normalized();
        var run = Tunables.Run;
        if (dist > run.ArcherKeepPx * 1.6f) { Enter(State.Approach); return Vector2.Zero; }
        if (dist < run.ArcherFleePx)
        {
            // Back off, away from the player (around obstacles), until it has room to shoot.
            AddWalk();
            var away = GlobalPosition - toTarget.Normalized() * 48;
            var dir = SteerTo(away);
            // Cornered: shoot anyway after a moment.
            if (StateElapsedMs >= 2500) StartAttack(ChooseAttack());
            return dir * Tuning.MoveSpeed;
        }
        if (!SeesTarget())
        {
            // No clear shot: walk around the cover toward the player.
            AddWalk();
            return SteerTo(Target!.GlobalPosition) * Tuning.CircleSpeed;
        }
        if (CircleDone)
        {
            StartAttack(ChooseAttack());
            return Vector2.Zero;
        }
        AddWalk();
        var tangent = Facing.Orthogonal() * CircleSign;
        var radial = Facing * Mathf.Clamp((dist - run.ArcherKeepPx) * 0.05f, -1f, 1f);
        return (tangent + radial).Normalized() * Tuning.CircleSpeed;
    }
}
