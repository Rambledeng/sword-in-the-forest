using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Combat;
using SwordInTheForest.Core;
using SwordInTheForest.Fx;
using SwordInTheForest.Ui;

namespace SwordInTheForest.Run;

/// <summary>
/// A whole run (RunManager in the spec): builds the seeded <see cref="RunPlan"/>, walks its steps
/// (tutorial → bamboo forest ×2 → upgrade → outside the tavern ×2 → upgrade → tavern → boss), keeps
/// the merit, karma and kill counters, has the sword speak, and ends in the run summary with retry.
/// The player persists from room to room; death ends the run.
/// </summary>
public partial class RunScene : Node2D
{
    private const string MusicDir = "res://assets/third_party/ninja_adventure/music";
    private readonly HashSet<string> _said = new();
    private ReadingWatcher _watcher = null!;
    private TutorialCoach? _coach;
    private double _summaryAtMs = -1;
    private bool _changing;
    private int _lastHealth;

    /// <summary>The title's start sequence ran: open the tutorial on the intro (zoom out, ghosts barge in).</summary>
    public static bool PlayIntro { get; set; }

    /// <summary>The seed for the next run started by Retry (null: random).</summary>
    public static int? NextSeed { get; set; }

    /// <summary>A soul fell here: the next soul (New soul) starts at this area's first room.</summary>
    public static Area? NextArea { get; set; }
    /// <summary>The upgrades the fallen soul had taken; the next soul keeps them.</summary>
    public static List<Upgrade> NextUpgrades { get; } = new();

    /// <summary>0: <see cref="NextSeed"/>, else --seed, else random.</summary>
    [Export] public int Seed { get; set; }
    /// <summary>Tests start mid-run (e.g. the boss).</summary>
    [Export] public int StartStep { get; set; }
    [Export] public bool PlayMusic { get; set; } = true;
    /// <summary>Retry and Title change scenes; scenarios turn this off.</summary>
    public bool ChangeScenes { get; set; } = true;
    public IPlayerControls? Controls { get; set; }
    public bool Invulnerable { get; set; }
    public float? SurrenderOverride { get; set; }

    public RunPlan Plan { get; private set; } = null!;
    public RunStats Stats { get; } = new();
    public int StepIndex { get; private set; }
    public RunStep Step => Plan.Steps[Math.Min(StepIndex, Plan.Steps.Count - 1)];
    public Room? Room { get; private set; }
    public Player Player { get; private set; } = null!;
    public Hud Hud { get; private set; } = null!;
    public SwordVoice Voice { get; private set; } = null!;
    public UpgradeScreen? Upgrades { get; private set; }
    public SummaryScreen? Summary { get; private set; }
    public PauseMenu Pause { get; private set; } = null!;
    public TuningPanel Tuning { get; private set; } = null!;
    public DevMenu Dev { get; private set; } = null!;
    public DirectorOverlay Overlay { get; private set; } = null!;
    public ShakeCamera Camera { get; private set; } = null!;
    public TavernBoss? Boss { get; private set; }
    public int RoomsVisited { get; private set; }
    /// <summary>This soul took up the sword where the last one fell (null: a fresh run).</summary>
    public Area? ContinuedFrom { get; private set; }
    /// <summary>Kneel, beg or fake beg at each posture break (first beggar genuine).</summary>
    public MercyRules Mercy { get; private set; } = null!;
    /// <summary>The run's first red attack gets a generous lead.</summary>
    public FirstRedLead FirstRed { get; private set; } = null!;

    public event Action<Room>? RoomEntered;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Pausable;
        var seed = Seed != 0 ? Seed : NextSeed ?? Cli.Seed ?? (int)(GD.Randi() % 999_999) + 1;
        NextSeed = null;
        Plan = RunPlan.Build(seed);
        Mercy = new MercyRules(Tunables.Run.FakeBegChance);
        FirstRed = new FirstRedLead(() => CombatServices.LeadTimes, Tunables.Run.FirstRedLeadMs);
        Telemetry.Instance.StartRun();
        GD.Print($"Run seed {seed}");

        Player = new Player { Invulnerable = Invulnerable };
        if (Controls != null) Player.Controls = Controls;
        Player.Died += _ => EndRun(false);
        Player.FinisherLanded += (_, e) => { if (e.IsGone) OnFinished(e); };
        Player.SparedEnemy += (_, _) => { };
        Stats.MeritGained += () => ValueFx(karma: false);
        Stats.KarmaGained += () => ValueFx(karma: true);

        Camera = new ShakeCamera { Target = Player };
        AddChild(Camera);
        Hud = new Hud { Player = Player, HintKey = "ui.run.hint" };
        AddChild(Hud);
        Voice = new SwordVoice();
        AddChild(Voice);
        Tuning = new TuningPanel();
        AddChild(Tuning);
        Overlay = new DirectorOverlay();
        AddChild(Overlay);
        Pause = new PauseMenu();
        AddChild(Pause);
        Dev = new DevMenu(this);
        AddChild(Dev);
        Pause.QuitRequested += ToTitle;

        var service = DirectorService.Instance;
        var r = Tunables.Run;
        _watcher = new ReadingWatcher(r.ReadingAttunedAt, r.ReadingShift, r.ReadingMinGap);
        if (service != null)
        {
            var est = service.Director.Estimate();
            if (est.Observations >= r.ReturningObservations) _watcher.Prime(est.MedianMs, est.Observations);
            service.Observed += OnObserved;
        }
        Enemy.AnyGlint += OnAnyGlint;
        var start = StartStep;
        var carried = new List<Upgrade>();
        if (start == 0 && NextArea is Area area)
        {
            // A new wandering soul picks up the sword where the last one fell.
            start = Plan.AreaStart(area);
            carried.AddRange(NextUpgrades);
            ContinuedFrom = area;
        }
        NextArea = null;
        NextUpgrades.Clear();
        BeginStep(start);
        foreach (var u in carried)
        {
            Apply(u);
            Stats.Take(u);
        }
        if (ContinuedFrom != null) SwordVoice.Say("sword.new_soul");
    }

    public override void _ExitTree()
    {
        Enemy.AnyGlint -= OnAnyGlint;
        if (DirectorService.Instance != null) DirectorService.Instance.Observed -= OnObserved;
        _coach?.End();
    }

    // ---- Steps ---------------------------------------------------------------------------

    private void BeginStep(int index)
    {
        _changing = false;
        StepIndex = index;
        if (index >= Plan.Steps.Count)
        {
            EndRun(true);
            return;
        }
        var step = Plan.Steps[index];
        if (step.Kind == RunStepKind.Upgrade) ShowUpgrade(step.Choices);
        else LoadRoom(step.Room!);
    }

    /// <summary>Developer jump: leave whatever is on screen and start step <paramref name="index"/>.</summary>
    public void JumpTo(int index)
    {
        if (Stats.Ended || index < 0 || index >= Plan.Steps.Count) return;
        if (Upgrades != null)
        {
            Upgrades.QueueFree();
            Upgrades = null;
        }
        GetTree().Paused = false;
        _changing = true;
        BeginStep(index);
    }

    /// <summary>The index of the boss room's step.</summary>
    public int BossStep => Plan.Steps.ToList().FindIndex(s => s.Room?.IsBoss == true);

    private void Advance()
    {
        if (Stats.Ended) return;
        BeginStep(StepIndex + 1);
    }

    private void LoadRoom(RoomPlan plan)
    {
        var previous = Room;
        var newArea = previous == null || previous.Plan.Area != plan.Area;
        if (previous != null)
        {
            previous.Actors.RemoveChild(Player);
            previous.QueueFree();
        }
        _coach?.End();
        _coach = null;
        Room = new Room(plan) { Name = $"Room{++RoomsVisited}" };
        AddChild(Room);
        MoveChild(Room, 0);
        FxLayer.Set(Room.Fx);
        Player.Position = Room.EntryPx;
        Room.Actors.AddChild(Player);
        Camera.LimitLeft = 0;
        Camera.LimitTop = 0;
        Camera.LimitRight = (int)Room.SizePx.X;
        Camera.LimitBottom = (int)Room.SizePx.Y;
        Camera.GlobalPosition = Player.GlobalPosition;
        Camera.ResetSmoothing();
        Camera.MakeCurrent();
        Room.Cleared += OnRoomCleared;
        Room.EnemySpawned += HookEnemy;
        Room.SpawnEnemies(Player, SurrenderOverride ?? Tunables.Run.SurrenderChance);

        if (newArea)
        {
            Player.RefillGourd();
            var key = plan.Area switch
            {
                Area.Bamboo => "bamboo",
                Area.Outside => "outside",
                Area.Tavern => "tavern",
                _ => "tutorial",
            };
            Hud.ShowAreaTitle($"ui.area.{key}");
            if (plan.Area != Area.Tutorial) SwordVoice.Say($"sword.area.{key}");
        }
        if (PlayMusic)
        {
            var music = MusicFor(plan);
            Sfx.Instance.PlayMusic($"{MusicDir}/{music}.ogg");
        }
        if (plan.Area == Area.Tutorial)
        {
            var ghost = Room.Spawned.OfType<TutorialGhost>().FirstOrDefault();
            if (ghost != null && PlayIntro)
            {
                PlayIntro = false;
                StartIntro(ghost);
            }
            else if (ghost != null)
            {
                var d = DirectorService.Instance;
                var r = Tunables.Run;
                var returning = d != null && d.Director.Observations >= r.ReturningObservations;
                _coach = new TutorialCoach(ghost, returning ? r.TutorialAttacksReturning : r.TutorialAttacks);
            }
        }
        if (plan.IsBoss)
        {
            Boss = Room.Spawned.OfType<TavernBoss>().FirstOrDefault();
            Hud.Boss = Boss;
            SwordVoice.Say("sword.boss.intro");
        }
        RoomEntered?.Invoke(Room);
    }

    // ---- The intro: the view pulls back from the sword to the grove, then ghosts barge in ----

    private double _introT = -1;
    private TutorialGhost? _introGhost;
    private Vector2 _ghostFrom, _ghostTo;
    private IPlayerControls? _savedControls;
    private readonly List<(Node2D Node, Vector2 From, Vector2 To)> _wisps = new();
    private const double IntroZoomEnd = 1.6, IntroBargeAt = 1.3, IntroEnd = 2.5;

    public bool InIntro => _introT >= 0;
    private CanvasLayer? _introFade;

    private void StartIntro(TutorialGhost ghost)
    {
        _introT = 0;
        _introGhost = ghost;
        _savedControls = Player.Controls;
        Player.Controls = new BotControls();
        // The soul stands where the sword was, in the middle of the grove.
        Player.Position = Room!.SizePx / 2 + new Vector2(0, 24);
        Camera.Zoom = new Vector2(3.5f, 3.5f);
        Camera.GlobalPosition = Player.GlobalPosition;
        Camera.ResetSmoothing();
        _ghostTo = ghost.Position;
        _ghostFrom = new Vector2(Room.SizePx.X / 2, -12);
        ghost.DrillMode = true;
        ghost.Visible = false;
        // From the title's black, the grove fades in.
        _introFade = new CanvasLayer { Layer = 40 };
        var black = new ColorRect { Color = Colors.Black, MouseFilter = Control.MouseFilterEnum.Ignore };
        black.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _introFade.AddChild(black);
        AddChild(_introFade);
        ghost.Position = _ghostFrom;
    }

    private void TickIntro(double dtMs)
    {
        _introT += dtMs / 1000.0;
        var t = _introT;
        var z = (float)Mathf.Clamp(t / IntroZoomEnd, 0, 1);
        var zoom = Mathf.Lerp(3.5f, 1f, 1 - (1 - z) * (1 - z));
        Camera.Zoom = new Vector2(zoom, zoom);
        if (_introFade != null) _introFade.GetChild<ColorRect>(0).Modulate = new Color(1, 1, 1, (float)Mathf.Clamp(1 - t / 0.7, 0, 1));
        if (_introGhost != null && IsInstanceValid(_introGhost) && t >= IntroBargeAt)
        {
            if (!_introGhost.Visible)
            {
                _introGhost.Visible = true;
                SheetFx.Spawn("smoke", 6, 14, _introGhost.BodyGlobal, z: 10);
                Sfx.Play("swing", 0.1f, 0.8f);
                SpawnWisps();
            }
            var k = (float)Mathf.Clamp((t - IntroBargeAt) / (IntroEnd - IntroBargeAt), 0, 1);
            _introGhost.Position = _ghostFrom.Lerp(_ghostTo, 1 - (1 - k) * (1 - k));
        }
        foreach (var (node, from, to) in _wisps)
        {
            if (!IsInstanceValid(node)) continue;
            var k = (float)Mathf.Clamp((t - IntroBargeAt) / (IntroEnd - IntroBargeAt + 0.4), 0, 1);
            node.Position = from.Lerp(to, k);
            node.Modulate = new Color(0.8f, 0.95f, 1f, 0.55f * (1 - k));
        }
        if (t < IntroEnd) return;
        EndIntro();
    }

    /// <summary>Two more spirits rush through the grove and fade (only the one stays to fight).</summary>
    private void SpawnWisps()
    {
        var tex = GD.Load<Texture2D>("res://assets/third_party/ninja_adventure/actors/spirit.png");
        var size = Room!.SizePx;
        foreach (var (from, to) in new[] { (new Vector2(size.X * 0.3f, -10), new Vector2(size.X * 0.1f, size.Y * 0.7f)), (new Vector2(size.X * 0.7f, -10), new Vector2(size.X * 0.92f, size.Y * 0.6f)) })
        {
            var s = new CharacterSprite(tex);
            Room.Fx.AddChild(s);
            s.Position = from;
            _wisps.Add((s, from, to));
        }
    }

    private void EndIntro()
    {
        _introT = -1;
        Camera.Zoom = Vector2.One;
        _introFade?.QueueFree();
        _introFade = null;
        if (_savedControls != null) Player.Controls = _savedControls;
        foreach (var (node, _, _) in _wisps) if (IsInstanceValid(node)) node.QueueFree();
        _wisps.Clear();
        var ghost = _introGhost;
        _introGhost = null;
        if (ghost == null || !IsInstanceValid(ghost)) return;
        ghost.Position = _ghostTo;
        ghost.DrillMode = false;
        var d = DirectorService.Instance;
        var r = Tunables.Run;
        var returning = d != null && d.Director.Observations >= r.ReturningObservations;
        _coach = new TutorialCoach(ghost, returning ? r.TutorialAttacksReturning : r.TutorialAttacks);
    }

    /// <summary>Each area has its own music: the grove, the village outside, the tavern's tension, the boss's fight.</summary>
    public static string MusicFor(RoomPlan plan) => plan.IsBoss ? "fight" : plan.Area switch
    {
        Area.Outside => "lost_village",
        Area.Tavern => "tension",
        _ => "dark_forest",
    };

    private void HookEnemy(Enemy e)
    {
        e.LeadTimesOverride = FirstRed;
        if (e is Spearman) SayOnce("sword.first_spear");
        if (e is not TutorialGhost and not TavernBoss) e.BegRoll = en => Mercy.Decide(en.SurrenderChance, en.NextRoll);
        e.Surrendered += _ =>
        {
            // The first beggar (always genuine) teaches spare versus finish.
            SayOnce("sword.first_surrender");
        };
        e.Spared += _ =>
        {
            Stats.Spare();
            SayOnce("sword.first_spare");
            SayOnce("sword.mercy.warn");
        };
        e.Betrayed += _ => SayOnce("sword.first_fake");
        if (e is TavernBoss boss) boss.PhaseChanged += _ => SwordVoice.Say("sword.boss.phase2");
    }

    /// <summary>Merit and karma are felt, not counted: glitter or smoke on the player.</summary>
    public int ValueFxPlayed { get; private set; }

    private void ValueFx(bool karma)
    {
        ValueFxPlayed++;
        MeritFx.Play(Player, Player.BodyCenter, karma, Plan.Seed * 31 + ValueFxPlayed);
    }

    /// <summary>A finisher killed <paramref name="e"/>.</summary>
    private void OnFinished(Enemy e)
    {
        Stats.Finished(e.DiedBegging);
        if (e.DiedBegging)
        {
            SayOnce("sword.first_karma");
            SayOnce("sword.mercy.warn");
        }
        else if (e.DiedFaking) SayOnce("sword.fake_seen");
        else if (e is not TavernBoss && e is not TutorialGhost) SayOnce("sword.first_finisher");
    }

    private void OnRoomCleared(Room room)
    {
        Stats.RoomCleared();
        Sfx.Play("room_clear", 0.02f, 0.6f);
        if (room.Plan.IsBoss)
        {
            SwordVoice.Say("sword.boss.dead");
            EndRun(true);
            return;
        }
        if (room.Plan.Area == Area.Tutorial)
        {
            _coach?.End();
            SwordVoice.Say("sword.tut.done");
            return;
        }
        SayOnce("sword.room_clear");
    }

    private void ShowUpgrade(IReadOnlyList<Upgrade> choices)
    {
        SwordVoice.Say("sword.upgrade");
        Upgrades = new UpgradeScreen(choices);
        Upgrades.Chosen += u =>
        {
            Apply(u);
            Stats.Take(u);
            Upgrades = null;
            GetTree().Paused = false;
            CallDeferred(MethodName.Advance);
        };
        AddChild(Upgrades);
        GetTree().Paused = true;
    }

    /// <summary>Applies a sword-intent upgrade to the player.</summary>
    public void Apply(Upgrade u)
    {
        var r = Tunables.Run;
        var m = Player.Mods;
        switch (u)
        {
            case Upgrade.Vitality:
                m.HealthBonus += r.VitalityHealth;
                Player.Heal(r.VitalityHealth);
                break;
            case Upgrade.BroadStance: m.PostureBonus += r.BroadStancePosture; break;
            case Upgrade.SteadyBreath: m.PostureDrainFactor *= r.SteadyBreathFactor; break;
            case Upgrade.KeenEdge: m.DamageBonus += r.KeenEdgeDamage; break;
            case Upgrade.DeepGourd: m.SipsBonus += r.DeepGourdSips; break;
            case Upgrade.IronDeflect: m.DeflectFactor *= r.IronDeflectFactor; break;
            case Upgrade.MercifulCut: m.FinisherHeal += r.MercifulCutHeal; break;
        }
    }

    // ---- The end ---------------------------------------------------------------------------

    /// <summary>This run's win was the player's first ending.</summary>
    public bool FirstEnding { get; private set; }

    /// <summary>Screenshot stage: end the run now.</summary>
    public void EndForShot(bool won) => EndRun(won);

    private void EndRun(bool won)
    {
        if (Stats.Ended) return;
        Stats.End(won);
        _coach?.End();
        if (won)
        {
            // The first ending unlocks Detail mode (saved in settings.cfg).
            FirstEnding = !UserSettings.EndingSeen;
            UserSettings.MarkEndingSeen();
        }
        if (!won)
        {
            DirectorService.Instance?.AddSoulFallen();
            SwordVoice.Say("sword.death");
        }
        _summaryAtMs = GameClock.Instance.NowMs + Tunables.Run.DeathToSummaryMs;
    }

    private void ShowSummary()
    {
        var d = DirectorService.Instance;
        var est = d?.Director.Estimate() ?? default;
        Summary = new SummaryScreen(Stats, est, d?.Leads.Adaptive ?? false, d?.SoulsFallen ?? 0, Plan.Rooms.Count(),
            Stats.Won ? null : FellIn) { FirstEnding = FirstEnding };
        Summary.RetryRequested += Retry;
        Summary.TitleRequested += ToTitle;
        AddChild(Summary);
        GetTree().Paused = true;
    }

    public void Retry()
    {
        GetTree().Paused = false;
        NextSeed = (int)(GD.Randi() % 999_999) + 1;
        NextArea = null;
        NextUpgrades.Clear();
        if (!Stats.Won)
        {
            // The next soul starts at the start of the area where this one fell, keeping its upgrades.
            NextArea = FellIn;
            NextUpgrades.AddRange(Stats.Upgrades);
        }
        if (ChangeScenes) GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/Run.tscn");
    }

    /// <summary>The area of the room this soul is in (or last was in).</summary>
    public Area FellIn => Room?.Plan.Area ?? Area.Tutorial;

    private void ToTitle()
    {
        GetTree().Paused = false;
        if (ChangeScenes) GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://scenes/Title.tscn");
    }

    // ---- Every frame -------------------------------------------------------------------

    public override void _PhysicsProcess(double delta)
    {
        var clock = GameClock.Instance;
        if (Stats.Ended)
        {
            if (Summary == null && _summaryAtMs >= 0 && clock.NowMs >= _summaryAtMs) ShowSummary();
            return;
        }
        Stats.ElapsedMs += clock.DeltaMs;
        if (InIntro) TickIntro(clock.DeltaMs);
        _coach?.Tick();
        if (Player.Deflects > 0) SayOnce("sword.first_deflect");
        if (Player.IsSlowed) SayOnce("sword.boss.jar");
        if (Player.Health < _lastHealth && Player.Health <= 3 && !Player.IsDead) SayOnce("sword.low_health");
        _lastHealth = Player.Health;
        if (!_changing && Room != null && Room.AtExit(Player.GlobalPosition))
        {
            _changing = true;
            CallDeferred(MethodName.Advance);
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsActionPressed("pause") || Upgrades != null || Summary != null || Tuning.IsOpen || Dev.IsOpen) return;
        GetViewport().SetInputAsHandled();
        Pause.Toggle(!Pause.IsOpen);
    }

    private void OnAnyGlint(Enemy e, ThreatWindow threat)
    {
        if (threat.Glint == GlintColor.Red) SayOnce("sword.first_red");
        else if (e is Archer) SayOnce("sword.first_arrow");
    }

    private void OnObserved()
    {
        var d = DirectorService.Instance;
        if (d == null || Stats.Ended) return;
        var est = d.Director.Estimate();
        var change = _watcher.Observe(est.MedianMs, est.Attunement, est.Observations);
        var args = new Dictionary<string, string> { ["ms"] = ((int)Math.Round(est.MedianMs)).ToString() };
        switch (change)
        {
            case ReadingChange.Attuned: SwordVoice.Say("sword.read.attuned", args); break;
            case ReadingChange.Quicker: SwordVoice.Say("sword.read.quicker", args); break;
            case ReadingChange.Slower: SwordVoice.Say("sword.read.slower", args); break;
        }
    }

    private void SayOnce(string key)
    {
        if (_said.Add(key)) SwordVoice.Say(key);
    }
}
