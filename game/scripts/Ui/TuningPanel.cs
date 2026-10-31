using System.Collections.Generic;
using Godot;
using SwordInTheForest.Core;

namespace SwordInTheForest.Ui;

/// <summary>
/// The live tuning panel (F1). Lists every number in res://tuning/*.tres; edits apply at once;
/// Save writes the files. Developer tool, so its labels stay in English. The game pauses while open.
/// </summary>
public partial class TuningPanel : CanvasLayer
{
    private PanelContainer _panel = null!;
    private Label _status = null!;
    private readonly HashSet<Resource> _dirty = new();

    public override void _Ready()
    {
        Layer = 100;
        ProcessMode = ProcessModeEnum.Always;
        // Developer tool: English only, never translated.
        _panel = new PanelContainer { Visible = false, AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled };
        _panel.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        _panel.OffsetLeft = -236;
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.06f, 0.07f, 0.08f, 0.92f), ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2 });
        _panel.Theme = CompactTheme();
        AddChild(_panel);

        var root = new VBoxContainer();
        _panel.AddChild(root);
        var header = new HBoxContainer();
        root.AddChild(header);
        header.AddChild(Dev("Tuning (F1)", new Color(1f, 0.85f, 0.5f)));
        header.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        var save = new Button { Text = "Save" };
        save.Pressed += Save;
        header.AddChild(save);
        var close = new Button { Text = "Close" };
        close.Pressed += Toggle;
        header.AddChild(close);
        _status = Dev("", new Color(0.7f, 0.9f, 0.7f));
        root.AddChild(_status);

        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        root.AddChild(scroll);
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(list);
        foreach (var res in Tunables.All()) AddSection(list, res);
    }

    private static Label Dev(string text, Color? color = null)
    {
        var l = new Label { Text = text, AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled };
        if (color != null) l.Modulate = color.Value;
        return l;
    }

    private void AddSection(VBoxContainer list, Resource res)
    {
        list.AddChild(Dev(res.ResourcePath.GetFile(), new Color(1f, 0.75f, 0.4f)));
        var grid = new GridContainer { Columns = 2 };
        list.AddChild(grid);
        foreach (var p in res.GetPropertyList())
        {
            var usage = (PropertyUsageFlags)(int)p["usage"];
            var name = (string)p["name"];
            if ((usage & PropertyUsageFlags.Category) != 0) continue;
            if ((usage & PropertyUsageFlags.Group) != 0)
            {
                if (name == "Resource") continue; // Godot's own resource_* group

                grid.AddChild(Dev(name, new Color(0.6f, 0.75f, 0.9f)));
                grid.AddChild(new Control());
                continue;
            }
            if ((usage & PropertyUsageFlags.ScriptVariable) == 0) continue;
            var type = (Variant.Type)(int)p["type"];
            Control? editor = type switch
            {
                Variant.Type.Int or Variant.Type.Float => NumberEditor(res, name, type, (string)p["hint_string"]),
                Variant.Type.Bool => BoolEditor(res, name),
                _ => null,
            };
            if (editor == null) continue;
            grid.AddChild(Dev(name));
            grid.AddChild(editor);
        }
    }

    /// <summary>Small line edits and buttons so many rows fit at 480×270.</summary>
    private static Theme CompactTheme()
    {
        var theme = new Theme();
        StyleBoxFlat Box(Color bg) => new()
        {
            BgColor = bg, ContentMarginLeft = 3, ContentMarginRight = 1, ContentMarginTop = 0, ContentMarginBottom = 0,
        };
        foreach (var style in new[] { "normal", "read_only" }) theme.SetStylebox(style, "LineEdit", Box(new Color(0.14f, 0.15f, 0.17f)));
        theme.SetStylebox("focus", "LineEdit", Box(new Color(0.22f, 0.2f, 0.12f)));
        foreach (var style in new[] { "normal", "hover", "pressed", "focus" })
            theme.SetStylebox(style, "Button", Box(style == "normal" ? new Color(0.2f, 0.22f, 0.25f) : new Color(0.3f, 0.3f, 0.22f)));
        theme.SetConstant("v_separation", "GridContainer", 1);
        theme.SetConstant("separation", "VBoxContainer", 1);
        return theme;
    }

    private Control NumberEditor(Resource res, string name, Variant.Type type, string hint)
    {
        var box = new SpinBox { CustomMinimumSize = new Vector2(64, 0), AllowGreater = true, AllowLesser = true, SelectAllOnFocus = true };
        var parts = hint.Split(',');
        if (parts.Length >= 2 && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var min)
            && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var max))
        {
            box.MinValue = min;
            box.MaxValue = max;
        }
        box.Step = parts.Length >= 3 && double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var step) ? step : (type == Variant.Type.Int ? 1 : 0.01);
        box.Value = res.Get(name).AsDouble();
        box.ValueChanged += v =>
        {
            res.Set(name, type == Variant.Type.Int ? Variant.From((int)System.Math.Round(v)) : Variant.From((float)v));
            _dirty.Add(res);
            _status.Text = $"{_dirty.Count} file(s) changed, not saved";
        };
        return box;
    }

    private Control BoolEditor(Resource res, string name)
    {
        var box = new CheckBox { ButtonPressed = res.Get(name).AsBool() };
        box.Toggled += v =>
        {
            res.Set(name, v);
            _dirty.Add(res);
            _status.Text = $"{_dirty.Count} file(s) changed, not saved";
        };
        return box;
    }

    private void Save()
    {
        var saved = 0;
        foreach (var res in _dirty)
        {
            var err = ResourceSaver.Save(res, res.ResourcePath);
            if (err == Error.Ok) saved++;
            else GD.PushError($"Tuning: could not save {res.ResourcePath}: {err}");
        }
        _status.Text = saved == _dirty.Count ? $"Saved {saved} file(s)" : $"Saved {saved} of {_dirty.Count}; see the log";
        _dirty.Clear();
    }

    public void Toggle()
    {
        _panel.Visible = !_panel.Visible;
        GetTree().Paused = _panel.Visible;
    }

    public bool IsOpen => _panel.Visible;

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("debug_tuning"))
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }
}
