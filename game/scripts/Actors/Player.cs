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
/// The wandering soul. A state machine (free, attack, guard, deflect, roll, hurt, broken, drink,
/// dead) driven by <see cref="IPlayerControls"/>, with a ~100 ms input buffer.
///
/// Defence follows the spec's response window: pressing guard while an attack's glint is showing
/// (between glint and impact) enters the deflect stance, which catches that attack at its impact;
/// a guard raised before the glint only blocks; a roll started in the window evades.
/// </summary>
public partial class Player : CharacterBody2D, IThreatTarget
{
    public enum State { Free, Attack, Guard, Deflect, Roll, Hurt, Broken, Drink, Dead }

    /// <summary>The body's centre relative to the feet (the node origin).</summary>
    public static readonly Vector2 BodyCenter = new(0, -7);
    public const float Radius = 6f;
    public const uint LayerWorld = 1;
    public const uint LayerPlayer = 2;
    public const uint LayerEnemy = 4;

    private const string ActorDir = "res://assets/third_party/ninja_adventure/actors";
    private const string WeaponDir = "res://assets/third_party/ninja_adventure/weapons";
    private const double DeflectTailMs = 90;
    private const double DeflectMaxMs = 1600;

    private readonly List<ThreatWindow> _threats = new();
    private readonly HashSet<ThreatWindow> _deflecting = new();
    private InputBuffer<PlayerAction> _buffer = new(100);
    private CharacterSprite _body = null!;
    private WeaponSprite _weapon = null!;
    private double _stateStartMs;
    private double _lastDeflectInputMs;
    private bool _attackHitDone;
    private bool _drinkHealed;
    private bool _swingSoundDone;
    private Vector2 _rollDir = Vector2.Down;
    private Vector2 _knockback;
    private double _flashUntilMs;
    private Color _flashColor = Colors.White;
    private double _walkMs;
    private double _lastAfterImageMs;
    private double _deflectFlashUntilMs;
    private double _lastGuardPressMs = -1e9;
    private double _prevGuardPressMs = -1e9;

    public IPlayerControls Controls { get; set; } = null!;
    public State Current { get; private set; } = State.Free;
    public int Health { get; private set; }
    public int Sips { get; private set; }
    public PostureMeter Posture { get; } = new(100);
    public Vector2 Facing { get; private set; } = Vector2.Down;
    public Vector2 AttackDir { get; private set; } = Vector2.Down;
    public int ComboStep { get; private set; }

    // Counters for the HUD and the scenarios.
    public int HitsTaken { get; private set; }
    public int Deflects { get; private set; }
    public int Blocks { get; private set; }
    public int Finishers { get; private set; }
    public int StrikesBlocked { get; private set; }

    public event Action<Player>? Died;

    public IReadOnlyList<ThreatWindow> OpenThreats => _threats;
    public double StateElapsedMs => Clock.NowMs - _stateStartMs;
    public bool IsDead => Current == State.Dead;
    public bool IsGuarding => Current is State.Guard or State.Deflect;
    public bool IsInvulnerable => Current == State.Roll && RollTimeline.IsInvulnerable(StateElapsedMs);
    public Vector2 BodyGlobal => GlobalPosition + BodyCenter;

    private static PlayerTuning T => Tunables.Player;
    private static GameClock Clock => GameClock.Instance;
    private static RollTimeline RollTimeline => new(T.RollDurationMs, T.RollInvulnStartMs, T.RollInvulnEndMs);

    public override void _Ready()
    {
        ProcessPhysicsPriority = -10;
        CollisionLayer = LayerPlayer;
        CollisionMask = LayerWorld | LayerEnemy;
        MotionMode = MotionModeEnum.Floating;
        AddChild(new CollisionShape2D { Shape = new CircleShape2D { Radius = Radius }, Position = new Vector2(0, -3) });
        AddChild(new Shadow());
        _weapon = new WeaponSprite(GD.Load<Texture2D>($"{WeaponDir}/katana_in_hand.png"));
        AddChild(_weapon);
        _body = new CharacterSprite(GD.Load<Texture2D>($"{ActorDir}/ninja_gray.png"));
        AddChild(_body);
        if (Controls == null)
        {
            var device = new DeviceControls();
            AddChild(device);
            Controls = device;
        }
        Revive();
    }

    /// <summary>Full health, posture and sips; back to the free state.</summary>
    public void Revive()
    {
        Health = T.MaxHealth;
        Sips = T.GourdSips;
        Posture.Max = T.MaxPosture;
        Posture.Reset();
        _knockback = Vector2.Zero;
        Enter(State.Free);
    }

    /// <summary>Refills health and posture without changing state (test drills).</summary>
    public void RefillForDrill()
    {
        Health = T.MaxHealth;
        Posture.Reset();
        if (Current is State.Dead or State.Broken or State.Hurt) Enter(State.Free);
    }

    // ---- IThreatTarget -------------------------------------------------------------------

    public void OpenThreat(ThreatWindow threat) => _threats.Add(threat);

    public void CloseThreat(ThreatWindow threat)
    {
        _threats.Remove(threat);
        _deflecting.Remove(threat);
    }

    // ---- Main loop ----------------------------------------------------------------------

    public override void _PhysicsProcess(double delta)
    {
        var now = Clock.NowMs;
        var dt = Clock.DeltaMs;
        Controls.Tick(now);
        _buffer.WindowMs = T.InputBufferMs;
        ReadPresses(now);
        if (dt <= 0)
        {
            UpdateVisuals(now);
            return;
        }

        Posture.Max = T.MaxPosture;
        Posture.Tick(dt, T.PostureDrainPerSec, T.PostureDrainDelayMs, holdDrain: IsGuarding);

        var velocity = Vector2.Zero;
        switch (Current)
        {
            case State.Free: velocity = UpdateFree(now); break;
            case State.Guard: velocity = UpdateGuard(now); break;
            case State.Deflect: velocity = UpdateDeflect(now); break;
            case State.Attack: velocity = UpdateAttack(now); break;
            case State.Roll: velocity = UpdateRoll(now); break;
            case State.Hurt:
                if (StateElapsedMs >= T.HurtMs) Enter(State.Free);
                break;
            case State.Broken:
                if (StateElapsedMs >= T.BrokenMs)
                {
                    Posture.Reset();
                    Enter(State.Free);
                }
                break;
            case State.Drink: velocity = UpdateDrink(now); break;
        }

        Move(velocity, dt);
        UpdateVisuals(now);
    }

    private void ReadPresses(double now)
    {
        foreach (PlayerAction a in Enum.GetValues(typeof(PlayerAction)))
        {
            if (!Controls.JustPressed(a)) continue;
            _buffer.Press(a, now);
            if (a == PlayerAction.Guard)
            {
                _prevGuardPressMs = _lastGuardPressMs;
                _lastGuardPressMs = now;
            }
            if (a is PlayerAction.Guard or PlayerAction.Roll)
                foreach (var t in _threats) t.RecordPress(now);
        }
    }

    private void Move(Vector2 desired, double dtMs)
    {
        var dt = (float)(dtMs / 1000.0);
        if (Current is State.Roll or State.Attack or State.Deflect)
            Velocity = desired;
        else
        {
            var rate = desired.LengthSquared() > Velocity.LengthSquared() ? T.Acceleration : T.Friction;
            Velocity = Velocity.MoveToward(desired, rate * dt);
        }
        _knockback = _knockback.MoveToward(Vector2.Zero, Tunables.Feel.KnockbackDecay * dt);
        var saved = Velocity;
        Velocity += _knockback;
        // MoveAndSlide integrates with the physics delta; scale so game time drives it.
        var scale = (float)(dtMs / 1000.0 / GetPhysicsProcessDeltaTime());
        Velocity *= scale;
        MoveAndSlide();
        Velocity = saved;
    }

    // ---- States -------------------------------------------------------------------------

    private void Enter(State s)
    {
        if (Current == State.Roll && s != State.Roll) CollisionMask = LayerWorld | LayerEnemy;
        Current = s;
        _stateStartMs = Clock.NowMs;
        if (s != State.Attack) ComboStep = 0;
    }

    private Vector2 AimOrFacing()
    {
        var aim = Controls.Aim(this);
        if (aim != Vector2.Zero) return aim;
        var move = Controls.Move;
        return move.LengthSquared() > 0.04f ? move.Normalized() : Facing;
    }

    /// <summary>
    /// The aim (mouse or right stick, else movement, else facing), nudged toward the nearest
    /// enemy in front: a wide cone when aiming by movement or facing, a narrow one otherwise.
    /// Free-angle, so diagonals and everything between work.
    /// </summary>
    public Vector2 AssistedAim()
    {
        var explicitAim = Controls.Aim(this) != Vector2.Zero;
        var aim = AimOrFacing();
        var halfCone = Mathf.DegToRad((explicitAim ? T.AimAssistExplicitDeg : T.AimAssistDeg) / 2f);
        Enemy? best = null;
        var bestDist = float.MaxValue;
        foreach (var e in Enemy.All(this))
        {
            if (!e.CanBeHit) continue;
            var to = e.BodyGlobal - BodyGlobal;
            var d = to.Length();
            if (d > T.AimAssistRangePx + Enemy.Radius || d < 0.01f) continue;
            if (Mathf.Abs(aim.AngleTo(to)) > halfCone) continue;
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best != null ? (best.BodyGlobal - BodyGlobal).Normalized() : aim;
    }

    private bool AnyThreatInWindow(double now) => _threats.Any(t => t.Result == null && t.InWindow(now));

    private bool TryRoll(double now)
    {
        if (!_buffer.TryConsume(PlayerAction.Roll, now)) return false;
        var move = Controls.Move;
        _rollDir = move.LengthSquared() > 0.04f ? move.Normalized() : Facing;
        Facing = _rollDir;
        foreach (var t in _threats.Where(t => t.Result == null)) t.RecordAction(DefenseAction.Roll, now);
        _deflecting.Clear();
        CollisionMask = LayerWorld;
        Enter(State.Roll);
        Sfx.Play("roll", 0.08f, 0.7f);
        SheetFx.Spawn("smoke_circular", 8, 30, GlobalPosition + new Vector2(0, -2), z: -1);
        return true;
    }

    /// <summary>Mashing guard (a press right after the previous one) only raises a block.</summary>
    private bool Mashed => _lastGuardPressMs - _prevGuardPressMs < T.DeflectMashLockoutMs;

    private bool TryGuard(double now)
    {
        if (!_buffer.TryConsume(PlayerAction.Guard, now)) return false;
        if (AnyThreatInWindow(now) && !Mashed) EnterDeflect(now);
        else Enter(State.Guard);
        return true;
    }

    private void EnterDeflect(double now)
    {
        foreach (var t in _threats.Where(t => t.Result == null && t.InWindow(now)))
        {
            t.RecordAction(DefenseAction.Guard, now);
            _deflecting.Add(t);
        }
        _lastDeflectInputMs = now;
        Facing = AssistedAim();
        if (Current != State.Deflect) Enter(State.Deflect);
    }

    private Vector2 UpdateFree(double now)
    {
        if (TryRoll(now) || TryGuard(now)) return Vector2.Zero;
        if (Controls.Held(PlayerAction.Guard))
        {
            Enter(State.Guard);
            return Vector2.Zero;
        }
        if (_buffer.TryConsume(PlayerAction.Attack, now))
        {
            EnterAttack(1, now);
            return Vector2.Zero;
        }
        if (_buffer.TryConsume(PlayerAction.Drink, now) && Sips > 0)
        {
            Sips--;
            _drinkHealed = false;
            Enter(State.Drink);
            Sfx.Play("drink", 0.05f, 0.8f);
            return Vector2.Zero;
        }
        var move = Controls.Move;
        if (move.LengthSquared() > 0.04f)
        {
            Facing = move.Normalized();
            _walkMs += Clock.DeltaMs;
        }
        else
        {
            _walkMs = 0;
            var aim = Controls.Aim(this);
            if (aim != Vector2.Zero) Facing = aim;
        }
        return move * T.MoveSpeed;
    }

    private Vector2 UpdateGuard(double now)
    {
        if (TryRoll(now) || TryGuard(now)) return Vector2.Zero;
        if (!Controls.Held(PlayerAction.Guard))
        {
            Enter(State.Free);
            return Vector2.Zero;
        }
        if (_buffer.TryConsume(PlayerAction.Attack, now))
        {
            EnterAttack(1, now);
            return Vector2.Zero;
        }
        Facing = AssistedAim();
        return Controls.Move * T.GuardMoveSpeed;
    }

    private Vector2 UpdateDeflect(double now)
    {
        if (TryRoll(now)) return Vector2.Zero;
        if (_buffer.TryConsume(PlayerAction.Guard, now) && !Mashed) EnterDeflect(now);
        var pending = _deflecting.Any(t => t.Result == null);
        var lastResolved = Mathf.Max(_lastDeflectInputMs, _deflectFlashUntilMs - 120);
        if ((!pending && now - lastResolved >= DeflectTailMs) || StateElapsedMs >= DeflectMaxMs)
        {
            _deflecting.Clear();
            Enter(Controls.Held(PlayerAction.Guard) ? State.Guard : State.Free);
        }
        return Vector2.Zero;
    }

    private (int Windup, int Recovery, int Damage, int Posture) ComboNumbers(int step) => step switch
    {
        1 => (T.Attack1WindupMs, T.Attack1RecoveryMs, T.Attack1Damage, T.Attack1Posture),
        2 => (T.Attack2WindupMs, T.Attack2RecoveryMs, T.Attack2Damage, T.Attack2Posture),
        _ => (T.Attack3WindupMs, T.Attack3RecoveryMs, T.Attack3Damage, T.Attack3Posture),
    };

    private void EnterAttack(int step, double now)
    {
        var aim = AssistedAim();
        AttackDir = aim;
        Facing = aim;
        Enter(State.Attack);
        ComboStep = step;
        _attackHitDone = false;
        _swingSoundDone = false;
        // Stamina is posture: every swing costs a little, so endless spam breaks your own guard.
        if (Posture.Add(T.AttackPostureCost)) BreakPosture();
    }

    private Vector2 UpdateAttack(double now)
    {
        var (windup, recovery, damage, posture) = ComboNumbers(ComboStep);
        var elapsed = StateElapsedMs;
        if (!_swingSoundDone)
        {
            _swingSoundDone = true;
            Sfx.Play("swing", 0.1f, 0.6f);
        }
        if (!_attackHitDone && elapsed >= windup)
        {
            _attackHitDone = true;
            DoAttackHit(damage, posture);
        }
        if (elapsed >= windup + T.ComboCancelFromMs)
        {
            if (TryRoll(now) || TryGuard(now)) return Vector2.Zero;
            if (ComboStep < 3 && _buffer.TryConsume(PlayerAction.Attack, now))
            {
                EnterAttack(ComboStep + 1, now);
                return Vector2.Zero;
            }
        }
        if (elapsed >= windup + recovery)
        {
            Enter(State.Free);
            return Vector2.Zero;
        }
        // A short lunge through the windup and the first frames after the cut.
        var lungeMs = Mathf.Max(windup, 60);
        return elapsed < lungeMs ? AttackDir * (T.AttackLungePx / (lungeMs / 1000f)) : Vector2.Zero;
    }

    private void DoAttackHit(int damage, int posture)
    {
        var origin = BodyGlobal;
        var halfArc = Mathf.DegToRad(T.AttackArcDeg / 2);
        var anyHit = false;
        var blocked = false;
        var finisher = false;
        foreach (var e in Enemy.All(this))
        {
            if (!e.CanBeHit) continue;
            var to = e.BodyGlobal - origin;
            if (to.Length() > T.AttackReachPx + Enemy.Radius) continue;
            if (to.Length() > 6 && Mathf.Abs(AttackDir.AngleTo(to)) > halfArc) continue;
            var result = e.TakeHit(damage, posture, AttackDir);
            if (result == Enemy.HitResult.Ignored) continue;
            if (result == Enemy.HitResult.Blocked)
            {
                blocked = true;
                continue;
            }
            anyHit = true;
            if (result == Enemy.HitResult.Finisher) { finisher = true; Finishers++; }
            SparkFx.Hit(origin + to * 0.7f);
        }

        var flip = ComboStep == 2;
        var slashAt = origin + AttackDir * 12;
        var big = ComboStep == 3 || finisher;
        var fx = SheetFx.Spawn("slash_arc", 6, big ? 26 : 34, slashAt, AttackDir.Angle() - Mathf.Pi / 2, flip, z: 15);
        if (!big) fx.Scale = new Vector2(0.8f, 0.8f);
        var feel = Tunables.Feel;
        if (finisher)
        {
            Clock.HitStop(feel.HitStopFinisherMs);
            ShakeCamera.Shake(feel.ShakeFinisher);
        }
        else if (anyHit)
        {
            Clock.HitStop(feel.HitStopHitMs);
            ShakeCamera.Shake(feel.ShakeHit);
        }
        else if (blocked)
        {
            // Caught on the bandit's saber: a short jolt back.
            Clock.HitStop(feel.HitStopBlockMs);
            ShakeCamera.Shake(feel.ShakeBlock);
            _knockback = -AttackDir * feel.KnockbackBlock * 0.6f;
            StrikesBlocked++;
        }
    }

    private Vector2 UpdateRoll(double now)
    {
        var elapsed = StateElapsedMs;
        if (RollTimeline.IsOver(elapsed))
        {
            Enter(State.Free);
            return Vector2.Zero;
        }
        if (now - _lastAfterImageMs >= 45)
        {
            _lastAfterImageMs = now;
            AfterImage.From(_body, new Color(0.75f, 0.85f, 1f));
        }
        // Fast start, eased stop; the profile 1.6·(1−x)^0.6 averages to 1 over the roll.
        var x = (float)(elapsed / T.RollDurationMs);
        var baseSpeed = T.RollDistancePx / (T.RollDurationMs / 1000f);
        return _rollDir * baseSpeed * 1.6f * Mathf.Pow(Mathf.Max(0f, 1f - x), 0.6f);
    }

    private Vector2 UpdateDrink(double now)
    {
        if (!_drinkHealed && StateElapsedMs >= T.DrinkHealAtMs)
        {
            _drinkHealed = true;
            Health = Mathf.Min(T.MaxHealth, Health + T.GourdHeal);
            Sfx.Play("heal", 0.02f);
            Flash(new Color(0.6f, 1f, 0.6f), 160);
        }
        if (StateElapsedMs >= T.DrinkMs)
        {
            Enter(State.Free);
            return Vector2.Zero;
        }
        return Controls.Move * T.DrinkMoveSpeed;
    }

    // ---- Being attacked -------------------------------------------------------------------

    /// <summary>Applies a resolved enemy attack. <paramref name="dir"/> points from the attacker to the player.</summary>
    public void ReceiveAttack(AttackData data, Resolution r, Vector2 dir, Vector2 contact)
    {
        if (IsDead) return;
        var feel = Tunables.Feel;
        switch (r.Outcome)
        {
            case Outcome.Success when r.Response == Response.Deflect:
                Deflects++;
                _deflectFlashUntilMs = Clock.NowMs + 120;
                SparkFx.Deflect(contact);
                Sfx.Play("deflect", 0.06f);
                Clock.HitStop(feel.HitStopDeflectMs);
                ShakeCamera.Shake(feel.ShakeDeflect);
                _knockback = dir * feel.KnockbackDeflect * 0.35f;
                if (Posture.Add(data.PostureDamage * T.DeflectPostureFactor)) BreakPosture();
                break;
            case Outcome.Blocked:
                Blocks++;
                _knockback = dir * feel.KnockbackBlock;
                if (Posture.Add(data.PostureDamage))
                {
                    BreakPosture();
                    break;
                }
                SparkFx.Block(contact);
                Sfx.Play("block", 0.06f);
                Clock.HitStop(feel.HitStopBlockMs);
                ShakeCamera.Shake(feel.ShakeBlock);
                break;
            case Outcome.Hit:
                TakeDamage(data.Damage, data.PostureDamage * T.HitPostureFactor, dir);
                break;
        }
    }

    private void TakeDamage(int damage, float posture, Vector2 dir)
    {
        var feel = Tunables.Feel;
        HitsTaken++;
        Health = Mathf.Max(0, Health - damage);
        Flash(new Color(1f, 0.25f, 0.2f), feel.FlashMs * 1.5);
        Sfx.Play("player_hurt", 0.05f);
        Clock.HitStop(feel.HitStopPlayerHurtMs);
        ShakeCamera.Shake(feel.ShakePlayerHurt);
        _knockback = dir * feel.KnockbackPlayerHurt;
        SparkFx.Hit(BodyGlobal);
        _deflecting.Clear();
        if (Health <= 0)
        {
            Enter(State.Dead);
            CollisionLayer = 0;
            Died?.Invoke(this);
            return;
        }
        if (Posture.Add(posture)) BreakPosture();
        else Enter(State.Hurt);
    }

    private void BreakPosture()
    {
        _deflecting.Clear();
        Enter(State.Broken);
        Sfx.Play("posture_break", 0.03f);
        Clock.HitStop(Tunables.Feel.HitStopDeflectMs);
        ShakeCamera.Shake(Tunables.Feel.ShakePlayerHurt);
        Flash(new Color(1f, 0.8f, 0.3f), 200);
        SheetFx.Spawn("smoke", 6, 20, BodyGlobal, z: 10);
    }

    /// <summary>Brings a dead player back where they stand (arena respawn).</summary>
    public void Respawn(Vector2 at)
    {
        GlobalPosition = at;
        CollisionLayer = LayerPlayer;
        Revive();
    }

    private void Flash(Color c, double ms)
    {
        _flashColor = c;
        _flashUntilMs = Clock.NowMs + ms;
    }

    // ---- Visuals -------------------------------------------------------------------------

    private void UpdateVisuals(double now)
    {
        var col = CharacterSprite.Column(Facing);
        var row = 0;
        var aimAngle = Facing.Angle();
        var side = Facing.Orthogonal(); // the sword hand side
        var (weaponPivot, bladeAngle) = WeaponSprite.Rest(BodyCenter, Facing);
        _body.Position = Vector2.Zero;
        _body.Modulate = Colors.White;
        _weapon.Visible = true;

        switch (Current)
        {
            case State.Free:
                row = _walkMs > 0 ? (int)(_walkMs / 125) % 4 : 0;
                break;
            case State.Attack:
            {
                row = CharacterSprite.RowAttack;
                var windup = ComboNumbers(ComboStep).Windup;
                var t = Mathf.Clamp((float)(StateElapsedMs / Mathf.Max(windup, 1)), 0f, 1f);
                var ease = 1f - Mathf.Pow(1f - t, 3);
                var sign = ComboStep == 2 ? -1f : 1f;
                var from = Mathf.DegToRad(ComboStep == 3 ? -130 : -100) * sign;
                var to = Mathf.DegToRad(ComboStep == 3 ? 110 : 80) * sign;
                bladeAngle = aimAngle + Mathf.Lerp(from, to, ease);
                weaponPivot = BodyCenter + AttackDir * 4;
                break;
            }
            case State.Guard:
            case State.Deflect:
            {
                var push = Current == State.Deflect ? 6 : 4;
                weaponPivot = BodyCenter + Facing * push + side * 3;
                bladeAngle = aimAngle - Mathf.Pi / 2 - 0.25f;
                row = Current == State.Deflect ? CharacterSprite.RowAttack : 0;
                break;
            }
            case State.Roll:
                row = CharacterSprite.RowCrouch;
                _weapon.Visible = false;
                _body.Position = new Vector2(0, -Mathf.Round(3 * Mathf.Sin(Mathf.Pi * (float)(StateElapsedMs / T.RollDurationMs))));
                if (IsInvulnerable) _body.Modulate = new Color(0.8f, 0.9f, 1f, 0.75f);
                break;
            case State.Hurt:
                _body.Position = new Vector2(Mathf.Round(Mathf.Sin((float)now * 0.09f)), 0);
                break;
            case State.Broken:
                row = CharacterSprite.RowCrouch;
                bladeAngle = Mathf.Pi / 2 + 0.6f;
                weaponPivot = BodyCenter + new Vector2(5, 4);
                _body.Modulate = ((int)(now / 90) % 2 == 0) ? Colors.White : new Color(1f, 0.85f, 0.6f);
                break;
            case State.Drink:
                col = 0;
                row = CharacterSprite.RowMisc;
                _weapon.Visible = false;
                break;
            case State.Dead:
                col = 0;
                row = CharacterSprite.RowMisc;
                _weapon.Visible = false;
                _body.Modulate = new Color(0.7f, 0.7f, 0.75f);
                break;
        }
        if (Current == State.Drink) _body.Frame = CharacterSprite.RowMisc * 4 + 1;
        else _body.Pose(col, row);

        _weapon.Point(weaponPivot, bladeAngle);
        Shadow.PlaceWeapon(this, _weapon, _body, behind: Facing.Y < -0.3f);
        _weapon.Modulate = now < _deflectFlashUntilMs ? new Color(1.6f, 1.5f, 1.1f) : Colors.White;

        var flashing = now < _flashUntilMs;
        _body.SetFlash(flashing ? 0.6f : 0f, _flashColor);
    }
}
