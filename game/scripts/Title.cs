using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Director;

namespace SwordInTheForest;

/// <summary>
/// Title screen: attack (left click, J, X/RB), interact or accept starts the test arena (M1).
/// Left/right picks the challenge and up/down turns adaptive timing on or off (keys or d-pad).
/// Uses _Input, not _UnhandledInput: the full-screen controls consume mouse clicks in the GUI
/// pass, which is why left click did nothing in M1.
/// </summary>
public partial class Title : Control
{
    private Label _challenge = null!;
    private Label _adaptive = null!;

    /// <summary>Scenarios turn this off to observe the start without leaving the scene.</summary>
    public bool ChangeScene { get; set; } = true;
    public bool StartRequested { get; private set; }

    public override void _Ready()
    {
        var stack = GetNode<VBoxContainer>("Center/Stack");
        var settings = new VBoxContainer();
        settings.AddThemeConstantOverride("separation", 0);
        stack.AddChild(settings);
        _challenge = SettingLabel(settings);
        _adaptive = SettingLabel(settings);
        var hint = SettingLabel(settings);
        hint.Text = "ui.title.settings_hint";
        hint.AutoTranslateMode = AutoTranslateModeEnum.Always;
        hint.Modulate = new Color(1, 1, 1, 0.45f);
        Refresh();
    }

    public override void _Input(InputEvent e)
    {
        if (StartRequested) return;
        if (ChangeSetting(e))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!(e.IsActionPressed("attack") || e.IsActionPressed("interact") || e.IsActionPressed("ui_accept"))) return;
        GetViewport().SetInputAsHandled();
        StartRequested = true;
        if (ChangeScene) GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/Arena.tscn");
    }

    private bool ChangeSetting(InputEvent e)
    {
        var d = DirectorService.Instance;
        if (d == null) return false;
        var step = e.IsActionPressed("ui_right") ? 1 : e.IsActionPressed("ui_left") ? -1 : 0;
        if (step != 0)
        {
            d.SetChallenge((Challenge)Mathf.PosMod((int)d.Leads.Challenge + step, 3));
        }
        else if (e.IsActionPressed("ui_up") || e.IsActionPressed("ui_down"))
        {
            d.SetAdaptive(!d.Leads.Adaptive);
        }
        else
        {
            return false;
        }
        Refresh();
        return true;
    }

    private void Refresh()
    {
        var d = DirectorService.Instance;
        _challenge.Visible = _adaptive.Visible = d != null;
        if (d == null) return;
        var challenge = d.Leads.Challenge switch
        {
            Challenge.Relaxed => "ui.settings.relaxed",
            Challenge.Hard => "ui.settings.hard",
            _ => "ui.settings.standard",
        };
        _challenge.Text = Tr("ui.title.challenge").Replace("{value}", Tr(challenge));
        _adaptive.Text = Tr("ui.title.adaptive").Replace("{value}", Tr(d.Leads.Adaptive ? "ui.settings.on" : "ui.settings.off"));
    }

    private static Label SettingLabel(Node parent)
    {
        var l = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutoTranslateMode = AutoTranslateModeEnum.Disabled,
            Modulate = new Color(1, 1, 1, 0.7f),
        };
        parent.AddChild(l);
        return l;
    }
}
