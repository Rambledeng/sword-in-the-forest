namespace SwordInTheForest.Combat
{
    /// <summary>What the player did about one attack, as written to telemetry.</summary>
    public enum Response
    {
        None,
        /// <summary>Guard pressed in the response window against a white attack.</summary>
        Deflect,
        /// <summary>Guard raised before the glint and held through a white impact.</summary>
        Block,
        Roll,
        /// <summary>Guard against a red attack: always wrong.</summary>
        Guard,
    }
}
