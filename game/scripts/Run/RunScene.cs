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
            var music = plan.IsBoss ? "fight" : plan.Area is Area.Outside or Area.Tavern ? "lost_village" : "dark_forest";
            Sfx.Instance.PlayMusic($"{MusicDir}/{music}.ogg");
        }
        if (plan.Area == Area.Tutorial)
        {
            var ghost = Room.Spawned.OfType<TutorialGhost>().FirstOrDefault();
            if (ghost != null)
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

    private void EndRun(bool won)
    {
        if (Stats.Ended) return;
        Stats.End(won);
        _coach?.End();
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
            Stats.Won ? null : FellIn);
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
