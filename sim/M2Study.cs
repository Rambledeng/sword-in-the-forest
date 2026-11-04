using SwordInTheForest.Director;

namespace SwordInTheForest.Sim;

/// <summary>The M2 "Done when" measurements on simulated players (reused by the tests and the CLI).</summary>
public sealed class M2Study
{
    public const int Players = 1000;
    public const int PlayerSeed = 7;

    public M2Study(int players = Players, int seed = PlayerSeed)
    {
        Grid = DirectorGrid.Default;
        Prior = Simulator.DefaultPrior(Grid);
        Population = Simulator.Players(PopulationModel.Default, players, seed);
        Seed = seed;
    }

    public DirectorGrid Grid { get; }
    public double[] Prior { get; }
    public PlayerParams[] Population { get; }
    public int Seed { get; }

    public PlayerResult[] Run(SimConfig cfg) => Simulator.RunAll(Grid, Prior, Population, cfg, Seed);

    /// <summary>Share of players whose estimated median is within <paramref name="toleranceMs"/> after <paramref name="attacks"/>.</summary>
    public static double ShareWithin(IEnumerable<PlayerResult> results, int attacks, double toleranceMs) =>
        results.Average(r => r.ErrorAfter(attacks) <= toleranceMs ? 1.0 : 0.0);

    /// <summary>Share of players whose run success rate is within <paramref name="points"/> of the target.</summary>
    public static double ShareOnTarget(IEnumerable<PlayerResult> results, double target, double points = 0.10) =>
        results.Average(r => Math.Abs(r.RunSuccessRate - target) <= points + 1e-9 ? 1.0 : 0.0);

    public static double SuccessSd(IReadOnlyList<PlayerResult> results)
    {
        var mean = results.Average(r => r.RunSuccessRate);
        return Math.Sqrt(results.Average(r => (r.RunSuccessRate - mean) * (r.RunSuccessRate - mean)));
    }
}
