using System;

namespace SwordInTheForest.Run
{
    /// <summary>
    /// Decides, for one run, whether a posture-broken enemy kneels or begs (投降忏悔), and whether
    /// its begging is genuine. The first beggar of a run is always genuine, so the sword can teach
    /// spare versus finish; after that a beggar is a fake with <see cref="FakeChance"/>.
    /// </summary>
    public sealed class MercyRules
    {
        public MercyRules(double fakeChance) => FakeChance = fakeChance;

        public double FakeChance { get; set; }
        /// <summary>Developer override for the next break (consumed by it).</summary>
        public BegKind? ForceNext { get; set; }
        /// <summary>Beggars so far this run (genuine and fake).</summary>
        public int Begs { get; private set; }
        public int Fakes { get; private set; }
        /// <summary>Fakes that have dropped the act this run.</summary>
        public int Reveals { get; private set; }

        /// <summary>
        /// How long a fake that drops the act takes to grab its weapon: the run's first reveal gets
        /// the longer <paramref name="firstMs"/> (the player has never seen one), later ones <paramref name="laterMs"/>.
        /// </summary>
        public int NextGrabMs(int firstMs, int laterMs) => Reveals++ == 0 ? Math.Max(firstMs, laterMs) : laterMs;

        /// <summary>
        /// One posture break. <paramref name="roll"/> gives uniform numbers in [0, 1); it is
        /// called once for the surrender roll and once more for a beggar after the first.
        /// </summary>
        public BegKind Decide(double surrenderChance, Func<double> roll)
        {
            BegKind kind;
            if (ForceNext is BegKind forced)
            {
                ForceNext = null;
                kind = forced;
            }
            else if (surrenderChance <= 0 || roll() >= surrenderChance) kind = BegKind.None;
            else kind = Begs > 0 && roll() < FakeChance ? BegKind.Fake : BegKind.Genuine;
            if (kind != BegKind.None) Begs++;
            if (kind == BegKind.Fake) Fakes++;
            return kind;
        }
    }
}
