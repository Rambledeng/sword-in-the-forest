using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// The intro: Start on the title runs hum, seal, grip and pull back, then the run opens zoomed in
/// on the soul, zooms out, the ghost barges in from the top, and the tutorial coach starts.
/// </summary>
public partial class IntroScenario : Scenario
{
    private Title _title = null!;
    private RunScene? _run;
    private bool _sawZoom;
    private bool _sawHiddenGhost;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _title = GD.Load<PackedScene>("res://scenes/Title.tscn").Instantiate<Title>();
            _title.ChangeScene = false;
            AddChild(_title);
            return;
        }
        if (frame == 3) _title.Begin();
        if (_run == null)
        {
            if (!_title.SequenceDone) return;
            Check(RunScene.PlayIntro, "the title did not ask for the intro");
            _title.QueueFree();
            _run = new RunScene { Seed = 7, PlayMusic = false, ChangeScenes = false };
            AddChild(_run);
            GD.Print($"  title sequence done at frame {frame}");
            return;
        }
        var ghost = _run.Room!.Spawned.OfType<TutorialGhost>().First();
        if (_run.InIntro)
        {
            if (_run.Camera.Zoom.X > 1.5f) _sawZoom = true;
            if (!ghost.Visible) _sawHiddenGhost = true;
            Check(ghost.AttacksMade == 0, "the ghost attacked during the intro");
            return;
        }
        Check(_sawZoom && _sawHiddenGhost, "no zoom out or no barging ghost");
        Check(_run.Camera.Zoom == Vector2.One, "the camera stayed zoomed");
        Check(!ghost.DrillMode && ghost.Visible, "the ghost is not fighting after the intro");
        Check(!RunScene.PlayIntro, "the intro flag was not consumed");
        GD.Print($"  intro over at frame {frame}");
        Pass();
    }
}
