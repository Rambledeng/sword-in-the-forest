using System;

namespace SwordInTheForest.Run
{
    /// <summary>
    /// Decides whether a defeated enemy drops food and which: a low <see cref="Chance"/>, then a
    /// weighted pick (rice common, roast chicken rare). Numbers come from run.tres.
    /// </summary>
    public sealed class FoodDrops
    {
        public FoodDrops(double chance, double riceWeight, double beefWeight, double chickenWeight)
        {
            Chance = chance;
            RiceWeight = riceWeight;
            BeefWeight = beefWeight;
            ChickenWeight = chickenWeight;
        }

        public double Chance { get; set; }
        public double RiceWeight { get; set; }
        public double BeefWeight { get; set; }
        public double ChickenWeight { get; set; }
        /// <summary>Food dropped so far this run.</summary>
        public int Dropped { get; private set; }
        /// <summary>Developer and test override for the next defeat (consumed by it).</summary>
        public Food? ForceNext { get; set; }

        /// <summary>
        /// One defeat. <paramref name="roll"/> gives uniform numbers in [0, 1); it is called once
        /// for the drop and once more for the kind when something drops.
        /// </summary>
        public Food? Roll(Func<double> roll)
        {
            Food? food;
            if (ForceNext is Food forced)
            {
                ForceNext = null;
                food = forced;
            }
            else if (Chance <= 0 || roll() >= Chance) food = null;
            else food = Pick(roll());
            if (food != null) Dropped++;
            return food;
        }

        private Food Pick(double u)
        {
            var total = Math.Max(0, RiceWeight) + Math.Max(0, BeefWeight) + Math.Max(0, ChickenWeight);
            if (total <= 0) return Food.Rice;
            var x = u * total;
            if ((x -= Math.Max(0, RiceWeight)) < 0) return Food.Rice;
            if ((x -= Math.Max(0, BeefWeight)) < 0) return Food.BraisedBeef;
            return Food.RoastChicken;
        }
    }
}
