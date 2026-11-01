using Godot;
using SwordInTheForest.Core;

namespace SwordInTheForest.Actors;

/// <summary>
/// Keyboard, mouse and gamepad through the input map. Aim: the right stick on a gamepad; the
/// mouse cursor only while the mouse is in use (moved or clicked within MouseAimIdleMs).
/// Otherwise there is no explicit aim and the player aims along movement or facing.
/// </summary>
public partial class DeviceControls : Node, IPlayerControls
{
    private const float StickAimDeadzone = 0.35f;

    private double _mouseUsedAtMs = double.NegativeInfinity;

    public bool UsingPad { get; private set; }

    /// <summary>The mouse moved or clicked recently enough to aim.</summary>
    public bool MouseActive =>
        !UsingPad && Now - _mouseUsedAtMs <= Tunables.Player.MouseAimIdleMs;

    private static double Now => GameClock.Instance?.NowMs ?? 0;

    public void Tick(double nowMs) { }

    public Vector2 Move => Input.GetVector("move_left", "move_right", "move_up", "move_down");

    public Vector2 Aim(Node2D player)
    {
        if (UsingPad)
        {
            var stick = Input.GetVector("aim_left", "aim_right", "aim_up", "aim_down");
            return stick.Length() >= StickAimDeadzone ? stick.Normalized() : Vector2.Zero;
        }
        if (!MouseActive) return Vector2.Zero;
        var toMouse = player.GetGlobalMousePosition() - (player.GlobalPosition + Player.BodyCenter);
        return toMouse.LengthSquared() > 4 ? toMouse.Normalized() : Vector2.Zero;
    }

    public bool JustPressed(PlayerAction action) => Input.IsActionJustPressed(ActionName(action));
    public bool Held(PlayerAction action) => Input.IsActionPressed(ActionName(action));

    public override void _Input(InputEvent e)
    {
        switch (e)
        {
            case InputEventJoypadButton:
                UsingPad = true;
                break;
            case InputEventJoypadMotion m when Mathf.Abs(m.AxisValue) > StickAimDeadzone:
                UsingPad = true;
                break;
            case InputEventKey:
                UsingPad = false;
                break;
            case InputEventMouseButton:
                UsingPad = false;
                _mouseUsedAtMs = Now;
                break;
            case InputEventMouseMotion mm when mm.Relative.LengthSquared() > 4:
                UsingPad = false;
                _mouseUsedAtMs = Now;
                break;
        }
    }

    private static string ActionName(PlayerAction a) => a switch
    {
        PlayerAction.Attack => "attack",
        PlayerAction.Guard => "guard",
        PlayerAction.Roll => "roll",
        PlayerAction.Interact => "interact",
        _ => "drink",
    };
}
