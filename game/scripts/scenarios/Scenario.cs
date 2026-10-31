using System;
using Godot;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Base for headless scenario tests. Runs <see cref="Frames"/> frames, calling
/// <see cref="Step"/> each frame, then exits 0 if no check failed, else 1.
/// Prints "SCENARIO PASS name" or "SCENARIO FAIL name: reason" as its last line.
/// </summary>
public partial class Scenario : Node
{
    [Export] public int Frames { get; set; } = 60;

    private int _frame;
    private bool _done;

    public override void _Process(double delta)
    {
        if (_done) return;
        try
        {
            Step(_frame);
        }
        catch (Exception e)
        {
            Fail(e.ToString());
            return;
        }
        if (++_frame >= Frames) Finish(null);
    }

    /// <summary>Called once per frame; throw or call <see cref="Fail"/> to fail.</summary>
    protected virtual void Step(int frame) { }

    protected void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    protected void Fail(string reason) => Finish(reason);

    private void Finish(string? failure)
    {
        if (_done) return;
        _done = true;
        var name = SceneFilePath.GetFile().GetBaseName();
        GD.Print(failure == null ? $"SCENARIO PASS {name}" : $"SCENARIO FAIL {name}: {failure}");
        GetTree().Quit(failure == null ? 0 : 1);
    }
}
