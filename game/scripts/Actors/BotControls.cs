using System.Collections.Generic;
using Godot;

namespace SwordInTheForest.Actors;

/// <summary>Scripted controls for tests: presses scheduled at game times, held for a duration.</summary>
public sealed class BotControls : IPlayerControls
{
    private readonly List<(double At, PlayerAction Action, double HoldMs, Vector2? Move)> _scheduled = new();
    private Vector2? _pressMove;
    private double _pressMoveUntil;
    private readonly HashSet<PlayerAction> _justPressed = new();
    private readonly Dictionary<PlayerAction, double> _heldUntil = new();
    private readonly HashSet<PlayerAction> _holdForever = new();
    private double _now;

    public Vector2 MoveInput { get; set; }
    public Vector2 AimInput { get; set; }

    /// <summary>Schedules a press; <paramref name="move"/> optionally steers while it is held (e.g. a roll's direction).</summary>
    public void Press(PlayerAction action, double atMs, double holdMs = 50, Vector2? move = null) =>
        _scheduled.Add((atMs, action, holdMs, move));

    public int Pending => _scheduled.Count;

    public void HoldForever(PlayerAction action, bool hold)
    {
        if (hold) _holdForever.Add(action);
        else _holdForever.Remove(action);
    }

    public void Tick(double nowMs)
    {
        _now = nowMs;
        _justPressed.Clear();
        for (var i = _scheduled.Count - 1; i >= 0; i--)
        {
            var s = _scheduled[i];
            if (nowMs < s.At - 1e-6) continue;
            _scheduled.RemoveAt(i);
            _justPressed.Add(s.Action);
            _heldUntil[s.Action] = nowMs + s.HoldMs;
            if (s.Move != null)
            {
                _pressMove = s.Move;
                _pressMoveUntil = nowMs + s.HoldMs;
            }
        }
        if (_pressMove != null && nowMs >= _pressMoveUntil - 1e-6) _pressMove = null;
    }

    public Vector2 Move => _pressMove ?? MoveInput;
    public Vector2 Aim(Node2D player) => AimInput;
    public bool JustPressed(PlayerAction action) => _justPressed.Contains(action);

    public bool Held(PlayerAction action) =>
        _holdForever.Contains(action) || (_heldUntil.TryGetValue(action, out var until) && _now < until - 1e-6);
}
