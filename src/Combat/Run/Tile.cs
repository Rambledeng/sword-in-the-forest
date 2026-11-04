namespace SwordInTheForest.Run
{
    public enum Tile
    {
        Floor,
        /// <summary>The room's edge (bamboo, trees or the tavern's wall).</summary>
        Wall,
        /// <summary>An obstacle inside the room (bamboo clump, rock, table).</summary>
        Obstacle,
        /// <summary>The way on: closed until the room is cleared, walkable after.</summary>
        Exit,
    }
}
