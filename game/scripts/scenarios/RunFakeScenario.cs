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
    private int _betrayals;

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
                _bot.Step();
                if (_run.Stats.Spared == 0) return;
                Check(_run.Mercy.Begs == 1 && _run.Mercy.Fakes == 0, "the first beggar was not genuine");
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
                Check(_fake.Revealed && _fake.Current == Enemy.State.Attack, $"the spared fake is {_fake.Current}, not attacking");
                Check(_run.Stats.Spared == 1 && _run.Stats.Merit == 1, "sparing a fake gave merit");
                Check(_run.Voice.Spoken.Contains("sword.first_fake"), "the sword did not call out the fake");
                GD.Print($"  frame {frame}: spared fake attacked");
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
