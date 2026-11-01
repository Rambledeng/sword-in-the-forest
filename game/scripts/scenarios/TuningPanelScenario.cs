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
        if (frame == 33)
        {
            // Live edits must survive garbage collection (Godot's resource cache is weak).
            var t = Core.Tunables.Player;
            var old = t.MoveSpeed;
            t.MoveSpeed = 123;
            t = null!;
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            Check(Core.Tunables.Player.MoveSpeed == 123, "an F1 edit was lost after garbage collection");
            Core.Tunables.Player.MoveSpeed = old;
        }
    }
}
