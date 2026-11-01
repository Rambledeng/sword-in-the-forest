using Godot;

namespace SwordInTheForest;

/// <summary>The arena's grass floor, drawn once from TilesetFloor with a few seeded variations.</summary>
public partial class ArenaFloor : Node2D
{
    private const string TileDir = "res://assets/third_party/ninja_adventure/tiles";
    private static readonly Vector2I Plain = new(11, 12);
    private static readonly Vector2I[] Variants = { new(12, 12), new(13, 12), new(14, 12), new(15, 12) };

    public Vector2I SizeTiles { get; set; }
    public int Seed { get; set; } = 7;

    // Held for the node's lifetime. Draw commands keep only the texture's RID, so a texture
    // loaded into a local inside _Draw was freed when the GC collected its C# wrapper, and the
    // floor turned white after a while of play (M1 playtest bug).
    private Texture2D _tiles = null!;

    public override void _Ready()
    {
        ZIndex = -100;
        _tiles = GD.Load<Texture2D>($"{TileDir}/tileset_floor.png");
    }

    public override void _Draw()
    {
        var tex = _tiles;
        var rng = new RandomNumberGenerator { Seed = (ulong)Seed };
        for (var y = 0; y < SizeTiles.Y; y++)
        for (var x = 0; x < SizeTiles.X; x++)
        {
            var r = rng.Randf();
            var tile = r < 0.12f ? Variants[rng.RandiRange(0, Variants.Length - 1)] : Plain;
            DrawTextureRectRegion(tex, new Rect2(x * 16, y * 16, 16, 16), new Rect2(tile.X * 16, tile.Y * 16, 16, 16));
        }
    }
}
