using System;
using Godot;

namespace SwordInTheForest.Core;

/// <summary>
/// Autoload "Clock": combat time in ms. Hit-stop freezes it while real time runs on.
/// Runs first in every physics tick, so all combat code in a tick sees the same time.
/// </summary>
public partial class GameClock : Node
{
    private double _hitStopMs;

    public static GameClock Instance { get; private set; } = null!;

    /// <summary>Game time in ms since start; frozen during hit-stop.</summary>
    public double NowMs { get; private set; }
    /// <summary>Game time that passed this physics tick (0 during hit-stop).</summary>
    public double DeltaMs { get; private set; }
    public bool Frozen => _hitStopMs > 0;
    public long Tick { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
        ProcessPhysicsPriority = -1000;
    }

    public override void _PhysicsProcess(double delta)
    {
        Tick++;
        var realMs = delta * 1000.0;
        if (_hitStopMs > 0)
        {
            _hitStopMs -= realMs;
            DeltaMs = 0;
            return;
        }
        DeltaMs = realMs;
        NowMs += realMs;
    }

    /// <summary>Freezes game time for <paramref name="ms"/> of real time, starting next tick.</summary>
    public void HitStop(double ms) => _hitStopMs = Math.Max(_hitStopMs, ms);
}
