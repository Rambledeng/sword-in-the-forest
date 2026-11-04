using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Bots;
using SwordInTheForest.Combat;
using SwordInTheForest.Core;
using SwordInTheForest.Director;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// The director wired into a real fight: a bot that deflects every glint after
/// <see cref="ResponseDelayMs"/> faces the free-roaming bandit (the bot never strikes back).
/// Adaptive on: leads stay in range, calibration then control, and the estimate settles on the
/// bot's reaction time. Adaptive off: every attack uses its designed lead exactly.
/// The F2 overlay is open the whole time, so its drawing runs too.
/// </summary>
public partial class DirectorScenario : Scenario
{
    private readonly BotControls _controls = new();
    private Arena _arena = null!;
    private Enemy _bandit = null!;
    private GlintBot _bot = null!;
    private DirectorService _service = null!;
    private int _firstLine;

    [Export] public bool Adaptive { get; set; } = true;
    [Export] public int Attacks { get; set; } = 24;
    [Export] public double ResponseDelayMs { get; set; } = 260;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _service = DirectorService.Instance ?? throw new InvalidOperationException("no Director autoload");
            Check(!_service.Persist, "scenario runs must not load or save the player's belief");
            _service.ResetBelief();
            _service.Leads.Adaptive = Adaptive;
            _service.Leads.Challenge = Challenge.Standard;
            _firstLine = Telemetry.Instance.Lines.Count;
            _arena = new Arena { AutoSpawn = false, WithUi = true, PlayMusic = false, Controls = _controls };
            AddChild(_arena);
            _arena.Overlay!.Toggle();
            _bandit = _arena.Spawn<SaberBandit>(_arena.Center + new Vector2(0, -40));
            _bot = new GlintBot(_controls, BotPolicy.Right, ResponseDelayMs);
            _bot.Attach();
            return;
        }
        Check(!_arena.Player.IsDead, "the bot died");
        if (IsInstanceValid(_bandit)) _controls.AimInput = (_bandit.BodyGlobal - _arena.Player.BodyGlobal).Normalized();
        var lines = Telemetry.Instance.Lines.Skip(_firstLine)
            .Select(l => (Dictionary<string, object?>)JsonLite.Parse(l)!).ToList();
        if (lines.Count < Attacks) return;
        _bot.Detach();
        if (Adaptive) CheckAdaptive(lines);
        else CheckFixed(lines);
        Pass();
    }

    private static int Int(Dictionary<string, object?> l, string key) => (int)Convert.ToDouble(l[key]);

    private void CheckFixed(List<Dictionary<string, object?>> lines)
    {
        foreach (var l in lines)
        {
            var attack = Tunables.Attack((string)l["attack"]!);
            Check(Int(l, "lead_ms") == attack.LeadDefaultMs,
                $"{attack.Id}: lead {l["lead_ms"]} ms, designed {attack.LeadDefaultMs} ms");
        }
        Check(_service.Director.Observations > 0, "the director stopped observing while adaptive timing was off");
        GD.Print($"  {lines.Count} attacks at their designed leads; director observed {_service.Director.Observations}");
    }

    private void CheckAdaptive(List<Dictionary<string, object?>> lines)
    {
        var calibration = Tunables.Combat.DirectorCalibrationAttacks;
        foreach (var l in lines)
        {
            var attack = Tunables.Attack((string)l["attack"]!);
            var lead = Int(l, "lead_ms");
            Check(lead >= Math.Max(200, attack.LeadMinMs) && lead <= Math.Min(800, attack.LeadMaxMs),
                $"{attack.Id}: lead {lead} ms outside {attack.LeadMinMs}–{attack.LeadMaxMs}");
        }
        var d = _service.Director;
        Check(d.Observations >= Attacks - 2, $"only {d.Observations} observations for {lines.Count} attacks");
        Check(_service.Leads.Mode == DirectorMode.Control, "still calibrating after the calibration attacks");
        var presses = lines.Where(l => l["press_ms"] != null).Select(l => Convert.ToDouble(l["press_ms"])).ToList();
        Check(presses.Count >= lines.Count - 2, "the bot missed presses");
        var bot = presses.Average() - d.Grid.InputDelayMs;
        var est = d.Estimate();
        Check(Math.Abs(est.MedianMs - bot) <= 40, $"estimate {est} vs the bot's {bot:0} ms");
        Check(est.Attunement > 0.5, $"attunement {est.Attunement:0.00} after {d.Observations} attacks");
        var control = lines.Skip(calibration + 1).ToList();
        var shorter = control.Average(l => Int(l, "lead_ms")) < control.Average(l => Tunables.Attack((string)l["attack"]!).LeadDefaultMs);
        Check(shorter, "a fast bot did not get shorter leads than the designed ones");
        GD.Print($"  {lines.Count} attacks; {est}; control leads {string.Join(",", control.Select(l => Int(l, "lead_ms")))}");
    }
}
