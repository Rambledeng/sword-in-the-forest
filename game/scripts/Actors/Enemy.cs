using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Combat;
using SwordInTheForest.Core;
using SwordInTheForest.Fx;
using SwordInTheForest.Run;
using SwordInTheForest.Tuning;

namespace SwordInTheForest.Actors;

/// <summary>
/// Shared enemy body: health, posture, the attack runner, reactions (stagger, deflected recoil,
/// kneel, finisher, death) and visuals. Subclasses supply the brain: which attack to start, and how
/// to move between attacks. Enemies differ only in their attack data and their state machine.
/// </summary>
public abstract partial class Enemy : CharacterBody2D
{
    public enum State { Approach, Circle, Attack, Recover, Stagger, Kneel, Surrender, Leave, Dead }
    public enum HitResult { Ignored, Hit, Blocked, Kneel, Finisher }

    public static readonly Vector2 BodyCenter = new(0, -7);
    public const float Radius = 6f;
    public const string Group = "enemies";

    /// <summary>Raised on every enemy glint (bots in the scenarios listen to this).</summary>
    public static event Action<Enemy, ThreatWindow>? AnyGlint;

    private static int _seedCounter = 1;

    private CharacterSprite _body = null!;
    private WeaponSprite _weapon = null!;
    private EnemyBars _bars = null!;
    private double _stateStartMs;
    private double _flashUntilMs;
    private Color _flashColor = Colors.White;
    private Vector2 _knockback;
    private double _walkMs;
    private int _staggerChain;
    private double _lastHitMs = -1e9;
    private double _swingStartMs = -1;
    private double _recoilUntilMs;
    private int _blockChain;
    private double _lastBlockMs = -1e9;
    private double _guardPoseUntilMs;
    private AttackData? _attack;
    private readonly Queue<AttackData> _drill = new();
    private WeaponSprite? _offhand;
    private Telegraph _telegraph = null!;
    private Vector2 _areaTarget;
    private Projectile? _projectile;
    /// <summary>How far the current shot can fly along its aim before a wall or obstacle stops it.</summary>
    private float _shotRangePx = float.MaxValue;
    private Vector2 _leaveFrom;

    protected Enemy()
    {
        Runner = new AttackRunner(CombatServices.LeadTimes, _seedCounter++);
    }

    public Player? Target { get; set; }
    public State Current { get; private set; } = State.Approach;
    public int Health { get; private set; }
    public PostureMeter Posture { get; } = new(100);
    public Vector2 Facing { get; protected set; } = Vector2.Down;
    /// <summary>The attack's direction, locked at the glint so a roll can carry the player out of it.</summary>
    public Vector2 AimDir { get; private set; } = Vector2.Down;
    public AttackRunner Runner { get; }
    public AttackData? CurrentAttack => _attack;

    /// <summary>Drill mode (tests): stand still and run queued attacks only.</summary>
    public bool DrillMode { get; set; }
    public bool DrillIdle => DrillMode && !Runner.Running && _drill.Count == 0 && Current != State.Attack;

    public event Action<Enemy>? Died;
    public event Action<Enemy>? Kneeled;
    /// <summary>It posture-broke and begs (投降忏悔) instead of kneeling.</summary>
    public event Action<Enemy>? Surrendered;
    /// <summary>The player let it go.</summary>
    public event Action<Enemy>? Spared;

    /// <summary>Chance it begs instead of kneeling when its posture breaks (0 in the test arena).</summary>
    public float SurrenderChance { get; set; }
    /// <summary>The run's mercy rules decide kneel, beg or fake beg at a posture break (null: roll <see cref="SurrenderChance"/>).</summary>
    public Func<Enemy, BegKind>? BegRoll { get; set; }
    /// <summary>Its begging is a trick (only meaningful while it begs).</summary>
    public bool IsFake { get; private set; }
    /// <summary>A random number in [0, 1) from the enemy's own generator (the run's mercy rules use it).</summary>
    public double NextRoll() => Rng.NextDouble();
    /// <summary>Colour variant tint for the body (random per spawn).</summary>
    public Color Tint { get; set; } = Colors.White;
    /// <summary>Steering around obstacles (null: walk straight).</summary>
    public INav? Nav { get; set; }
    /// <summary>The enemy's own random numbers (surrender rolls, attack choice); reseed for a repeatable run.</summary>
    public Random Rng { get; set; } = new(_seedCounter * 7919);
    /// <summary>Attacks whose telemetry line is complete.</summary>
    public int AttacksMade { get; private set; }
    /// <summary>It begged and was cut down anyway (karma).</summary>
    public bool DiedBegging { get; private set; }
    public bool WasSpared { get; private set; }
    public bool IsBegging => Current == State.Surrender;

    public bool CanBeHit => Current is not (State.Dead or State.Leave);
    /// <summary>Spared, it stands up and walks out through the room's entrance.</summary>
    public bool IsLeaving => Current == State.Leave;
    public bool IsKneeling => Current == State.Kneel;
    public bool IsGone => Current == State.Dead;
    /// <summary>Where a thrown attack lands: the player's position at the glint.</summary>
    public Vector2 AreaTarget => _areaTarget;
    public bool InSwing => _swingStartMs >= 0;
    public double MaxHealth => Tuning.MaxHealth;
    public Vector2 BodyGlobal => GlobalPosition + BodyCenter;
    public double StateElapsedMs => Clock.NowMs - _stateStartMs;

    protected abstract EnemyTuning Tuning { get; }
    protected abstract string SheetPath { get; }
    protected abstract string WeaponPath { get; }
    protected static GameClock Clock => GameClock.Instance;

    /// <summary>The brain: choose the next attack to start.</summary>
    protected abstract AttackData ChooseAttack();

    /// <summary>False while it cannot be broken or hurt (the tutorial ghost before calibration ends).</summary>
    public virtual bool Breakable => true;
    /// <summary>False when a finisher only ends a phase (the boss's first).</summary>
    protected virtual bool FinisherKills => true;
    /// <summary>Windups are multiplied by this (the boss's second phase).</summary>
    protected virtual float WindupFactor => 1f;
    protected virtual float SpriteScale => 1f;
    protected virtual string? OffhandWeaponPath => null;
    protected virtual float BodyAlpha => 1f;

    /// <summary>A random number in [0, 1) from the enemy's own generator.</summary>
    protected float Roll() => (float)Rng.NextDouble();

    /// <summary>The fast attack it answers a string of blocked strikes with (null = none).</summary>
    protected virtual AttackData? CounterAttack => null;

    // Counters for the scenarios.
    public int BlockedHits { get; private set; }
    public int Counters { get; private set; }

    /// <summary>
    /// Blocks the player's strikes while it is free to (approaching or circling); open while it
    /// attacks, recovers from an attack (the player's opening), is staggered or kneels.
    /// </summary>
    public virtual bool IsBlocking => Current is State.Approach or State.Circle;

    public static IEnumerable<Enemy> All(Node from) => from.GetTree().GetNodesInGroup(Group).OfType<Enemy>();

    public override void _Ready()
    {
        AddToGroup(Group);
        CollisionLayer = Player.LayerEnemy;
        CollisionMask = Player.LayerWorld | Player.LayerPlayer | Player.LayerEnemy;
        MotionMode = MotionModeEnum.Floating;
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Radius }, Position = new Vector2(0, -3) });
        AddChild(new Shadow());
        _weapon = new WeaponSprite(GD.Load<Texture2D>(WeaponPath));
        AddChild(_weapon);
        if (OffhandWeaponPath != null)
        {
            _offhand = new WeaponSprite(GD.Load<Texture2D>(OffhandWeaponPath)) { Scale = new Vector2(SpriteScale, SpriteScale) };
            AddChild(_offhand);
        }
        _weapon.Scale = new Vector2(SpriteScale, SpriteScale);
        _body = new CharacterSprite(GD.Load<Texture2D>(SheetPath)) { Scale = new Vector2(SpriteScale, SpriteScale) };
        AddChild(_body);
        _telegraph = new Telegraph(this) { ZIndex = -1 };
        AddChild(_telegraph);
        _bars = new EnemyBars(this);
        AddChild(_bars);

        Health = Tuning.MaxHealth;
        Posture.Max = Tuning.MaxPosture;
        Runner.LeadTimes = CombatServices.LeadTimes;
        Runner.Glint += OnGlint;
        Runner.Swing += OnSwing;
        Runner.Impact += OnImpact;
        Runner.Finished += OnFinished;
        Runner.Finalized += t =>
        {
            AttacksMade++;
            Telemetry.Instance.Write(t);
            DirectorService.Instance?.Observe(t);
        };
        Enter(State.Approach);
    }

    public override void _ExitTree()
    {
        Runner.Flush();
    }

    /// <summary>Queues an attack for drill mode.</summary>
    public void Drill(AttackData attack) => _drill.Enqueue(attack);

    /// <summary>Resets health and posture (test drills).</summary>
    public void RefillForDrill()
    {
        Health = Tuning.MaxHealth;
        Posture.Reset();
        if (Current is State.Kneel or State.Stagger) Enter(State.Circle);
    }

    protected void Enter(State s)
    {
        Current = s;
        _stateStartMs = Clock.NowMs;
    }

    // ---- Main loop ----------------------------------------------------------------------

    public override void _PhysicsProcess(double delta)
    {
        var now = Clock.NowMs;
        var dt = Clock.DeltaMs;
        if (dt <= 0)
        {
            UpdateVisuals(now);
            return;
        }
        var t = Tuning;
        Posture.Max = t.MaxPosture;
        Posture.Tick(dt, t.PostureDrainPerSec, t.PostureDrainDelayMs, holdDrain: Current == State.Kneel);
        Runner.LatePressGraceMs = Tunables.Combat.LatePressGraceMs;
        Runner.Target = Target;
        if (now - _lastHitMs > 1500) _staggerChain = 0;

        var velocity = Vector2.Zero;
        var targetAlive = Target != null && IsInstanceValid(Target) && !Target.IsDead;
        var toTarget = targetAlive ? Target!.BodyGlobal - BodyGlobal : Vector2.Zero;
        var dist = toTarget.Length();

        switch (Current)
        {
            case State.Approach:
            case State.Circle:
                if (DrillMode)
                {
                    if (targetAlive) Facing = toTarget.Normalized();
                    if (_drill.Count > 0) StartAttack(_drill.Dequeue());
                    break;
                }
                velocity = Current == State.Approach ? UpdateApproach(toTarget, dist, targetAlive) : UpdateCircle(toTarget, dist, targetAlive);
                break;
            case State.Attack:
            case State.Recover:
                velocity = UpdateAttack(toTarget, dist, targetAlive, now, dt);
                break;
            case State.Stagger:
                if (StateElapsedMs >= t.StaggerMs)
                {
                    if (_staggerChain >= t.MaxStaggerChain && targetAlive && !DrillMode)
                    {
                        _staggerChain = 0;
                        Counters++;
                        StartAttack(CounterAttack ?? ChooseAttack());
                    }
                    else Enter(State.Circle);
                }
                break;
            case State.Kneel:
                if (Health > 0 && StateElapsedMs >= t.KneelMs)
                {
                    Posture.Reset(t.MaxPosture * t.PostureAfterKneel);
                    Enter(State.Circle);
                }
                break;
            case State.Surrender:
                break;
            case State.Leave:
                velocity = UpdateLeave();
                break;
            case State.Dead:
                if (StateElapsedMs >= 1200) QueueFree();
                break;
        }

        Runner.Advance(now);
        MoveBody(velocity, dt);
        UpdateVisuals(now);
    }

    protected virtual Vector2 UpdateApproach(Vector2 toTarget, float dist, bool targetAlive)
    {
        if (!targetAlive) return Vector2.Zero;
        Facing = toTarget.Normalized();
        _walkMs += Clock.DeltaMs;
        if (dist <= Tuning.CircleDistancePx + 4)
        {
            BeginCircle();
            return Vector2.Zero;
        }
        return SteerTo(Target!.GlobalPosition) * Tuning.MoveSpeed;
    }

    /// <summary>The direction to walk toward a point, around the room's obstacles.</summary>
    protected Vector2 SteerTo(Vector2 globalPoint)
    {
        var straight = (globalPoint - GlobalPosition).Normalized();
        if (Nav == null) return straight;
        var d = Nav.Steer(GlobalPosition, globalPoint);
        return d == Vector2.Zero ? straight : d;
    }

    /// <summary>Spared: stand up, then walk to the entrance and fade out there.</summary>
    private Vector2 UpdateLeave()
    {
        var r = Tunables.Run;
        if (StateElapsedMs < r.SpareStandMs) return Vector2.Zero;
        var to = Nav?.LeavePoint ?? _leaveFrom + Vector2.Down * 200;
        var arrived = GlobalPosition.DistanceTo(to) < 6;
        if (arrived || StateElapsedMs >= r.SpareLeaveMaxMs)
        {
            if (LeaveFadeElapsed < 0) _leaveFadeStartMs = Clock.NowMs;
            if (LeaveFadeElapsed >= 500) QueueFree();
            return Vector2.Zero;
        }
        _walkMs += Clock.DeltaMs;
        var dir = SteerTo(to);
        Facing = dir;
        return dir * Tuning.MoveSpeed * r.SpareLeaveSpeedFactor;
    }

    private double _leaveFadeStartMs = -1;
    private double LeaveFadeElapsed => _leaveFadeStartMs < 0 ? -1 : Clock.NowMs - _leaveFadeStartMs;

    protected void BeginCircle()
    {
        Enter(State.Circle);
        _circleMs = Tuning.CircleMinMs + Rng.NextDouble() * (Tuning.CircleMaxMs - Tuning.CircleMinMs);
        _circleSign = Rng.Next(2) == 0 ? -1 : 1;
    }

    protected void AddWalk() => _walkMs += Clock.DeltaMs;

    private double _circleMs;
    private int _circleSign = 1;
    protected bool CircleDone => StateElapsedMs >= _circleMs && AttackSlotFree;

    /// <summary>Fewer than MaxConcurrentAttacks other enemies are between their windup and impact.</summary>
    protected bool AttackSlotFree
    {
        get
        {
            var max = Tunables.Combat.MaxConcurrentAttacks;
            if (max <= 0) return true;
            var busy = 0;
            foreach (var e in All(this))
                if (e != this && e.Current == State.Attack && e.Runner.Phase is AttackPhase.Windup or AttackPhase.Lead) busy++;
            return busy < max;
        }
    }
    protected int CircleSign => _circleSign;

    protected virtual Vector2 UpdateCircle(Vector2 toTarget, float dist, bool targetAlive)
    {
        if (!targetAlive) { _walkMs = 0; return Vector2.Zero; }
        Facing = toTarget.Normalized();
        if (dist > Tuning.CircleDistancePx * 1.8f) { Enter(State.Approach); return Vector2.Zero; }
        // A posture-broken player is punished at once with the fast counter.
        var punish = Target!.Current == Player.State.Broken && CounterAttack != null && dist <= Tuning.AttackRangePx * 1.5f;
        if (CircleDone || punish)
        {
            StartAttack(punish ? CounterAttack! : ChooseAttack());
            return Vector2.Zero;
        }
        _walkMs += Clock.DeltaMs;
        var tangent = Facing.Orthogonal() * _circleSign;
        var radial = Facing * Mathf.Clamp((dist - Tuning.CircleDistancePx) * 0.08f, -1f, 1f);
        return (tangent + radial).Normalized() * Tuning.CircleSpeed;
    }

    private Vector2 UpdateAttack(Vector2 toTarget, float dist, bool targetAlive, double now, double dt)
    {
        if (_attack == null) return Vector2.Zero;
        if (Runner.Phase == AttackPhase.Windup)
        {
            // Track the player until the glint, and step in if out of reach.
            if (targetAlive)
            {
                var want = toTarget.Normalized();
                var maxTurn = Mathf.DegToRad(Tuning.TurnSpeedDeg) * (float)(dt / 1000.0);
                var angle = Mathf.Clamp(Facing.AngleTo(want), -maxTurn, maxTurn);
                Facing = Facing.Rotated(angle);
                AimDir = Facing;
                if (!DrillMode && dist > Tuning.AttackRangePx)
                {
                    _walkMs += Clock.DeltaMs;
                    return SteerTo(Target!.GlobalPosition) * Tuning.MoveSpeed * 0.9f;
                }
            }
            _walkMs = 0;
            return Vector2.Zero;
        }
        if (Runner.Phase == AttackPhase.Lead && _swingStartMs >= 0 && _attack.SwingMs > 0 && !DrillMode && _attack.Projectile == "")
            return AimDir * (_attack.LungePx / (_attack.SwingMs / 1000f));
        return Vector2.Zero;
    }

    private void MoveBody(Vector2 desired, double dtMs)
    {
        var dt = (float)(dtMs / 1000.0);
        _knockback = _knockback.MoveToward(Vector2.Zero, Tunables.Feel.KnockbackDecay * dt);
        Velocity = (desired + _knockback) * (float)(dtMs / 1000.0 / GetPhysicsProcessDeltaTime());
        MoveAndSlide();
    }

    // ---- Attacks ------------------------------------------------------------------------

    protected void StartAttack(AttackData attack)
    {
        _attack = attack;
        _swingStartMs = -1;
        Runner.LeadTimes = CombatServices.LeadTimes;
        Runner.Start(attack.ToSpec(WindupFactor), Clock.NowMs, attack.SwingMs);
        Enter(State.Attack);
    }

    private void OnGlint(ThreatWindow threat)
    {
        AimDir = Facing;
        if (Target != null && IsInstanceValid(Target)) _areaTarget = Target.BodyGlobal;
        GlintFx.Spawn(threat.Glint == GlintColor.Red, () => _weapon.TipGlobal);
        Sfx.Play(threat.Glint == GlintColor.Red ? "glint_red" : "glint_white", 0f);
        AnyGlint?.Invoke(this, threat);
    }

    private void OnSwing(ThreatWindow threat)
    {
        _swingStartMs = Clock.NowMs;
        _shotRangePx = float.MaxValue;
        var a = _attack;
        if (a != null && a.Projectile != "")
        {
            Sfx.Play(a.Projectile == "arrow" ? "arrow_shot" : "swing", 0.1f, 0.8f);
            var from = _weapon.GlobalPosition;
            var along = Target != null && IsInstanceValid(Target) ? (Target.BodyGlobal - BodyGlobal).Dot(AimDir) : a.ReachPx;
            var to = a.IsArea ? _areaTarget : BodyGlobal + AimDir * Mathf.Clamp(along, 8, a.ReachPx);
            var stuck = false;
            if (!a.IsArea)
            {
                // Arrows stop at walls and obstacles.
                _shotRangePx = ShotRange(a.ReachPx);
                if (_shotRangePx < Mathf.Clamp(along, 8, a.ReachPx))
                {
                    to = BodyGlobal + AimDir * Mathf.Max(4, _shotRangePx);
                    stuck = true;
                }
            }
            _projectile = Projectile.Launch(a.Projectile, from, to, a.SwingMs, a.IsArea);
            _projectile.Stuck = stuck;
            return;
        }
        Sfx.Play("swing", 0.1f, 0.7f);
    }

    /// <summary>How far a shot along <see cref="AimDir"/> flies before a wall or obstacle stops it (at most <paramref name="reach"/>).</summary>
    public float ShotRange(float reach) => ShotRangeFor(AimDir, reach);

    /// <summary>The same along any direction (the aim line while it still tracks).</summary>
    public float ShotRangeFor(Vector2 dir, float reach)
    {
        if (Nav == null) return reach;
        var end = Nav.ShotEnd(BodyGlobal, BodyGlobal + dir * reach);
        return Mathf.Min(reach, BodyGlobal.DistanceTo(end));
    }

    /// <summary>A clear straight line from this enemy to the player's body (no wall or obstacle between).</summary>
    public bool SeesTarget()
    {
        if (Nav == null || Target == null || !IsInstanceValid(Target)) return true;
        return Nav.ShotEnd(BodyGlobal, Target.BodyGlobal).DistanceTo(Target.BodyGlobal) < 1f;
    }

    /// <summary>Whether the player's body is inside this attack's hit area at the impact.</summary>
    private bool InReach(AttackData attack, Player target)
    {
        var to = target.BodyGlobal - BodyGlobal;
        if (attack.IsArea) return target.BodyGlobal.DistanceTo(_areaTarget) <= attack.WidthPx + Player.Radius;
        if (attack.IsLine)
        {
            var along = to.Dot(AimDir);
            // A shot that met a wall or obstacle never reaches anything behind it.
            if (attack.Projectile != "" && along > _shotRangePx) return false;
            var across = Mathf.Abs(to.Dot(AimDir.Orthogonal()));
            return along >= -Player.Radius && along <= attack.ReachPx + Player.Radius && across <= attack.WidthPx + Player.Radius;
        }
        return to.Length() <= attack.ReachPx + Player.Radius &&
               (to.Length() < 6 || Mathf.Abs(AimDir.AngleTo(to)) <= Mathf.DegToRad(attack.ArcDeg / 2));
    }

    private void OnImpact(ThreatWindow threat)
    {
        Enter(State.Recover);
        var attack = _attack!;
        if (attack.Projectile == "")
        {
            var reachFx = BodyGlobal + AimDir * (attack.IsLine ? Mathf.Min(attack.ReachPx, 40) * 0.6f : attack.ReachPx * 0.55f);
            SheetFx.Spawn("slash_curved", 4, 28, reachFx, AimDir.Angle(), modulate: attack.Red ? new Color(1f, 0.55f, 0.5f) : null, z: 12);
        }
        var target = Target;
        if (target == null || !IsInstanceValid(target) || target.IsDead)
        {
            threat.Resolve(new ImpactState(false, false, false));
            _projectile?.Land(false, false);
            return;
        }
        var to = target.BodyGlobal - BodyGlobal;
        var inReach = InReach(attack, target);
        // Guard and deflect only cover the player's front arc; rolls evade from any direction.
        var toAttacker = -to;
        var angleDeg = toAttacker.LengthSquared() > 0.01f
            ? Mathf.RoundToInt(Mathf.RadToDeg(Mathf.Abs(target.Facing.AngleTo(toAttacker))))
            : 0;
        var covers = angleDeg <= Tunables.Player.GuardArcDeg / 2f;
        var r = threat.Resolve(new ImpactState(inReach, target.IsInvulnerable, target.IsGuarding, covers, angleDeg));
        var deflected = r.Response == Response.Deflect && r.Outcome == Outcome.Success;
        _projectile?.Land(inReach && r.Outcome == Outcome.Hit, inReach && (deflected || r.Outcome == Outcome.Blocked));
        _projectile = null;
        if (!inReach) return; // a whiff, or a right response that also left the reach: no contact
        var contact = attack.Projectile != "" ? target.BodyGlobal - to.Normalized() * 6 : BodyGlobal + to * 0.6f;
        target.ReceiveAttack(attack, r, to.LengthSquared() > 0 ? to.Normalized() : AimDir, contact);
        if (r.Response == Response.Deflect && r.Outcome == Outcome.Success) OnDeflected(attack);
        // The full opening after an impact is earned by defending: after a clean hit on the
        // player the bandit recovers faster.
        else if (r.Outcome == Outcome.Hit) Runner.ScaleRecovery(Tuning.RecoveryAfterHitFactor);
    }

    private void OnDeflected(AttackData attack)
    {
        if (attack.Projectile == "") _knockback = -AimDir * Tunables.Feel.KnockbackDeflect;
        _recoilUntilMs = Clock.NowMs + Tuning.DeflectedRecoilMs + 60;
        var factor = Target?.Mods.DeflectFactor ?? 1f;
        if (!DrillMode && Posture.Add(attack.DeflectPostureDamage * factor))
        {
            EnterKneel();
            return;
        }
        if (attack.Next == null) Runner.ExtendRecovery(Tuning.DeflectedRecoilMs);
    }

    private void OnFinished(AttackSpec spec)
    {
        if (Current != State.Recover) return;
        if (_attack?.Next != null)
        {
            StartAttack(_attack.Next);
            return;
        }
        _attack = null;
        Enter(State.Circle);
        _circleMs = DrillMode ? 0 : GD.RandRange(Tuning.CircleMinMs, Tuning.CircleMaxMs);
        _circleSign = GD.Randf() < 0.5f ? -1 : 1;
    }

    // ---- Being hit ----------------------------------------------------------------------

    /// <summary>A player's cut. <paramref name="dir"/> points from the player to this enemy.</summary>
    public HitResult TakeHit(int damage, int posture, Vector2 dir)
    {
        if (Current == State.Dead) return HitResult.Ignored;
        var feel = Tunables.Feel;
        _lastHitMs = Clock.NowMs;
        Flash(Colors.White, feel.FlashMs);
        if (Current is State.Kneel or State.Surrender)
        {
            if (Current == State.Kneel && !FinisherKills)
            {
                OnPhaseFinisher(dir);
                return HitResult.Finisher;
            }
            Die(dir, begging: Current == State.Surrender);
            return HitResult.Finisher;
        }

        if (IsBlocking)
        {
            Block(posture, dir);
            return Current == State.Kneel ? HitResult.Kneel : HitResult.Blocked;
        }

        if (Breakable) Health = Mathf.Max(0, Health - damage);
        Sfx.Play("enemy_hit", 0.08f);
        OnHurt();
        var armored = Current == State.Attack;
        _knockback = dir * feel.KnockbackEnemyHit * (armored ? 0.3f : 1f);
        // Trading into a windup is a poor deal: the hit lands but barely moves its posture,
        // and the bandit strikes at once (its glint shows next frame, with the usual lead).
        var postureDamage = armored ? posture * Tuning.ArmoredPostureFactor : posture;
        if (armored && Tuning.HitInWindupStrikesNow) Runner.SkipToGlint(Clock.NowMs);
        if (Posture.Add(postureDamage) || Health == 0)
        {
            EnterKneel();
            return HitResult.Kneel;
        }
        // A stagger is not extended by further hits; once it ends the bandit guards again.
        if (!armored && !DrillMode && Current != State.Stagger)
        {
            _staggerChain++;
            Runner.Abort();
            _attack = null;
            Enter(State.Stagger);
        }
        return HitResult.Hit;
    }

    /// <summary>A strike caught on the saber: reduced posture damage, no health damage; enough in a row and it counters.</summary>
    private void Block(int posture, Vector2 dir)
    {
        var t = Tuning;
        var now = Clock.NowMs;
        BlockedHits++;
        _blockChain = now - _lastBlockMs > t.BlockChainResetMs ? 1 : _blockChain + 1;
        _lastBlockMs = now;
        _guardPoseUntilMs = now + 220;
        Facing = -dir;
        _knockback = dir * Tunables.Feel.KnockbackEnemyHit * 0.4f;
        Sfx.Play("block", 0.08f);
        SparkFx.Block(BodyGlobal - dir * 5);
        if (Posture.Add(posture * t.BlockPostureFactor))
        {
            EnterKneel();
            return;
        }
        var counter = CounterAttack;
        if (counter != null && !DrillMode && t.BlocksBeforeCounter > 0 && _blockChain >= t.BlocksBeforeCounter)
        {
            _blockChain = 0;
            Counters++;
            AimDir = Facing;
            StartAttack(counter);
        }
    }

    /// <summary>After any health damage (the boss checks its phase).</summary>
    protected virtual void OnHurt() { }

    /// <summary>A finisher that does not kill (the boss's first): the subclass decides what follows.</summary>
    protected virtual void OnPhaseFinisher(Vector2 dir) { }

    /// <summary>Back on its feet at once (stagger state), with fresh posture.</summary>
    protected void Recover(float posture)
    {
        Posture.Reset(posture);
        Runner.Abort();
        _attack = null;
        _staggerChain = 0;
        Enter(State.Stagger);
    }

    protected void SetHealth(int health) => Health = Mathf.Clamp(health, 0, Tuning.MaxHealth);

    /// <summary>The player lets a begging enemy go: +1 merit.</summary>
    public bool Spare()
    {
        if (Current != State.Surrender) return false;
        WasSpared = true;
        Runner.Flush();
        _attack = null;
        Enter(State.Leave);
        _leaveFrom = GlobalPosition;
        _leaveFadeStartMs = -1;
        CollisionLayer = 0;
        CollisionMask = Player.LayerWorld;
        Sfx.Play("spare", 0.03f);
        Spared?.Invoke(this);
        return true;
    }

    private void EnterKneel()
    {
        if (!Breakable)
        {
            // Not yet: shrug it off.
            Posture.Reset(Posture.Max * 0.6f);
            Sfx.Play("block", 0.08f);
            SparkFx.Block(BodyGlobal);
            return;
        }
        Runner.Abort();
        _attack = null;
        var beg = DrillMode ? BegKind.None
            : BegRoll != null ? BegRoll(this)
            : SurrenderChance > 0 && Roll() < SurrenderChance ? BegKind.Genuine : BegKind.None;
        if (beg != BegKind.None)
        {
            IsFake = beg == BegKind.Fake;
            Enter(State.Surrender);
            Posture.Reset(Posture.Max);
            Sfx.Play("posture_break", 0.03f);
            Clock.HitStop(Tunables.Feel.HitStopDeflectMs);
            SheetFx.Spawn("smoke", 6, 18, BodyGlobal + new Vector2(0, 2), z: 10);
            DropWeapon();
            Surrendered?.Invoke(this);
            return;
        }
        Enter(State.Kneel);
        Posture.Reset(Posture.Max);
        Sfx.Play("posture_break", 0.03f);
        Clock.HitStop(Tunables.Feel.HitStopDeflectMs);
        ShakeCamera.Shake(Tunables.Feel.ShakeDeflect);
        SheetFx.Spawn("smoke", 6, 18, BodyGlobal + new Vector2(0, 2), z: 10);
        Kneeled?.Invoke(this);
    }

    private void DropWeapon()
    {
        var dropped = new Sprite2D
        {
            Texture = GD.Load<Texture2D>(WeaponPath),
            TextureFilter = TextureFilterEnum.Nearest,
            Rotation = Mathf.Pi / 2 + 0.3f,
            Modulate = new Color(0.85f, 0.85f, 0.85f),
            ZIndex = -2,
        };
        FxLayer.Add(dropped, GlobalPosition + new Vector2(10, -2));
    }

    private void Die(Vector2 dir, bool begging = false)
    {
        DiedBegging = begging;
        Runner.Flush();
        _attack = null;
        Enter(State.Dead);
        CollisionLayer = 0;
        CollisionMask = Player.LayerWorld;
        _knockback = dir * Tunables.Feel.KnockbackEnemyHit * 1.5f;
        Sfx.Play("finisher", 0.03f);
        Flash(new Color(1f, 0.3f, 0.25f), 220);
        SparkFx.Finisher(BodyGlobal);
        SheetFx.Spawn("cut_x", 4, 18, BodyGlobal, z: 16);
        Died?.Invoke(this);
    }

    /// <summary>Developer: dies at once, counted as a plain kill.</summary>
    public void Kill()
    {
        if (Current is State.Dead or State.Leave) return;
        Health = 0;
        Die(Vector2.Down);
    }

    private void Flash(Color c, double ms)
    {
        _flashColor = c;
        _flashUntilMs = Clock.NowMs + ms;
    }

    // ---- Visuals ------------------------------------------------------------------------

    private void UpdateVisuals(double now)
    {
        var col = CharacterSprite.Column(Current is State.Attack or State.Recover ? AimDir : Facing);
        var row = 0;
        var aimAngle = (Current is State.Attack or State.Recover ? AimDir : Facing).Angle();
        var side = Facing.Orthogonal();
        var (pivot, blade) = WeaponSprite.Rest(BodyCenter, Facing);
        _body.Position = Vector2.Zero;
        _body.Modulate = Colors.White;
        _weapon.Visible = true;

        switch (Current)
        {
            case State.Approach:
            case State.Circle:
                row = _walkMs > 0 ? (int)(_walkMs / 140) % 4 : 0;
                break;
            case State.Attack:
            {
                var a = _attack;
                var windup = Mathf.Max(1, Runner.Timeline.GlintMs);
                var elapsed = now - Runner.StartMs;
                if (Runner.Phase == AttackPhase.Windup || _swingStartMs < 0)
                {
                    // Raise the blade over the windup, then hold it cocked with a slight tremble.
                    var t = Mathf.Clamp((float)(elapsed / windup), 0f, 1f);
                    var ease = t * t * (3 - 2 * t);
                    var cocked = Mathf.DegToRad(a?.Id == "saber_chop" ? -175 : -120);
                    blade = aimAngle + Mathf.Lerp(Mathf.DegToRad(-150), cocked, ease);
                    pivot = BodyCenter + AimDir * 2 + side * 3 - new Vector2(0, 2 * ease);
                    if (Runner.Phase == AttackPhase.Lead) pivot += new Vector2(((int)(now / 50) % 2) * 0.5f, 0);
                    row = Runner.Phase == AttackPhase.Lead ? CharacterSprite.RowAttack : 0;
                }
                else
                {
                    var swing = Mathf.Max(1, a?.SwingMs ?? 1);
                    var t = Mathf.Clamp((float)((now - _swingStartMs) / swing), 0f, 1f);
                    var cocked = Mathf.DegToRad(a?.Id == "saber_chop" ? -175 : -120);
                    blade = aimAngle + Mathf.Lerp(cocked, Mathf.DegToRad(70), t);
                    pivot = BodyCenter + AimDir * 5;
                    row = CharacterSprite.RowAttack;
                }
                break;
            }
            case State.Recover:
                row = CharacterSprite.RowAttack;
                blade = aimAngle + Mathf.DegToRad(now < _recoilUntilMs ? -60 : 70);
                pivot = BodyCenter + AimDir * (now < _recoilUntilMs ? 2 : 5);
                break;
            case State.Stagger:
                _body.Position = new Vector2(Mathf.Round(Mathf.Sin((float)now * 0.08f)), 0);
                break;
            case State.Surrender:
                // Weapon dropped, bowing low: begging for its life.
                row = CharacterSprite.RowCrouch;
                col = 0;
                _weapon.Visible = false;
                _body.Position = new Vector2(0, (int)(now / 260) % 2);
                break;
            case State.Kneel:
                row = CharacterSprite.RowCrouch;
                col = 0;
                blade = Mathf.Pi / 2 - 0.5f;
                pivot = BodyCenter + new Vector2(-6, 5);
                break;
            case State.Leave:
                _weapon.Visible = false;
                if (StateElapsedMs < Tunables.Run.SpareStandMs)
                {
                    // Getting up slowly, still bowed.
                    row = StateElapsedMs < Tunables.Run.SpareStandMs * 0.5 ? CharacterSprite.RowCrouch : 0;
                    col = 0;
                }
                else row = _walkMs > 0 ? (int)(_walkMs / 160) % 4 : 0;
                if (LeaveFadeElapsed >= 0) _body.Modulate = new Color(1, 1, 1, Mathf.Clamp(1f - (float)LeaveFadeElapsed / 500f, 0f, 1f));
                break;
            case State.Dead:
                col = 0;
                row = CharacterSprite.RowMisc;
                _weapon.Visible = false;
                var fade = Mathf.Clamp(1f - (float)(StateElapsedMs - 500) / 700f, 0f, 1f);
                _body.Modulate = new Color(1, 1, 1, fade);
                break;
        }
        if (now < _guardPoseUntilMs && Current is State.Approach or State.Circle)
        {
            pivot = BodyCenter + Facing * 5 + side * 2;
            blade = Facing.Angle() - Mathf.Pi / 2 + 0.25f;
            row = CharacterSprite.RowAttack;
            col = CharacterSprite.Column(Facing);
        }
        if (_attack != null && Current is State.Attack or State.Recover && _attack.Projectile == "arrow")
        {
            // A bow held across the aim.
            pivot = BodyCenter + AimDir * 5;
            blade = aimAngle;
        }
        else if (_attack != null && Current == State.Attack && _attack.Projectile == "jar")
        {
            _weapon.Visible = false;
        }
        else if (_attack != null && Current is State.Attack or State.Recover && _attack.IsLine)
        {
            // A thrust: drawn back through the windup, driven along the aim in the strike.
            var reach = Runner.Phase == AttackPhase.Recovery ? 7f : _swingStartMs >= 0 ? 9f : -1f;
            pivot = BodyCenter + AimDir * reach + side * 2;
            blade = aimAngle;
        }
        _body.Pose(col, row);
        _body.Scale = new Vector2(SpriteScale, SpriteScale);
        _weapon.Point(pivot, blade);
        if (_offhand != null)
        {
            _offhand.Visible = _weapon.Visible;
            var off = new Vector2(-(pivot - BodyCenter).X, (pivot - BodyCenter).Y) + BodyCenter;
            _offhand.Point(off, Mathf.Pi - blade);
        }
        _body.SelfModulate = new Color(Tint.R, Tint.G, Tint.B, BodyAlpha);
        _telegraph.QueueRedraw();
        Shadow.PlaceWeapon(this, _weapon, _body, behind: (Current is State.Attack or State.Recover ? AimDir : Facing).Y < -0.3f);
        _body.SetFlash(now < _flashUntilMs ? 0.9f : 0f, _flashColor);
        _bars.QueueRedraw();
    }
}
