using Godot;
using SwordInTheForest.Core;

namespace SwordInTheForest.Fx;

/// <summary>A one-shot animation from a horizontal sprite sheet; it follows game time, so it freezes during hit-stop.</summary>
public partial class SheetFx : Sprite2D
{
    private const string Dir = "res://assets/third_party/ninja_adventure/fx";
    private readonly float _fps;
    private double _elapsedMs;

    public SheetFx(string sheet, int frames, float fps)
    {
        Texture = GD.Load<Texture2D>($"{Dir}/{sheet}.png");
        Hframes = frames;
        _fps = fps;
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public SheetFx() { }

    public static SheetFx Spawn(string sheet, int frames, float fps, Vector2 at, float rotation = 0, bool flipV = false, Color? modulate = null, int z = 0)
    {
        var fx = new SheetFx(sheet, frames, fps) { Rotation = rotation, FlipV = flipV, ZIndex = z };
        if (modulate != null) fx.Modulate = modulate.Value;
        FxLayer.Add(fx, at);
        return fx;
    }

    public override void _Process(double delta)
    {
        if (GameClock.Instance.Frozen) return;
        _elapsedMs += delta * 1000.0;
        var frame = (int)(_elapsedMs / 1000.0 * _fps);
        if (frame >= Hframes) { QueueFree(); return; }
        Frame = frame;
    }
}
