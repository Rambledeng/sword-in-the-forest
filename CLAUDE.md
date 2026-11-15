# Sword in the Forest: working rules

A Godot 4.7.2 .NET wuxia roguelike. The demo (v0.1.1) is released; work now follows
`docs/ROADMAP.md`, mostly in local sessions on Windows.

## Start of a session
1. Read `PROGRESS.md` (one page).
2. Read only the roadmap item you were given: `grep -n -A8 "\*\*<ID> ·" docs/ROADMAP.md`.
   If it is marked `[design: X]`, read `docs/design/X.md`; if that file is missing, stop and ask.
3. Give a short plan and wait for approval before writing code, unless told to go ahead.
Do not read `docs/history/`, `docs/PLAN.md` or `docs/report.md` unless the task is about them.

## Where things are
- `src/Combat` (attack timeline, response window, posture, telemetry; `Run/` has the run plan,
  room templates, mercy, upgrades, perks, food) and `src/Director` (the reaction-time filter).
  Both are .NET Standard 2.1, C# 9, and never reference Godot. Rules belong here, with unit tests.
- `game/scripts`: `Actors` (player, enemies), `Run` (RunScene, Room), `Ui`, `Core` (autoloads:
  settings, director service, telemetry, tunables), `Fx`, `Bots`, `scenarios`.
- `game/tuning/*.tres` numbers; `game/localization/strings.csv` text; `game/scenes/scenarios/` tests.
- `sim/` and `analysis/` produce `docs/report.md` (`tools/report.sh`; edit the template, not the report).

## Build and test
- `dotnet build` and `dotnet test` (repo root) pass before every commit. The solution includes
  the game project.
- Scenarios, headless: `tools/scenario.sh [name ...]` (no name = all). Pass = exit 0 and a final
  `SCENARIO PASS <name>` line. Logs in `.logs/scenario-<name>.log`. Run the ones your change
  touches while working, and all of them before the pull request.
- New scenario: `game/scenes/scenarios/<name>.tscn` whose root script derives from
  `SwordInTheForest.Scenarios.Scenario` (override `Step(frame)`, use `Check`).
- Screenshots: `tools/screenshot.sh <scene> [locale] [frames] [name]` → `docs/screens/`.
  Readback lags one frame: capture one frame after the moment you want.
- The scripts are bash and read the Godot binary from `GODOT` (default `godot`).
- CLI flags after `--`: `--locale en|nl|zh_CN`, `--scenario <name>`, `--dev`.

### Local commands (Windows)
Not set up yet: roadmap item A1 fills this in (Git Bash, `GODOT=<path to the console .exe>`).
Until then CI on Linux is the referee for scenarios.

## Conventions
- C# with nullable reference types on; one public type per file.
- Every feature gets a unit test or a scenario.
- Player-facing text is a key in `strings.csv` (en, nl, zh_CN). Ranbao writes the sword's
  Chinese first; English and Dutch follow it. List new Chinese lines for review in the pull request.
- Tunable numbers live in `game/tuning/*.tres`, never as literals in code.
- Everything random in a run uses a seeded stream; a new feature gets its own stream so old
  seeds keep their layouts.
- Anything that changes what the player's answers mean (auto-deflects, slow motion, a loss of
  control) must tell the director, or its estimate is polluted.
- Assets come from `game/assets/third_party/` or are drawn in code; each new one goes in
  CREDITS.md with its source and licence, including the tool and terms for AI-made ones.

## Keep sessions cheap
- Never list or read `game/assets/` or `docs/screens/` recursively; find files by name.
- Use grep to find a symbol before opening a file, and read the part you need.
- Send long output to a file and read its tail; tests print one-line summaries.
- Don't re-read a file you just wrote. Don't start subagents unless asked.
- If one error survives three fix attempts, stop, note it in PROGRESS.md, and ask.

## End of a session
1. All tests green.
2. PROGRESS.md: update "Now", add at most three lines to the log, and fix the known issues and
   open questions your change touched. Tick the item in `docs/ROADMAP.md`.
3. Commit on a branch, push, open a pull request to main.
