using Godot;

namespace SwordInTheForest.Scenarios;

/// <summary>The title scene loads, survives some frames, and every input action has keyboard/mouse and gamepad bindings.</summary>
public partial class BootScenario : Scenario
{
    private static readonly string[] Actions =
    {
        "move_left", "move_right", "move_up", "move_down",
        "attack", "guard", "roll", "interact", "drink", "pause",
    };

    private Node? _title;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _title = GD.Load<PackedScene>("res://scenes/Title.tscn").Instantiate();
            AddChild(_title);
            foreach (var action in Actions)
            {
                Check(InputMap.HasAction(action), $"missing action {action}");
                var events = InputMap.ActionGetEvents(action);
                var hasPc = false;
                var hasPad = false;
                foreach (var e in events)
                {
                    hasPc |= e is InputEventKey or InputEventMouseButton;
                    hasPad |= e is InputEventJoypadButton or InputEventJoypadMotion;
                }
                Check(hasPc && hasPad, $"{action} needs keyboard/mouse and gamepad bindings");
            }
        }
        Check(IsInstanceValid(_title) && _title!.IsInsideTree(), "title scene left the tree");
        Check(GetViewport().GetVisibleRect().Size == new Vector2(480, 270), "base resolution is not 480x270");
    }
}
