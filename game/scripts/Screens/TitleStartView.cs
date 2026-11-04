using Godot;

namespace SwordInTheForest.Screens;

/// <summary>Screenshot stage: the title, with Start pressed after a few frames, so the intro can be captured.</summary>
public partial class TitleStartView : Node
{
    private int _frame;

    public override void _Ready()
    {
        var title = GD.Load<PackedScene>("res://scenes/Title.tscn").Instantiate<Title>();
        AddChild(title);
    }

    public override void _Process(double delta)
    {
        if (++_frame == 5) GetChild<Title>(0).Begin();
    }
}
