using System;
using System.Collections.Generic;
using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Ui;

/// <summary>
/// The sword-intent (剑意) choice after an area: three cards; mouse, keys (← → and attack/accept)
/// or a gamepad pick one. The game is paused while it shows.
/// </summary>
public partial class UpgradeScreen : CanvasLayer
{
    private readonly IReadOnlyList<Upgrade> _choices;
    private readonly List<Button> _buttons = new();
    private bool _done;

    public UpgradeScreen(IReadOnlyList<Upgrade> choices) => _choices = choices;
    public UpgradeScreen() => _choices = Array.Empty<Upgrade>();

    public event Action<Upgrade>? Chosen;
    public IReadOnlyList<Upgrade> Choices => _choices;

    public static string Key(Upgrade u) => u switch
    {
        Upgrade.Vitality => "vitality",
        Upgrade.BroadStance => "broad_stance",
        Upgrade.SteadyBreath => "steady_breath",
        Upgrade.KeenEdge => "keen_edge",
        Upgrade.DeepGourd => "deep_gourd",
        Upgrade.IronDeflect => "iron_deflect",
        _ => "merciful_cut",
    };

    public override void _Ready()
    {
        Layer = 20;
        ProcessMode = ProcessModeEnum.Always;
        var dim = new ColorRect { Color = new Color(0, 0, 0, 0.72f) };
        dim.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(dim);
        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);
        var stack = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        stack.AddThemeConstantOverride("separation", 8);
        center.AddChild(stack);
        stack.AddChild(new Label { Text = "ui.upgrade.title", HorizontalAlignment = HorizontalAlignment.Center });
        var row = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        row.AddThemeConstantOverride("separation", 6);
        stack.AddChild(row);
        for (var i = 0; i < _choices.Count; i++)
        {
            var u = _choices[i];
            var k = Key(u);
            var b = new Button
            {
                AutoTranslateMode = Node.AutoTranslateModeEnum.Disabled,
                Text = Tr($"upgrade.{k}.name") + "\n\n" + Tr($"upgrade.{k}.desc"),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(132, 92),
                Alignment = HorizontalAlignment.Center,
                VerticalIconAlignment = VerticalAlignment.Top,
            };
            var index = i;
            b.Pressed += () => Choose(index);
            row.AddChild(b);
            _buttons.Add(b);
        }
        stack.AddChild(new Label { Text = "ui.upgrade.hint", HorizontalAlignment = HorizontalAlignment.Center, Modulate = new Color(1, 1, 1, 0.5f) });
        if (_buttons.Count > 0) _buttons[0].CallDeferred(Control.MethodName.GrabFocus);
        Sfx.Play("upgrade", 0.02f, 0.7f);
    }

    public override void _Input(InputEvent e)
    {
        // Attack also confirms (keyboard and pad players never need the mouse).
        if (!e.IsActionPressed("attack") || e is InputEventMouseButton) return;
        for (var i = 0; i < _buttons.Count; i++)
        {
            if (!_buttons[i].HasFocus()) continue;
            GetViewport().SetInputAsHandled();
            Choose(i);
            return;
        }
    }

    public void Choose(int index)
    {
        if (_done || index < 0 || index >= _choices.Count) return;
        _done = true;
        Sfx.Play("ui_accept", 0.02f);
        Chosen?.Invoke(_choices[index]);
        QueueFree();
    }
}
