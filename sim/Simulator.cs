using SwordInTheForest.Director;

namespace SwordInTheForest.Sim;

/// <summary>Drives the shipped Director library with simulated players.</summary>
public static class Simulator
{
    /// <summary>The prior the game ships: fitted to 20,000 training players from the default population.</summary>
    public static double[] DefaultPrior(DirectorGrid grid) => Prior.FromPopulation(grid, PopulationModel.Default);

    /// <summary>Test players, drawn with a seed different from the prior's training set.</summary>
    public static PlayerParams[] Players(PopulationModel population, int count, int seed)
    {
        var rng = new Random(seed);
        var players = new PlayerParams[count];
        for (var i = 0; i < count; i++) players[i] = population.Sample(rng);
        return players;
    }

    public static PlayerResult Run(DirectorGrid grid, double[] prior, PlayerParams truth, SimConfig cfg, int seed)
    {
        var director = ReactionDirector.FromPrior(grid, prior);
        var player = new SimPlayer(truth);
        var rng = new Random(seed);
        var total = cfg.CalibrationAttacks + cfg.ControlAttacks;
        var medians = new double[total];
        var leads = new int[total];
        var success = new bool[total];
        var target = cfg.Challenge.Target();
        for (var i = 0; i < total; i++)
        {
            var mode = i < cfg.CalibrationAttacks ? DirectorMode.Calibrate : DirectorMode.Control;
            var lead = cfg.Adaptive ? director.NextLeadMs(cfg.AttackBounds, mode, target) : cfg.FixedLeadMs;
            var e = player.Respond(lead, rng);
            director.Observe(e);
            leads[i] = lead;
            success[i] = e.Success;
            medians[i] = director.Estimate().MedianMs;
        }
        return new PlayerResult(truth, medians, leads, success, cfg.CalibrationAttacks);
    }

    /// <summary>Runs every player (in parallel; each player's run is deterministic from its seed).</summary>
    public static PlayerResult[] RunAll(DirectorGrid grid, double[] prior, IReadOnlyList<PlayerParams> players, SimConfig cfg, int seed)
    {
        grid.PhiTable.GetHashCode(); // build the shared table before the threads start
        var results = new PlayerResult[players.Count];
        Parallel.For(0, players.Count, i => results[i] = Run(grid, prior, players[i], cfg, seed * 7919 + i));
        return results;
    }
}
