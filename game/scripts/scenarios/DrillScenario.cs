using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;
using SwordInTheForest.Combat;
using SwordInTheForest.Core;
using SwordInTheForest.Tuning;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Runs a bot player against a drilled saber bandit: for every (lead time, attack) pair the two
/// are reset face to face, the bandit performs the attack (and the rest of its combo), and the
/// bot answers each glint by its policy. Every telemetry line is validated against
/// docs/telemetry.schema.json and handed to <see cref="CheckLine"/>.
/// </summary>
public abstract partial class DrillScenario : Scenario
{
    private const double GapMs = 250;
    private readonly Queue<(int Lead, string Attack)> _plan = new();
    private JsonSchemaLite _schema = null!;
    private int _firstLine;
    private double _idleSinceMs = -1;
    private bool _running;
    private (int Lead, string Attack) _current;

    protected Arena Arena { get; private set; } = null!;
    protected Player Player => Arena.Player;
    protected Enemy Bandit { get; private set; } = null!;
    protected BotControls Controls { get; } = new();
    protected GlintBot Bot { get; private set; } = null!;
    protected List<Dictionary<string, object?>> Lines { get; } = new();

    protected override bool FailOnTimeout => true;
    /// <summary>Bots that press continuously never leave the queue empty.</summary>
    protected virtual bool IgnorePendingPresses => false;
    protected abstract BotPolicy Policy { get; }
    protected virtual double ResponseDelayMs => 150;
    protected abstract IEnumerable<int> LeadTimes { get; }
    protected abstract IEnumerable<string> AttackIds { get; }

    /// <summary>Checks one validated telemetry line.</summary>
    protected abstract void CheckLine(Dictionary<string, object?> line, string context);

    /// <summary>Checks the whole run.</summary>
    protected abstract void CheckTotals();

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            Setup();
            return;
        }
        if (Done) return;
        var now = GameClock.Instance.NowMs;
        if (_running)
        {
            var idle = Bandit.DrillIdle && Player.OpenThreats.Count == 0 && (Controls.Pending == 0 || IgnorePendingPresses);
            if (!idle) { _idleSinceMs = -1; return; }
            if (_idleSinceMs < 0) { _idleSinceMs = now; return; }
            if (now - _idleSinceMs < GapMs) return;
            _running = false;
        }
        if (_plan.Count == 0)
        {
            Finish();
            return;
        }
        _current = _plan.Dequeue();
        Reset();
        CombatServices.LeadTimes = new FixedLeadTimeSource(_current.Lead);
        Bandit.Drill(Tunables.Attack(_current.Attack));
        _running = true;
        _idleSinceMs = -1;
    }

    private void Setup()
    {
        var schemaPath = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "docs", "telemetry.schema.json"));
        _schema = new JsonSchemaLite(File.ReadAllText(schemaPath));
        Arena = new Arena { AutoSpawn = false, WithUi = false, PlayMusic = false, Controls = Controls };
        AddChild(Arena);
        Bandit = Arena.Spawn<SaberBandit>(Arena.Center);
        Bandit.DrillMode = true;
        Bot = new GlintBot(Controls, Policy, ResponseDelayMs);
        Bot.Attach();
        _firstLine = Telemetry.Instance.Lines.Count;
        foreach (var lead in LeadTimes)
        foreach (var attack in AttackIds)
            _plan.Enqueue((lead, attack));
        GD.Print($"  drill: {_plan.Count} runs, policy {Policy}, delay {ResponseDelayMs} ms");
    }

    private void Reset()
    {
        Player.GlobalPosition = Arena.Center + new Vector2(0, 22);
        Player.Velocity = Vector2.Zero;
        Bandit.GlobalPosition = Arena.Center;
        Bandit.Velocity = Vector2.Zero;
        Controls.AimInput = Vector2.Up;
        Player.RefillForDrill();
        Bandit.RefillForDrill();
    }

    private void Finish()
    {
        Bot.Detach();
        CombatServices.ResetLeadTimes();
        var lines = Telemetry.Instance.Lines.Skip(_firstLine).ToList();
        Check(lines.Count > 0, "no telemetry lines were written");
        foreach (var raw in lines)
        {
            var problems = _schema.ValidateLine(raw);
            Check(problems.Count == 0, $"telemetry line fails the schema: {raw}: {string.Join("; ", problems)}");
            var parsed = (Dictionary<string, object?>)JsonLite.Parse(raw)!;
            Lines.Add(parsed);
            CheckLine(parsed, raw);
        }
        CheckTotals();
        GD.Print($"  {lines.Count} telemetry lines valid; player hit {Player.HitsTaken}x, deflects {Player.Deflects}, blocks {Player.Blocks}");
        Pass();
    }

    protected static string S(Dictionary<string, object?> line, string key) => (string)line[key]!;
}
