using System.Collections.Generic;

namespace SwordInTheForest.Run
{
    /// <summary>
    /// Checks a planned room: the exit (if any) is reachable from the entry, every spawn is
    /// reachable, and every spawn stands on open floor (its tile and all eight neighbours walkable).
    /// </summary>
    public static class RoomValidator
    {
        public static List<string> Problems(RoomPlan room)
        {
            var t = room.Template;
            var problems = new List<string>();
            if (!t.Passable(t.Entry)) problems.Add($"{t.Id}: entry {t.Entry} is not walkable");
            var reach = RoomNav.Reachable(t, t.Entry);
            if (!t.IsBoss && t.Exits.Count == 0) problems.Add($"{t.Id}: no exit");
            foreach (var e in t.Exits)
                if (!reach.Contains(e)) problems.Add($"{t.Id}: exit {e} unreachable");
            foreach (var s in room.Spawns)
            {
                if (!reach.Contains(s.At)) problems.Add($"{t.Id}: {s.Kind} at {s.At} unreachable");
                if (t.At(s.At) != Tile.Floor) problems.Add($"{t.Id}: {s.Kind} at {s.At} not on floor");
                for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                {
                    var n = new TilePos(s.At.X + dx, s.At.Y + dy);
                    if (t.At(n) != Tile.Floor) problems.Add($"{t.Id}: {s.Kind} at {s.At} is next to {t.At(n)} at {n}");
                }
                if (s.At.Manhattan(t.Entry) < 5) problems.Add($"{t.Id}: {s.Kind} at {s.At} too close to the entry");
            }
            return problems;
        }
    }
}
