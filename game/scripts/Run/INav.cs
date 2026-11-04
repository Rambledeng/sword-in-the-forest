using Godot;

namespace SwordInTheForest.Run;

/// <summary>Steering around a room's obstacles: the direction to walk from one point toward another.</summary>
public interface INav
{
    /// <summary>A unit direction (zero when there is no way).</summary>
    Vector2 Steer(Vector2 fromGlobal, Vector2 toGlobal);
}
