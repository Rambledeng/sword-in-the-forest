using SwordInTheForest.Combat;

namespace SwordInTheForest.Core;

/// <summary>Services the combat code asks for. M2 replaces <see cref="LeadTimes"/> with the director.</summary>
public static class CombatServices
{
    private static ILeadTimeSource? _override;

    /// <summary>
    /// Where every attack gets its lead time. Default: each attack's designed lead, or the F1
    /// "FixedLeadOverrideMs" when it is above zero.
    /// </summary>
    public static ILeadTimeSource LeadTimes
    {
        get => _override ?? TunedLeadTimes.Instance;
        set => _override = value;
    }

    public static void ResetLeadTimes() => _override = null;
}
