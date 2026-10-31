using Godot;

namespace SwordInTheForest.Fx;

/// <summary>Where one-shot effects are spawned: the arena's effect layer, or the scene root.</summary>
public static class FxLayer
{
    private static Node2D? _layer;

    public static void Set(Node2D layer) => _layer = layer;

    public static void Add(Node2D fx, Vector2 globalPosition)
    {
        Node parent = GodotObject.IsInstanceValid(_layer) && _layer!.IsInsideTree()
            ? _layer
            : ((SceneTree)Engine.GetMainLoop()).CurrentScene;
        parent.AddChild(fx);
        fx.GlobalPosition = globalPosition;
    }
}
