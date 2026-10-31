# Sword in the Forest · 林中剑

A top-down wuxia soulslike roguelike demo in Godot 4 (C#), in which the sword learns its
wielder: a Bayesian director estimates your reaction time and times enemy warnings to
the challenge you choose. **Work in progress**; see [docs/SPEC.md](docs/SPEC.md) and
[PROGRESS.md](PROGRESS.md).

## Build and test

Requirements: Godot 4.7.2 .NET and the .NET 8 SDK.

```bash
dotnet build && dotnet test      # Director library, tests, and the game assembly
tools/scenario.sh                # headless Godot scenario tests
tools/screenshot.sh Title        # render a scene to docs/screens/ (needs a display or xvfb)
```

Open `game/project.godot` in Godot to play.

## Layout

| Path | What |
| --- | --- |
| `game/` | Godot project (C#) |
| `src/Director/` | Director library, .NET Standard 2.1, no Godot |
| `tests/` | xUnit tests (Director, localization) |
| `sim/` | Simulator console app |
| `analysis/` | Python figures and report |
| `docs/` | Spec, report, figures, screenshots |

## Licence

Code is MIT. Assets are listed with their licences in [CREDITS.md](CREDITS.md).
