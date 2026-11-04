using Godot;
using SwordInTheForest.Run;

namespace SwordInTheForest.Screens;

/// <summary>
/// Screenshot staging for menus: the title with settings open, the pause menu (with or without
/// settings), and the summary after a death or a win, each over a seeded run.
/// </summary>
public partial class MenuView : Node
{
    [Export] public string Mode { get; set; } = "pause";

    private RunScene? _run;
    private int _frame;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        if (Mode == "title_settings")
        {
            var title = GD.Load<PackedScene>("res://scenes/Title.tscn").Instantiate<Title>();
            title.ChangeScene = false;
            AddChild(title);
            title.CallDeferred(Title.MethodName.OpenSettingsForShot);
            return;
        }
        RunScene.NextArea = null;
        _run = new RunScene { Seed = 7, StartStep = Mode.StartsWith("summary") ? 5 : 1, PlayMusic = false, ChangeScenes = false };
        AddChild(_run);
    }

    public override void _Process(double delta)
    {
        if (_run == null || ++_frame != 20) return;
        switch (Mode)
        {
            case "pause":
                _run.Pause.Toggle(true);
                break;
            case "pause_settings":
                _run.Pause.Toggle(true);
                _run.Pause.Settings.Open();
                break;
            case "summary_loss":
                _run.Apply(Upgrade.Vitality);
                _run.Stats.Take(Upgrade.Vitality);
                _run.Stats.Finished(begging: false);
                _run.Stats.Spare();
                _run.Player.Kill();
                break;
            case "summary_win":
                _run.Stats.Finished(begging: true);
                _run.Stats.Finished(begging: false);
                _run.Stats.Spare();
                _run.Stats.Take(Upgrade.IronDeflect);
                _run.Stats.Take(Upgrade.DeepGourd);
                _run.EndForShot(true);
                break;
        }
    }
}
