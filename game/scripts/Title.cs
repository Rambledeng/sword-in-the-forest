using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Director;
using SwordInTheForest.Intro;
using SwordInTheForest.Run;

namespace SwordInTheForest;

/// <summary>
/// Title screen: a close-up of the sword in the grove (<see cref="TitleArt"/>). Attack (left
/// click, J, X/RB), interact or accept starts: the sword hums, the seal falls, a hand grips the
/// hilt, the view pulls back, and the run opens on the intro (<see cref="RunScene.PlayIntro"/>).
/// Left/right picks the challenge and up/down turns adaptive timing on or off (keys or d-pad).
/// Uses _Input, not _UnhandledInput: the full-screen controls consume mouse clicks in the GUI
/// pass, which is why left click did nothing in M1.
/// </summary>
public partial class Title : Control
{
    private Label _challenge = null!;
    private Label _adaptive = null!;
    private TitleArt _art = null!;
    private Control _menu = null!;
    private double _seqT = -1;
    private bool _left;

    /// <summary>Seconds of the start sequence: hum, seal, grip, pull back.</summary>
    private const double HumEnd = 0.9, SealStart = 0.6, SealEnd = 1.5, GripStart = 1.3, GripEnd = 2.3, PullEnd = 3.1;

    /// <summary>Scenarios turn this off to observe the start without leaving the scene.</summary>
    public bool ChangeScene { get; set; } = true;
    public bool StartRequested { get; private set; }

    public override void _Ready()
    {
        _art = new TitleArt();
        AddChild(_art);
        MoveChild(_art, 1);
        // The menu sits in a column on the left; the sword fills the right.
        var shade = new ColorRect { Color = new Color(0.01f, 0.03f, 0.02f, 0.45f), Position = Vector2.Zero, Size = new Vector2(250, 270), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(shade);
        _menu = GetNode<Control>("Center");
        _menu.SetAnchorsPreset(LayoutPreset.LeftWide);
        _menu.OffsetRight = 250;
        MoveChild(_menu, -1);
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
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        hint.CustomMinimumSize = new Vector2(220, 0);
        var name = GetNode<Label>("Center/Stack/TitleLabel");
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        name.CustomMinimumSize = new Vector2(230, 0);
        Refresh();
    }

    public override void _Input(InputEvent e)
    {
        if (StartRequested)
        {
            // Pressing again skips to the pull back.
            if (_seqT >= 0 && _seqT < GripEnd && (e.IsActionPressed("attack") || e.IsActionPressed("ui_accept")))
            {
                _seqT = GripEnd;
                GetViewport().SetInputAsHandled();
            }
            return;
        }
        if (ChangeSetting(e))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!(e.IsActionPressed("attack") || e.IsActionPressed("interact") || e.IsActionPressed("ui_accept"))) return;
        GetViewport().SetInputAsHandled();
        Begin();
    }

    /// <summary>Starts the start sequence (also used by the screenshot stage).</summary>
    public void Begin()
    {
        if (StartRequested) return;
        StartRequested = true;
        _seqT = 0;
        Sfx.Play("sword_voice", 0f, 1f);
    }

    public override void _Process(double delta)
    {
        if (_seqT < 0 || _left) return;
        // A second press skips the rest.
        _seqT += delta;
        var t = _seqT;
        _art.Hum = (float)Mathf.Clamp(t / HumEnd, 0, 1);
        _art.SealFall = (float)Mathf.Clamp((t - SealStart) / (SealEnd - SealStart), 0, 1);
        _art.Grip = (float)Mathf.Clamp((t - GripStart) / (GripEnd - GripStart), 0, 1);
        _menu.Modulate = new Color(1, 1, 1, (float)Mathf.Clamp(1 - t / 0.5, 0, 1));
        shadeFade();
        void shadeFade() { foreach (var c in GetChildren()) if (c is ColorRect r && r.Name != "Background") r.Modulate = _menu.Modulate; }
        var pull = (float)Mathf.Clamp((t - GripEnd) / (PullEnd - GripEnd), 0, 1);
        var scale = Mathf.Lerp(1f, 0.3f, pull * pull);
        // Pull back around the sword, fading into the forest of the first room.
        _art.Scale = new Vector2(scale, scale);
        _art.Position = TitleArt.SwordBase - TitleArt.SwordBase * scale + (new Vector2(240, 150) - TitleArt.SwordBase) * pull;
        _art.Modulate = new Color(1, 1, 1, 1f - pull);
        if (t < PullEnd) return;
        _left = true;
        RunScene.PlayIntro = true;
        if (ChangeScene) GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/Run.tscn");
    }

    /// <summary>Scenarios: the start sequence has finished.</summary>
    public bool SequenceDone => _left;

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
