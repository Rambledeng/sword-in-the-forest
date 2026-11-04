namespace SwordInTheForest.Run
{
    /// <summary>A change in the sword's reading of the player worth a spoken line.</summary>
    public enum ReadingChange
    {
        None,
        /// <summary>The estimate became sure enough to name a number.</summary>
        Attuned,
        /// <summary>The player has become noticeably quicker since the last line.</summary>
        Quicker,
        /// <summary>The player has become noticeably slower since the last line.</summary>
        Slower,
    }
}
