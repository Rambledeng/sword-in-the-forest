using Godot;

namespace SwordInTheForest.Fx;

/// <summary>A fading copy of a character frame, left behind during a roll.</summary>
public partial class AfterImage : Sprite2D
{
    private const float LifeMs = 160;
    private double _elapsedMs;

    public static void From(Sprite2D source, Color tint)
    {
        var a = new AfterImage
        {
            Texture = source.Texture,
            Hframes = source.Hframes,
            Vframes = source.Vframes,
            Frame = source.Frame,
            Offset = source.Offset,
            FlipH = source.FlipH,
            TextureFilter = TextureFilterEnum.Nearest,
            Modulate = tint,
            ZIndex = -1,
        };
        FxLayer.Add(a, source.GlobalPosition.Round());
    }

    public override void _Process(double delta)
    {
        _elapsedMs += delta * 1000.0;
        if (_elapsedMs >= LifeMs) { QueueFree(); return; }
        Modulate = new Color(Modulate, 0.5f * (1f - (float)(_elapsedMs / LifeMs)));
    }
}
