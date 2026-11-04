using System;
using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Director;

namespace SwordInTheForest.Ui;

/// <summary>
/// Settings, from the title and the pause menu: language (switches at once), master, music and
/// effects volume, challenge, adaptive timing and (after the first ending) Detail mode. Saved in settings.cfg. Mouse, keys or pad:
/// up/down moves, left/right changes, Esc or Back closes.
/// </summary>
public partial class SettingsPanel : CanvasLayer
{
    private Control _root = null!;
    private Button _language = null!;
    private Button _challenge = null!;
    private Button _adaptive = null!;
    private Button _detail = null!;
    private Label _detailLabel = null!;
    private HSlider _master = null!;
    private Button _back = null!;

    public bool IsOpen => _root.Visible;
    /// <summary>The Detail mode row (hidden until the first ending).</summary>
    public Button DetailButton => _detail;
    public event Action? Closed;

    public override void _Ready()
    {
        Layer = 60;
        ProcessMode = ProcessModeEnum.Always;
        _root = new Control { Visible = false };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.7f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(dim);
        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(center);
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.09f, 0.08f, 0.95f), BorderColor = new Color(0.55f, 0.45f, 0.3f),
            BorderWidthBottom = 1, BorderWidthTop = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 6,
        });
        center.AddChild(panel);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 4);
        panel.AddChild(stack);
        stack.AddChild(new Label { Text = "ui.settings.title", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1f, 0.9f, 0.6f) });
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 10);
        grid.AddThemeConstantOverride("v_separation", 2);
        stack.AddChild(grid);

        _language = Cycle(grid, "ui.settings.language", UserSettings.CycleLocale);
        _master = Slider(grid, "ui.settings.master", UserSettings.Master, v => UserSettings.SetVolumes(master: v));
        Slider(grid, "ui.settings.music", UserSettings.Music, v => UserSettings.SetVolumes(music: v));
        Slider(grid, "ui.settings.effects", UserSettings.Effects, v =>
        {
            UserSettings.SetVolumes(effects: v);
            Sfx.Play("ui_move", 0.02f, 0.8f);
        });
        _challenge = Cycle(grid, "ui.settings.challenge", step =>
        {
            var d = DirectorService.Instance;
            d?.SetChallenge((Challenge)Mathf.PosMod((int)d.Leads.Challenge + step, 3));
        });
        _adaptive = Cycle(grid, "ui.settings.adaptive", _ =>
        {
            var d = DirectorService.Instance;
            d?.SetAdaptive(!d.Leads.Adaptive);
        });
        _detail = Cycle(grid, "ui.settings.detail", _ => UserSettings.SetDetailMode(!UserSettings.DetailMode));
        _detailLabel = grid.GetChild<Label>(grid.GetChildCount() - 2);
        _back = new Button { Text = "ui.settings.back" };
        _back.Pressed += Close;
        stack.AddChild(_back);
        Refresh();
    }

    private static void Row(GridContainer grid, string key, Control control)
    {
        grid.AddChild(new Label { Text = key, VerticalAlignment = VerticalAlignment.Center });
        control.CustomMinimumSize = new Vector2(124, 0);
        grid.AddChild(control);
    }

    /// <summary>A value that cycles: click or attack for the next, left/right to step.</summary>
    private Button Cycle(GridContainer grid, string key, Action<int> change)
    {
        var b = new Button { AutoTranslateMode = AutoTranslateModeEnum.Disabled };
        b.Pressed += () => { change(1); Refresh(); };
        b.GuiInput += e =>
        {
            var step = e.IsActionPressed("ui_right") ? 1 : e.IsActionPressed("ui_left") ? -1 : 0;
            if (step == 0) return;
            change(step);
            Refresh();
            b.AcceptEvent();
        };
        Row(grid, key, b);
        return b;
    }

    private static HSlider Slider(GridContainer grid, string key, float value, Action<float> changed)
    {
        var s = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.05, Value = value, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        s.ValueChanged += v => changed((float)v);
        Row(grid, key, s);
        return s;
    }

    private void Refresh()
    {
        _language.Text = $"< {UserSettings.LocaleName(UserSettings.Locale)} >";
        // Detail mode appears once the first ending is reached.
        _detail.Visible = _detailLabel.Visible = UserSettings.EndingSeen;
        _detail.Text = $"< {Tr(UserSettings.DetailMode ? "ui.settings.on" : "ui.settings.off")} >";
        var d = DirectorService.Instance;
        _challenge.Disabled = _adaptive.Disabled = d == null;
        if (d == null) return;
        var challenge = d.Leads.Challenge switch
        {
            Challenge.Relaxed => "ui.settings.relaxed",
            Challenge.Hard => "ui.settings.hard",
            _ => "ui.settings.standard",
        };
        _challenge.Text = $"< {Tr(challenge)} >";
        _adaptive.Text = $"< {Tr(d.Leads.Adaptive ? "ui.settings.on" : "ui.settings.off")} >";
    }

    public void Open()
    {
        Refresh();
        _root.Visible = true;
        _language.GrabFocus();
    }

    public void Close()
    {
        if (!IsOpen) return;
        _root.Visible = false;
        Closed?.Invoke();
    }

    public override void _Input(InputEvent e)
    {
        if (!IsOpen) return;
        if (e.IsActionPressed("pause") || e.IsActionPressed("ui_cancel"))
        {
            GetViewport().SetInputAsHandled();
            Close();
            return;
        }
        // Attack (J, pad X) presses the focused button, like accept.
        if (e.IsActionPressed("attack") && e is not InputEventMouseButton && GetViewport().GuiGetFocusOwner() is Button b)
        {
            GetViewport().SetInputAsHandled();
            b.EmitSignal(BaseButton.SignalName.Pressed);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && _language != null) Refresh();
    }
}
