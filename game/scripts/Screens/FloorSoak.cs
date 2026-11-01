using System;
using System.IO;
using Godot;

namespace SwordInTheForest.Screens;

/// <summary>
/// Long-running render check for the "floor turns white" bug: runs the arena (with a bandit) for
/// <see cref="Frames"/> frames, forcing .NET garbage collection every second so anything that
/// relies on a collectable C# reference fails fast, then saves docs/screens/soak_floor.png and
/// checks the grass is still grass. Prints "SOAK PASS" or "SOAK FAIL" and exits 0 or 1.
/// Run: tools/screenshot.sh screens/FloorSoak "" 100000 x
/// </summary>
public partial class FloorSoak : Node2D
{
    private int _frame;
    private Arena _arena = null!;

    [Export] public int Frames { get; set; } = 1200;

    public override void _Ready()
    {
        _arena = new Arena { AutoSpawn = true, WithUi = true, PlayMusic = false };
        AddChild(_arena);
    }

    public override void _Process(double delta)
    {
        _frame++;
        if (_frame % 60 == 0)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        if (_frame < Frames) return;
        if (_frame == Frames) return; // let the last collection's frame render; read back next frame
        var image = GetViewport().GetTexture().GetImage();
        var dir = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "docs", "screens"));
        Directory.CreateDirectory(dir);
        image.SavePng(Path.Combine(dir, "soak_floor.png"));
        // Sample the floor in a band around the screen centre, away from HUD and bamboo.
        int white = 0, green = 0, total = 0;
        for (var y = 60; y < 220; y += 3)
        for (var x = 40; x < 440; x += 3)
        {
            var c = image.GetPixel(x, y);
            total++;
            if (c.R > 0.9f && c.G > 0.9f && c.B > 0.9f) white++;
            if (c.G > c.R && c.G > c.B) green++;
        }
        var ok = green > total * 0.6 && white < total * 0.05;
        GD.Print($"SOAK {(ok ? "PASS" : "FAIL")} frames={Frames} green={green}/{total} white={white}");
        GetTree().Quit(ok ? 0 : 1);
    }
}
