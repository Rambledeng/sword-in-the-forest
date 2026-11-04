using System.Diagnostics;
using System.Globalization;
using SwordInTheForest.Director;
using SwordInTheForest.Sim;

// Simulated players against the Director library. Writes CSV for analysis/ and prints the M2
// "Done when" numbers. Usage: dotnet run --project sim -c Release -- [--players N] [--out DIR]
var players = M2Study.Players;
var outDir = Path.Combine("analysis", "out");
for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--players") players = int.Parse(args[i + 1], CultureInfo.InvariantCulture);
    if (args[i] == "--out") outDir = args[i + 1];
}
Directory.CreateDirectory(outDir);
var inv = CultureInfo.InvariantCulture;
var sw = Stopwatch.StartNew();
var study = new M2Study(players);

using var summary = new StreamWriter(Path.Combine(outDir, "players.csv"));
summary.WriteLine("player,true_median_ms,true_spread,true_lapse,challenge,timing,est_median_15,err_15,est_median_end,run_success,mean_lead_ms");
using var trace = new StreamWriter(Path.Combine(outDir, "estimates.csv"));
trace.WriteLine("player,challenge,attack,lead_ms,success,est_median_ms,true_median_ms");

foreach (var challenge in new[] { Challenge.Relaxed, Challenge.Standard, Challenge.Hard })
{
    var target = challenge.Target();
    var adaptive = study.Run(new SimConfig { Challenge = challenge });
    var fixedRun = study.Run(new SimConfig { Challenge = challenge, Adaptive = false });
    foreach (var (label, results) in new[] { ("adaptive", adaptive), ("fixed", fixedRun) })
    {
        for (var p = 0; p < results.Length; p++)
        {
            var r = results[p];
            summary.WriteLine(string.Join(",", p, r.Truth.MedianMs.ToString("0.0", inv), r.Truth.Spread.ToString("0.000", inv),
                r.Truth.Lapse.ToString("0.000", inv), challenge, label, r.MedianAfter[14].ToString("0.0", inv),
                r.ErrorAfter(15).ToString("0.0", inv), r.MedianAfter[^1].ToString("0.0", inv),
                r.RunSuccessRate.ToString("0.000", inv), r.Leads.Skip(r.CalibrationAttacks).Average().ToString("0.0", inv)));
            if (label == "adaptive")
                for (var k = 0; k < r.Leads.Length; k++)
                    trace.WriteLine(string.Join(",", p, challenge, k + 1, r.Leads[k], r.Success[k] ? 1 : 0,
                        r.MedianAfter[k].ToString("0.0", inv), r.Truth.MedianMs.ToString("0.0", inv)));
        }
    }
    Console.WriteLine($"{challenge} (target {target:0.00}): on target ±10 pts adaptive {M2Study.ShareOnTarget(adaptive, target):P1} " +
                      $"(sd {M2Study.SuccessSd(adaptive):0.000}), fixed {M2Study.ShareOnTarget(fixedRun, target):P1} (sd {M2Study.SuccessSd(fixedRun):0.000})");
    if (challenge == Challenge.Standard)
        Console.WriteLine($"Estimate within 40 ms after 15 attacks: {M2Study.ShareWithin(adaptive, 15, 40):P1}; " +
                          $"median error {adaptive.Select(r => r.ErrorAfter(15)).OrderBy(e => e).ElementAt(adaptive.Length / 2):0.0} ms");
}
Console.WriteLine($"{players} players in {sw.Elapsed.TotalSeconds:0.0} s; CSV in {outDir}");
