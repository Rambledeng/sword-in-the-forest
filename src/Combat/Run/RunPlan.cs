using System;
using System.Collections.Generic;
using System.Linq;

namespace SwordInTheForest.Run
{
    /// <summary>
    /// A whole run, assembled from a seed: the tutorial room, two bamboo-forest rooms, an upgrade,
    /// two rooms outside the tavern, an upgrade, one tavern room and the boss. Each area's rooms
    /// are drawn from its handmade templates, with a random enemy set on random spawn points.
    /// </summary>
    public sealed class RunPlan
    {
        public const int UpgradeChoices = 3;

        // Enemy sets per area and room index within the area.
        private static readonly EnemyKind[][][] BambooSets =
        {
            new[] { new[] { EnemyKind.Saber }, new[] { EnemyKind.Saber, EnemyKind.Saber } },
            new[] { new[] { EnemyKind.Saber, EnemyKind.Saber } },
        };

        private static readonly EnemyKind[][][] OutsideSets =
        {
            new[] { new[] { EnemyKind.Spear, EnemyKind.Saber }, new[] { EnemyKind.Spear, EnemyKind.Archer } },
            new[]
            {
                new[] { EnemyKind.Saber, EnemyKind.Spear, EnemyKind.Archer },
                new[] { EnemyKind.Spear, EnemyKind.Spear, EnemyKind.Archer },
                new[] { EnemyKind.Saber, EnemyKind.Saber, EnemyKind.Archer },
            },
        };

        private static readonly EnemyKind[][][] TavernSets =
        {
            new[]
            {
                new[] { EnemyKind.Saber, EnemyKind.Spear, EnemyKind.Archer },
                new[] { EnemyKind.Saber, EnemyKind.Saber, EnemyKind.Spear },
                new[] { EnemyKind.Spear, EnemyKind.Archer, EnemyKind.Archer },
            },
        };

        /// <summary>Colour variants per regular enemy (0 is the sheet's own colours).</summary>
        public const int Variants = 5;

        private RunPlan(int seed, IReadOnlyList<RunStep> steps)
        {
            Seed = seed;
            Steps = steps;
        }

        public int Seed { get; }
        public IReadOnlyList<RunStep> Steps { get; }
        public IEnumerable<RoomPlan> Rooms => Steps.Where(s => s.Room != null).Select(s => s.Room!);

        public static RunPlan Build(int seed)
        {
            var rng = new Random(seed);
            var steps = new List<RunStep>
            {
                RunStep.ForRoom(PlaceRoom(RoomTemplates.Tutorial, new[] { EnemyKind.Ghost }, 0, rng)),
            };
            AddArea(steps, Area.Bamboo, BambooSets, rng);
            steps.Add(RunStep.ForUpgrade(DrawUpgrades(rng)));
            AddArea(steps, Area.Outside, OutsideSets, rng);
            steps.Add(RunStep.ForUpgrade(DrawUpgrades(rng)));
            AddArea(steps, Area.Tavern, TavernSets, rng);
            steps.Add(RunStep.ForRoom(PlaceRoom(RoomTemplates.Boss, new[] { EnemyKind.Boss }, 1, rng)));
            return new RunPlan(seed, steps);
        }

        private static void AddArea(List<RunStep> steps, Area area, EnemyKind[][][] sets, Random rng)
        {
            var templates = Shuffle(RoomTemplates.For(area).ToList(), rng);
            for (var i = 0; i < sets.Length; i++)
            {
                var options = sets[i];
                var set = options[rng.Next(options.Length)];
                steps.Add(RunStep.ForRoom(PlaceRoom(templates[i % templates.Count], set, i, rng)));
            }
        }

        private static RoomPlan PlaceRoom(RoomTemplate t, IReadOnlyList<EnemyKind> set, int index, Random rng)
        {
            var spawns = new List<Spawn>();
            var points = Shuffle(t.SpawnPoints.ToList(), rng);
            var next = 0;
            foreach (var kind in set)
            {
                if (kind == EnemyKind.Boss && t.BossSpawn != null)
                {
                    spawns.Add(new Spawn(kind, t.BossSpawn.Value, 0));
                    continue;
                }
                if (next >= points.Count) throw new InvalidOperationException($"{t.Id}: not enough spawn points");
                var variant = kind == EnemyKind.Ghost ? 0 : rng.Next(Variants);
                spawns.Add(new Spawn(kind, points[next++], variant));
            }
            return new RoomPlan(t, spawns, index, rng.Next());
        }

        private static IReadOnlyList<Upgrade> DrawUpgrades(Random rng)
        {
            var all = Shuffle(Enum.GetValues(typeof(Upgrade)).Cast<Upgrade>().ToList(), rng);
            return all.Take(UpgradeChoices).ToList();
        }

        private static List<T> Shuffle<T>(List<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                var tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
            return list;
        }
    }
}
