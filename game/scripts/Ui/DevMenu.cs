using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Ui;

/// <summary>
/// The developer menu (F3), English only like F1 and F2: god mode, jump to any room or straight
/// to the boss, and force the next posture break to beg (genuine or fake). The game pauses while open.
/// </summary>
public partial class DevMenu : CanvasLayer
{
    private readonly RunScene _run;
    private PanelContainer _panel = null!;
    private CheckBox _god = null!;
    private Label _status = null!;

    public DevMenu(RunScene run) => _run = run;
    public DevMenu() => _run = null!;

    public bool IsOpen => _panel.Visible;

    public override void _Ready()
    {
        Layer = 101;
        ProcessMode = ProcessModeEnum.Always;
        _panel = new PanelContainer { Visible = false, AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled };
        _panel.SetAnchorsPreset(Control.LayoutPreset.LeftWide);
        _panel.OffsetRight = 170;
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0.06f, 0.07f, 0.08f, 0.92f), ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2 });
        AddChild(_panel);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        _panel.AddChild(scroll);
        var root = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        root.AddThemeConstantOverride("separation", 1);
        scroll.AddChild(root);

        root.AddChild(Dev("Developer (F3)", new Color(1f, 0.85f, 0.5f)));
        _god = new CheckBox { Text = "God mode", AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled };
        _god.Toggled += v => _run.Player.Invulnerable = v;
        root.AddChild(_god);
        root.AddChild(Button("Next break: genuine beg", () => Force(BegKind.Genuine)));
        root.AddChild(Button("Next break: fake beg", () => Force(BegKind.Fake)));
        root.AddChild(Button("Clear room", ClearRoom));
        root.AddChild(Button("Die", () => { Toggle(false); _run.Player.Kill(); }));
        _status = Dev("", new Color(0.7f, 0.9f, 0.7f));
        root.AddChild(_status);

        root.AddChild(Dev("Jump to", new Color(1f, 0.75f, 0.4f)));
        root.AddChild(Button("Boss", () => Jump(_run.BossStep)));
        for (var i = 0; i < _run.Plan.Steps.Count; i++)
        {
            var step = _run.Plan.Steps[i];
            var index = i;
            var label = step.Room != null ? $"{i}  {step.Room.Template.Id}" : $"{i}  upgrade";
            root.AddChild(Button(label, () => Jump(index)));
        }
    }

    private static Label Dev(string text, Color? color = null)
    {
        var l = new Label { Text = text, AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled };
        if (color != null) l.Modulate = color.Value;
        return l;
    }

    private static Button Button(string text, System.Action pressed)
    {
        var b = new Button { Text = text, AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled, Alignment = HorizontalAlignment.Left };
        b.Pressed += pressed;
        return b;
    }

    public void Force(BegKind kind)
    {
        _run.Mercy.ForceNext = kind;
        _status.Text = $"Next break begs ({kind.ToString().ToLowerInvariant()})";
    }

    public void Jump(int step)
    {
        Toggle(false);
        _run.JumpTo(step);
    }

    private void ClearRoom()
    {
        var room = _run.Room;
        if (room == null) return;
        foreach (var e in room.Alive.ToList().Where(IsInstanceValid)) e.Kill();
        Toggle(false);
    }

    public void Toggle(bool open)
    {
        if (open && (_run.Summary != null || _run.Upgrades != null || _run.Pause.IsOpen || _run.Tuning.IsOpen)) return;
        _panel.Visible = open;
        _god.SetPressedNoSignal(_run.Player.Invulnerable);
        GetTree().Paused = open;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!DevTools.Enabled || !e.IsActionPressed("debug_dev")) return;
        Toggle(!IsOpen);
        GetViewport().SetInputAsHandled();
    }
}
