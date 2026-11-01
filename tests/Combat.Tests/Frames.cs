namespace Combat.Tests;

/// <summary>Frame times at the game's 60 Hz physics rate.</summary>
internal static class Frames
{
    public const int Hz = 60;
    public static double Ms(int frame) => CombatTime.FrameMs(frame, Hz);
    /// <summary>The first frame whose time is at or after <paramref name="ms"/>.</summary>
    public static int AtOrAfter(double ms) => (int)Math.Ceiling(ms * Hz / 1000.0 - 1e-9);
}
