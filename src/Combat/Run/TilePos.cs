using System;

namespace SwordInTheForest.Run
{
    /// <summary>A tile coordinate in a room (x to the right, y down).</summary>
    public readonly struct TilePos : IEquatable<TilePos>
    {
        public TilePos(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public int Manhattan(TilePos o) => Math.Abs(X - o.X) + Math.Abs(Y - o.Y);
        public bool Equals(TilePos o) => X == o.X && Y == o.Y;
        public override bool Equals(object? obj) => obj is TilePos p && Equals(p);
        public override int GetHashCode() => X * 397 ^ Y;
        public override string ToString() => $"({X},{Y})";
        public static bool operator ==(TilePos a, TilePos b) => a.Equals(b);
        public static bool operator !=(TilePos a, TilePos b) => !a.Equals(b);
    }
}
