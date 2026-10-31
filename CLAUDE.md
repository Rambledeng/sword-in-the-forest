# Sword in the Forest: working rules

Read docs/SPEC.md and PROGRESS.md before anything else. Work only on the
milestone named in the kickoff message, and stop at its "Done when" list.

## Build and test
- `dotnet build` and `dotnet test` (repo root, SwordInTheForest.sln) pass before every commit.
  The solution includes the Godot game project, so `dotnet build` also compiles the game.
- Scenario tests run headless: `tools/scenario.sh [name ...]` (no name = all in
  game/scenes/scenarios/). It builds, imports on first run, then for each runs
  `godot --headless --path game -- --scenario <name>`. Pass = exit 0 AND a final
  `SCENARIO PASS <name>` line. Logs go to .logs/scenario-<name>.log.
- New scenario: a scene in game/scenes/scenarios/<name>.tscn whose root script
  derives from `SwordInTheForest.Scenarios.Scenario` (override `Step(frame)`, use `Check`).
- Screenshots (this cloud machine can render via Xvfb + Mesa):
  `tools/screenshot.sh <scene> [locale] [frames] [name]` -> docs/screens/<name or scene>.png
  (scene path is relative to game/scenes without .tscn; output is the 480x270 viewport).
  `tools/screenshot.sh screens/Showcase "" 6000 x` stages a seeded fight and saves combat_*.png.
  Viewport readback lags one frame: capture one frame after the moment you want.
- Other CLI flags after `--`: `--locale en|nl|zh_CN`.
- Toolchain here: Godot 4.7.2 .NET at `godot`, .NET SDK 8. Godot.NET.Sdk 4.7.2 targets net8.0.

## Conventions
- C# with nullable reference types on; one public type per file.
- src/Director and src/Combat target .NET Standard 2.1 (C# 9) and never reference Godot.
  Combat rules (response window, roll frames, AttackRunner, telemetry format) live in src/Combat.
- Player-facing text is a key in game/localization/strings.csv (en, nl, zh_CN).
  tests/Localization.Tests fails on missing languages, mismatched placeholders,
  bad key prefixes, or scene text that is not a key.
- Tunable numbers live in game/tuning/*.tres, never as literals in code.
- Assets come from game/assets/third_party/ or are drawn in code;
  every new one goes in CREDITS.md.

## Keep sessions cheap
- Never list or read game/assets/ recursively; find files by name.
- Send long output to a file and read its tail; tests print one-line summaries.
- Don't re-read a file you just wrote. Don't start subagents unless asked.
- If one error survives three fix attempts, stop, log it in PROGRESS.md, and ask.

## End of every session
1. All tests green.
2. PROGRESS.md updated: done, next, known issues, questions for the playtest.
3. Commit, push the milestone branch, open a pull request to main.
