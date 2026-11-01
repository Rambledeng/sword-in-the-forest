using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Attack spam does not beat the bandit: a bot that only mashes attack (no defence) is blocked
/// while the bandit circles, gets countered by the fast saber_counter (with its own glint), pays
/// posture for every swing, and does not kill the bandit in 15 s.
/// </summary>
public partial class BotSpamScenario : Scenario
{
    private readonly BotControls _controls = new();
    private Arena _arena = null!;
    private Enemy _bandit = null!;
    private float _maxPosture;
    private int _firstLine;
    private int _diedAt = -1;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _arena = new Arena { AutoSpawn = false, WithUi = false, PlayMusic = false, Controls = _controls };
            AddChild(_arena);
            _bandit = _arena.Spawn<SaberBandit>(_arena.Center + new Vector2(0, 10));
            _firstLine = Telemetry.Instance.Lines.Count;
            return;
        }
        var p = _arena.Player;
        if (p.IsDead && _diedAt < 0) _diedAt = frame;
        if (p.HitsTaken == 0) _maxPosture = Mathf.Max(_maxPosture, (float)p.Posture.Value);
        if (IsInstanceValid(_bandit))
        {
            var to = _bandit.BodyGlobal - p.BodyGlobal;
            _controls.MoveInput = to.Length() > 18 ? to.Normalized() : Vector2.Zero;
            if (frame % 3 == 0) _controls.Press(PlayerAction.Attack, GameClock.Instance.NowMs, 30);
        }
        if (OS.HasEnvironment("SPAM_TRACE") && frame % 10 == 0 && IsInstanceValid(_bandit))
            GD.Print($"  f{frame} bandit {_bandit.Current} hp {_bandit.Health} post {_bandit.Posture.Value:0} blocked {_bandit.BlockedHits} | player {p.Current} hp {p.Health} post {p.Posture.Value:0}");
        if (!IsInstanceValid(_bandit) || _bandit.Current == Enemy.State.Dead) _diedAt = frame;
        if (frame < Frames - 1 && _diedAt < 0) return;
        if (Done) return;

        Check(IsInstanceValid(_bandit) && _bandit.Current != Enemy.State.Dead, "spamming attack killed the bandit");
        Check(_bandit.BlockedHits >= 3, $"the bandit blocked only {_bandit.BlockedHits} strikes");
        Check(_bandit.Counters >= 1, "the bandit never counterattacked after blocking");
        var counterLines = Telemetry.Instance.Lines.Skip(_firstLine).Count(l => l.Contains("\"attack\":\"saber_counter\""));
        Check(counterLines >= 1, "no saber_counter glint reached telemetry");
        var cost = Tunables.Player.AttackPostureCost;
        Check(_maxPosture >= cost * 3 - 0.01f, $"swings should cost posture: max {_maxPosture} before any hit, cost {cost}");
        Pass();
        GD.Print($"  frame {frame}, player {(p.IsDead ? "dead" : "alive")} (hits taken {p.HitsTaken}, swings blocked {p.StrikesBlocked}), bandit health {_bandit.Health}");
        GD.Print($"  blocked {_bandit.BlockedHits}, counters {_bandit.Counters}, counter glints {counterLines}, bandit health {_bandit.Health}, player max posture {_maxPosture:0}");
    }
}
