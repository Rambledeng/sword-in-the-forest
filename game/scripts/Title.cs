using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Director;
using SwordInTheForest.Intro;
using SwordInTheForest.Run;
using SwordInTheForest.Ui;

namespace SwordInTheForest;

/// <summary>
/// Title screen: a close-up of the sword in the grove (<see cref="TitleArt"/>) with a menu column:
/// Start, the language picker and Settings. Attack (J, pad X), interact or accept presses the
/// focused button (Start at first); a left click on the art also starts. Starting plays the
/// sequence: the sword hums, the seal falls, a hand grips the hilt, the view pulls back, and the
/// run opens on the intro (<see cref="RunScene.PlayIntro"/>).
/// Uses _Input, not _UnhandledInput: the full-screen controls consume mouse clicks in the GUI pass.
/// </summary>
public partial class Title : Control
{
    private const float MenuWidth = 236;
    private Label _challenge = null!;
    private TitleArt _art = null!;
    private Control _menu = null!;
    private ColorRect _shade = null!;
    private Button _start = null!;
    private Button _language = null!;
    private Button _settingsButton = null!;
    private SettingsPanel _settings = null!;
    private double _seqT = -1;
    private bool _left;

    /// <summary>Seconds of the start sequence: hum, seal, grip, pull back.</summary>
    private const double HumEnd = 0.9, SealStart = 0.6, SealEnd = 1.5, GripStart = 1.3, GripEnd = 2.3, PullEnd = 3.1;

    /// <summary>Scenarios turn this off to observe the start without leaving the scene.</summary>
    public bool ChangeScene { get; set; } = true;
    public bool StartRequested { get; private set; }
    /// <summary>Scenarios: the start sequence has finished.</summary>
    public bool SequenceDone => _left;
    public SettingsPanel Settings => _settings;
    public Button LanguageButton => _language;

    public override void _Ready()
    {
        _art = new TitleArt();
        AddChild(_art);
        MoveChild(_art, 1);
        // The menu sits in a column on the left; the sword fills the right.
        _shade = new ColorRect { Color = new Color(0.01f, 0.03f, 0.02f, 0.5f), Size = new Vector2(MenuWidth, 270), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_shade);
        _menu = GetNode<Control>("Center");
        _menu.SetAnchorsPreset(LayoutPreset.LeftWide);
        _menu.OffsetRight = MenuWidth;
        MoveChild(_menu, -1);
        var stack = GetNode<VBoxContainer>("Center/Stack");
        stack.AddThemeConstantOverride("separation", 6);
        var name = GetNode<Label>("Center/Stack/TitleLabel");
        name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        name.CustomMinimumSize = new Vector2(MenuWidth - 12, 0);
        GetNode<Label>("Center/Stack/PressStart").Visible = false;

        var buttons = new VBoxContainer();
        buttons.AddThemeConstantOverride("separation", 2);
        stack.AddChild(buttons);
        _start = MenuButton(buttons, "ui.title.start", Begin);
        _language = MenuButton(buttons, "", () => { UserSettings.CycleLocale(1); Refresh(); });
        _language.AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        _settingsButton = MenuButton(buttons, "ui.title.settings", OpenSettings);
        _challenge = new Label { HorizontalAlignment = HorizontalAlignment.Center, AutoTranslateMode = AutoTranslateModeEnum.Disabled, Modulate = new Color(1, 1, 1, 0.55f) };
        stack.AddChild(_challenge);

        _settings = new SettingsPanel();
        AddChild(_settings);
        _settings.Closed += () => { Refresh(); _settingsButton.GrabFocus(); };
        Refresh();
        _start.CallDeferred(Control.MethodName.GrabFocus);
        Sfx.Instance?.PlayMusic("res://assets/third_party/ninja_adventure/music/melancholia.ogg");
    }

    private static Button MenuButton(Container parent, string text, System.Action pressed)
    {
        var b = new Button { Text = text, CustomMinimumSize = new Vector2(150, 0), SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        b.Pressed += pressed;
        parent.AddChild(b);
        return b;
    }

    /// <summary>Screenshot stage.</summary>
    public void OpenSettingsForShot() => _settings.Open();

    private void OpenSettings()
    {
        if (StartRequested) return;
        _settings.Open();
    }

    public override void _Input(InputEvent e)
    {
        if (_settings.IsOpen) return;
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
        if (e is InputEventMouseButton mb)
        {
            // Clicks in the menu column go to its buttons; a click on the art starts.
            if (!mb.Pressed || mb.ButtonIndex != MouseButton.Left || mb.Position.X < MenuWidth) return;
            GetViewport().SetInputAsHandled();
            Begin();
            return;
        }
        var focused = GetViewport().GuiGetFocusOwner() as Button;
        if (focused == _language && (e.IsActionPressed("ui_left") || e.IsActionPressed("ui_right")))
        {
            GetViewport().SetInputAsHandled();
            UserSettings.CycleLocale(e.IsActionPressed("ui_right") ? 1 : -1);
            Refresh();
            return;
        }
        if (!(e.IsActionPressed("attack") || e.IsActionPressed("interact") || e.IsActionPressed("ui_accept"))) return;
        GetViewport().SetInputAsHandled();
        if (focused != null && focused != _start) focused.EmitSignal(BaseButton.SignalName.Pressed);
        else Begin();
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
        _seqT += delta;
        var t = _seqT;
        _art.Hum = (float)Mathf.Clamp(t / HumEnd, 0, 1);
        _art.SealFall = (float)Mathf.Clamp((t - SealStart) / (SealEnd - SealStart), 0, 1);
        _art.Grip = (float)Mathf.Clamp((t - GripStart) / (GripEnd - GripStart), 0, 1);
        _menu.Modulate = _shade.Modulate = new Color(1, 1, 1, (float)Mathf.Clamp(1 - t / 0.5, 0, 1));
        var pull = (float)Mathf.Clamp((t - GripEnd) / (PullEnd - GripEnd), 0, 1);
        var scale = Mathf.Lerp(1f, 0.3f, pull * pull);
        // Pull back around the sword, fading to black; the run fades in from there.
        _art.Scale = new Vector2(scale, scale);
        _art.Position = TitleArt.SwordBase - TitleArt.SwordBase * scale + (new Vector2(240, 150) - TitleArt.SwordBase) * pull;
        _art.Modulate = new Color(1, 1, 1, 1f - pull);
        if (t < PullEnd) return;
        _left = true;
        RunScene.PlayIntro = true;
        if (ChangeScene) GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/Run.tscn");
    }

    private void Refresh()
    {
        _language.Text = $"< {UserSettings.LocaleName(UserSettings.Locale)} >";
        var d = DirectorService.Instance;
        _challenge.Visible = d != null;
        if (d == null) return;
        var challenge = d.Leads.Challenge switch
        {
            Challenge.Relaxed => "ui.settings.relaxed",
            Challenge.Hard => "ui.settings.hard",
            _ => "ui.settings.standard",
        };
        _challenge.Text = Tr("ui.title.challenge").Replace("{value}", Tr(challenge)) + "\n" +
                          Tr("ui.title.adaptive").Replace("{value}", Tr(d.Leads.Adaptive ? "ui.settings.on" : "ui.settings.off"));
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && _language != null) Refresh();
    }
}
