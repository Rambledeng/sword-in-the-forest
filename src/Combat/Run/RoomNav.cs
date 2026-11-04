using System.Collections.Generic;

namespace SwordInTheForest.Run
{
    /// <summary>Breadth-first search on a room's tiles (4-neighbour): reachability and the next step toward a goal.</summary>
    public static class RoomNav
    {
        private static readonly TilePos[] Dirs = { new TilePos(1, 0), new TilePos(-1, 0), new TilePos(0, 1), new TilePos(0, -1) };

        public static HashSet<TilePos> Reachable(RoomTemplate t, TilePos from)
        {
            var seen = new HashSet<TilePos>();
            if (!t.Passable(from)) return seen;
            var queue = new Queue<TilePos>();
            queue.Enqueue(from);
            seen.Add(from);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var d in Dirs)
                {
                    var n = new TilePos(p.X + d.X, p.Y + d.Y);
                    if (!t.Passable(n) || !seen.Add(n)) continue;
                    queue.Enqueue(n);
                }
            }
            return seen;
        }

        /// <summary>
        /// The tile to walk to next on a shortest path from <paramref name="from"/> to
        /// <paramref name="goal"/>, or null when the goal is unreachable or already reached.
        /// Exits count as walkable only when <paramref name="exitsOpen"/>.
        /// </summary>
        public static TilePos? NextStep(RoomTemplate t, TilePos from, TilePos goal, bool exitsOpen)
        {
            if (from == goal) return null;
            bool Walk(TilePos p) => t.At(p) == Tile.Floor || (exitsOpen && t.At(p) == Tile.Exit);
            if (!Walk(goal)) return null;
            // Search backwards from the goal so the first step out of 'from' falls out directly.
            var prev = new Dictionary<TilePos, TilePos>();
            var queue = new Queue<TilePos>();
            queue.Enqueue(goal);
            prev[goal] = goal;
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var d in Dirs)
                {
                    var n = new TilePos(p.X + d.X, p.Y + d.Y);
                    if (prev.ContainsKey(n)) continue;
                    if (n == from)
                    {
                        return p;
                    }
                    if (!Walk(n)) continue;
                    prev[n] = p;
                    queue.Enqueue(n);
                }
            }
            return null;
        }
    }
}
