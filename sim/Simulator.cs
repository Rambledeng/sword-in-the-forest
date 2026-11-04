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

    public static PlayerResult Run(DirectorGrid grid, double[] prior, PlayerParams truth, SimConfig cfg, int seed) =>
        Run(grid, prior, new SimPlayer(truth), cfg, seed);

    public static PlayerResult Run(DirectorGrid grid, double[] prior, SimPlayer player, SimConfig cfg, int seed)
    {
        var director = ReactionDirector.FromPrior(grid, prior);
        var rng = new Random(seed);
        var total = cfg.CalibrationAttacks + cfg.ControlAttacks;
        var medians = new double[total];
        var leads = new int[total];
        var success = new bool[total];
        var predicted = new double[total];
        var lows = new double[total];
        var highs = new double[total];
        var target = cfg.Challenge.Target();
        for (var i = 0; i < total; i++)
        {
            var mode = i < cfg.CalibrationAttacks ? DirectorMode.Calibrate : DirectorMode.Control;
            var lead = cfg.Adaptive ? director.NextLeadMs(cfg.AttackBounds, mode, target) : cfg.FixedLeadMs;
            var e = player.Respond(lead, rng);
            predicted[i] = cfg.Learn ? director.Observe(e) : director.PredictSuccess(lead);
            leads[i] = lead;
            success[i] = e.Success;
            var est = director.Estimate();
            medians[i] = est.MedianMs;
            lows[i] = est.LowMs;
            highs[i] = est.HighMs;
        }
        // The truth is scored on the player's actual median (it differs from the log-normal one for other models).
        var truth = new PlayerParams(player.Reaction.MedianMs, player.Truth.Spread, player.Truth.Lapse);
        return new PlayerResult(truth, medians, leads, success, cfg.CalibrationAttacks, predicted, lows, highs);
    }

    /// <summary>Runs every player (in parallel; each player's run is deterministic from its seed).</summary>
    public static PlayerResult[] RunAll(DirectorGrid grid, double[] prior, IReadOnlyList<PlayerParams> players, SimConfig cfg, int seed)
    {
        grid.PhiTable.GetHashCode(); // build the shared table before the threads start
        var results = new PlayerResult[players.Count];
        Parallel.For(0, players.Count, i => results[i] = Run(grid, prior, players[i], cfg, seed * 7919 + i));
        return results;
    }

    /// <summary>As above, for players with any reaction-time model.</summary>
    public static PlayerResult[] RunAll(DirectorGrid grid, double[] prior, IReadOnlyList<SimPlayer> players, SimConfig cfg, int seed)
    {
        grid.PhiTable.GetHashCode();
        var results = new PlayerResult[players.Count];
        Parallel.For(0, players.Count, i => results[i] = Run(grid, prior, players[i], cfg, seed * 7919 + i));
        return results;
    }
}
