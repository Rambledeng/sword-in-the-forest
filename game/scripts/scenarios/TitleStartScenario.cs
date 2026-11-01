using Godot;

namespace SwordInTheForest.Scenarios;

/// <summary>Left click, J and gamepad X each start the game from the title screen.</summary>
public partial class TitleStartScenario : Scenario
{
    private Title? _title;

    private void Fresh()
    {
        _title?.QueueFree();
        _title = GD.Load<PackedScene>("res://scenes/Title.tscn").Instantiate<Title>();
        _title.ChangeScene = false;
        AddChild(_title);
    }

    protected override void Step(int frame)
    {
        switch (frame)
        {
            case 0: Fresh(); break;
            case 2: Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = true, Position = new Vector2(240, 135), ButtonMask = MouseButtonMask.Left }); break;
            case 4:
                Check(_title!.StartRequested, "left click should start the game");
                Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = false, Position = new Vector2(240, 135) });
                Fresh();
                break;
            case 6: Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.J, Keycode = Key.J, Pressed = true }); break;
            case 8:
                Check(_title!.StartRequested, "J should start the game");
                Input.ParseInputEvent(new InputEventKey { PhysicalKeycode = Key.J, Keycode = Key.J, Pressed = false });
                Fresh();
                break;
            case 10: Input.ParseInputEvent(new InputEventJoypadButton { Device = 0, ButtonIndex = JoyButton.X, Pressed = true }); break;
            case 12:
                Check(_title!.StartRequested, "gamepad X should start the game");
                Input.ParseInputEvent(new InputEventJoypadButton { Device = 0, ButtonIndex = JoyButton.X, Pressed = false });
                break;
        }
    }
}
