using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using SwordInTheForest.Actors;
using SwordInTheForest.Combat;
using SwordInTheForest.Core;

namespace SwordInTheForest.Scenarios;

/// <summary>
/// Drives the real player through the input map with simulated device events: first a gamepad
/// (stick movement and aim, X attack, LB guard, A roll, Y gourd, and an LB deflect 150 ms after a
/// glint), then keyboard and mouse (WASD, left click, right click, Space, R and a right-click
/// deflect). A real gamepad still needs a human check.
/// </summary>
public partial class InputDevicesScenario : Scenario
{
    private readonly SortedDictionary<int, List<Action>> _script = new();
    private Arena _arena = null!;
    private DeviceControls _device = null!;
    private Vector2 _mark;
    private int _sipsBefore;
    private double _deflectAt = -1;
    private Func<InputEvent>? _deflectPress;
    private Func<InputEvent>? _deflectRelease;
    private int _releaseAtFrame = -1;
    private int _deflectsBefore;

    protected override bool FailOnTimeout => true;
    private Player P => _arena.Player;

    private void At(int frame, Action a)
    {
        if (!_script.TryGetValue(frame, out var list)) _script[frame] = list = new List<Action>();
        list.Add(a);
    }

    private static void Send(InputEvent e) => Input.ParseInputEvent(e);
    private static InputEvent PadButton(JoyButton b, bool down) => new InputEventJoypadButton { Device = 0, ButtonIndex = b, Pressed = down };
    private static InputEvent PadAxis(JoyAxis a, float v) => new InputEventJoypadMotion { Device = 0, Axis = a, AxisValue = v };
    private static InputEvent KeyEvent(Key k, bool down) => new InputEventKey { PhysicalKeycode = k, Keycode = k, Pressed = down };
    private static InputEvent Mouse(MouseButton b, bool down) => new InputEventMouseButton { ButtonIndex = b, Pressed = down, ButtonMask = down ? (MouseButtonMask)(1 << ((int)b - 1)) : 0 };

    protected override void Step(int frame)
    {
        if (frame == 0)
        {
            _arena = new Arena { AutoSpawn = false, WithUi = true, PlayMusic = false };
            AddChild(_arena);
            BuildScript();
            Enemy.AnyGlint += OnGlint;
            return;
        }
        _device ??= P.GetChildren().OfType<DeviceControls>().Single();
        if (_deflectAt >= 0 && GameClock.Instance.NowMs >= _deflectAt - 1e-6)
        {
            _deflectAt = -1;
            Send(_deflectPress!());
            _releaseAtFrame = frame + 3;
        }
        if (frame == _releaseAtFrame) Send(_deflectRelease!());
        if (_script.TryGetValue(frame, out var actions)) foreach (var a in actions) a();
        if (frame > _script.Keys.Max())
        {
            Enemy.AnyGlint -= OnGlint;
            Pass();
        }
    }

    private void OnGlint(Enemy e, ThreatWindow t) => _deflectAt = t.GlintMs + 150;

    private void BuildScript()
    {
        // ---- Gamepad ----
        At(5, () => { _mark = P.GlobalPosition; Send(PadAxis(JoyAxis.LeftX, 1f)); });
        At(30, () =>
        {
            Check(_device.UsingPad, "a stick event should switch aim to the gamepad");
            Check(P.GlobalPosition.X - _mark.X > 20, $"left stick should move the player right, moved {P.GlobalPosition.X - _mark.X:0.0}");
            Send(PadAxis(JoyAxis.LeftX, 0f));
        });
        At(40, () => Send(PadAxis(JoyAxis.RightX, -1f)));
        At(46, () => Check(P.Facing.X < -0.9f, $"right stick should aim left, facing {P.Facing}"));
        At(47, () => Send(PadButton(JoyButton.X, true)));
        At(49, () => Check(P.Current == Player.State.Attack && P.ComboStep == 1, $"X should attack, state {P.Current}"));
        At(50, () => Send(PadButton(JoyButton.X, false)));
        At(51, () => Check(P.AttackDir.X < -0.9f, $"the attack should follow the right stick, dir {P.AttackDir}"));
        At(52, () => Send(PadAxis(JoyAxis.RightX, 0f)));
        At(90, () => Send(PadButton(JoyButton.LeftShoulder, true)));
        At(93, () => Check(P.Current == Player.State.Guard, $"held LB should guard, state {P.Current}"));
        At(110, () => Check(P.Current == Player.State.Guard, "guard should hold while LB is held"));
        At(111, () => Send(PadButton(JoyButton.LeftShoulder, false)));
        At(114, () => Check(P.Current == Player.State.Free, $"releasing LB should end the guard, state {P.Current}"));
        At(120, () => Send(PadButton(JoyButton.A, true)));
        At(122, () => Check(P.Current == Player.State.Roll, $"A should roll, state {P.Current}"));
        At(123, () => Send(PadButton(JoyButton.A, false)));
        At(160, () => { _sipsBefore = P.Sips; Send(PadButton(JoyButton.Y, true)); });
        At(162, () => Check(P.Current == Player.State.Drink && P.Sips == _sipsBefore - 1, $"Y should drink, state {P.Current}"));
        At(163, () => Send(PadButton(JoyButton.Y, false)));
        At(240, () => SpawnChop(() => PadButton(JoyButton.LeftShoulder, true), () => PadButton(JoyButton.LeftShoulder, false)));
        At(400, () => CheckDeflect("gamepad LB"));

        // ---- Keyboard and mouse ----
        At(410, () => { _mark = P.GlobalPosition; Send(KeyEvent(Key.W, true)); });
        At(430, () =>
        {
            Check(!_device.UsingPad, "a key event should switch back to keyboard and mouse");
            Check(_mark.Y - P.GlobalPosition.Y > 15, $"W should move the player up, moved {_mark.Y - P.GlobalPosition.Y:0.0}");
            Send(KeyEvent(Key.W, false));
        });
        At(440, () => Send(Mouse(MouseButton.Left, true)));
        At(442, () => Check(P.Current == Player.State.Attack, $"left click should attack, state {P.Current}"));
        At(443, () => Send(Mouse(MouseButton.Left, false)));
        At(480, () => Send(Mouse(MouseButton.Right, true)));
        At(483, () => Check(P.Current == Player.State.Guard, $"held right click should guard, state {P.Current}"));
        At(484, () => Send(Mouse(MouseButton.Right, false)));
        At(490, () => Send(KeyEvent(Key.Space, true)));
        At(492, () => Check(P.Current == Player.State.Roll, $"Space should roll, state {P.Current}"));
        At(493, () => Send(KeyEvent(Key.Space, false)));
        At(540, () => { _sipsBefore = P.Sips; Send(KeyEvent(Key.R, true)); });
        At(542, () => Check(P.Current == Player.State.Drink && P.Sips == _sipsBefore - 1, $"R should drink, state {P.Current}"));
        At(543, () => Send(KeyEvent(Key.R, false)));
        At(620, () => SpawnChop(() => Mouse(MouseButton.Right, true), () => Mouse(MouseButton.Right, false)));
        At(780, () => CheckDeflect("right click"));

        // ---- F1 tuning panel ----
        At(790, () => Send(KeyEvent(Key.F1, true)));
        At(792, () => { Check(_arena.Tuning!.IsOpen && GetTree().Paused, "F1 should open the tuning panel and pause"); Send(KeyEvent(Key.F1, false)); });
        At(794, () => Send(KeyEvent(Key.F1, true)));
        At(796, () => { Check(!_arena.Tuning!.IsOpen && !GetTree().Paused, "F1 again should close the panel"); Send(KeyEvent(Key.F1, false)); });
    }

    private void SpawnChop(Func<InputEvent> press, Func<InputEvent> release)
    {
        _deflectPress = press;
        _deflectRelease = release;
        _deflectsBefore = P.Deflects;
        foreach (var old in _arena.Enemies.ToList()) old.QueueFree();
        var bandit = _arena.Spawn<SaberBandit>(P.GlobalPosition + new Vector2(0, -22));
        bandit.DrillMode = true;
        bandit.Drill(Tunables.Attack("saber_chop"));
    }

    private void CheckDeflect(string device)
    {
        Check(P.Deflects == _deflectsBefore + 1, $"{device} 150 ms after the glint should deflect; deflects {P.Deflects - _deflectsBefore}, hits {P.HitsTaken}");
        var last = Telemetry.Instance.Lines.LastOrDefault() ?? "";
        Check(last.Contains("\"response\":\"deflect\"") && last.Contains("\"outcome\":\"success\""), $"telemetry should log the deflect: {last}");
        GD.Print($"  {device}: deflect ok");
    }
}
