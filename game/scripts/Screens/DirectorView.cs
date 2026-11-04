using System;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Director;

namespace SwordInTheForest.Screens;

/// <summary>
/// Screenshot staging for the F2 overlay and the attunement meter: the arena with a bandit, and a
/// director that has seen 20 attacks from a simulated player (median 300 ms) at Standard.
/// tools/screenshot.sh screens/DirectorView "" 30 director_overlay
/// </summary>
public partial class DirectorView : Node2D
{
    public override void _Ready()
    {
        var service = DirectorService.Instance!;
        service.Leads.Adaptive = true;
        service.Leads.Challenge = Challenge.Standard;
        var rng = new Random(11);
        var bounds = new LeadTimeBounds(220, 700);
        for (var i = 0; i < 20; i++)
        {
            var mode = i < 12 ? DirectorMode.Calibrate : DirectorMode.Control;
            var lead = service.Director.NextLeadMs(bounds, mode, 0.7);
            var press = 25 + 300 * Math.Exp(0.22 * PopulationModel.Gaussian(rng));
            service.Director.Observe(rng.NextDouble() < 0.05 ? ThreatEvent.Lapse(lead, 120) : ThreatEvent.Timed(lead, Math.Round(press)));
        }
        service.Leads.NextLeadMs(Tunables.Attack("saber_slash_1").ToSpec());
        var arena = new Arena { AutoSpawn = false, PlayMusic = false };
        AddChild(arena);
        arena.Spawn<SaberBandit>(arena.Center + new Vector2(-30, -40));
        arena.Overlay!.Toggle();
    }
}
