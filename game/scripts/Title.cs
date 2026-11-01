using Godot;

namespace SwordInTheForest;

/// <summary>
/// Title screen: attack (left click, J, X/RB), interact or accept starts the test arena (M1).
/// Uses _Input, not _UnhandledInput: the full-screen controls consume mouse clicks in the GUI
/// pass, which is why left click did nothing in M1.
/// </summary>
public partial class Title : Control
{
    /// <summary>Scenarios turn this off to observe the start without leaving the scene.</summary>
    public bool ChangeScene { get; set; } = true;
    public bool StartRequested { get; private set; }

    public override void _Input(InputEvent e)
    {
        if (StartRequested) return;
        if (!(e.IsActionPressed("attack") || e.IsActionPressed("interact") || e.IsActionPressed("ui_accept"))) return;
        GetViewport().SetInputAsHandled();
        StartRequested = true;
        if (ChangeScene) GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/Arena.tscn");
    }
}
