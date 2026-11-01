using System.Collections.Generic;
using System.IO;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;
using SwordInTheForest.Combat;
using SwordInTheForest.Core;

namespace SwordInTheForest.Screens;

/// <summary>
/// A staged, seeded fight that saves screenshots of key moments to docs/screens/ (needs a
/// renderer: run it through tools/screenshot.sh screens/Showcase). Moments: glint, deflect,
/// player cut, kneel, finisher, roll against a red thrust, and getting hit.
/// </summary>
public partial class Showcase : Node2D
{
    private readonly BotControls _controls = new();
    private readonly HashSet<string> _taken = new();
    private readonly List<(int Frame, string Name)> _pending = new();
    private Arena _arena = null!;
    private Enemy? _bandit;
    private GlintBot _bot = null!;
    private int _frame;
    private int _phase;
    private int _lastHealth;
    private int _lastDeflects;
    private int _lastHits;
    private int _phaseStart;

    [Export] public bool Panel { get; set; }

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // keep running while F1 pauses the arena
        GD.Seed(42);
        _arena = new Arena { AutoSpawn = false, WithUi = true, PlayMusic = false, Controls = _controls };
        AddChild(_arena);
        _bot = new GlintBot(_controls, BotPolicy.Right, 150);
        _bot.Attach();
        Enemy.AnyGlint += (_, t) => Shot(t.Glint == GlintColor.Red ? "combat_glint_red" : "combat_glint", 1);
        SpawnDuel();
    }

    public override void _ExitTree() => _bot.Detach();

    private void SpawnDuel()
    {
        _bandit = _arena.Spawn<SaberBandit>(_arena.Center + new Vector2(10, -40));
        _lastHealth = _bandit.Health;
        _bandit.Kneeled += _ => Shot("combat_kneel", 4);
        _bandit.Died += _ => { Shot("combat_finisher", 1); _phase = 1; _phaseStart = _frame + 90; };
    }

    /// <summary>Saves a shot <paramref name="delay"/> frames from now. Viewport readback lags one frame, hence the +1.</summary>
    private void Shot(string name, int delay)
    {
        if (_taken.Contains(name)) return;
        _taken.Add(name);
        _pending.Add((_frame + delay + 1, name));
    }

    public override void _Process(double delta)
    {
        _frame++;
        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            if (_pending[i].Frame > _frame) continue;
            Save(_pending[i].Name);
            _pending.RemoveAt(i);
        }
        var p = _arena.Player;
        if (p.Deflects > _lastDeflects) { _lastDeflects = p.Deflects; Shot("combat_deflect", 1); }
        if (p.HitsTaken > _lastHits) { _lastHits = p.HitsTaken; Shot("combat_hurt", 1); }
        if (p.Current == Player.State.Roll && p.StateElapsedMs > 90) Shot("combat_roll", 0);

        switch (_phase)
        {
            case 0:
                DuelBot(p);
                break;
            case 1 when _frame >= _phaseStart:
                // A red thrust, answered with a roll.
                _bandit = _arena.Spawn<SaberBandit>(p.GlobalPosition + new Vector2(-16, -14));
                _bandit.DrillMode = true;
                _bandit.Drill(Tunables.Attack("test_thrust_red"));
                _phase = 2;
                _phaseStart = _frame + 150;
                break;
            case 2 when _frame >= _phaseStart && _bandit != null && _bandit.DrillIdle:
                // Then a chop the bot does not answer.
                _bot.Policy = BotPolicy.Never;
                _bandit.GlobalPosition = p.GlobalPosition + new Vector2(0, -20);
                _bandit.Drill(Tunables.Attack("saber_chop"));
                _phase = 3;
                break;
            case 3 when _taken.Contains("combat_hurt") && _pending.Count == 0:
                if (Panel) { _arena.Tuning!.Toggle(); Shot("tuning_panel", 3); _phase = 4; break; }
                Done();
                break;
            case 4 when _pending.Count == 0:
                Done();
                break;
        }
        if (_frame > 6000) Done();
    }

    private void DuelBot(Player p)
    {
        if (_bandit == null || !IsInstanceValid(_bandit)) return;
        if (_bandit.Health < _lastHealth) { _lastHealth = _bandit.Health; Shot("combat_cut", 1); }
        var to = _bandit.BodyGlobal - p.BodyGlobal;
        _controls.AimInput = to.Normalized();
        var open = _bandit.Current is Enemy.State.Recover or Enemy.State.Stagger or Enemy.State.Kneel;
        var threatened = p.OpenThreats.Count > 0 || _bandit.Current == Enemy.State.Attack;
        _controls.MoveInput = !threatened && to.Length() > 20 ? to.Normalized() : Vector2.Zero;
        if (open && to.Length() < 30 && p.Current is Player.State.Free or Player.State.Attack)
            _controls.Press(PlayerAction.Attack, GameClock.Instance.NowMs, 30);
    }

    private void Save(string name)
    {
        var dir = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "docs", "screens"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{name}.png");
        var err = GetViewport().GetTexture().GetImage().SavePng(file);
        GD.Print(err == Error.Ok ? $"SCREENSHOT {file} (frame {_frame})" : $"SCREENSHOT FAILED {name} {err}");
    }

    private void Done()
    {
        GD.Print($"SHOWCASE DONE {string.Join(",", _taken)}");
        GetTree().Quit(0);
    }
}
