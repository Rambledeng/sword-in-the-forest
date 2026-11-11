# Progress

The current state on one page. Keep it under about 120 lines: when the log at the bottom
passes ten entries, move the oldest to `docs/history/`. How the demo was built, milestone by
milestone, is in `docs/history/demo-progress.md`.

## Now

- **Released:** v0.1.1 (November 2026) on GitHub Releases and
  <https://rambledeng.itch.io/sword-in-the-forest>, Windows and Linux.
- **Phase:** A, "Move home" (see `docs/ROADMAP.md`). Development is now local on Windows.
- **Next item:** A1, the Windows toolchain.
- **Tests:** 183 unit tests (Combat 145, Director 35, Localization 3) and 32 headless scenarios,
  all passing in CI on Linux. Not yet run on Windows.

## What exists

- **Run:** tutorial → bamboo forest ×2 → upgrade → outside the tavern ×2 → upgrade → tavern →
  the Landlady (two phases). Ten handmade room templates, seeded. A new soul restarts at the
  area where the last one fell and keeps its upgrades.
- **Combat:** attack combo, guard, deflect, roll, finisher, gourd, posture on both sides, one
  attacker at a time. Enemies: saber bandit, spearman (red thrust), archer, tutorial ghost, boss.
- **Director:** a grid filter over reaction time, controlling every attack's lead from the first
  one; three challenge levels; an off switch. Evaluated on simulated players in `docs/report.md`.
- **Mercy:** broken enemies kneel or beg; one beggar in three is a fake with a tell; merit and
  karma feed the sword's closing verdict and nothing else yet.
- **Growth:** 10 upgrades (flash step is one), 5 starter perks, food drops, soul tint.
- **Presentation:** the sword's voice (about 40 lines), hidden values until the first win and
  then Detail mode, title intro, per-area music and atmosphere, display and audio settings.
- **Languages:** en, nl, zh_CN for every string. The Dutch is machine-drafted and unreviewed.
- **Pipeline:** CI on every push (build, tests, report, scenarios, screenshots, a Windows
  editor check); a `v*` tag exports both builds and pushes them to GitHub and itch.io.
- **Developer tools:** F1 live tuning, F2 director overlay, F3 developer menu; off in release
  builds unless started with `-- --dev` or `SWORD_DEV=1`.

## Known issues

- F1 "Save" writes to `res://`, so it works from the project and not from an exported build.
- The Windows build is unsigned and has no custom icon.
- No key rebinding (roadmap A4). No button to open the log folder (roadmap A3).
- Gamepad RT for the flash step is mapped as a trigger axis and was never tried on a real pad.
- Flash step passes guards but does not dodge. Food left in a room is lost on leaving it.
- Enemies steer by tile BFS and can hug a table corner for a moment.
- One reaction-time distribution covers deflects and rolls; rolls may need their own offset
  once there is real data.
- Combat time freezes during hit-stop, so a glint shown during a freeze gets up to 90 ms of
  extra real time. The director sees game time.
- Players with a true median above about 620 ms cannot reach the Standard target inside the
  800 ms lead cap.
- Window mode and size were only tested headless. The first launch opens at 1440×810.
- `run_bot` timed out once in CI in early November and never again; `SCENARIO_TRACE=1` traces it.

## Open questions for playtests

Answer these from what players do (roadmap B2), then delete them.

- Starter perks: noticeable, or too mild? Should the sword comment on them?
- Food at 12%: too rare? Should it also restore posture?
- Flash step: are 64 px and 5 s right? Should it dodge?
- The fake beggar: is the tell fair, is one in three right, is the first slow grab readable?
- Is finishing a beggar a real choice, or does everyone spare?
- One attacker at a time: fair, or too polite?
- The tutorial: is 12 ghost attacks too long?
- The boss: is the finisher that ends phase 1 readable? Is the wine jar fair?
- Merit glitter and karma smoke: seen in a busy fight?
- Should Detail mode turn on by itself once unlocked?
- Does the sword's voice read as wuxia in Chinese?

## Playtest notes

- One friend played v0.1.x in November 2026: largely positive, little detail because the demo
  is short. No specific issues recorded.

## Log

Newest first. Three lines per merged pull request: what changed, what to play, what is open.

- **2026-11-11 · Post-demo housekeeping.** The demo spec and the milestone log moved to
  `docs/history/`; `docs/ROADMAP.md`, `docs/PLAN.md` and `docs/design/` added; CLAUDE.md
  rewritten for local work; `tools/screenshot.sh` no longer requires xvfb. No game changes.
