namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Chooses the lead time (glint to impact, ms) for the next attack aimed at the player.
    /// M1 uses <see cref="FixedLeadTimeSource"/>; M2 plugs the adaptive director in here.
    /// </summary>
    public interface ILeadTimeSource
    {
        int NextLeadMs(AttackSpec attack);
    }
}
