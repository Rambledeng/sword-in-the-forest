using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Director;
using SwordInTheForest.Run;

namespace SwordInTheForest.Ui;

/// <summary>
/// The run summary: win or loss, the sword's closing verdict, merit, karma, kills, souls fallen,
/// and what the sword learned. Until the first ending (and afterwards unless Detail mode is on)
/// merit, karma and the numbers stay hidden: the sword's reading is a word (quick, learning, slow).
/// New soul or title.
/// </summary>
public partial class SummaryScreen : CanvasLayer
{
    private readonly RunStats _stats;
    private readonly Estimate _estimate;
    private readonly bool _adaptive;
    private readonly int _soulsFallen;
    private readonly int _totalRooms;
    private readonly Area? _nextArea;
    private Button _retry = null!;

    public SummaryScreen(RunStats stats, Estimate estimate, bool adaptive, int soulsFallen, int totalRooms, Area? nextArea = null)
    {
        _nextArea = nextArea;
        _stats = stats;
        _estimate = estimate;
        _adaptive = adaptive;
        _soulsFallen = soulsFallen;
        _totalRooms = totalRooms;
    }

    public SummaryScreen() => _stats = new RunStats();

    /// <summary>This win was the first ending: say that Detail mode is now in the settings.</summary>
    public bool FirstEnding { get; set; }

    public event Action? RetryRequested;
    public event Action? TitleRequested;
    /// <summary>The rendered lines (scenarios read these).</summary>
    public List<string> Lines { get; } = new();

    public override void _Ready()
    {
        Layer = 25;
        ProcessMode = ProcessModeEnum.Always;
        var dim = new ColorRect { Color = new Color(0.02f, 0.03f, 0.03f, 0.88f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(dim);
        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 1);
        center.AddChild(stack);

        Add(stack, Tr(_stats.Won ? "summary.title.win" : "summary.title.loss"), new Color(1f, 0.9f, 0.6f));
        var verdict = Add(stack, Tr(_stats.VerdictKey), new Color(0.95f, 0.92f, 0.85f));
        verdict.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        verdict.CustomMinimumSize = new Vector2(330, 0);
        stack.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });

        var t = TimeSpan.FromMilliseconds(_stats.ElapsedMs);
        var details = UserSettings.ShowDetails;
        if (details) Add(stack, Fill("summary.merit_karma", ("merit", _stats.Merit), ("karma", _stats.Karma)));
        Add(stack, Fill("summary.kills_spared", ("kills", _stats.Kills), ("spared", _stats.Spared)));
        Add(stack, Fill("summary.souls", ("deaths", _soulsFallen)));
        Add(stack, Fill("summary.rooms", ("rooms", _stats.RoomsCleared), ("total", _totalRooms))
            + "   " + Tr("summary.time").Replace("{time}", $"{(int)t.TotalMinutes}:{t.Seconds:00}"));
        if (_stats.Perk is Run.StarterPerk perk)
            Add(stack, Tr("summary.perk").Replace("{perk}", PerkText.Name(perk)));
        if (_stats.Upgrades.Count > 0)
            Add(stack, Tr("summary.upgrades").Replace("{list}", string.Join(", ", _stats.Upgrades.Select(u => Tr($"upgrade.{UpgradeScreen.Key(u)}.name")))));
        stack.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });

        var e = _estimate;
        if (details)
        {
            Add(stack, Fill("summary.reading", ("median", (int)Math.Round(e.MedianMs)), ("low", (int)Math.Round(e.LowMs)), ("high", (int)Math.Round(e.HighMs))), new Color(0.7f, 0.9f, 1f));
            Add(stack, Fill("summary.attunement", ("pct", (int)Math.Round(e.Attunement * 100)), ("n", e.Observations)), new Color(0.7f, 0.9f, 1f));
        }
        else
        {
            Add(stack, Tr(ReadingKey(e)), new Color(0.7f, 0.9f, 1f));
        }
        if (!_adaptive) Add(stack, Tr("summary.adaptive_off"), new Color(1f, 0.7f, 0.5f));
        if (FirstEnding) Add(stack, Tr("summary.detail_unlocked"), new Color(1f, 0.85f, 0.6f));
        if (_nextArea is Area area)
        {
            stack.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
            Add(stack, Tr("summary.next_soul").Replace("{area}", Tr($"ui.area.{AreaKey(area)}")), new Color(1f, 0.85f, 0.6f));
        }
        stack.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });

        var buttons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        buttons.AddThemeConstantOverride("separation", 8);
        stack.AddChild(buttons);
        _retry = new Button { Text = "summary.retry" };
        _retry.Pressed += () => RetryRequested?.Invoke();
        buttons.AddChild(_retry);
        var title = new Button { Text = "summary.to_title" };
        title.Pressed += () => TitleRequested?.Invoke();
        buttons.AddChild(title);
        _retry.CallDeferred(Control.MethodName.GrabFocus);
    }

    /// <summary>The sword's reading in a word: unread, quick, learning or slow.</summary>
    public static string ReadingKey(Estimate e)
    {
        var r = Tunables.Run;
        if (e.Observations == 0 || e.Attunement < r.ReadingAttunedAt) return "summary.reading.unread";
        if (e.MedianMs <= r.ReadingQuickMs) return "summary.reading.quick";
        if (e.MedianMs >= r.ReadingSlowMs) return "summary.reading.slow";
        return "summary.reading.learning";
    }

    public static string AreaKey(Area area) => area switch
    {
        Area.Bamboo => "bamboo",
        Area.Outside => "outside",
        Area.Tavern => "tavern",
        _ => "tutorial",
    };

    private string Fill(string key, params (string Name, int Value)[] args)
    {
        var s = Tr(key);
        foreach (var (name, value) in args) s = s.Replace("{" + name + "}", value.ToString());
        return s;
    }

    /// <summary>Every line wraps at this width, so long lines in any language stay on the 480 px screen (layout, not tuning).</summary>
    private const float LineWidthPx = 440;

    private Label Add(Container parent, string text, Color? color = null)
    {
        var l = new Label
        {
            Text = text,
            AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled,
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = color ?? Colors.White,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(LineWidthPx, 0),
        };
        parent.AddChild(l);
        Lines.Add(text);
        return l;
    }

    public override void _Input(InputEvent e)
    {
        if (e.IsActionPressed("attack") && e is not InputEventMouseButton && _retry.HasFocus())
        {
            GetViewport().SetInputAsHandled();
            RetryRequested?.Invoke();
        }
    }
}
