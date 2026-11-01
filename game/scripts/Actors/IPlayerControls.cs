using Godot;

namespace SwordInTheForest.Actors;

/// <summary>Where the player's intent comes from: real devices, or a test bot.</summary>
public interface IPlayerControls
{
    /// <summary>Called once per physics tick before anything is read.</summary>
    void Tick(double nowMs);

    /// <summary>Movement, length at most 1.</summary>
    Vector2 Move { get; }

    /// <summary>Unit aim direction from the player, or zero when there is no aim input.</summary>
    Vector2 Aim(Node2D player);

    bool JustPressed(PlayerAction action);
    bool Held(PlayerAction action);
}
