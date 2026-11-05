using System;

namespace SwordInTheForest.Run
{
    /// <summary>Picks each new soul's <see cref="StarterPerk"/> from the soul's seed (the one its tint uses).</summary>
    public static class StarterPerks
    {
        public static readonly StarterPerk[] All = (StarterPerk[])Enum.GetValues(typeof(StarterPerk));

        /// <summary>
        /// The perk for the soul with <paramref name="soulSeed"/>. Its own stream (the seed is mixed
        /// first), so the perk does not follow the tint's hue.
        /// </summary>
        public static StarterPerk Pick(int soulSeed) => All[new Random(unchecked(soulSeed * 31 + 11)).Next(All.Length)];

        /// <summary>The upgrade whose effect (and icon) the perk borrows.</summary>
        public static Upgrade Echoes(StarterPerk p) => p switch
        {
            StarterPerk.Hale => Upgrade.Vitality,
            StarterPerk.Sturdy => Upgrade.BroadStance,
            StarterPerk.EvenBreath => Upgrade.SteadyBreath,
            StarterPerk.SpareSip => Upgrade.DeepGourd,
            _ => Upgrade.IronDeflect,
        };
    }
}
