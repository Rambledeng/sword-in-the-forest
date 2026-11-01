using SwordInTheForest.Combat;

namespace SwordInTheForest.Core;

/// <summary>Services the combat code asks for.</summary>
public static class CombatServices
{
    private static ILeadTimeSource? _override;

    /// <summary>
    /// Where every attack gets its lead time. Default: the director (designed leads while adaptive
    /// timing is off), or the F1 "FixedLeadOverrideMs" when it is above zero.
    /// </summary>
    public static ILeadTimeSource LeadTimes
    {
        get => _override ?? TunedLeadTimes.Instance;
        set => _override = value;
    }

    public static void ResetLeadTimes() => _override = null;
}
