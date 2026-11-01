namespace SwordInTheForest.Combat
{
    /// <summary>Shared time helpers. Combat time is in milliseconds as doubles; frames at 60 Hz are not whole numbers.</summary>
    public static class CombatTime
    {
        /// <summary>Tolerance for comparing frame times against millisecond boundaries.</summary>
        public const double Epsilon = 1e-6;

        public static double FrameMs(int frame, int ticksPerSecond = 60) => frame * 1000.0 / ticksPerSecond;
    }
}
