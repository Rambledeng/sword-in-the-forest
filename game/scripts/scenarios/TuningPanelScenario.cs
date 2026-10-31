using Godot;

namespace SwordInTheForest.Scenarios;

/// <summary>The F1 tuning panel opens, pauses the game, lists the tuning files, and closes again.</summary>
public partial class TuningPanelScenario : Scenario
{
    private Arena _arena = null!;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _arena = new Arena { AutoSpawn = true, WithUi = true, PlayMusic = false };
            AddChild(_arena);
            return;
        }
        if (frame == 5)
        {
            GD.Print("  opening");
            _arena.Tuning!.Toggle();
            GD.Print("  opened");
        }
        if (frame == 7) Check(_arena.Tuning!.IsOpen && GetTree().Paused, "panel should be open and the game paused");
        if (frame == 30) _arena.Tuning!.Toggle();
        if (frame == 32) Check(!GetTree().Paused, "closing the panel should unpause");
    }
}
