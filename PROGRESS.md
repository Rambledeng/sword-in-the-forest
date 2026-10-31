# Progress

## M0 · Toolchain and skeleton (2026-10-31)

### Done
- Cloud setup script worked as written: Godot 4.7.2 .NET (`godot` on PATH), .NET SDK 8.0.133,
  xvfb. No fallbacks needed. Godot.NET.Sdk 4.7.2 builds against net8.0, so the setup script
  needs no change.
- Repo layout from the spec: `game/`, `src/Director/` (netstandard2.1, a placeholder
  `LeadTimeBounds`), `tests/Director.Tests/`, `tests/Localization.Tests/`, `sim/` (stub),
  `analysis/`, `docs/`, `tools/`, `.github/workflows/ci.yml`. The spec has moved to `docs/SPEC.md`.
- Godot project: C#, Compatibility renderer, 480×270 viewport with integer scaling (3× window,
  1440×810), nearest filtering, pixel snapping. The input map covers keyboard, mouse and gamepad
  for move, aim (right stick), attack, guard, roll, interact, drink and pause, plus F1 and F3
  for debugging.
- `Cli` autoload: `--scenario <name>`, `--screenshot <scene>`, `--locale <code>`.
- Scenario runner (`tools/scenario.sh`) with two scenarios: `boot` (the title loads, the base
  resolution is right, every action has PC and pad bindings) and `localization` (the title label
  shows en/nl/zh_CN text and is changed under pseudolocalization:
  `[Ŝŵôŕd́ íή ŧh̀é F́ôŕéšŧ]`). I checked that a deliberately broken check makes the runner fail.
- The screenshot renders in the session through Xvfb and Mesa: `docs/screens/Title.png`.
- Localization unit tests: every key has all three languages, placeholders match, key prefixes
  are valid, and scene `text` values are keys.
- CI (`ci.yml`): build, `dotnet test`, Godot download (cached), headless scenarios and a
  best-effort screenshot. Logs and screens are uploaded as an artifact.
- CLAUDE.md (real commands), README stub, MIT LICENSE, CREDITS.md, strings.csv with
  `ui.title.name`.

### Status of "Done when"
- [x] `dotnet build` and `dotnet test` pass in the session (7 tests). CI: see the PR checks.
- [x] Headless scenarios pass in the session. CI: see the PR checks.
- [x] One screenshot renders in the session.
- [x] One label shows its text in all three languages and under pseudolocalization.

### Next (M1 · Combat core)
- Asset intake (your part): Ninja Adventure picks plus the Fusion Pixel 12px SC font under
  `game/assets/`.
- Player state machine, AttackRunner, saber bandit, test arena, tuning `.tres` and the F1 panel.

### Known issues
- No CJK font is bundled yet. The headless check passes because it compares strings, but a
  zh_CN screenshot would show missing glyphs until the Fusion Pixel font lands (asset intake).
- The game window uses Godot's default font, not pixel art; this changes with the font intake.

### Questions for the playtest
- Bindings are a first guess: attack is LMB/J/RB, guard is RMB/K/LB, roll is Space/L/B,
  interact is E/A, drink is R/Y. Change them if they feel wrong.
