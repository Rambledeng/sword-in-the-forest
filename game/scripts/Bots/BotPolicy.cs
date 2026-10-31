namespace SwordInTheForest.Bots;

public enum BotPolicy
{
    /// <summary>Deflect white glints, roll from red ones.</summary>
    Right,
    /// <summary>Never responds.</summary>
    Never,
    /// <summary>Taps guard after every glint, red ones included.</summary>
    GuardTap,
    /// <summary>Holds guard the whole time.</summary>
    GuardHold,
}
