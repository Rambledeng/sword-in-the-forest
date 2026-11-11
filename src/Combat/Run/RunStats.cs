using System;
using System.Collections.Generic;

namespace SwordInTheForest.Run
{
    /// <summary>
    /// The run's counters (功德 merit, 业障 karma, 怨 kills, spared) and its end. Finishing a kneeling
    /// enemy is merit (the realm's enemies are captured evil ghosts); sparing one that begs is merit;
    /// cutting down one that begs is karma.
    /// </summary>
    public sealed class RunStats
    {
        private readonly List<Upgrade> _upgrades = new List<Upgrade>();

        public int Merit { get; private set; }
        public int Karma { get; private set; }
        public int Kills { get; private set; }
        public int Spared { get; private set; }
        public int RoomsCleared { get; private set; }
        public bool Ended { get; private set; }
        public bool Won { get; private set; }
        public double ElapsedMs { get; set; }
        public IReadOnlyList<Upgrade> Upgrades => _upgrades;
        /// <summary>This soul's starter perk (none in drills and test arenas).</summary>
        public StarterPerk? Perk { get; set; }

        /// <summary>Merit was gained (the game shows a gold glitter instead of a counter).</summary>
        public event Action? MeritGained;
        /// <summary>Karma was gained (a wisp of dark smoke).</summary>
        public event Action? KarmaGained;

        /// <summary>An enemy killed by a finisher; <paramref name="begging"/> when it had surrendered.</summary>
        public void Finished(bool begging)
        {
            Kills++;
            if (begging) AddKarma();
            else AddMerit();
        }

        /// <summary>An enemy killed without a finisher (its health ran out).</summary>
        public void Killed() => Kills++;

        public void Spare()
        {
            Spared++;
            AddMerit();
        }

        private void AddMerit()
        {
            Merit++;
            MeritGained?.Invoke();
        }

        private void AddKarma()
        {
            Karma++;
            KarmaGained?.Invoke();
        }

        public void RoomCleared() => RoomsCleared++;

        public void Take(Upgrade u) => _upgrades.Add(u);

        public int Count(Upgrade u)
        {
            var n = 0;
            foreach (var x in _upgrades) if (x == u) n++;
            return n;
        }

        public void End(bool won)
        {
            if (Ended) return;
            Ended = true;
            Won = won;
        }

        /// <summary>Merit at least two ahead of karma leans to merit; any karma ahead of merit leans to karma.</summary>
        public Lean Lean => Merit - Karma >= 2 ? Lean.Merit : Karma > Merit ? Lean.Karma : Lean.Balanced;

        /// <summary>The key of the sword's closing line for this run.</summary>
        public string VerdictKey => $"sword.verdict.{(Won ? "win" : "loss")}_{Lean.ToString().ToLowerInvariant()}";
    }
}
