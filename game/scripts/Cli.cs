using System;
using System.IO;
using Godot;

namespace SwordInTheForest;

/// <summary>
/// Autoload that reads command-line user args (after "--"):
///   --scenario &lt;name&gt;     run res://scenes/scenarios/&lt;name&gt;.tscn and exit 0 (pass) or 1 (fail)
///   --screenshot &lt;scene&gt;  render res://scenes/&lt;scene&gt;.tscn and save docs/screens/&lt;scene&gt;.png
///   --locale &lt;code&gt;       start in this locale (en, nl, zh_CN)
///   --frames &lt;n&gt;          with --screenshot: frames to wait before the shot (default 10)
///   --shot-name &lt;name&gt;    with --screenshot: output file name without .png
/// </summary>
public partial class Cli : Node
{
    private const int ScreenshotWarmupFrames = 10;

    private string? _screenshotScene;
    private string? _shotName;
    private int _framesUntilShot = -1;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // screenshots still count frames while paused
        var args = OS.GetCmdlineUserArgs();
        string? scenario = null;
        for (var i = 0; i < args.Length; i++)
        {
            var value = i + 1 < args.Length ? args[i + 1] : null;
            switch (args[i])
            {
                case "--locale" when value != null:
                    TranslationServer.SetLocale(value);
                    break;
                case "--scenario" when value != null:
                    scenario = value;
                    break;
                case "--screenshot" when value != null:
                    _screenshotScene = value;
                    break;
                case "--frames" when value != null && int.TryParse(value, out var frames):
                    _framesUntilShot = frames;
                    break;
                case "--shot-name" when value != null:
                    _shotName = value;
                    break;
            }
        }
        if (scenario != null)
        {
            StartScene($"res://scenes/scenarios/{scenario}.tscn");
        }
        else if (_screenshotScene != null)
        {
            StartScene($"res://scenes/{_screenshotScene}.tscn");
            if (_framesUntilShot < 0) _framesUntilShot = ScreenshotWarmupFrames;
        }
    }

    public override void _Process(double delta)
    {
        if (_framesUntilShot < 0 || --_framesUntilShot > 0) return;
        _framesUntilShot = -1;
        var image = GetViewport().GetTexture().GetImage();
        var dir = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "docs", "screens"));
        Directory.CreateDirectory(dir);
        var file = Path.Combine(dir, $"{_shotName ?? _screenshotScene!.Replace('/', '_')}.png");
        var error = image.SavePng(file);
        GD.Print(error == Error.Ok ? $"SCREENSHOT {file}" : $"SCREENSHOT FAILED {error}");
        GetTree().Quit(error == Error.Ok ? 0 : 1);
    }

    private void StartScene(string path)
    {
        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr($"Scene not found: {path}");
            GetTree().Quit(1);
            return;
        }
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, path);
    }
}
