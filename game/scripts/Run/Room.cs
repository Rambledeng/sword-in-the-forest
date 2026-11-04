using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Core;

namespace SwordInTheForest.Run;

/// <summary>
/// One room of a run, built from its template: the floor, the edge (bamboo, trees or the tavern's
/// wall), obstacles, the exit (closed until every enemy is dead or spared) and the enemies.
/// Enemies steer around obstacles through <see cref="INav"/>.
/// </summary>
public partial class Room : Node2D, INav
{
    public const int TilePx = 16;
    private const string Nature = "res://assets/third_party/ninja_adventure/tiles/tileset_nature.png";
    private static readonly Rect2 Stalk = new(176, 128, 16, 48);
    private static readonly Rect2 Shoot = new(192, 144, 16, 32);
    private static readonly Rect2 Bush = new(160, 144, 16, 16);
    private static readonly Rect2 Tree = new(0, 0, 32, 32);
    private static readonly Rect2 Stump = new(64, 128, 16, 16);
    private static readonly Rect2 Barrel = new(144, 144, 16, 16);
    private static readonly Rect2 Rock = new(256, 144, 16, 16);
    private static readonly Rect2 BigRock = new(208, 128, 32, 32);

    private readonly List<Enemy> _alive = new();
    private readonly List<Node2D> _gate = new();
    private StaticBody2D? _gateBody;
    private Texture2D _nature = null!;
    private RandomNumberGenerator _rng = null!;
    private bool _spawned;

    public Room(RoomPlan plan) => Plan = plan;
    public Room() => Plan = null!;

    public RoomPlan Plan { get; }
    public RoomTemplate T => Plan.Template;
    public Node2D Actors { get; private set; } = null!;
    public Node2D Fx { get; private set; } = null!;
    public IReadOnlyList<Enemy> Alive => _alive;
    public List<Enemy> Spawned { get; } = new();
    public bool IsClear { get; private set; }
    public Vector2 SizePx => new(T.Width * TilePx, T.Height * TilePx);
    public Vector2 EntryPx => TileCenter(T.Entry) + new Vector2(0, 4);

    /// <summary>Every enemy is dead or spared; the exit is open.</summary>
    public event Action<Room>? Cleared;
    public event Action<Enemy>? EnemySpawned;

    public static Vector2 TileCenter(TilePos p) => new(p.X * TilePx + TilePx / 2f, p.Y * TilePx + TilePx / 2f);
    public static TilePos TileAt(Vector2 local) => new(Mathf.FloorToInt(local.X / TilePx), Mathf.FloorToInt(local.Y / TilePx));

    public override void _Ready()
    {
        _nature = GD.Load<Texture2D>(Nature);
        _rng = new RandomNumberGenerator { Seed = (ulong)(uint)Plan.Seed };
        AddChild(MakeFloor());
        Actors = new Node2D { YSortEnabled = true, Name = "Actors" };
        AddChild(Actors);
        Fx = new Node2D { Name = "Fx" };
        AddChild(Fx);
        BuildCollision();
        BuildDecor();
        BuildGate();
    }

    // ---- Enemies -------------------------------------------------------------------------

    /// <summary>Spawns the planned enemies, aimed at <paramref name="player"/>.</summary>
    public void SpawnEnemies(Player player, float surrenderChance)
    {
        if (_spawned) return;
        _spawned = true;
        var i = 0;
        foreach (var s in Plan.Spawns)
        {
            var e = Create(s.Kind);
            e.Position = TileCenter(s.At) + new Vector2(0, 4);
            e.Target = player;
            e.Nav = this;
            e.Rng = new Random(Plan.Seed + 31 * ++i);
            e.Tint = VariantTint(s.Variant);
            e.SurrenderChance = s.Kind is EnemyKind.Saber or EnemyKind.Spear or EnemyKind.Archer ? surrenderChance : 0f;
            e.Died += Gone;
            e.Spared += Gone;
            _alive.Add(e);
            Spawned.Add(e);
            Actors.AddChild(e);
            EnemySpawned?.Invoke(e);
        }
        if (_alive.Count == 0) Clear();
    }

    public static Enemy Create(EnemyKind kind) => kind switch
    {
        EnemyKind.Ghost => new TutorialGhost(),
        EnemyKind.Spear => new Spearman(),
        EnemyKind.Archer => new Archer(),
        EnemyKind.Boss => new TavernBoss(),
        _ => new SaberBandit(),
    };

    private static Color VariantTint(int v) => v switch
    {
        1 => new Color(1f, 0.82f, 0.82f),
        2 => new Color(0.82f, 0.9f, 1f),
        3 => new Color(0.88f, 1f, 0.82f),
        4 => new Color(1f, 0.95f, 0.75f),
        _ => Colors.White,
    };

    private void Gone(Enemy e)
    {
        if (!_alive.Remove(e)) return;
        if (_alive.Count == 0) Clear();
    }

    private void Clear()
    {
        if (IsClear) return;
        IsClear = true;
        _gateBody?.QueueFree();
        _gateBody = null;
        foreach (var g in _gate) g.QueueFree();
        _gate.Clear();
        QueueRedraw();
        Cleared?.Invoke(this);
    }

    /// <summary>The player stands on an open exit tile.</summary>
    public bool AtExit(Vector2 playerGlobal)
    {
        if (!IsClear) return false;
        var p = TileAt(ToLocal(playerGlobal) + new Vector2(0, -4));
        return T.At(p) == Tile.Exit;
    }

    /// <summary>The middle of the exit, for bots.</summary>
    public Vector2 ExitGlobal => ToGlobal(T.Exits.Aggregate(Vector2.Zero, (a, e) => a + TileCenter(e)) / Math.Max(1, T.Exits.Count));

    // ---- INav ----------------------------------------------------------------------------

    public Vector2 Steer(Vector2 fromGlobal, Vector2 toGlobal)
    {
        var from = ToLocal(fromGlobal);
        var to = ToLocal(toGlobal);
        if (Clear(from, to)) return (to - from).Normalized();
        var next = RoomNav.NextStep(T, TileAt(from), TileAt(to), IsClear);
        if (next == null) return (to - from).Normalized();
        return (TileCenter(next.Value) + new Vector2(0, 2) - from).Normalized();
    }

    public Vector2 ShotEnd(Vector2 fromGlobal, Vector2 toGlobal)
    {
        var a = ToLocal(fromGlobal);
        var b = ToLocal(toGlobal);
        var len = a.DistanceTo(b);
        if (len < 0.01f) return toGlobal;
        var dir = (b - a) / len;
        // Start a little out from the shooter so its own feet never block the shot.
        for (var d = 4f; d <= len; d += 2f)
            if (BlocksShot(a + dir * d)) return ToGlobal(a + dir * Mathf.Max(0, d - 2f));
        return toGlobal;
    }

    public Vector2 LeavePoint => ToGlobal(EntryPx);

    private bool BlocksShot(Vector2 local)
    {
        var t = T.At(TileAt(local));
        return t == Tile.Wall || t == Tile.Obstacle;
    }

    /// <summary>A straight walk from a to b (with the body's width) crosses no blocked tile.</summary>
    private bool Clear(Vector2 a, Vector2 b)
    {
        var len = a.DistanceTo(b);
        var dir = len > 0.01f ? (b - a) / len : Vector2.Zero;
        var side = dir.Orthogonal() * 6;
        for (var d = 0f; d <= len; d += 6)
        {
            var p = a + dir * d;
            if (Blocked(p) || Blocked(p + side) || Blocked(p - side)) return false;
        }
        return true;
    }

    private bool Blocked(Vector2 local)
    {
        var t = T.At(TileAt(local));
        return t == Tile.Wall || t == Tile.Obstacle || (t == Tile.Exit && !IsClear);
    }

    // ---- Building ------------------------------------------------------------------------

    private Node2D MakeFloor()
    {
        var size = new Vector2I(T.Width, T.Height);
        return T.Area switch
        {
            Area.Outside => new ArenaFloor
            {
                SizeTiles = size, Seed = Plan.Seed, Plain = new Vector2I(1, 8),
                Variants = new[] { new Vector2I(8, 9), new Vector2I(9, 10) }, VariantChance = 0.1f,
            },
            Area.Tavern => new ArenaFloor
            {
                SizeTiles = size, Seed = Plan.Seed, Sheet = "tileset_interior_floor", Plain = new Vector2I(12, 2),
                Variants = Array.Empty<Vector2I>(),
            },
            _ => new ArenaFloor { SizeTiles = size, Seed = Plan.Seed },
        };
    }

    private void BuildCollision()
    {
        var body = new StaticBody2D { CollisionLayer = Player.LayerWorld, CollisionMask = 0, Name = "Walls" };
        AddChild(body);
        // One rectangle per horizontal run of blocked tiles.
        for (var y = 0; y < T.Height; y++)
        {
            var x = 0;
            while (x < T.Width)
            {
                var t = T.At(new TilePos(x, y));
                if (t != Tile.Wall && t != Tile.Obstacle) { x++; continue; }
                var start = x;
                while (x < T.Width && T.At(new TilePos(x, y)) is Tile.Wall or Tile.Obstacle) x++;
                AddRect(body, new Rect2(start * TilePx, y * TilePx, (x - start) * TilePx, TilePx));
            }
        }
        // Beyond the exits: the player triggers the exit before reaching this.
        AddRect(body, new Rect2(-TilePx, -2 * TilePx, (T.Width + 2) * TilePx, TilePx + 4));
    }

    private static void AddRect(StaticBody2D body, Rect2 r) =>
        body.AddChild(new CollisionShape2D { Shape = new RectangleShape2D { Size = r.Size }, Position = r.Position + r.Size / 2 });

    private void BuildGate()
    {
        if (T.Exits.Count == 0) return;
        _gateBody = new StaticBody2D { CollisionLayer = Player.LayerWorld, CollisionMask = 0, Name = "Gate" };
        AddChild(_gateBody);
        foreach (var e in T.Exits)
        {
            AddRect(_gateBody, new Rect2(e.X * TilePx, e.Y * TilePx, TilePx, TilePx));
            var at = TileCenter(e) + new Vector2(0, 7);
            switch (T.Area)
            {
                case Area.Tavern:
                    _gate.Add(Box(at - new Vector2(8, 15), new Vector2(16, 15), new Color(0.36f, 0.22f, 0.12f), new Color(0.5f, 0.33f, 0.18f)));
                    break;
                case Area.Outside:
                    _gate.Add(Place(Bush, at, 0.95f));
                    _gate.Add(Place(Rock, at + new Vector2(0, -6), 1f));
                    break;
                default:
                    _gate.Add(Place(Stalk, at + new Vector2(-4, 0), 0.9f));
                    _gate.Add(Place(Stalk, at + new Vector2(4, 1), 1f));
                    break;
            }
        }
    }

    public override void _Draw()
    {
        // The open way on: a soft light on the exit tiles.
        if (!IsClear) return;
        foreach (var e in T.Exits)
        {
            var r = new Rect2(e.X * TilePx, e.Y * TilePx, TilePx, TilePx);
            DrawRect(r, new Color(1f, 0.95f, 0.7f, 0.25f));
        }
    }

    private void BuildDecor()
    {
        for (var y = 0; y < T.Height; y++)
        for (var x = 0; x < T.Width; x++)
        {
            var p = new TilePos(x, y);
            var t = T.At(p);
            if (t == Tile.Wall) DecorEdge(p);
            else if (t == Tile.Obstacle) DecorObstacle(p);
        }
        if (T.Area == Area.Tavern) DrawTavernWalls();
    }

    private void DecorEdge(TilePos p)
    {
        var feet = TileCenter(p) + new Vector2(0, 7);
        switch (T.Area)
        {
            case Area.Tavern:
                break; // drawn as one wall
            case Area.Outside:
                if ((p.X + p.Y) % 2 == 0) Place(Tree, feet + new Vector2(_rng.RandiRange(-2, 2), 0), _rng.RandfRange(0.8f, 1f));
                else Place(Bush, feet, _rng.RandfRange(0.85f, 1f));
                break;
            default:
                Place(Stalk, feet + new Vector2(_rng.RandiRange(-3, 0), 0), _rng.RandfRange(0.75f, 0.95f));
                Place(_rng.Randf() < 0.5f ? Stalk : Shoot, feet + new Vector2(_rng.RandiRange(3, 6), -2), _rng.RandfRange(0.85f, 1f));
                break;
        }
    }

    private void DecorObstacle(TilePos p)
    {
        var feet = TileCenter(p) + new Vector2(0, 7);
        switch (T.Area)
        {
            case Area.Tavern:
            {
                var wide = T.At(new TilePos(p.X - 1, p.Y)) == Tile.Obstacle || T.At(new TilePos(p.X + 1, p.Y)) == Tile.Obstacle;
                var tall = T.At(new TilePos(p.X, p.Y - 1)) == Tile.Obstacle || T.At(new TilePos(p.X, p.Y + 1)) == Tile.Obstacle;
                if (wide && !tall) Box(feet - new Vector2(8, 12), new Vector2(16, 12), new Color(0.42f, 0.26f, 0.14f), new Color(0.6f, 0.4f, 0.22f));
                else if (wide || tall) Box(feet - new Vector2(8, 14), new Vector2(16, 14), new Color(0.45f, 0.28f, 0.15f), new Color(0.62f, 0.42f, 0.24f));
                else Place(Barrel, feet, 1f);
                break;
            }
            case Area.Outside:
            {
                var r = _rng.Randf();
                Place(r < 0.45f ? Rock : r < 0.7f ? Stump : r < 0.85f ? Barrel : Bush, feet, _rng.RandfRange(0.85f, 1f));
                break;
            }
            default:
                Place(Stalk, feet + new Vector2(-4, 0), _rng.RandfRange(0.8f, 1f));
                Place(Stalk, feet + new Vector2(4, 2), _rng.RandfRange(0.85f, 1f));
                Place(Bush, feet + new Vector2(0, 3), 1f);
                break;
        }
    }

    private void DrawTavernWalls()
    {
        var wall = new Color(0.25f, 0.16f, 0.1f);
        var trim = new Color(0.42f, 0.28f, 0.16f);
        var w = T.Width * TilePx;
        var h = T.Height * TilePx;
        var layer = new Node2D { ZIndex = -50 };
        AddChild(layer);
        void Rect(Rect2 r, Color c) => layer.AddChild(new ColorRect { Position = r.Position, Size = r.Size, Color = c, MouseFilter = Control.MouseFilterEnum.Ignore });
        Rect(new Rect2(0, 0, w, TilePx), wall);
        Rect(new Rect2(0, TilePx - 3, w, 3), trim);
        Rect(new Rect2(0, h - TilePx, w, TilePx), wall);
        Rect(new Rect2(0, h - TilePx, w, 2), trim);
        Rect(new Rect2(0, 0, TilePx, h), wall);
        Rect(new Rect2(TilePx - 2, TilePx, 2, h - 2 * TilePx), trim);
        Rect(new Rect2(w - TilePx, 0, TilePx, h), wall);
        Rect(new Rect2(w - TilePx, TilePx, 2, h - 2 * TilePx), trim);
        // Lanterns along the top wall.
        for (var x = 3 * TilePx; x < w - 2 * TilePx; x += 6 * TilePx)
            Rect(new Rect2(x, 4, 4, 6), new Color(0.95f, 0.45f, 0.25f));
    }

    private Node2D Place(Rect2 region, Vector2 feet, float tint)
    {
        var s = new Sprite2D
        {
            Texture = _nature,
            RegionEnabled = true,
            RegionRect = region,
            Offset = new Vector2(0, -region.Size.Y / 2),
            Position = feet.Round(),
            Modulate = new Color(tint, tint, tint),
            TextureFilter = TextureFilterEnum.Nearest,
        };
        Actors.AddChild(s);
        return s;
    }

    /// <summary>A code-drawn block (a table or the tavern door), y-sorted with the actors.</summary>
    private Node2D Box(Vector2 topLeft, Vector2 size, Color body, Color top)
    {
        var n = new Node2D { Position = (topLeft + new Vector2(size.X / 2, size.Y)).Round() };
        n.AddChild(new ColorRect { Position = new Vector2(-size.X / 2, -size.Y), Size = size, Color = body, MouseFilter = Control.MouseFilterEnum.Ignore });
        n.AddChild(new ColorRect { Position = new Vector2(-size.X / 2, -size.Y), Size = new Vector2(size.X, 3), Color = top, MouseFilter = Control.MouseFilterEnum.Ignore });
        Actors.AddChild(n);
        return n;
    }
}
