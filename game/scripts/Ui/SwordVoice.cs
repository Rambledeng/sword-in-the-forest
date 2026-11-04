using System.Collections.Generic;
using Godot;
using SwordInTheForest.Core;

namespace SwordInTheForest.Ui;

/// <summary>
/// The sword's text box: short lines that never pause play. Lines queue and show one at a time
/// for <c>RunTuning.SwordLineMs</c>. Placeholders like {ms} are filled after translation.
/// </summary>
public partial class SwordVoice : CanvasLayer
{
    private const int MaxQueued = 3;
    private readonly Queue<(string Key, Dictionary<string, string>? Args)> _queue = new();
    private PanelContainer _panel = null!;
    private Label _text = null!;
    private double _shownMs = -1;

    public static SwordVoice? Instance { get; private set; }

    /// <summary>Every line spoken this run, in order (scenarios check these).</summary>
    public List<string> Spoken { get; } = new();
    public string? Current { get; private set; }

    public override void _EnterTree() => Instance = this;

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready()
    {
        Layer = 11;
        _panel = new PanelContainer { Visible = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(0.04f, 0.05f, 0.06f, 0.82f),
            BorderColor = new Color(0.75f, 0.68f, 0.45f, 0.7f),
            BorderWidthTop = 1, BorderWidthBottom = 1, BorderWidthLeft = 1, BorderWidthRight = 1,
            ContentMarginLeft = 6, ContentMarginRight = 6, ContentMarginTop = 2, ContentMarginBottom = 3,
        });
        _panel.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        AddChild(_panel);
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 5);
        _panel.AddChild(row);
        var icon = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://assets/third_party/ninja_adventure/weapons/katana_in_hand.png"),
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            Modulate = new Color(1f, 0.92f, 0.7f),
        };
        row.AddChild(icon);
        _text = new Label
        {
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(250, 0),
            Modulate = new Color(1f, 0.96f, 0.85f),
        };
        row.AddChild(_text);
    }

    /// <summary>Queues a line by key (sword.*). Returns false if it was dropped (queue full).</summary>
    public static bool Say(string key, Dictionary<string, string>? args = null) => Instance?.Enqueue(key, args) ?? false;

    private bool Enqueue(string key, Dictionary<string, string>? args)
    {
        Spoken.Add(key);
        if (_queue.Count >= MaxQueued) return false;
        _queue.Enqueue((key, args));
        if (Current == null) ShowNext();
        return true;
    }

    private void ShowNext()
    {
        if (_queue.Count == 0)
        {
            Current = null;
            _panel.Visible = false;
            return;
        }
        var (key, args) = _queue.Dequeue();
        var text = Tr(key);
        if (args != null) foreach (var kv in args) text = text.Replace("{" + kv.Key + "}", kv.Value);
        Current = key;
        _text.Text = text;
        _panel.Visible = true;
        _shownMs = 0;
        Sfx.Play("sword_voice", 0.08f, 0.35f);
        CallDeferred(MethodName.Place);
    }

    private void Place()
    {
        _panel.ResetSize();
        var vp = GetViewport().GetVisibleRect().Size;
        _panel.Position = new Vector2(Mathf.Round((vp.X - _panel.Size.X) / 2), vp.Y - _panel.Size.Y - 22);
    }

    public override void _Process(double delta)
    {
        if (Current == null) return;
        _shownMs += delta * 1000.0;
        // A waiting line shortens the current one.
        var limit = Tunables.Run.SwordLineMs * (_queue.Count > 0 ? 0.6 : 1.0);
        if (_shownMs >= limit) ShowNext();
    }
}
