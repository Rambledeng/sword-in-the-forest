using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Twenty seeds, every room built for real: each spawn point and the entry must be free of the
/// room's collision shapes (a body-sized circle overlaps no wall or obstacle), and every exit tile
/// must be open once the room is cleared. The plan's reachability is unit-tested in RunPlanTests.
/// </summary>
public partial class RunSeedsScenario : Scenario
{
    private readonly List<(int Seed, RoomPlan Plan)> _rooms = new();
    private int _next;
    private Room? _room;
    private int _checked;

    [Export] public int Seeds { get; set; } = 20;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            for (var s = 1; s <= Seeds; s++)
                foreach (var r in RunPlan.Build(s).Rooms) _rooms.Add((s, r));
            return;
        }
        if (_room == null)
        {
            if (_next >= _rooms.Count)
            {
                GD.Print($"  {_checked} rooms from {Seeds} seeds checked");
                Pass();
                return;
            }
            _room = new Room(_rooms[_next].Plan);
            AddChild(_room);
            return;
        }
        // One physics frame later the room's shapes are in the space.
        if (frame % 2 == 1) return;
        var (seed, plan) = _rooms[_next];
        var space = _room.GetWorld2D().DirectSpaceState;
        var circle = new CircleShape2D { Radius = Player.Radius };
        bool Free(Vector2 feet)
        {
            var q = new PhysicsShapeQueryParameters2D
            {
                Shape = circle,
                Transform = new Transform2D(0, _room.ToGlobal(feet + new Vector2(0, -3))),
                CollisionMask = Player.LayerWorld,
            };
            return space.IntersectShape(q, 4).Count == 0;
        }
        Check(!Free(Room.TileCenter(new TilePos(0, 5))), "sanity: the query must see the edge wall");
        Check(Free(_room.EntryPx), $"seed {seed} {plan.Template.Id}: the entry overlaps a wall");
        foreach (var s in plan.Spawns)
            Check(Free(Room.TileCenter(s.At) + new Vector2(0, 4)), $"seed {seed} {plan.Template.Id}: {s.Kind} at {s.At} overlaps a wall");
        Check(plan.IsBoss || plan.Template.Exits.All(e => _room.T.Passable(e)), $"seed {seed} {plan.Template.Id}: exit not passable");
        _room.QueueFree();
        _room = null;
        _next++;
        _checked++;
    }
}
