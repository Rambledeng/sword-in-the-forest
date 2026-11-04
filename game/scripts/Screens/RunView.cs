using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Screens;

/// <summary>
/// Screenshot staging: a seeded run from <see cref="StartStep"/>, played by the invulnerable bot
/// (or standing still), so tools/screenshot.sh can capture rooms, enemies and screens.
/// </summary>
public partial class RunView : Node
{
    private readonly BotControls _controls = new();
    private RunScene _run = null!;
    private RunBot? _bot;

    [Export] public int Seed { get; set; } = 7;
    [Export] public int StartStep { get; set; }
    [Export] public bool Play { get; set; } = true;
    [Export] public bool ChooseUpgrades { get; set; } = true;
    /// <summary>Open the F2 overlay and let the director learn live (adaptive timing on, fresh belief).</summary>
    [Export] public bool ShowDirector { get; set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _run = new RunScene { Seed = Seed, StartStep = StartStep, PlayMusic = false, ChangeScenes = false, Controls = _controls, Invulnerable = true };
        if (ShowDirector)
        {
            DirectorService.Instance!.ResetBelief();
            DirectorService.Instance.Leads.Adaptive = true;
        }
        AddChild(_run);
        if (ShowDirector) _run.Overlay.Toggle();
        if (!Play) return;
        _bot = new RunBot(_run, _controls);
        _bot.Attach();
    }

    public override void _ExitTree() => _bot?.Detach();

    public override void _PhysicsProcess(double delta)
    {
        if (_bot == null || (!ChooseUpgrades && _run.Upgrades != null)) return;
        _bot.Step();
    }
}
