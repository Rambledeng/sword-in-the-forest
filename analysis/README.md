Python (pandas, matplotlib) that turns the simulator's CSV into the report's figures. Filled in M5.

The simulator (M2) writes the CSV here:

    dotnet run --project sim -c Release -- [--players 1000] [--out analysis/out]

- `analysis/out/players.csv`: one row per player × challenge × timing (adaptive or fixed): true
  median, spread and lapse, estimate and error after 15 attacks, final estimate, success rate over
  the 60 run attacks, mean lead.
- `analysis/out/estimates.csv`: the adaptive runs attack by attack (lead, success, estimate).

`analysis/out/` is git-ignored; it takes about 6 s to regenerate.
