using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Combat;
using SwordInTheForest.Core;
using SwordInTheForest.Fx;
using SwordInTheForest.Tuning;

namespace SwordInTheForest.Actors;

/// <summary>
/// Shared enemy body: health, posture, the attack runner, reactions (stagger, deflected recoil,
/// kneel, finisher, death) and visuals. Subclasses supply the brain: which attack to start, and how
/// to move between attacks. Enemies differ only in their attack data and their state machine.
/// </summary>
public abstract partial class Enemy : CharacterBody2D
{
    public enum State { Approach, Circle, Attack, Recover, Stagger, Kneel, Dead }
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

    public bool CanBeHit => Current != State.Dead;
    public bool IsKneeling => Current == State.Kneel;
    public Vector2 BodyGlobal => GlobalPosition + BodyCenter;
    public double StateElapsedMs => Clock.NowMs - _stateStartMs;

    protected abstract EnemyTuning Tuning { get; }
    protected abstract string SheetPath { get; }
    protected abstract string WeaponPath { get; }
    protected static GameClock Clock => GameClock.Instance;

    /// <summary>The brain: choose the next attack to start.</summary>
    protected abstract AttackData ChooseAttack();

    /// <summary>The fast attack it answers a string of blocked strikes with (null = none).</summary>
    protected virtual AttackData? CounterAttack => null;

    // Counters for the scenarios.
    public int BlockedHits { get; private set; }
    public int Counters { get; private set; }

    /// <summary>
    /// Blocks the player's strikes while it is free to (approaching or circling); open while it
    /// attacks, recovers from an attack (the player's opening), is staggered or kneels.
    /// </summary>
    public bool IsBlocking => Current is State.Approach or State.Circle;

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
        _body = new CharacterSprite(GD.Load<Texture2D>(SheetPath));
        AddChild(_body);
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
            case State.Dead:
                if (StateElapsedMs >= 1200) QueueFree();
                break;
        }

        Runner.Advance(now);
        MoveBody(velocity, dt);
        UpdateVisuals(now);
    }

    private Vector2 UpdateApproach(Vector2 toTarget, float dist, bool targetAlive)
    {
        if (!targetAlive) return Vector2.Zero;
        Facing = toTarget.Normalized();
        _walkMs += Clock.DeltaMs;
        if (dist <= Tuning.CircleDistancePx + 4)
        {
            Enter(State.Circle);
            _circleMs = GD.RandRange(Tuning.CircleMinMs, Tuning.CircleMaxMs);
            _circleSign = GD.Randf() < 0.5f ? -1 : 1;
            return Vector2.Zero;
        }
        return Facing * Tuning.MoveSpeed;
    }

    private double _circleMs;
    private int _circleSign = 1;

    private Vector2 UpdateCircle(Vector2 toTarget, float dist, bool targetAlive)
    {
        if (!targetAlive) { _walkMs = 0; return Vector2.Zero; }
        Facing = toTarget.Normalized();
        if (dist > Tuning.CircleDistancePx * 1.8f) { Enter(State.Approach); return Vector2.Zero; }
        // A posture-broken player is punished at once with the fast counter.
        var punish = Target!.Current == Player.State.Broken && CounterAttack != null && dist <= Tuning.AttackRangePx * 1.5f;
        if (StateElapsedMs >= _circleMs || punish)
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
                    return want * Tuning.MoveSpeed * 0.9f;
                }
            }
            _walkMs = 0;
            return Vector2.Zero;
        }
        if (Runner.Phase == AttackPhase.Lead && _swingStartMs >= 0 && _attack.SwingMs > 0 && !DrillMode)
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
        Runner.Start(attack.ToSpec(), Clock.NowMs, attack.SwingMs);
        Enter(State.Attack);
    }

    private void OnGlint(ThreatWindow threat)
    {
        AimDir = Facing;
        GlintFx.Spawn(threat.Glint == GlintColor.Red, () => _weapon.TipGlobal);
        Sfx.Play(threat.Glint == GlintColor.Red ? "glint_red" : "glint_white", 0f);
        AnyGlint?.Invoke(this, threat);
    }

    private void OnSwing(ThreatWindow threat)
    {
        _swingStartMs = Clock.NowMs;
        Sfx.Play("swing", 0.1f, 0.7f);
    }

    private void OnImpact(ThreatWindow threat)
    {
        Enter(State.Recover);
        var attack = _attack!;
        var reachFx = BodyGlobal + AimDir * (attack.ReachPx * 0.55f);
        SheetFx.Spawn("slash_curved", 4, 28, reachFx, AimDir.Angle(), modulate: attack.Red ? new Color(1f, 0.55f, 0.5f) : null, z: 12);
        var target = Target;
        if (target == null || !IsInstanceValid(target) || target.IsDead)
        {
            threat.Resolve(new ImpactState(false, false, false));
            return;
        }
        var to = target.BodyGlobal - BodyGlobal;
        var inReach = to.Length() <= attack.ReachPx + Player.Radius &&
                      (to.Length() < 6 || Mathf.Abs(AimDir.AngleTo(to)) <= Mathf.DegToRad(attack.ArcDeg / 2));
        // Guard and deflect only cover the player's front arc; rolls evade from any direction.
        var toAttacker = -to;
        var angleDeg = toAttacker.LengthSquared() > 0.01f
            ? Mathf.RoundToInt(Mathf.RadToDeg(Mathf.Abs(target.Facing.AngleTo(toAttacker))))
            : 0;
        var covers = angleDeg <= Tunables.Player.GuardArcDeg / 2f;
        var r = threat.Resolve(new ImpactState(inReach, target.IsInvulnerable, target.IsGuarding, covers, angleDeg));
        if (!inReach) return; // a whiff, or a right response that also left the reach: no contact
        var contact = BodyGlobal + to * 0.6f;
        target.ReceiveAttack(attack, r, to.LengthSquared() > 0 ? to.Normalized() : AimDir, contact);
        if (r.Response == Response.Deflect && r.Outcome == Outcome.Success) OnDeflected(attack);
        // The full opening after an impact is earned by defending: after a clean hit on the
        // player the bandit recovers faster.
        else if (r.Outcome == Outcome.Hit) Runner.ScaleRecovery(Tuning.RecoveryAfterHitFactor);
    }

    private void OnDeflected(AttackData attack)
    {
        _knockback = -AimDir * Tunables.Feel.KnockbackDeflect;
        _recoilUntilMs = Clock.NowMs + Tuning.DeflectedRecoilMs + 60;
        if (!DrillMode && Posture.Add(attack.DeflectPostureDamage))
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
        if (Current == State.Kneel)
        {
            Die(dir);
            return HitResult.Finisher;
        }

        if (IsBlocking)
        {
            Block(posture, dir);
            return Current == State.Kneel ? HitResult.Kneel : HitResult.Blocked;
        }

        Health = Mathf.Max(0, Health - damage);
        Sfx.Play("enemy_hit", 0.08f);
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

    private void EnterKneel()
    {
        Runner.Abort();
        _attack = null;
        Enter(State.Kneel);
        Posture.Reset(Posture.Max);
        Sfx.Play("posture_break", 0.03f);
        Clock.HitStop(Tunables.Feel.HitStopDeflectMs);
        ShakeCamera.Shake(Tunables.Feel.ShakeDeflect);
        SheetFx.Spawn("smoke", 6, 18, BodyGlobal + new Vector2(0, 2), z: 10);
        Kneeled?.Invoke(this);
    }

    private void Die(Vector2 dir)
    {
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
            case State.Kneel:
                row = CharacterSprite.RowCrouch;
                col = 0;
                blade = Mathf.Pi / 2 - 0.5f;
                pivot = BodyCenter + new Vector2(-6, 5);
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
        _body.Pose(col, row);
        _weapon.Point(pivot, blade);
        Shadow.PlaceWeapon(this, _weapon, _body, behind: (Current is State.Attack or State.Recover ? AimDir : Facing).Y < -0.3f);
        _body.SetFlash(now < _flashUntilMs ? 0.9f : 0f, _flashColor);
        _bars.QueueRedraw();
    }
}
