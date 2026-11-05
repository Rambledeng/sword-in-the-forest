using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Fake beggars: the first beggar of a run is genuine even at fake chance 1; a fake that is spared
/// attacks at once and gives no merit; another fake attacks when the player turns its back on it.
/// </summary>
public partial class RunFakeScenario : Scenario
{
    private readonly BotControls _controls = new();
    private RunScene _run = null!;
    private RunBot _bot = null!;
    private int _stage;
    private Enemy? _fake;
    private Enemy? _first;
    private int _betrayals;
    private double _grabStartMs;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            DirectorService.Instance?.ResetBelief();
            _run = new RunScene { Seed = 7, StartStep = 2, PlayMusic = false, ChangeScenes = false, Controls = _controls, Invulnerable = true, SurrenderOverride = 1f };
            AddChild(_run);
            _run.Mercy.FakeChance = 1;
            _run.RoomEntered += r => { foreach (var e in r.Spawned) e.Betrayed += _ => _betrayals++; };
            foreach (var e in _run.Room!.Spawned) e.Betrayed += _ => _betrayals++;
            _bot = new RunBot(_run, _controls) { SparePolicy = _ => true };
            _bot.Attach();
            return;
        }
        var beggar = _run.Room!.Spawned.FirstOrDefault(e => IsInstanceValid(e) && e.IsBegging);
        switch (_stage)
        {
            case 0: // the first beggar is genuine; the bot spares it
                // Check the first beggar itself when it kneels: another enemy may break (and, at
                // fake chance 1, beg as a fake) before the bot has finished sparing this one.
                if (_first == null && beggar != null)
                {
                    _first = beggar;
                    Check(!beggar.IsFake && _run.Mercy.Begs == 1 && _run.Mercy.Fakes == 0, "the first beggar was not genuine");
                }
                _bot.Step();
                if (_run.Stats.Spared == 0) return;
                Check(_first != null && !_first.IsFake, "the bot spared something other than the first beggar");
                GD.Print($"  frame {frame}: first beggar genuine and spared");
                _stage = 1;
                return;
            case 1: // the next is a fake (chance 1); sparing it makes it attack
                _bot.Step();
                if (beggar != null && _fake == null)
                {
                    Check(beggar.IsFake, "the second beggar is genuine at fake chance 1");
                    _fake = beggar;
                }
                if (_fake == null || _betrayals == 0) return;
                Check(_fake.Revealed && _fake.Current == Enemy.State.Grab && _fake.WeaponOnGround, $"the spared fake is {_fake.Current}, not grabbing its weapon");
                Check(_run.Stats.Spared == 1 && _run.Stats.Merit == 1, "sparing a fake gave merit");
                Check(_run.Voice.Spoken.Contains("sword.first_spare"), "the sword said nothing about the genuine beggar walking away");
                Check(_run.Voice.Spoken.Contains("sword.spared_fake"), "the sword did not scorn sparing the fake");
                GD.Print($"  frame {frame}: spared fake grabs its weapon");
                _grabStartMs = GameClock.Instance.NowMs;
                _stage = 10;
                return;
            case 10: // the grab: weapon off the ground, then the normal attack
                _bot.Step();
                if (_fake!.Current == Enemy.State.Grab) return;
                var grabMs = GameClock.Instance.NowMs - _grabStartMs;
                Check(_fake.Current == Enemy.State.Attack && !_fake.WeaponOnGround, $"after the grab the fake is {_fake.Current}");
                // The run's first fake grabs slowly; later ones take the usual time.
                Check(System.Math.Abs(grabMs - Tunables.Run.FirstFakeGrabMs) < 40, $"the first fake's grab took {grabMs:0} ms, not {Tunables.Run.FirstFakeGrabMs}");
                GD.Print($"  frame {frame}: spared fake attacked after a {grabMs:0} ms grab");
                _run.Mercy.ForceNext = BegKind.Fake;
                _fake = null;
                _stage = 2;
                return;
            case 2: // break the next one (forced fake), then turn away from it
                if (beggar == null) { _bot.Step(); return; }
                Check(beggar.IsFake, "the forced fake is genuine");
                _fake = beggar;
                _stage = 3;
                return;
            case 3:
                if (_betrayals >= 2)
                {
                    Check(_fake!.Revealed, "the wrong enemy attacked");
                    Check(System.Math.Abs(_fake.GrabMs - Tunables.Run.FakeGrabMs) < 1, $"the second fake's grab is {_fake.GrabMs:0} ms, not {Tunables.Run.FakeGrabMs}");
                    Check(_run.Voice.Spoken.Contains("sword.first_fake"), "the sword did not call out the fake at your back");
                    GD.Print($"  frame {frame}: the fake struck when the player turned away");
                    Pass();
                    return;
                }
                Check(IsInstanceValid(_fake) && _fake!.IsBegging, "the fake stopped begging without attacking");
                var away = (_run.Player.BodyGlobal - _fake.BodyGlobal).Normalized();
                var dist = _run.Player.BodyGlobal.DistanceTo(_fake.BodyGlobal);
                // Walk away while close; then stand still with the back turned (facing stays away).
                _controls.MoveInput = dist < 50 ? away : Vector2.Zero;
                _controls.AimInput = Vector2.Zero;
                return;
        }
    }
}
