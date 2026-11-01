using Godot;

namespace SwordInTheForest.Actors;

/// <summary>
/// A weapon held in the hand: the pack's "SpriteInHand" images have the hilt at the top and the
/// blade pointing down. <see cref="Point"/> aims the blade along a world direction from the pivot.
/// </summary>
public partial class WeaponSprite : Node2D
{
    private readonly Sprite2D _sprite = new();

    public WeaponSprite(Texture2D texture)
    {
        _sprite.Texture = texture;
        _sprite.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        // Hilt about two pixels below the pivot, blade along +Y before rotation.
        _sprite.Offset = new Vector2(0, texture.GetHeight() / 2f - 2);
        AddChild(_sprite);
        Length = texture.GetHeight() - 2;
    }

    public WeaponSprite() { }

    public float Length { get; }

    /// <summary>The blade's tip in global coordinates.</summary>
    public Vector2 TipGlobal => GlobalPosition + Vector2.Down.Rotated(GlobalRotation) * Length;

    /// <summary>
    /// Resting pose: held low at the side, blade angled down and outward, on the screen-right
    /// side unless facing left.
    /// </summary>
    public static (Vector2 Pivot, float Angle) Rest(Vector2 bodyCenter, Vector2 facing)
    {
        var left = facing.X < -0.5f && Mathf.Abs(facing.X) > Mathf.Abs(facing.Y) * 0.9f;
        var s = left ? -1f : 1f;
        return (bodyCenter + new Vector2(5 * s, 3), Mathf.Pi / 2 - 0.45f * s);
    }

    /// <summary>Places the hilt at <paramref name="localPivot"/> and points the blade along <paramref name="bladeAngle"/> (radians).</summary>
    public void Point(Vector2 localPivot, float bladeAngle)
    {
        Position = localPivot.Round();
        Rotation = bladeAngle - Mathf.Pi / 2;
    }
}
