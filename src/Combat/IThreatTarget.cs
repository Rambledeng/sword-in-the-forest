namespace SwordInTheForest.Combat
{
    /// <summary>The player as an attack sees it: told when a threat's window opens and when it is done.</summary>
    public interface IThreatTarget
    {
        /// <summary>The glint: the response window is open.</summary>
        void OpenThreat(ThreatWindow threat);

        /// <summary>The threat is finished (telemetry written or the attack aborted).</summary>
        void CloseThreat(ThreatWindow threat);
    }
}
