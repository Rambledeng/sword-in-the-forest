using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;

namespace SwordInTheForest.Ui;

/// <summary>
/// Health hearts (2 health each), gourd sips, and the posture bar at the bottom centre, which grows
/// from the middle outward and turns red as it nears breaking.
/// </summary>
public partial class Hud : CanvasLayer
{
    private const string UiDir = "res://assets/third_party/ninja_adventure/ui";
    private Texture2D _heart = null!;
    private Texture2D _gourd = null!;
    private Label _sips = null!;
    private Label _fallen = null!;
    private Label _hint = null!;
    private Control _canvas = null!;

    public Player? Player { get; set; }

    public override void _Ready()
    {
        Layer = 10;
        _heart = GD.Load<Texture2D>($"{UiDir}/heart.png");
        _gourd = GD.Load<Texture2D>($"{UiDir}/gourd.png");
        _canvas = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        _canvas.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _canvas.Draw += DrawHud;
        AddChild(_canvas);

        _sips = new Label { Position = new Vector2(22, 24) };
        _canvas.AddChild(_sips);
        _hint = new Label { Text = "ui.arena.hint", Modulate = new Color(1, 1, 1, 0.55f) };
        _hint.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _hint.GrowHorizontal = Control.GrowDirection.Begin;
        _hint.Position = new Vector2(-4, 2);
        _canvas.AddChild(_hint);
        _fallen = new Label { Text = "ui.arena.fallen", HorizontalAlignment = HorizontalAlignment.Center, Visible = false };
        _fallen.SetAnchorsPreset(Control.LayoutPreset.Center);
        _fallen.GrowHorizontal = Control.GrowDirection.Both;
        _fallen.GrowVertical = Control.GrowDirection.Both;
        _canvas.AddChild(_fallen);
    }

    public override void _Process(double delta)
    {
        if (Player == null || !IsInstanceValid(Player)) return;
        _sips.Text = Tr("ui.hud.gourd_sips").Replace("{sips}", Player.Sips.ToString());
        _fallen.Visible = Player.IsDead;
        _hint.Position = new Vector2(_canvas.Size.X - _hint.Size.X - 4, 2);
        _canvas.QueueRedraw();
    }

    private void DrawHud()
    {
        if (Player == null || !IsInstanceValid(Player)) return;
        var max = Tunables.Player.MaxHealth;
        var hearts = (max + 1) / 2;
        for (var i = 0; i < hearts; i++)
        {
            var inHeart = Mathf.Clamp(Player.Health - 2 * i, 0, 2);
            var frame = inHeart * 2; // sheet: empty, ¼, ½, ¾, full
            _canvas.DrawTextureRectRegion(_heart, new Rect2(4 + i * 15, 4, 16, 16), new Rect2(frame * 16, 0, 16, 16));
        }
        _canvas.DrawTexture(_gourd, new Vector2(6, 24), Player.Sips > 0 ? Colors.White : new Color(1, 1, 1, 0.35f));

        // Posture bar.
        var size = _canvas.Size;
        const float w = 110;
        var y = size.Y - 12;
        var x0 = Mathf.Round(size.X / 2 - w / 2);
        var f = (float)Player.Posture.Fraction;
        var broken = Player.Current == Player.State.Broken;
        _canvas.DrawRect(new Rect2(x0 - 2, y - 2, w + 4, 7), new Color(0.05f, 0.05f, 0.07f, 0.8f));
        _canvas.DrawRect(new Rect2(x0 - 1, y - 1, w + 2, 5), new Color(0.35f, 0.3f, 0.25f, 0.9f), false);
        var half = Mathf.Round(w * f / 2);
        if (half > 0 || broken)
        {
            var c = broken
                ? ((int)(Time.GetTicksMsec() / 100) % 2 == 0 ? new Color(1f, 0.2f, 0.15f) : new Color(1f, 0.85f, 0.4f))
                : new Color(1f, 0.78f, 0.25f).Lerp(new Color(1f, 0.3f, 0.12f), f * f);
            if (broken) half = w / 2;
            _canvas.DrawRect(new Rect2(size.X / 2 - half, y, half * 2, 3), c);
            _canvas.DrawRect(new Rect2(size.X / 2 - half, y, half * 2, 1), new Color(1f, 1f, 0.85f, 0.5f));
        }
        _canvas.DrawRect(new Rect2(Mathf.Round(size.X / 2) - 0.5f, y - 2, 1, 7), new Color(1, 1, 1, 0.35f));
    }
}
