using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Fx;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Arrows stop at obstacles: an archer in drill mode shoots at a player standing behind a row of
/// obstacles (no hit may land), then at the same player in the open (hits must land).
/// </summary>
public partial class ArrowWallScenario : Scenario
{
    private static readonly RoomTemplate Template = new("arrow_wall", Area.Outside, false, new[]
    {
        "##########",
        "#........#",
        "#...s....#",
        "#........#",
        "#.oooooo.#",
        "#........#",
        "#...P....#",
        "##########",
    });

    private Room _room = null!;
    private Archer _archer = null!;
    private int _hitsBehind = -1;
    private int _shotsBehind;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            var plan = new RoomPlan(Template, new[] { new Spawn(EnemyKind.Archer, new TilePos(4, 2), 0) }, 0, 11);
            _room = new Room(plan);
            AddChild(_room);
            FxLayer.Set(_room.Fx);
            var player = new Player { Invulnerable = true, Position = _room.EntryPx };
            _room.Actors.AddChild(player);
            _room.SpawnEnemies(player, 0f);
            _archer = _room.Spawned.OfType<Archer>().Single();
            _archer.DrillMode = true;
            return;
        }
        var p = _room.Actors.GetChildren().OfType<Player>().Single();
        if (_archer.DrillIdle) _archer.Drill(Tunables.Attack("archer_shot"));
        if (_hitsBehind < 0)
        {
            Check(!_archer.SeesTarget(), "the archer sees through the obstacles");
            if (_archer.AttacksMade >= 3)
            {
                _hitsBehind = p.HitsTaken;
                _shotsBehind = _archer.AttacksMade;
                Check(_hitsBehind == 0, $"{_hitsBehind} arrows went through the obstacles");
                // Into the open, on the archer's row.
                p.GlobalPosition = _room.ToGlobal(Room.TileCenter(new TilePos(8, 2)) + new Vector2(0, 4));
            }
            return;
        }
        Check(_archer.SeesTarget(), "the archer cannot see an open target");
        if (_archer.AttacksMade >= _shotsBehind + 3)
        {
            Check(p.HitsTaken >= 2, $"only {p.HitsTaken} of 3 open shots hit");
            GD.Print($"  behind cover: 0 hits in {_shotsBehind} shots; in the open: {p.HitsTaken} of 3");
            Pass();
        }
    }
}
