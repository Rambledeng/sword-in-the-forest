using SwordInTheForest.Combat;

namespace SwordInTheForest.Core;

/// <summary>
/// The game's lead times: the F1 panel's FixedLeadOverrideMs when it is above zero, otherwise the
/// director (which returns each attack's designed default while adaptive timing is off).
/// </summary>
public sealed class TunedLeadTimes : ILeadTimeSource
{
    public static readonly TunedLeadTimes Instance = new();

    public int NextLeadMs(AttackSpec attack)
    {
        var o = Tunables.Combat.FixedLeadOverrideMs;
        if (o > 0) return o;
        return DirectorService.Instance?.Leads.NextLeadMs(attack) ?? attack.LeadDefaultMs;
    }
}
