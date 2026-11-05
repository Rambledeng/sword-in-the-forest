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
    private Label _attunement = null!;
    private Control _canvas = null!;

    private Label _prompt = null!;
    private Label _area = null!;
    private HBoxContainer _perk = null!;
    private double _perkUntilMs;
    /// <summary>The starter-perk line under the area title is on screen (tests).</summary>
    public bool PerkShown => _perk.Visible;
    public string PerkLine { get; private set; } = "";
    private Label _bossName = null!;
    private double _areaUntilMs = -1;

    public Player? Player { get; set; }
    /// <summary>The top-right hint (arena: F1/F2/Esc; run: Esc pause).</summary>
    public string HintKey { get; set; } = "ui.arena.hint";
    /// <summary>Shows the boss's name and health bar at the top while set.</summary>
    public Enemy? Boss { get; set; }
    /// <summary>The attunement meter is on screen (Detail mode only).</summary>
    public bool AttunementShown => _attunement.Visible;

    /// <summary>瞬步, once taken: its icon beside the gourd, dim with a filling line while it recharges.</summary>
    private void DrawFlashStep()
    {
        if (!Player!.Mods.FlashStep) return;
        var at = new Vector2(42, 22);
        var charge = Player.FlashStepCharge;
        _canvas.DrawTexture(PixelIcons.Drawn("flash_step"), at, charge >= 1f ? Colors.White : new Color(1, 1, 1, 0.35f));
        if (charge >= 1f) return;
        _canvas.DrawRect(new Rect2(at + new Vector2(1, 15), new Vector2(14, 2)), new Color(0, 0, 0, 0.6f));
        _canvas.DrawRect(new Rect2(at + new Vector2(1, 15), new Vector2(Mathf.Round(14 * charge), 2)), new Color(0.6f, 0.85f, 1f));
    }

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
        _hint = new Label { Text = HintKey, Modulate = new Color(1, 1, 1, 0.55f) };
        _hint.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        _hint.GrowHorizontal = Control.GrowDirection.Begin;
        _hint.Position = new Vector2(-4, 2);
        _canvas.AddChild(_hint);
        _attunement = new Label { Text = "ui.hud.attunement", Position = new Vector2(4, 38), Modulate = new Color(1, 1, 1, 0.8f) };
        _canvas.AddChild(_attunement);
        _fallen = new Label { Text = "ui.arena.fallen", HorizontalAlignment = HorizontalAlignment.Center, Visible = false };
        _fallen.SetAnchorsPreset(Control.LayoutPreset.Center);
        _fallen.GrowHorizontal = Control.GrowDirection.Both;
        _fallen.GrowVertical = Control.GrowDirection.Both;
        _canvas.AddChild(_fallen);
        _prompt = new Label { Text = "ui.hud.spare_prompt", HorizontalAlignment = HorizontalAlignment.Center, Visible = false, Modulate = new Color(1f, 0.95f, 0.75f) };
        _prompt.GrowHorizontal = Control.GrowDirection.Both;
        _prompt.Position = new Vector2(0, -36);
        _canvas.AddChild(_prompt);
        _area = new Label { HorizontalAlignment = HorizontalAlignment.Center, Visible = false, AutoTranslateMode = AutoTranslateModeEnum.Disabled };
        _area.AddThemeFontSizeOverride("font_size", 24);
        _area.GrowHorizontal = Control.GrowDirection.Both;
        _area.Position = new Vector2(0, 44);
        _canvas.AddChild(_area);
        _perk = new HBoxContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _perk.AddThemeConstantOverride("separation", 4);
        _perk.AddChild(new TextureRect { StretchMode = TextureRect.StretchModeEnum.KeepCentered, MouseFilter = Control.MouseFilterEnum.Ignore });
        _perk.AddChild(new Label { AutoTranslateMode = AutoTranslateModeEnum.Disabled, VerticalAlignment = VerticalAlignment.Center });
        _canvas.AddChild(_perk);
        _bossName = new Label { Text = "combat.boss.name", HorizontalAlignment = HorizontalAlignment.Center, Visible = false, Modulate = new Color(1f, 0.85f, 0.8f) };
        _bossName.GrowHorizontal = Control.GrowDirection.Both;
        _bossName.AddThemeConstantOverride("outline_size", 3);
        _bossName.AddThemeColorOverride("font_outline_color", new Color(0.08f, 0.04f, 0.03f));
        _bossName.Position = new Vector2(0, 2);
        _canvas.AddChild(_bossName);
    }

    /// <summary>Shows an area's name large for a moment (key, e.g. ui.area.bamboo).</summary>
    public void ShowAreaTitle(string key)
    {
        _area.Text = Tr(key);
        _area.Visible = true;
        _areaUntilMs = Time.GetTicksMsec() + (double)Tunables.Run.AreaTitleMs;
    }

    /// <summary>A short line with an icon under the area title: the new soul's starter perk.</summary>
    public void ShowPerk(Texture2D icon, string text)
    {
        ((TextureRect)_perk.GetChild(0)).Texture = icon;
        ((Label)_perk.GetChild(1)).Text = text;
        PerkLine = text;
        _perk.Visible = true;
        _perk.ResetSize();
        _perkUntilMs = Time.GetTicksMsec() + (double)Tunables.Run.AreaTitleMs;
    }

    public override void _Process(double delta)
    {
        if (Player == null || !IsInstanceValid(Player)) return;
        _sips.Text = Tr("ui.hud.gourd_sips").Replace("{sips}", Player.Sips.ToString());
        _fallen.Visible = Player.IsDead;
        _hint.Text = HintKey;
        var bossShown = Boss != null && IsInstanceValid(Boss) && !Boss.IsGone;
        _hint.Visible = !bossShown;
        var w = _canvas.Size.X;
        _bossName.Position = new Vector2(Mathf.Round((w - _bossName.Size.X) / 2), 2);
        _area.Position = new Vector2(Mathf.Round((w - _area.Size.X) / 2), 44);
        _prompt.Position = new Vector2(Mathf.Round((w - _prompt.Size.X) / 2), _canvas.Size.Y - 70);
        _prompt.Visible = !Player.IsDead && Player.SpareCandidate() != null;
        _perk.Position = new Vector2(Mathf.Round((w - _perk.Size.X) / 2), 76);
        if (_perk.Visible)
        {
            var left = _perkUntilMs - Time.GetTicksMsec();
            _perk.Visible = left > 0;
            _perk.Modulate = new Color(1f, 0.95f, 0.85f, Mathf.Clamp((float)left / 600f, 0f, 1f));
        }
        if (_area.Visible)
        {
            var left = _areaUntilMs - Time.GetTicksMsec();
            _area.Visible = left > 0;
            _area.Modulate = new Color(1f, 0.93f, 0.75f, Mathf.Clamp((float)left / 600f, 0f, 1f));
        }
        _bossName.Visible = bossShown;
        _hint.Position = new Vector2(_canvas.Size.X - _hint.Size.X - 4, 2);
        _canvas.QueueRedraw();
    }

    private void DrawHud()
    {
        if (Player == null || !IsInstanceValid(Player)) return;
        var max = Player.MaxHealth;
        var hearts = (max + 1) / 2;
        for (var i = 0; i < hearts; i++)
        {
            var inHeart = Mathf.Clamp(Player.Health - 2 * i, 0, 2);
            var frame = inHeart * 2; // sheet: empty, ¼, ½, ¾, full
            _canvas.DrawTextureRectRegion(_heart, new Rect2(4 + i * 15, 4, 16, 16), new Rect2(frame * 16, 0, 16, 16));
        }
        _canvas.DrawTexture(_gourd, new Vector2(6, 24), Player.Sips > 0 ? Colors.White : new Color(1, 1, 1, 0.35f));
        DrawFlashStep();
        DrawAttunement();

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
        DrawBoss(size);
    }

    private void DrawBoss(Vector2 size)
    {
        if (Boss == null || !IsInstanceValid(Boss) || Boss.IsGone) return;
        // A fixed frame; the fills scale inside it and never leave it.
        const float w = 200;
        var x0 = Mathf.Round(size.X / 2 - w / 2);
        var y = Mathf.Round(_bossName.Position.Y + _bossName.Size.Y + 1);
        var frame = new Rect2(x0, y, w, 7);
        var inner = frame.Grow(-1);
        _canvas.DrawRect(frame, new Color(0.05f, 0.05f, 0.07f, 0.85f));
        var hp = Mathf.Clamp(Boss.Health / (float)Mathf.Max(1.0, Boss.MaxHealth), 0f, 1f);
        var fill = Mathf.Round(inner.Size.X * hp);
        if (fill > 0) _canvas.DrawRect(new Rect2(inner.Position, new Vector2(fill, 3)), new Color(0.85f, 0.2f, 0.18f));
        var posture = Mathf.Clamp((float)Boss.Posture.Fraction, 0f, 1f);
        var half = Mathf.Round(inner.Size.X * posture / 2);
        if (half > 0) _canvas.DrawRect(new Rect2(inner.Position.X + inner.Size.X / 2 - half, inner.Position.Y + 4, half * 2, 1), new Color(1f, 0.78f, 0.25f));
        // Phase marker at half health, inside the frame.
        var mark = inner.Position.X + Mathf.Round(inner.Size.X * Mathf.Clamp(Tunables.Run.BossPhase2At, 0f, 1f));
        _canvas.DrawRect(new Rect2(mark, inner.Position.Y, 1, 3), new Color(1, 1, 1, 0.6f));
        _canvas.DrawRect(frame, new Color(0.6f, 0.45f, 0.4f, 0.8f), false, 1f);
    }

    /// <summary>The attunement meter (人剑合一): how sure the sword is about the player's reaction time (Detail mode only).</summary>
    private void DrawAttunement()
    {
        var director = DirectorService.Instance;
        // Hidden until the first ending, then shown in Detail mode.
        _attunement.Visible = director != null && UserSettings.ShowDetails;
        if (!_attunement.Visible) return;
        var a = (float)director.Director.Estimate().Attunement;
        const float w = 44;
        var y = _attunement.Position.Y + _attunement.Size.Y + 1;
        _canvas.DrawRect(new Rect2(4, y, w + 2, 5), new Color(0.05f, 0.05f, 0.07f, 0.8f));
        var fill = Mathf.Round(w * a);
        if (fill > 0) _canvas.DrawRect(new Rect2(5, y + 1, fill, 3), new Color(0.55f, 0.85f, 1f).Lerp(new Color(1f, 0.95f, 0.7f), a));
        var off = director.Leads.Adaptive ? 1f : 0.4f;
        _canvas.DrawRect(new Rect2(4, y, w + 2, 5), new Color(0.6f, 0.7f, 0.75f, 0.6f * off), false);
    }
}
