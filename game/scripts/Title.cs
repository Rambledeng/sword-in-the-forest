using Godot;

namespace SwordInTheForest;

/// <summary>Title screen: attack, interact or accept starts the test arena (M1).</summary>
public partial class Title : Control
{
    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("attack") || e.IsActionPressed("interact") || e.IsActionPressed("ui_accept"))
        {
            GetViewport().SetInputAsHandled();
            GetTree().ChangeSceneToFile("res://scenes/Arena.tscn");
        }
    }
}
