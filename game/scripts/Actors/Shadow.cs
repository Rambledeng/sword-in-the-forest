using Godot;

namespace SwordInTheForest.Actors;

/// <summary>A soft oval under a character's feet, so it reads as standing on the ground.</summary>
public partial class Shadow : Node2D
{
    public float Width { get; set; } = 6;

    public override void _Draw()
    {
        var c = new Color(0, 0, 0, 0.28f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(1, 0.4f));
        DrawCircle(Vector2.Zero, Width, c);
    }

    /// <summary>Puts the weapon just behind or just in front of the body in draw order.</summary>
    public static void PlaceWeapon(Node parent, Node2D weapon, Node2D body, bool behind)
    {
        var wantIndex = behind ? body.GetIndex() - 1 : body.GetIndex() + 1;
        if (weapon.GetIndex() == wantIndex) return;
        parent.MoveChild(weapon, behind ? body.GetIndex() : body.GetIndex());
    }
}
