using Godot;
using SwordInTheForest.Core;

namespace SwordInTheForest.Fx;

/// <summary>Follows a target with pixel-snapped position and trauma-based shake (shake = trauma²).</summary>
public partial class ShakeCamera : Camera2D
{
    private float _trauma;
    private float _time;

    public static ShakeCamera? Current { get; private set; }
    public Node2D? Target { get; set; }

    public override void _EnterTree() => Current = this;

    public override void _ExitTree()
    {
        if (Current == this) Current = null;
    }

    public static void Shake(float amount)
    {
        if (Current != null) Current._trauma = Mathf.Min(1f, Current._trauma + amount);
    }

    public override void _Process(double delta)
    {
        var feel = Tunables.Feel;
        _time += (float)delta;
        _trauma = Mathf.Max(0f, _trauma - feel.ShakeDecayPerSec * (float)delta);
        if (Target != null && IsInstanceValid(Target)) GlobalPosition = (Target.GlobalPosition + new Vector2(0, -8)).Round();
        var s = _trauma * _trauma * feel.ShakeMaxPx;
        Offset = new Vector2(
            Mathf.Round(s * Mathf.Sin(_time * 71f)),
            Mathf.Round(s * Mathf.Sin(_time * 53f + 1.3f)));
    }
}
