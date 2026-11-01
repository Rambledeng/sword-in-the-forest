using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Deflects alone still break the bandit: a bot that only deflects (never attacks) breaks its
/// posture in about five deflects (4–6 accepted), with the real AI and posture drain.
/// </summary>
public partial class DeflectBreakScenario : Scenario
{
    private readonly BotControls _controls = new();
    private Arena _arena = null!;
    private Enemy _bandit = null!;
    private GlintBot _bot = null!;
    private int _deflectsAtKneel = -1;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _arena = new Arena { AutoSpawn = false, WithUi = false, PlayMusic = false, Controls = _controls };
            AddChild(_arena);
            _bandit = _arena.Spawn<SaberBandit>(_arena.Center + new Vector2(0, -20));
            _bandit.Kneeled += _ => _deflectsAtKneel = _arena.Player.Deflects;
            _bot = new GlintBot(_controls, BotPolicy.Right, 150);
            _bot.Attach();
            return;
        }
        var p = _arena.Player;
        Check(!p.IsDead, "the deflecting bot died");
        if (IsInstanceValid(_bandit)) _controls.AimInput = (_bandit.BodyGlobal - p.BodyGlobal).Normalized();
        if (_deflectsAtKneel < 0) return;
        _bot.Detach();
        GD.Print($"  bandit kneeled after {_deflectsAtKneel} deflects (hits taken {p.HitsTaken})");
        Check(_deflectsAtKneel is >= 4 and <= 6, $"breaking the bandit took {_deflectsAtKneel} deflects; want about 5");
        Pass();
    }
}
