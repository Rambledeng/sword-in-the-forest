using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Fx;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>Every room of a run has its weather particles, and the music changes with each area (and for the boss).</summary>
public partial class AtmosphereScenario : Scenario
{
    protected override void Step(int frame)
    {
        if (frame != 1) return;
        var plan = RunPlan.Build(7);
        var music = new Dictionary<Area, HashSet<string>>();
        foreach (var room in plan.Rooms)
        {
            var r = new Room(room);
            AddChild(r);
            var atmosphere = r.GetNode<Atmosphere>("Atmosphere");
            Check(atmosphere.Count > 0, $"{room.Template.Id} has no weather");
            var key = room.IsBoss ? (Area)99 : room.Area;
            if (!music.TryGetValue(key, out var set)) music[key] = set = new HashSet<string>();
            set.Add(RunScene.MusicFor(room));
            Check(ResourceLoader.Exists($"res://assets/third_party/ninja_adventure/music/{RunScene.MusicFor(room)}.ogg"), $"missing music for {room.Template.Id}");
            r.QueueFree();
        }
        var tracks = music.Where(kv => kv.Key != Area.Tutorial).Select(kv => kv.Value.Single()).ToList();
        Check(tracks.Distinct().Count() == tracks.Count, $"areas share music: {string.Join(", ", music.Select(kv => $"{kv.Key}={string.Join("/", kv.Value)}"))}");
        GD.Print($"  music: {string.Join(", ", music.Select(kv => $"{kv.Key}={string.Join("/", kv.Value)}"))}");
    }
}
