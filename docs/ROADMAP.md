# Roadmap

An ordered backlog for everything after the demo (v0.1.1). It replaces the demo spec, which is
archived in `docs/history/demo-spec.md`. The reasoning behind the order, the budget and the
non-code work is in `docs/PLAN.md`.

## How to use this page

- **Work top to bottom.** Order is the plan; dates are guesses. Within a phase, items can swap
  if one is blocked.
- **One item per sitting.** Start a session with "Do roadmap item D2" and Claude reads only that
  item. If an item turns out bigger than one sitting, split it here first.
- **Sizes:** S = one sitting (1–2 h of your time), M = two or three sittings, L = must be split
  before starting.
- **Design first:** an item marked **[design: X]** needs the one-page design file
  `docs/design/X.md` approved before any code. See `docs/design/README.md`.
- **Done** means: a test or scenario covers it, numbers are in `game/tuning/`, text is in
  `strings.csv` in three languages, you have played it, and it is ticked here.
- Tick items with `[x]` and add the version tag that shipped them. Ideas that are not planned go
  in the parking lot at the bottom, not into a phase.

## Where the game is (v0.1.1, November 2026)

A 10–15 minute vertical slice: tutorial, bamboo forest ×2, outside the tavern ×2, the tavern and
the Landlady. Deflect and posture combat, three enemy types and a two-phase boss, the adaptive
director, mercy with fake beggars, 10 upgrades (one is the flash step), 5 starter perks, food
drops, hidden values with Detail mode, the sword's voice, and English, Dutch and Chinese.
Windows and Linux builds release to GitHub and itch.io from a tag.

What it lacks to be a game people return to: anything that persists between runs, a reason to
choose between rooms, enough content for a run longer than a quarter of an hour, and any
consequence of merit and karma beyond the closing line.

## The version ladder

Each rung is a release you could stop at and still have something whole. The windows assume
3–4 hours a week until the thesis is handed in and about 8 a week after; they are guesses.

| Version | Name | A player can | Phases | Rough window |
| --- | --- | --- | --- | --- |
| v0.1.x | Demo, listened to | play the demo with the worst rough edges fixed | A, B | Nov 2026 – Jan 2027 |
| v0.2 | Roguelike core | earn sword-intent, buy and find things, keep something after death | C, D | Feb – Jun 2027 |
| v0.3 | The road | choose a path through two levels with shops, elites and events | E | Jul – Dec 2027 |
| v0.4 | The sword remembers | see merit and karma change the world, and reach a first ending | F | early 2028 |
| v0.5 | Steam demo | wishlist the game and play a polished demo on Steam | G1–G4 | when the art direction is settled |
| v1.0 | Release | play all levels and all three endings | G5–G7 | decided after v0.4 |

**The scope rule.** After level 2 ships, measure how many sittings it took. Levels 3 and 4 are
built only if that number, times two, is something you are willing to spend. If not, 1.0 is two
levels with all three endings, and levels 3 and 4 become a free update or never.

## Phase A · Move home (November 2026)

Make the laptop the main workplace. Nothing here changes the game.

- [ ] **A1 · Windows toolchain (S).** `dotnet test` and `tools/scenario.sh` run in Git Bash with
  `GODOT` pointing at the console `.exe`. Record the exact commands in CLAUDE.md under "Local
  commands". The scripts already take `GODOT` from the environment; `timeout` and `grep` come
  with Git for Windows. If Git Bash cannot be made to work, write `tools/scenario.ps1` with the
  same pass rule (exit 0 and a `SCENARIO PASS` line).
- [ ] **A2 · Screenshots on Windows (S).** `tools/screenshot.sh` now skips `xvfb-run` when it is
  not installed (prepared in the cloud, not yet run on Windows). Check one screenshot, then decide
  whether `tools/gifs.sh` is worth porting (it needs ffmpeg) or stays a cloud and CI job.
- [ ] **A3 · Playtest kit (S).** A Settings button that opens the log folder (planned in the demo
  spec, never built), a "copy my last run" zip or path line, and `docs/playtest_form.md` with
  the eight questions from PLAN section 7. No data leaves the machine unless the player sends it.
- [ ] **A4 · Key rebinding (M).** Keyboard, mouse and gamepad, saved in `settings.cfg`. Steam
  players expect it and it is the first accessibility request you will get.
- [ ] **A5 · Decide the asset and licence policy (decision, no code).** The repo is public and the
  code is MIT. Before the first asset that is not CC0 goes in, pick one: keep art in a private
  repository or Git submodule, or keep everything public under a licence that reserves the
  art. See PLAN section 5.

## Phase B · Listen (November 2026 – January 2027)

The demo has had one outside player. Every later phase is cheaper if this one is done first.

- [ ] **B1 · Five playtests.** Three to five people, with at least one Dutch reader, one Chinese
  reader who is not you, and one person who does not play soulslikes. Watch one of them play
  without helping. Collect the form and the log folder.
- [ ] **B2 · Triage (S, repeats).** Turn the notes into a ranked list in PROGRESS.md. Fix the top
  five; park the rest. Answer the open tuning questions listed in PROGRESS.md with what you saw.
- [ ] **B3 · Real players in the report (M).** Add "Does it work on a real person?" to
  `docs/report.md` from the collected logs: each player's belief after their runs, and the prior
  refitted from real data against the simulated one. Good for the cloud: it has a clear test.
- [ ] **B4 · Dutch review (S).** Apply the Dutch reader's corrections and remove the
  "machine-drafted" note from CREDITS.md.
- [ ] **B5 · Project page on ranbaodeng.com.** Pitch, GIFs, the itch.io link, and the report.
- [ ] **B6 · Tag v0.1.2** when the fixes are in.

## Phase C · Design pages (during the thesis months)

Thinking, not code, so it fits short sittings and costs few tokens. Each is a chat that ends in
one page in `docs/design/`. They are listed in the order the code will need them.

- [ ] **C1 · `economy.md`.** What sword-intent (剑意) is earned from and spent on, what is lost on
  death, and what carries between runs (剑鸣). Whether upgrades stay "pick one of three" or become
  purchases. One currency or two.
- [ ] **C2 · `sword-qi.md`.** The 剑气 bar: what fills it (deflects), what spends it, and the three
  skills 瞬步, 无懈 and 乾坤. Whether the flash step keeps its cooldown or moves to qi.
- [ ] **C3 · `save.md`.** What the profile stores, where, and how old saves are upgraded.
- [ ] **C4 · `loot.md`.** Relics (遗物), food, and whether the three equipment slots (冠, 衣, 履)
  are worth having beside relics.
- [ ] **C5 · `run-map.md`.** How the player chooses a direction, the room types, how long a level
  and a run should be, and how level 2 differs from level 1.
- [ ] **C6 · `awareness.md`.** Hostile, neutral and passive enemies, and backstab (背刺).
- [ ] **C7 · `merit-karma.md`.** What rising merit and karma change in play, the peace (和平) and
  madness (疯魔) states, and how the three clears decide an ending.
- [ ] **C8 · `story.md`.** What the player learns and when: the sword's hints per level, the first
  soul, 鹤箬子, and the three endings as scenes. You write this one in Chinese first.
- [ ] **C9 · `art-direction.md`.** Written after the spike in E1.

## Phase D · Roguelike core (v0.2)

- [ ] **D1 · Profile save (M) [design: save].** A versioned save file separate from
  `settings.cfg`, with a migration test from version 1. Souls fallen and "ending seen" move into
  it. Everything else in this phase writes to it, so it comes first.
- [ ] **D2 · Sword-intent drops (M) [design: economy].** Enemies give 剑意; a HUD counter in the
  sword's style; the run summary shows it. Amounts in `run.tres`.
- [ ] **D3 · Spending in a run (M) [design: economy].** The upgrade screen follows the economy
  page: either priced choices or a free pick plus priced extras.
- [ ] **D4 · Sword qi (M) [design: sword-qi].** The bar, filled by deflects, shown beside posture.
  The flash step follows the design page.
- [ ] **D5 · 无懈 (M) [design: sword-qi].** For a short time every attack is deflected. It must
  tell the director that these answers carry no timing information.
- [ ] **D6 · 乾坤 (M) [design: sword-qi].** One wide cut that knocks back and sometimes stuns.
- [ ] **D7 · Upgrade pool to 20 (M, two batches of five).** Aim for pairs that combine, such as
  Dread with a wider finisher. Add rarity if the economy page asks for it.
- [ ] **D8 · Relics (M) [design: loot].** Rare run-long passives, five to start, dropped by elites
  later and by the boss now.
- [ ] **D9 · Between runs: 剑鸣 (M, maybe L) [design: economy].** A screen in the grove where kept
  sword-intent buys permanent boosts for every later soul. Keep the first version to five boosts.
- [ ] **D10 · Souls that differ (M).** Ten or more starter perks, some with a drawback; each soul
  gets a generated name and a look that differs by more than a tint. The sword comments on the
  perk once.
- [ ] **D11 · Balance by simulation (M, cloud).** A headless bot plays a few hundred seeded runs
  per build and reports win rate, run length and which upgrades win. A long job with a clear
  test, so it suits a cloud session overnight.
- [ ] **D12 · Tag v0.2**, new GIFs, a devlog post.

## Phase E · The road (v0.3)

- [ ] **E1 · Art-direction spike (M).** Before any new level is drawn: restyle the player, one
  enemy and one bamboo room three ways (the current pack with a palette pass, hand-edited pixel
  art, AI-assisted and then cleaned by hand) and compare screenshots. Write `art-direction.md`.
  It comes first here because every room built before the decision is a room to redo.
- [ ] **E2 · Choose a direction (M) [design: run-map].** After a room, two or three exits, each
  marked with what lies beyond. `RunPlan` becomes a small graph; the seed tests grow with it.
- [ ] **E3 · Elite rooms (M).** A stronger variant of each enemy with one extra move; drops a relic.
- [ ] **E4 · Shop (S–M).** Spend sword-intent on food, an upgrade or a relic.
- [ ] **E5 · Rest room (S).** A shrine: heal or refill the gourd, not both.
- [ ] **E6 · Awareness and backstab (M) [design: awareness].** Enemies can be unaware, neutral or
  passive; a cut from behind on an unaware one is a backstab that earns karma.
- [ ] **E7 · The wine seller 卖酒人 (M).** An event room. Honest sellers heal; false ones drug the
  wine and spring an ambush. Striking an honest one is karma.
- [ ] **E8 · More level 1 rooms (S, repeats).** From 10 templates to about 20, a few at a time.
- [ ] **E9 · Three new enemies (M each).** Chosen for a new thing to read, not a new sprite: the
  staff (棍, a chain of quick whites to deflect in rhythm), hidden weapons (暗器, small fast
  projectiles from mid range) and the hammer (锤, a slow red area you must leave).
- [ ] **E10 · Level 2: 树林 → 山间路 → 山顶庙 (L, split into four).** Tiles and atmosphere; eight
  room templates; enemy mixes and the Taoist (道士) who seals skills; the temple boss, which
  needs its own design page.
- [ ] **E11 · Run length check.** A two-level run should take 25–35 minutes. Tune with D11's bot.
- [ ] **E12 · Tag v0.3.**

## Phase F · The sword remembers (v0.4)

- [ ] **F1 · Merit and karma change play (M) [design: merit-karma].** High merit: more enemies
  surrender, and some are stunned instead of killed. High karma: enemies flee in fear and
  finishers happen on their own.
- [ ] **F2 · Peace 和平 (M).** Sometimes a room's enemies rise, turn and salute instead of fighting.
- [ ] **F3 · Madness 疯魔 (M).** The sword takes the player's hands for a few kills and refills qi.
  It must pause the director's learning while it lasts.
- [ ] **F4 · The hall of souls (M).** Every fallen soul is saved with its name, look, perk and
  where it fell, and can be seen from the title. The endings need this data.
- [ ] **F5 · The first soul (S–M) [design: story].** The sword mentions it more often as runs go by.
- [ ] **F6 · Face and name 捏脸 (M–L).** Depends on the art direction: layered parts if sprites
  allow it, otherwise a choice of ready-made looks. Only the first soul is made by hand.
- [ ] **F7 · The mortal ending 凡人 (L, split) [design: story].** Built first because it is the
  smallest: 鹤箬子 appears, the sword chooses, and both answers are short scenes.
- [ ] **F8 · The merit ending 功德 (L, split).** The first soul holds the sword against the waves
  of resentment and 风源虹·怨.
- [ ] **F9 · The karma ending 业障 (L, split).** The fallen souls return, then the first soul, then
  鹤箬子 as the last boss.
- [ ] **F10 · The sword's lines for all of the above.** You write the Chinese; English and Dutch
  follow.
- [ ] **F11 · Tag v0.4.**

## Phase G · Steam

G1 to G4 do not wait for phase F. Start them as soon as E1 has settled the look, because a store
page gathers wishlists for as long as it is up.

- [ ] **G1 · Store page (M, mostly your time).** Check that the name is free on Steam, open a
  Steamworks account (US$100, see PLAN section 4), and publish "Coming soon" with a capsule
  image, five screenshots, a 30–60 second trailer and the description in three languages.
- [ ] **G2 · Steam build (M).** `release.yml` uploads to Steam beside itch.io. Steam Cloud through
  Auto-Cloud (no code). Achievements through an open-source binding. Check the Linux build on a
  Steam Deck, or ask someone who owns one.
- [ ] **G3 · Steam demo (M).** Level 1 with the v0.2 loop, polished, with a wishlist button on
  the summary screen.
- [ ] **G4 · Next Fest.** A game may enter once, so enter the last one before release, not the first
  one available.
- [ ] **G5 · Decide: Early Access or straight to 1.0.** Decide after v0.4 with real numbers.
- [ ] **G6 · Remaining levels by the scope rule**, the final boss, credits, a last balance pass.
- [ ] **G7 · After release:** judge a mobile port by what sold. Until then, only avoid choices
  that would block it (PLAN section 9).

## Parking lot

In the original design but not planned. Each needs a reason to come back.

- Darts and thrown weapons (飞镖) for the player: a second combat system beside deflecting.
- Money beside sword-intent: one currency until the economy page says otherwise.
- Other weapons for the player.
- Levels 3 (大院) and 4 (衙门): see the scope rule.
- The remaining enemy weapons (剑, 斧, 弩, 鞭) beyond the three in E9.
- macOS and web builds; voice acting; online features.
