using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Combat;

namespace SwordInTheForest.Bots;

/// <summary>
/// A scripted player for tests: answers every enemy glint after a set delay, according to its
/// policy. Presses go through <see cref="BotControls"/>, so the real player state machine runs.
/// </summary>
public sealed class GlintBot
{
    private readonly BotControls _controls;

    public GlintBot(BotControls controls, BotPolicy policy, double delayMs)
    {
        _controls = controls;
        Policy = policy;
        DelayMs = delayMs;
    }

    public BotPolicy Policy { get; set; }
    public double DelayMs { get; set; }
    public int GlintsSeen { get; private set; }

    public void Attach()
    {
        Enemy.AnyGlint += OnGlint;
        _controls.HoldForever(PlayerAction.Guard, Policy == BotPolicy.GuardHold);
    }

    public void Detach() => Enemy.AnyGlint -= OnGlint;

    private void OnGlint(Enemy enemy, ThreatWindow threat)
    {
        GlintsSeen++;
        var at = threat.GlintMs + DelayMs;
        switch (Policy)
        {
            case BotPolicy.Right when threat.Glint == GlintColor.Red:
                var target = enemy.Target;
                var away = target != null ? (target.BodyGlobal - enemy.BodyGlobal).Normalized() : Vector2.Down;
                // Roll diagonally away: out of the thrust's line.
                _controls.Press(PlayerAction.Roll, at, 40, away.Rotated(0.8f));
                break;
            case BotPolicy.Right:
            case BotPolicy.GuardTap:
                _controls.Press(PlayerAction.Guard, at, 40);
                break;
        }
    }
}
