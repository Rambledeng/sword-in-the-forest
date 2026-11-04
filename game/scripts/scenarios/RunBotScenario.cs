using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;
using SwordInTheForest.Combat;
using SwordInTheForest.Core;
using SwordInTheForest.Director;
using SwordInTheForest.Run;
using SwordInTheForest.Ui;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// An invulnerable bot plays a seeded run headless and must reach the summary.
/// Mode "full": the whole run from the tutorial (the ghost calibrates the director) to a win.
/// Mode "boss": starts at the boss; both phases must trigger and the win summary must show.
/// Mode "mercy": the second bamboo room (two bandits), where every broken enemy begs; the bot spares one and finishes
/// the next, and merit, karma and the sword's lines must follow.
/// </summary>
public partial class RunBotScenario : Scenario
{
    private readonly BotControls _controls = new();
    private RunScene _run = null!;
    private RunBot _bot = null!;
    private DirectorService? _service;
    private int _firstLine;
    private bool _sawCalibrate;
    private bool _sawPhase2;
    private int _steps = -1;
    private readonly HashSet<string> _rooms = new();

    [Export] public int Seed { get; set; } = 7;
    [Export] public string Mode { get; set; } = "full";

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _service = DirectorService.Instance;
            Check(_service is { Persist: false }, "scenario runs must not touch the player's files");
            _service!.ResetBelief();
            _service.Leads.Adaptive = Mode == "full";
            _firstLine = Telemetry.Instance.Lines.Count;
            _run = new RunScene
            {
                Seed = Seed,
                StartStep = Mode switch { "boss" => 8, "mercy" => 2, _ => 0 },
                PlayMusic = false,
                ChangeScenes = false,
                Controls = _controls,
                Invulnerable = true,
                SurrenderOverride = Mode == "mercy" ? 1f : null,
            };
            AddChild(_run);
            _bot = new RunBot(_run, _controls);
            var spared = 0;
            _bot.SparePolicy = _ => spared++ % 2 == 0;
            _bot.Attach();
            return;
        }
        if (_run.Room != null) _rooms.Add(_run.Room.Name);
        if (_run.StepIndex != _steps)
        {
            _steps = _run.StepIndex;
            GD.Print($"  frame {frame}: step {_steps} {(_run.Step.Room?.Template.Id ?? "upgrade")}");
        }
        if (_service!.Leads.Mode == DirectorMode.Calibrate && _run.Step.Room?.Area == Area.Tutorial) _sawCalibrate = true;
        if (_run.Boss != null && GodotObject.IsInstanceValid(_run.Boss) && _run.Boss.Phase == 2) _sawPhase2 = true;
        if (Mode == "mercy") { CheckMercy(frame); return; }
        _bot.Step();
        if (_run.Summary == null) return;
        _bot.Detach();
        var s = _run.Stats;
        Check(s.Won, "the run did not end in a win");
        Check(_run.Summary.Lines.Count >= 7, "the summary is missing lines");
        var lines = Telemetry.Instance.Lines.Skip(_firstLine).Select(l => (Dictionary<string, object?>)JsonLite.Parse(l)!).ToList();
        var attacks = lines.Select(l => (string)l["attack"]!).ToHashSet();
        var spoken = _run.Voice.Spoken;
        GD.Print($"  summary at frame {frame}: rooms {s.RoomsCleared}, merit {s.Merit}, karma {s.Karma}, kills {s.Kills}, spared {s.Spared}, " +
                 $"attacks {lines.Count} ({string.Join(",", attacks)}), time {s.ElapsedMs / 1000:0}s, upgrades {string.Join(",", s.Upgrades)}");
        GD.Print($"  sword said: {string.Join(" ", spoken.Distinct())}");
        Check(_sawPhase2, "boss phase 2 never triggered");
        Check(attacks.Contains("boss_charge") || attacks.Contains("boss_cleave_1"), "no phase 1 boss attacks");
        Check(spoken.Contains("sword.boss.phase2") && spoken.Contains("sword.boss.dead"), "boss lines missing");
        Check(_run.Summary.Lines.Any(l => l.Contains(" ms")), "the summary does not show the reading");
        if (Mode == "full")
        {
            Check(s.RoomsCleared == 7, $"cleared {s.RoomsCleared} of 7 rooms");
            Check(_rooms.Count == 7, $"visited {_rooms.Count} rooms");
            Check(_bot.UpgradesChosen == 2 && s.Upgrades.Count == 2, "expected two upgrade choices");
            Check(_sawCalibrate, "the tutorial ghost never calibrated");
            Check(_service.Leads.Mode == DirectorMode.Control, "still calibrating after the tutorial");
            foreach (var id in new[] { "ghost_slash", "saber_slash_1", "archer_shot" })
                Check(attacks.Contains(id), $"no {id} in telemetry");
            Check(attacks.Contains("spear_thrust") || attacks.Contains("spear_sweep"), "no spearman attacks");
            foreach (var key in new[] { "sword.tut.wake", "sword.tut.glint", "sword.tut.break", "sword.first_deflect", "sword.area.bamboo", "sword.area.outside", "sword.upgrade", "sword.read.attuned" })
                Check(spoken.Contains(key), $"the sword never said {key}");
            var schemaPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "docs", "telemetry.schema.json"));
            var schema = new JsonSchemaLite(System.IO.File.ReadAllText(schemaPath));
            foreach (var raw in Telemetry.Instance.Lines.Skip(_firstLine))
            {
                var problems = schema.ValidateLine(raw);
                Check(problems.Count == 0, $"telemetry line fails the schema: {raw}: {string.Join("; ", problems)}");
            }
        }
        else
        {
            Check(attacks.Contains("boss_jar") || attacks.Contains("boss_flurry_1"), "no phase 2 attacks before she fell");
        }
        Pass();
    }

    private int _mercyStage;

    private void CheckMercy(int frame)
    {
        _bot.Step();
        var s = _run.Stats;
        if (_run.Room is { IsClear: true } && _mercyStage == 0)
        {
            _mercyStage = 1;
            GD.Print($"  room cleared at frame {frame}: merit {s.Merit}, karma {s.Karma}, spared {s.Spared}, kills {s.Kills}");
            var n = _run.Room.Spawned.Count;
            Check(s.Spared + s.Kills == n, $"{n} enemies, but {s.Spared} spared and {s.Kills} killed");
            Check(s.Spared >= 1, "nobody was spared");
            if (n >= 2) Check(s.Karma >= 1, "finishing a beggar gave no karma");
            Check(s.Merit == s.Spared, $"merit {s.Merit} should equal spared {s.Spared} (no kneeling finishers here)");
            var spoken = _run.Voice.Spoken;
            foreach (var key in new[] { "sword.first_surrender", "sword.first_spare" })
                Check(spoken.Contains(key), $"the sword never said {key}");
            if (n >= 2) Check(spoken.Contains("sword.first_karma"), "the sword never said sword.first_karma");
            Check(_run.Room.Spawned.All(e => !GodotObject.IsInstanceValid(e) || e.WasSpared || e.DiedBegging), "an enemy kneeled instead of begging");
            Pass();
        }
    }
}
