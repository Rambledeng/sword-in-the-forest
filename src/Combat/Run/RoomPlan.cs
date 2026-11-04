using System.Collections.Generic;

namespace SwordInTheForest.Run
{
    /// <summary>One room of a run: its template and the enemies placed in it.</summary>
    public sealed class RoomPlan
    {
        public RoomPlan(RoomTemplate template, IReadOnlyList<Spawn> spawns, int indexInArea, int seed)
        {
            Template = template;
            Spawns = spawns;
            IndexInArea = indexInArea;
            Seed = seed;
        }

        public RoomTemplate Template { get; }
        public IReadOnlyList<Spawn> Spawns { get; }
        public Area Area => Template.Area;
        public bool IsBoss => Template.IsBoss;
        /// <summary>0 for the first room of its area (the area's title shows there).</summary>
        public int IndexInArea { get; }
        /// <summary>Seed for the room's decoration and enemy behaviour.</summary>
        public int Seed { get; }
    }
}
