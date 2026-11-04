using Godot;

namespace SwordInTheForest.Run;

/// <summary>Steering around a room's obstacles: the direction to walk from one point toward another.</summary>
public interface INav
{
    /// <summary>A unit direction (zero when there is no way).</summary>
    Vector2 Steer(Vector2 fromGlobal, Vector2 toGlobal);

    /// <summary>Where a straight shot from one point toward another first meets a wall or obstacle (the target itself when clear).</summary>
    Vector2 ShotEnd(Vector2 fromGlobal, Vector2 toGlobal);

    /// <summary>Where a spared enemy walks to leave the room (the entrance).</summary>
    Vector2 LeavePoint { get; }
}
