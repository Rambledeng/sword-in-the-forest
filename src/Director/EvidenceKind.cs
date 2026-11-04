namespace SwordInTheForest.Director
{
    /// <summary>What one attack tells the director about the player.</summary>
    public enum EvidenceKind
    {
        /// <summary>A press after the glint: its time is a reaction time (late presses included).</summary>
        Timed,
        /// <summary>No press at all: a miss at the lead time.</summary>
        NoResponse,
        /// <summary>A miss that has nothing to do with speed (wrong button, out of position).</summary>
        Lapse,
    }
}
