using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Ui;

namespace SwordInTheForest.Run;

/// <summary>
/// The tutorial room's script: the sword talks the player through moving, deflecting, blocking,
/// rolling and the gourd while the ghost attacks with white cuts only (red comes with the
/// spearman). The director controls from the first attack: the M5 study showed calibration does
/// not speed up learning. After <see cref="Attacks"/> attacks the ghost can be broken and finished.
/// </summary>
public sealed class TutorialCoach
{
    private readonly TutorialGhost _ghost;
    private readonly double _startMs;
    private int _said;
    private bool _glintSaid;
    private bool _finishSaid;

    public TutorialCoach(TutorialGhost ghost, int attacks)
    {
        _ghost = ghost;
        Attacks = attacks;
        _startMs = GameClock.Instance.NowMs;
    }

    public int Attacks { get; }
    public bool BreakableNow => _ghost.Breakable;

    public void Tick()
    {
        if (!Godot.GodotObject.IsInstanceValid(_ghost)) return;
        var t = GameClock.Instance.NowMs - _startMs;
        var made = _ghost.AttacksMade;
        Once(0, t >= 500, "sword.tut.wake");
        Once(1, t >= 4500, "sword.tut.move");
        if (!_glintSaid && (made > 0 || _ghost.Runner.Phase == Combat.AttackPhase.Lead))
        {
            _glintSaid = true;
            SwordVoice.Say("sword.tut.glint");
        }
        Once(2, made >= 3, "sword.tut.block");
        Once(3, made >= 5, "sword.tut.roll");
        Once(4, made >= 7, "sword.tut.gourd");
        if (!_ghost.Breakable && made >= Attacks)
        {
            _ghost.MakeBreakable();
            SwordVoice.Say("sword.tut.break");
        }
        if (!_finishSaid && _ghost.IsKneeling)
        {
            _finishSaid = true;
            SwordVoice.Say("sword.tut.finish");
        }
    }

    /// <summary>The tutorial is over (kept for callers; nothing to undo since there is no calibration).</summary>
    public void End() { }

    private void Once(int step, bool when, string key)
    {
        if (!when || _said != step) return;
        _said++;
        SwordVoice.Say(key);
    }
}
