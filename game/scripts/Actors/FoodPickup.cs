using Godot;
using SwordInTheForest.Core;
using SwordInTheForest.Run;
using SwordInTheForest.Ui;

namespace SwordInTheForest.Actors;

/// <summary>
/// Food a defeated enemy dropped (米饭, 酱牛肉, 烧鸡). It hops out of the body, then waits on the
/// floor until the player walks over it while hurt: it heals a little and its name floats up.
/// </summary>
public partial class FoodPickup : Node2D
{
    private const double HopMs = 320;
    private const float HopPx = 10;
    private readonly Vector2 _from;
    private Sprite2D _sprite = null!;
    private double _ageMs;

    public FoodPickup(Food food, Vector2 from, Vector2 to)
    {
        Food = food;
        _from = from;
        Position = to.Round();
    }

    public FoodPickup() { }

    public Food Food { get; }
    public Player? Player { get; set; }
    public bool Eaten { get; private set; }
    /// <summary>Raised when the player eats it, with the health it gave.</summary>
    public event System.Action<FoodPickup, int>? EatenBy;

    public static string Key(Food f) => f switch
    {
        Food.Rice => "combat.food.rice",
        Food.BraisedBeef => "combat.food.braised_beef",
        _ => "combat.food.roast_chicken",
    };

    public static int HealFor(Food f)
    {
        var r = Tunables.Run;
        return f switch
        {
            Food.Rice => r.RiceHeal,
            Food.BraisedBeef => r.BeefHeal,
            _ => r.ChickenHeal,
        };
    }

    public override void _Ready()
    {
        AddChild(new Shadow { Width = 5 });
        _sprite = new Sprite2D
        {
            Texture = PixelIcons.For(Food),
            Offset = new Vector2(0, -6),
            TextureFilter = TextureFilterEnum.Nearest,
        };
        AddChild(_sprite);
        Place();
    }

    public override void _Process(double delta)
    {
        if (Eaten) return;
        var clock = GameClock.Instance;
        _ageMs += clock?.DeltaMs ?? delta * 1000;
        Place();
        if (_ageMs < HopMs || Player == null || !IsInstanceValid(Player) || Player.IsDead) return;
        if (Player.Health >= Player.MaxHealth) return;
        if (Player.GlobalPosition.DistanceTo(GlobalPosition) > Tunables.Run.FoodPickupPx) return;
        Eat();
    }

    /// <summary>The player eats it now (also used by tests).</summary>
    public void Eat()
    {
        if (Eaten || Player == null) return;
        Eaten = true;
        var heal = HealFor(Food);
        Player.Heal(heal);
        Sfx.Play("heal", 0.05f, 0.7f);
        var label = new Label
        {
            AutoTranslateMode = AutoTranslateModeEnum.Disabled,
            Text = Tr("combat.food.eaten").Replace("{food}", Tr(Key(Food))).Replace("{health}", heal.ToString()),
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.75f, 1f, 0.7f),
            Size = new Vector2(120, 14),
            Position = new Vector2(-60, -30),
            ZIndex = 30,
        };
        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_outline_color", new Color(0, 0, 0));
        label.AddThemeConstantOverride("outline_size", 3);
        var holder = new Node2D { Position = Position, ZIndex = 30 };
        holder.AddChild(label);
        GetParent().AddChild(holder);
        var tween = holder.CreateTween();
        tween.TweenProperty(holder, "position:y", holder.Position.Y - 12, 0.9);
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0, 0.9).SetDelay(0.4);
        tween.TweenCallback(Callable.From(holder.QueueFree));
        EatenBy?.Invoke(this, heal);
        QueueFree();
    }

    /// <summary>A small arc from the body to the floor, then a slow bob.</summary>
    private void Place()
    {
        if (_ageMs < HopMs)
        {
            var t = (float)(_ageMs / HopMs);
            var arc = 4 * t * (1 - t) * HopPx;
            _sprite.Position = (_from - Position).Lerp(Vector2.Zero, t) + new Vector2(0, -arc);
        }
        else _sprite.Position = new Vector2(0, Mathf.Round(Mathf.Sin((float)(_ageMs / 380.0)) * 0.8f - 0.4f));
    }
}
