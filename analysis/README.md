Python (pandas, matplotlib) that turns the simulator's CSV into the report's figures.

## M5 evaluation: one command

    pip install -r analysis/requirements.txt
    tools/report.sh

It runs the simulator's M5 study (`dotnet run --project sim -c Release -- --study m5`, about
30 s for 29 runs × 1,000 players). Then `analysis/evaluate.py` draws `docs/figures/fig*.png` and
fills `analysis/report_template.md` into `docs/report.md`. Edit the template, not the report.
Every number in the report is a `$placeholder` computed by `evaluate.py`. The full set is
also written to `analysis/out/summary.json`. Everything is seeded, so reruns are identical.

The M5 CSV (in `analysis/out/`, git-ignored, about 85 MB):

- `runs.csv`: one row per run: experiment (`learning`, `shifted`, `challenge`,
  `wrong_model`), population (`faster` ×0.75, `default`, `slower` ×1.4), reaction model
  (`lognormal`, `exgaussian`), prior (`population`, `uniform`), calibration attacks, learn
  (0 = prior only), challenge, target, timing (`adaptive`, `fixed` 420 ms).
- `players.csv`: each run's players: true median, spread, lapse (and μ, σ, τ for ex-Gaussian).
- `attacks.csv`: every attack: control phase or not, lead, success, the success predicted
  before it, and the estimate and 80% interval after it.

## M2 study

    dotnet run --project sim -c Release -- [--players 1000] [--out analysis/out]

This writes `players.csv` (per player × challenge × timing) and `estimates.csv` (adaptive runs,
attack by attack), and prints the M2 "Done when" numbers. Note: it uses the same file name
`players.csv` as the M5 study, with a different layout, so run `tools/report.sh` again
afterwards.
