using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Run;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Food drops: a defeated enemy drops food (forced here), which waits while the player is at full
/// health and heals its tuned amount once the player is hurt and walks over it. At drop chance 0
/// nothing drops.
/// </summary>
public partial class FoodDropScenario : Scenario
{
    private readonly BotControls _controls = new();
    private RunScene _run = null!;
    private FoodPickup? _food;
    private int _drops;
    private int _stage;
    private int _waitFrom;
    private int _healthBefore;
    private float _chance;

    protected override bool FailOnTimeout => true;

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _run = new RunScene { Seed = 7, StartStep = 2, PlayMusic = false, ChangeScenes = false, Controls = _controls, Invulnerable = true };
            AddChild(_run);
            _run.FoodDropped += f => { _food = f; _drops++; };
            _chance = Tunables.Run.FoodDropChance;
            Check(_chance > 0 && _chance < 0.5f, $"food drop chance {_chance} is not low");
            return;
        }
        if (frame < 10) return;
        var enemies = _run.Room!.Spawned.Where(e => IsInstanceValid(e) && !e.IsGone).ToList();
        switch (_stage)
        {
            case 0: // force a roast chicken and defeat an enemy
                Check(enemies.Count >= 2, "the room has fewer than two enemies");
                _run.FoodRules.ForceNext = Food.RoastChicken;
                enemies[0].Kill();
                Check(_drops == 1 && _food != null && _food.Food == Food.RoastChicken, "no roast chicken dropped");
                GD.Print($"  frame {frame}: {Tr(FoodPickup.Key(_food!.Food))} dropped");
                _stage = 1;
                _waitFrom = frame;
                return;
            case 1: // standing on it at full health: it stays
                _run.Player.GlobalPosition = _food!.GlobalPosition;
                if (frame - _waitFrom < 40) return;
                Check(IsInstanceValid(_food) && !_food.Eaten, "food was eaten at full health");
                _run.Player.Wound(4);
                _healthBefore = _run.Player.Health;
                _stage = 2;
                _waitFrom = frame;
                return;
            case 2: // hurt: it is eaten and heals its amount
                if (!_food!.Eaten) _run.Player.GlobalPosition = _food.GlobalPosition;
                if (!_food.Eaten)
                {
                    if (frame - _waitFrom > 30) Fail("the hurt player standing on the food did not eat it");
                    return;
                }
                var gain = _run.Player.Health - _healthBefore;
                Check(gain == Tunables.Run.ChickenHeal, $"roast chicken healed {gain}, not {Tunables.Run.ChickenHeal}");
                GD.Print($"  frame {frame}: eaten, +{gain} health");
                _stage = 3;
                _waitFrom = frame;
                return;
            case 3: // (a moment for its name to float up, so screenshots can see it)
                if (frame - _waitFrom < 20) return;
                // At chance 0, a defeat drops nothing.
                Tunables.Run.FoodDropChance = 0;
                enemies[0].Kill();
                Tunables.Run.FoodDropChance = _chance;
                Check(_drops == 1, "food dropped at chance 0");
                Pass();
                return;
        }
    }
}
