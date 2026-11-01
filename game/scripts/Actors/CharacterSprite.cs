using Godot;

namespace SwordInTheForest.Actors;

/// <summary>
/// A Ninja Adventure character sheet: 4 columns (down, up, left, right) by 7 rows
/// (rows 0–3 walk, 4 attack, 5 jump/crouch, 6 dead / item / special / special).
/// The node's origin is at the character's feet.
/// </summary>
public partial class CharacterSprite : Sprite2D
{
    public const int RowAttack = 4;
    public const int RowCrouch = 5;
    public const int RowMisc = 6;

    private static Shader? _flashShader;
    private ShaderMaterial _material = null!;

    public CharacterSprite(Texture2D sheet)
    {
        Texture = sheet;
        Hframes = 4;
        Vframes = 7;
        Offset = new Vector2(0, -8);
        TextureFilter = TextureFilterEnum.Nearest;
    }

    public CharacterSprite() { }

    public override void _Ready()
    {
        _flashShader ??= GD.Load<Shader>("res://shaders/flash.gdshader");
        _material = new ShaderMaterial { Shader = _flashShader };
        Material = _material;
    }

    /// <summary>The sheet column for a direction: 0 down, 1 up, 2 left, 3 right.</summary>
    public static int Column(Vector2 dir)
    {
        if (dir == Vector2.Zero) return 0;
        if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y) * 1.05f) return dir.X < 0 ? 2 : 3;
        return dir.Y < 0 ? 1 : 0;
    }

    public void Pose(int column, int row) => Frame = Mathf.Clamp(row, 0, 6) * 4 + Mathf.Clamp(column, 0, 3);

    public void SetFlash(float amount, Color color)
    {
        _material.SetShaderParameter("flash", amount);
        _material.SetShaderParameter("flash_color", color);
    }
}
