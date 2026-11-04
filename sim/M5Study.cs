using System.Diagnostics;
using System.Globalization;
using SwordInTheForest.Director;

namespace SwordInTheForest.Sim;

/// <summary>
/// The M5 evaluation (docs/SPEC.md, "Evaluation in M5"), on simulated players whose true values
/// are known. Writes three tidy CSV files for analysis/:
/// <list type="bullet">
/// <item><c>runs.csv</c>: one row per run (experiment, population, reaction model, prior, calibration, challenge, timing).</item>
/// <item><c>players.csv</c>: the true parameters of every player in every run.</item>
/// <item><c>attacks.csv</c>: every attack (lead, success, predicted success, estimate and its 80% interval after it).</item>
/// </list>
/// </summary>
public sealed class M5Study
{
    /// <summary>Faster and slower populations: the median of medians × 0.75 (≈250 ms) and × 1.4 (≈460 ms).</summary>
    public static readonly (string Name, double Factor)[] Populations = { ("faster", 0.75), ("default", 1.0), ("slower", 1.4) };
    public const int ExGaussianSeed = 11;

    private readonly int _players;
    private readonly DirectorGrid _grid = DirectorGrid.Default;
    private readonly double[] _popPrior;
    private readonly double[] _uniform;
    private readonly List<Run> _runs = new();

    public M5Study(int players = M2Study.Players)
    {
        _players = players;
        _popPrior = Simulator.DefaultPrior(_grid);
        _uniform = Prior.Uniform(_grid);
        Plan();
    }

    public IReadOnlyList<Run> Runs => _runs;

    /// <summary>One configuration run over the whole test population.</summary>
    public sealed record Run(int Id, string Experiment, string Population, string Model, string Prior, SimConfig Config)
    {
        public string Timing => Config.Adaptive ? "adaptive" : "fixed";
    }

    private void Plan()
    {
        // 1 + 3: learning speed, on the default and on shifted populations (Standard challenge).
        foreach (var (pop, _) in Populations)
        {
            Add("learning", pop, "lognormal", "population", new SimConfig { Learn = false });
            Add("learning", pop, "lognormal", "uniform", new SimConfig());
            Add("learning", pop, "lognormal", "uniform", new SimConfig { CalibrationAttacks = 0 });
            Add("learning", pop, "lognormal", "population", new SimConfig());
            Add("learning", pop, "lognormal", "population", new SimConfig { CalibrationAttacks = 0 });
            if (pop != "default") Add("shifted", pop, "lognormal", "population", new SimConfig { Adaptive = false });
        }
        // 2 + 4: success rates per challenge, adaptive against fixed, for log-normal and ex-Gaussian bots.
        foreach (var model in new[] { "lognormal", "exgaussian" })
            foreach (var c in new[] { Challenge.Relaxed, Challenge.Standard, Challenge.Hard })
                foreach (var adaptive in new[] { true, false })
                    Add(model == "lognormal" ? "challenge" : "wrong_model", "default", model, "population",
                        new SimConfig { Challenge = c, Adaptive = adaptive });
    }

    private void Add(string experiment, string population, string model, string prior, SimConfig cfg) =>
        _runs.Add(new Run(_runs.Count, experiment, population, model, prior, cfg));

    public IReadOnlyList<SimPlayer> PlayersFor(string population, string model)
    {
        var factor = Populations.First(p => p.Name == population).Factor;
        // The same seed for every population: each player keeps its z-scores, only the scale moves.
        var truths = Simulator.Players(PopulationModel.Default.Shifted(factor), _players, M2Study.PlayerSeed);
        if (model == "lognormal") return truths.Select(t => new SimPlayer(t)).ToArray();
        var rng = new Random(ExGaussianSeed);
        return truths.Select(t => new SimPlayer(t, ExGaussianTime.Like(t, rng))).ToArray();
    }

    public PlayerResult[] Execute(Run run) =>
        Simulator.RunAll(_grid, run.Prior == "uniform" ? _uniform : _popPrior, PlayersFor(run.Population, run.Model), run.Config, M2Study.PlayerSeed);

    public void WriteCsv(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var inv = CultureInfo.InvariantCulture;
        var sw = Stopwatch.StartNew();
        using var runs = new StreamWriter(Path.Combine(outDir, "runs.csv"));
        runs.WriteLine("run,experiment,population,model,prior,calibration_attacks,learn,challenge,target,timing");
        using var players = new StreamWriter(Path.Combine(outDir, "players.csv"));
        players.WriteLine("run,player,true_median_ms,true_spread,true_lapse,mu_ms,sigma_ms,tau_ms");
        using var attacks = new StreamWriter(Path.Combine(outDir, "attacks.csv"));
        attacks.WriteLine("run,player,attack,control,lead_ms,success,predicted,est_median_ms,est_low_ms,est_high_ms");
        foreach (var run in _runs)
        {
            var c = run.Config;
            runs.WriteLine(string.Join(",", run.Id, run.Experiment, run.Population, run.Model, run.Prior, c.CalibrationAttacks,
                c.Learn ? 1 : 0, c.Challenge, c.Challenge.Target().ToString("0.00", inv), run.Timing));
            var sims = PlayersFor(run.Population, run.Model);
            var results = Execute(run);
            for (var p = 0; p < results.Length; p++)
            {
                var r = results[p];
                var eg = sims[p].Reaction as ExGaussianTime;
                players.WriteLine(string.Join(",", run.Id, p, r.Truth.MedianMs.ToString("0.0", inv), r.Truth.Spread.ToString("0.000", inv),
                    r.Truth.Lapse.ToString("0.000", inv), eg?.MuMs.ToString("0.0", inv) ?? "", eg?.SigmaMs.ToString("0.0", inv) ?? "",
                    eg?.TauMs.ToString("0.0", inv) ?? ""));
                for (var k = 0; k < r.Leads.Length; k++)
                    attacks.WriteLine(string.Join(",", run.Id, p, k + 1, k >= r.CalibrationAttacks ? 1 : 0, r.Leads[k], r.Success[k] ? 1 : 0,
                        r.Predicted[k].ToString("0.0000", inv), r.MedianAfter[k].ToString("0.0", inv),
                        r.LowAfter[k].ToString("0.0", inv), r.HighAfter[k].ToString("0.0", inv)));
            }
            Console.WriteLine($"run {run.Id,2} {run.Experiment,-11} {run.Population,-7} {run.Model,-10} {run.Prior,-10} cal {c.CalibrationAttacks,2} " +
                              $"learn {(c.Learn ? 1 : 0)} {c.Challenge,-8} {run.Timing,-8} mean success {results.Average(x => x.RunSuccessRate):0.000}");
        }
        Console.WriteLine($"M5: {_runs.Count} runs × {_players} players in {sw.Elapsed.TotalSeconds:0.0} s; CSV in {outDir}");
    }
}
