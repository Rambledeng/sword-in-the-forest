using SwordInTheForest.Combat;

namespace SwordInTheForest.Core;

/// <summary>Fixed lead times, with the F1 panel's override applied when it is set.</summary>
public sealed class TunedLeadTimes : ILeadTimeSource
{
    public static readonly TunedLeadTimes Instance = new();
    private readonly FixedLeadTimeSource _fixed = new();

    public int NextLeadMs(AttackSpec attack)
    {
        var o = Tunables.Combat.FixedLeadOverrideMs;
        _fixed.FixedMs = o > 0 ? o : null;
        return _fixed.NextLeadMs(attack);
    }
}
