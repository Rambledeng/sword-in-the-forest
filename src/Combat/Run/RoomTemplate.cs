using System;
using System.Collections.Generic;

namespace SwordInTheForest.Run
{
    /// <summary>
    /// A handmade room as text, one character per 16 px tile:
    /// <c>#</c> edge, <c>o</c> obstacle, <c>.</c> floor, <c>P</c> the player's entry,
    /// <c>X</c> the exit (closed until the room is clear), <c>s</c> an enemy spawn point,
    /// <c>B</c> the boss's spawn point.
    /// </summary>
    public sealed class RoomTemplate
    {
        private readonly Tile[,] _tiles;
        private readonly List<TilePos> _spawnPoints = new List<TilePos>();
        private readonly List<TilePos> _exits = new List<TilePos>();

        public RoomTemplate(string id, Area area, bool isBoss, string[] rows)
        {
            Id = id;
            Area = area;
            IsBoss = isBoss;
            Rows = rows;
            Height = rows.Length;
            Width = rows.Length > 0 ? rows[0].Length : 0;
            _tiles = new Tile[Width, Height];
            for (var y = 0; y < Height; y++)
            {
                if (rows[y].Length != Width) throw new ArgumentException($"{id}: row {y} is {rows[y].Length} wide, expected {Width}");
                for (var x = 0; x < Width; x++)
                {
                    var c = rows[y][x];
                    var p = new TilePos(x, y);
                    switch (c)
                    {
                        case '#': _tiles[x, y] = Tile.Wall; break;
                        case 'o': _tiles[x, y] = Tile.Obstacle; break;
                        case 'X': _tiles[x, y] = Tile.Exit; _exits.Add(p); break;
                        case 'P': Entry = p; break;
                        case 's': _spawnPoints.Add(p); break;
                        case 'B': BossSpawn = p; break;
                        case '.': break;
                        default: throw new ArgumentException($"{id}: unknown tile '{c}' at {p}");
                    }
                }
            }
        }

        public string Id { get; }
        public Area Area { get; }
        public bool IsBoss { get; }
        public string[] Rows { get; }
        public int Width { get; }
        public int Height { get; }
        public TilePos Entry { get; }
        public TilePos? BossSpawn { get; }
        public IReadOnlyList<TilePos> SpawnPoints => _spawnPoints;
        public IReadOnlyList<TilePos> Exits => _exits;

        public bool InBounds(TilePos p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;

        public Tile At(TilePos p) => InBounds(p) ? _tiles[p.X, p.Y] : Tile.Wall;

        /// <summary>Walkable once the room is clear (exits included).</summary>
        public bool Passable(TilePos p) => At(p) == Tile.Floor || At(p) == Tile.Exit;
    }
}
