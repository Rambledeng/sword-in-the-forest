using SwordInTheForest.Run;

namespace Combat.Tests;

public class RunPlanTests
{
    [Fact]
    public void Templates_AreWellFormed()
    {
        foreach (var t in RoomTemplates.All)
        {
            Assert.True(t.Width >= 28 && t.Height >= 16, $"{t.Id} is {t.Width}x{t.Height}");
            Assert.Equal(Tile.Floor, t.At(t.Entry));
            if (t.IsBoss) Assert.NotNull(t.BossSpawn);
            else
            {
                Assert.NotEmpty(t.Exits);
                Assert.All(t.Exits, e => Assert.Equal(0, e.Y));
                Assert.True(t.SpawnPoints.Count >= (t.Area == Area.Tutorial ? 1 : 6), $"{t.Id}: {t.SpawnPoints.Count} spawn points");
            }
            for (var x = 0; x < t.Width; x++)
                Assert.NotEqual(Tile.Floor, t.At(new TilePos(x, t.Height - 1)));
        }
        Assert.Equal(3, RoomTemplates.For(Area.Bamboo).Count);
        Assert.Equal(3, RoomTemplates.For(Area.Outside).Count);
        Assert.Equal(2, RoomTemplates.For(Area.Tavern).Count);
    }

    [Fact]
    public void TwentySeeds_ProduceValidRuns()
    {
        for (var seed = 1; seed <= 20; seed++) AssertValid(RunPlan.Build(seed));
    }

    [Fact]
    public void ManySeeds_ProduceValidRuns()
    {
        for (var seed = 1000; seed < 1500; seed++) AssertValid(RunPlan.Build(seed));
    }

    private static void AssertValid(RunPlan plan)
    {
        var steps = plan.Steps;
        Assert.Equal(9, steps.Count);
        var expected = new[]
        {
            (RunStepKind.Room, Area.Tutorial), (RunStepKind.Room, Area.Bamboo), (RunStepKind.Room, Area.Bamboo),
            (RunStepKind.Upgrade, Area.Bamboo), (RunStepKind.Room, Area.Outside), (RunStepKind.Room, Area.Outside),
            (RunStepKind.Upgrade, Area.Outside), (RunStepKind.Room, Area.Tavern), (RunStepKind.Room, Area.Tavern),
        };
        for (var i = 0; i < steps.Count; i++)
        {
            Assert.Equal(expected[i].Item1, steps[i].Kind);
            if (steps[i].Kind == RunStepKind.Room) Assert.Equal(expected[i].Item2, steps[i].Room!.Area);
            else Assert.Equal(3, steps[i].Choices.Distinct().Count());
        }
        Assert.True(steps[8].Room!.IsBoss);
        Assert.Equal(EnemyKind.Boss, Assert.Single(steps[8].Room!.Spawns).Kind);
        Assert.Equal(EnemyKind.Ghost, Assert.Single(steps[0].Room!.Spawns).Kind);
        Assert.NotEqual(steps[1].Room!.Template, steps[2].Room!.Template);
        Assert.NotEqual(steps[4].Room!.Template, steps[5].Room!.Template);
        foreach (var room in plan.Rooms)
        {
            var problems = RoomValidator.Problems(room);
            Assert.True(problems.Count == 0, $"seed {plan.Seed}: " + string.Join("; ", problems));
            Assert.Equal(room.Spawns.Count, room.Spawns.Select(s => s.At).Distinct().Count());
            if (room.Area == Area.Bamboo) Assert.All(room.Spawns, s => Assert.Equal(EnemyKind.Saber, s.Kind));
        }
        Assert.Contains(plan.Rooms, r => r.Spawns.Any(s => s.Kind == EnemyKind.Spear));
        Assert.Contains(plan.Rooms, r => r.Spawns.Any(s => s.Kind == EnemyKind.Archer));
    }

    [Fact]
    public void SameSeed_SameRun_DifferentSeeds_DifferentLayouts()
    {
        string Describe(RunPlan p) => string.Join("|", p.Steps.Select(s => s.Room == null
            ? string.Join(",", s.Choices)
            : s.Room.Template.Id + ":" + string.Join(",", s.Room.Spawns.Select(x => $"{x.Kind}{x.At}{x.Variant}"))));
        Assert.Equal(Describe(RunPlan.Build(42)), Describe(RunPlan.Build(42)));
        var distinct = Enumerable.Range(1, 20).Select(s => Describe(RunPlan.Build(s))).Distinct().Count();
        Assert.Equal(20, distinct);
    }

    [Fact]
    public void Validator_CatchesBlockedExitsAndBadSpawns()
    {
        var blocked = new RoomTemplate("blocked", Area.Bamboo, false, new[]
        {
            "####XX####",
            "#........#",
            "#oooooooo#",
            "#........#",
            "#..s.....#",
            "#........#",
            "#....P...#",
            "##########",
        });
        var bad = new RoomPlan(blocked, new[] { new Spawn(EnemyKind.Saber, new TilePos(1, 1), 0) }, 0, 1);
        var problems = RoomValidator.Problems(bad);
        Assert.Contains(problems, p => p.Contains("exit"));
        Assert.Contains(problems, p => p.Contains("unreachable"));
        Assert.Contains(problems, p => p.Contains("next to"));
    }

    [Fact]
    public void NextStep_WalksAroundObstacles()
    {
        var t = RoomTemplates.BambooStream;
        var from = t.Entry;
        var goal = t.Exits[0];
        var p = from;
        for (var i = 0; i < 200 && p != goal; i++)
        {
            var next = RoomNav.NextStep(t, p, goal, exitsOpen: true);
            Assert.NotNull(next);
            Assert.Equal(1, next!.Value.Manhattan(p));
            Assert.True(t.Passable(next.Value));
            p = next.Value;
        }
        Assert.Equal(goal, p);
        Assert.Null(RoomNav.NextStep(t, from, goal, exitsOpen: false));
    }

    [Fact]
    public void AreaStart_IsTheFirstRoomOfEachArea()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            var plan = RunPlan.Build(seed);
            foreach (var area in new[] { Area.Tutorial, Area.Bamboo, Area.Outside, Area.Tavern })
            {
                var i = plan.AreaStart(area);
                Assert.Equal(area, plan.Steps[i].Room!.Area);
                Assert.True(i == 0 || plan.Steps[i - 1].Room?.Area != area);
            }
            Assert.Equal(0, plan.AreaStart(Area.Tutorial));
        }
    }

    [Fact]
    public void UpgradeChoices_AreDistinct_AndEveryUpgradeIsOffered()
    {
        var seen = new HashSet<Upgrade>();
        for (var seed = 1; seed <= 200; seed++)
            foreach (var step in RunPlan.Build(seed).Steps.Where(s => s.Kind == RunStepKind.Upgrade))
            {
                Assert.Equal(RunPlan.UpgradeChoices, step.Choices.Distinct().Count());
                seen.UnionWith(step.Choices);
            }
        Assert.Equal(Enum.GetValues(typeof(Upgrade)).Length, seen.Count);
    }
}
