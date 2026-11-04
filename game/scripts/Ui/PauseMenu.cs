using System;
using Godot;
using SwordInTheForest.Core;

namespace SwordInTheForest.Ui;

/// <summary>Esc / Start: pause, with Resume and Quit to title.</summary>
public partial class PauseMenu : CanvasLayer
{
    private Control _root = null!;
    private Button _resume = null!;

    public bool IsOpen => _root.Visible;
    public event Action? QuitRequested;

    public override void _Ready()
    {
        Layer = 30;
        ProcessMode = ProcessModeEnum.Always;
        _root = new Control { Visible = false };
        _root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_root);
        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.6f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(dim);
        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(center);
        var stack = new VBoxContainer();
        stack.AddThemeConstantOverride("separation", 4);
        center.AddChild(stack);
        stack.AddChild(new Label { Text = "ui.pause.title", HorizontalAlignment = HorizontalAlignment.Center });
        _resume = new Button { Text = "ui.pause.resume" };
        _resume.Pressed += () => Toggle(false);
        stack.AddChild(_resume);
        var quit = new Button { Text = "ui.pause.quit" };
        quit.Pressed += () =>
        {
            Toggle(false);
            QuitRequested?.Invoke();
        };
        stack.AddChild(quit);
    }

    public void Toggle(bool open)
    {
        if (open == _root.Visible) return;
        _root.Visible = open;
        GetTree().Paused = open;
        Sfx.Play("ui_move", 0.02f, 0.6f);
        if (open) _resume.GrabFocus();
    }
}
