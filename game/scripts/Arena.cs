using System.Collections.Generic;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;
using SwordInTheForest.Fx;
using SwordInTheForest.Ui;

namespace SwordInTheForest;

/// <summary>
/// The M1 test arena: a bamboo-fenced clearing, the player, saber bandits that respawn, the HUD
/// and the F1 tuning panel. Scenarios reuse it with <see cref="AutoSpawn"/> off and a bot player.
/// </summary>
public partial class Arena : Node2D
{
    public const int WidthTiles = 34;
    public const int HeightTiles = 22;
    private const string NatureTiles = "res://assets/third_party/ninja_adventure/tiles/tileset_nature.png";

    private readonly List<Enemy> _enemies = new();
    private double _respawnPlayerAtMs = -1;
    private double _respawnEnemiesAtMs = -1;
    private int _spawnIndex;

    [Export] public bool AutoSpawn { get; set; } = true;
    [Export] public bool WithUi { get; set; } = true;
    [Export] public bool PlayMusic { get; set; } = true;

    public Player Player { get; private set; } = null!;
    public Node2D Actors { get; private set; } = null!;
    public ShakeCamera Camera { get; private set; } = null!;
    public TuningPanel? Tuning { get; private set; }
    public IReadOnlyList<Enemy> Enemies => _enemies;
    public Vector2 Center => new(WidthTiles * 8, HeightTiles * 8);

    /// <summary>Set before adding the arena to the tree to drive the player with a bot.</summary>
    public IPlayerControls? Controls { get; set; }

    public override void _Ready()
    {
        // Scenarios run with ProcessMode Always; the arena itself must still pause under F1.
        ProcessMode = ProcessModeEnum.Pausable;
        AddChild(new ArenaFloor { SizeTiles = new Vector2I(WidthTiles, HeightTiles) });
        BuildWalls();
        Actors = new Node2D { YSortEnabled = true, Name = "Actors" };
        AddChild(Actors);
        BuildBamboo();
        var fx = new Node2D { Name = "Fx" };
        AddChild(fx);
        FxLayer.Set(fx);

        Player = new Player { Position = Center + new Vector2(0, 40) };
        if (Controls != null) Player.Controls = Controls;
        Player.Died += _ => _respawnPlayerAtMs = GameClock.Instance.NowMs + Tunables.Combat.RespawnDelayMs;
        Actors.AddChild(Player);

        Camera = new ShakeCamera
        {
            Target = Player,
            LimitLeft = 0,
            LimitTop = 0,
            LimitRight = WidthTiles * 16,
            LimitBottom = HeightTiles * 16,
        };
        AddChild(Camera);
        Camera.MakeCurrent();

        if (WithUi)
        {
            AddChild(new Hud { Player = Player });
            Tuning = new TuningPanel();
            AddChild(Tuning);
        }
        if (PlayMusic) Sfx.Instance.PlayMusic("res://assets/third_party/ninja_adventure/music/dark_forest.ogg");
        if (AutoSpawn) SpawnWave();
    }

    public T Spawn<T>(Vector2 at) where T : Enemy, new()
    {
        var e = new T { Position = at, Target = Player };
        e.Died += OnEnemyDied;
        e.TreeExiting += () => _enemies.Remove(e); // freed without dying (tests, scene change)
        _enemies.Add(e);
        Actors.AddChild(e);
        return e;
    }

    private void SpawnWave()
    {
        var count = Tunables.Combat.ArenaEnemies;
        for (var i = 0; i < count; i++)
        {
            var angle = -Mathf.Pi / 2 + (i - (count - 1) / 2f) * 0.9f + (_spawnIndex++ % 2) * 0.2f;
            Spawn<SaberBandit>(Center + new Vector2(0, 40) + Vector2.Right.Rotated(angle) * 110);
        }
    }

    private void OnEnemyDied(Enemy e)
    {
        _enemies.Remove(e);
        if (AutoSpawn && _enemies.Count == 0) _respawnEnemiesAtMs = GameClock.Instance.NowMs + Tunables.Combat.RespawnDelayMs;
    }

    public override void _PhysicsProcess(double delta)
    {
        var now = GameClock.Instance.NowMs;
        if (_respawnPlayerAtMs >= 0 && now >= _respawnPlayerAtMs)
        {
            _respawnPlayerAtMs = -1;
            Player.Respawn(Center + new Vector2(0, 40));
        }
        if (_respawnEnemiesAtMs >= 0 && now >= _respawnEnemiesAtMs)
        {
            _respawnEnemiesAtMs = -1;
            SpawnWave();
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("pause") && WithUi && Tuning is { IsOpen: false })
        {
            GetTree().Paused = false;
            GetTree().ChangeSceneToFile("res://scenes/Title.tscn");
        }
    }

    private void BuildWalls()
    {
        var body = new StaticBody2D { CollisionLayer = Player.LayerWorld, CollisionMask = 0 };
        AddChild(body);
        var w = WidthTiles * 16f;
        var h = HeightTiles * 16f;
        // Inner walkable area: one tile in from the sides and bottom, two from the top (taller bamboo).
        AddWall(body, new Rect2(-32, -32, w + 64, 32 + 30));
        AddWall(body, new Rect2(-32, h - 14, w + 64, 46));
        AddWall(body, new Rect2(-32, 0, 32 + 14, h));
        AddWall(body, new Rect2(w - 14, 0, 46, h));
    }

    private static void AddWall(StaticBody2D body, Rect2 r) =>
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = r.Size }, Position = r.Position + r.Size / 2 });

    private void BuildBamboo()
    {
        var tex = GD.Load<Texture2D>(NatureTiles);
        var rng = new RandomNumberGenerator { Seed = 11 };
        var stalk = new Rect2(176, 128, 16, 48);
        var shoot = new Rect2(192, 144, 16, 32);
        var bush = new Rect2(160, 144, 16, 16);
        void Place(Rect2 region, Vector2 feet, float tint)
        {
            var s = new Sprite2D
            {
                Texture = tex,
                RegionEnabled = true,
                RegionRect = region,
                Offset = new Vector2(0, -region.Size.Y / 2),
                Position = feet.Round(),
                Modulate = new Color(tint, tint, tint),
                TextureFilter = TextureFilterEnum.Nearest,
            };
            Actors.AddChild(s);
        }
        var w = WidthTiles * 16;
        var h = HeightTiles * 16;
        // Two staggered rows along the top, one row along each other side.
        for (var x = 0; x <= w; x += 11)
        {
            Place(stalk, new Vector2(x + rng.RandiRange(-2, 2), 18), rng.RandfRange(0.7f, 0.85f));
            Place(stalk, new Vector2(x + 5 + rng.RandiRange(-2, 2), 30), rng.RandfRange(0.9f, 1f));
            Place(rng.Randf() < 0.5f ? stalk : shoot, new Vector2(x + rng.RandiRange(-2, 2), h + 6), rng.RandfRange(0.85f, 1f));
        }
        for (var y = 30; y < h; y += 12)
        {
            Place(stalk, new Vector2(4 + rng.RandiRange(-2, 2), y), rng.RandfRange(0.85f, 1f));
            Place(stalk, new Vector2(w - 4 + rng.RandiRange(-2, 2), y), rng.RandfRange(0.85f, 1f));
        }
        for (var i = 0; i < 6; i++)
        {
            var at = new Vector2(rng.RandfRange(40, w - 40), rng.RandfRange(60, h - 30));
            if (at.DistanceTo(Center) < 120) continue;
            Place(bush, at, rng.RandfRange(0.85f, 1f));
        }
    }
}
