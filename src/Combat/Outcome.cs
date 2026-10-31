namespace SwordInTheForest.Combat
{
    /// <summary>How one attack ended for the player, as written to telemetry.</summary>
    public enum Outcome
    {
        /// <summary>The right response, pressed between the glint and the impact (in reach or not).</summary>
        Success,
        /// <summary>A white attack met by a guard raised before the glint: no damage, posture only.</summary>
        Blocked,
        /// <summary>Invulnerable from a roll started before the glint.</summary>
        Evaded,
        Hit,
        /// <summary>No right response, but the player was out of reach at the impact.</summary>
        Whiff,
    }
}
