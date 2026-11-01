using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;
using SwordInTheForest.Core;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// The full loop against the real bandit AI: the bot deflects every glint and cuts back whenever
/// the bandit is open (recovering, staggered or kneeling). The bandit must kneel from a posture
/// break and die to a finisher, and the bot must survive.
/// </summary>
public partial class DuelScenario : Scenario
{
    private readonly BotControls _controls = new();
    private Arena _arena = null!;
    private Enemy _bandit = null!;
    private GlintBot _bot = null!;
    private bool _kneeled;
    private bool _died;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _arena = new Arena { AutoSpawn = false, WithUi = false, PlayMusic = false, Controls = _controls };
            AddChild(_arena);
            _bandit = _arena.Spawn<SaberBandit>(_arena.Center + new Vector2(0, -50));
            _bandit.Kneeled += _ => _kneeled = true;
            _bandit.Died += _ => _died = true;
            _bot = new GlintBot(_controls, BotPolicy.Right, 150);
            _bot.Attach();
            return;
        }
        var player = _arena.Player;
        Check(!player.IsDead, $"the bot died (deflects {player.Deflects}, hits taken {player.HitsTaken})");
        if (_died)
        {
            _bot.Detach();
            Check(_kneeled, "the bandit died without kneeling first");
            Check(player.Finishers == 1, $"expected one finisher, got {player.Finishers}");
            Check(player.Deflects >= 2, $"expected deflects, got {player.Deflects}");
            Check(Sfx.Instance.Played.ContainsKey("deflect") && Sfx.Instance.Played.ContainsKey("finisher") && Sfx.Instance.Played.ContainsKey("glint_white"),
                "sound hooks did not fire");
            GD.Print($"  bandit finished at frame {frame}: deflects {player.Deflects}, hits taken {player.HitsTaken}, health {player.Health}");
            Pass();
            return;
        }
        if (!IsInstanceValid(_bandit)) return;

        var to = _bandit.BodyGlobal - player.BodyGlobal;
        _controls.AimInput = to.Normalized();
        var open = _bandit.Current is Enemy.State.Recover or Enemy.State.Stagger or Enemy.State.Kneel;
        var threatened = player.OpenThreats.Count > 0 || _bandit.Current == Enemy.State.Attack;
        _controls.MoveInput = !threatened && to.Length() > 20 ? to.Normalized() : Vector2.Zero;
        if (open && to.Length() < 30 && player.Current is Player.State.Free or Player.State.Attack)
            _controls.Press(PlayerAction.Attack, GameClock.Instance.NowMs, 30);
    }
}
