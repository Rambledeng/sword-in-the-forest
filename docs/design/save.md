# Profile save (roadmap item: D1)

Status: draft, written 2026-11-15, waiting for Ranbao's answers below

## Why
The only things remembered today are two values tucked into `settings.cfg`. Everything in
phase D needs a place to live that survives updates, so this comes first.

## What the player sees
1. Nothing new while it works. Progress is simply there on the next launch.
2. Settings has "Reset progress", with a second confirmation. It does not touch the settings
   or what the sword has learned about the player's reactions.
3. If the file is damaged, the game starts from the last good copy and says so in one line.
4. From the second sitting of D1: quitting in the middle of a run offers "Continue" on the
   title screen, at the start of the room the soul was in.

## Rules
1. **One file:** `user://profile.json`, UTF-8 JSON, with `"version": 1` as its first field.
   One profile, no slots.
2. **Three files, three jobs.** `settings.cfg` is the machine's preferences. `director.sav`
   is the estimate of the player's reaction time on this machine. `profile.json` is progress.
   Only the profile is meant for Steam Cloud: a reaction time measured on one screen is wrong
   for another.
3. **Version 1 holds:**
   - `intent_bank`, and `cry`: the rank of each 剑鸣 boost.
   - `souls_fallen` (仇) and `kills` (怨), lifetime.
   - `clears`: one record per won run (date, seed, merit, karma, verdict), and `ending_seen`.
   - `souls`: one record per soul (seed, name, perk, where it fell or that it won, merit,
     karma). The hall of souls (F4) and the endings read this, so it is kept from the start.
   - `unlocks`: Detail mode, and later anything else unlocked once.
   - `run`: empty, or a run in progress (rule 7).
4. **Rules without Godot.** Reading, writing, defaults and upgrading are in `src/Combat/Profile`
   using the existing `JsonLite`. The game only moves bytes to and from `user://`.
5. **Upgrading.** Loading runs `Migrate` step by step from the file's version to the current
   one. Every version that ever shipped keeps one sample file in the tests, forever.
   Unknown fields are kept when the file is written back.
6. **Safe writes.** Write `profile.json.tmp`, then rename it over the file; the previous file
   is kept as `profile.json.bak`. If loading fails, try the backup. If that fails too, rename
   the damaged file to `profile.broken-<date>.json` and start fresh. A file from a newer game
   version is never overwritten: the game says so and runs without saving.
7. **A run in progress** is the seed, the choices made on the road, the step reached, and the
   soul's state at the door: health, sips, qi, 剑意, upgrades, relics, merit, karma, perk. It is
   written on entering a room and removed when the run ends. Rooms come from the seed, so
   nothing else is needed. No saving inside a fight.
8. **Moving in.** On the first launch with a profile, `ending_seen` and `souls_fallen` are
   copied from `settings.cfg`. They stay there too, unread, so an older build still starts.
9. **When to write:** at the end of a run, after a purchase in the grove, on entering a room.
10. `--profile <path>` picks another file. Scenarios always use a temporary one.

## What it touches
- `UserSettings` and `DirectorService` stop owning `ending_seen` and `souls_fallen`.
- The summary screen and the title read counts from the profile.
- Every later item in phase D writes through this, never to a file of its own.
- CI: the sample-file test runs with the unit tests. G2 points Steam Auto-Cloud at one file.

## Not in this version
- Several profiles, import and export, cloud conflict handling beyond Steam's own.
- Encryption or checksums against editing. It is a single-player game and the code is public.
- Saving in the middle of a fight. Statistics beyond what the list above needs.

## Decisions for Ranbao
1. **Continue after quitting mid-run?** I recommend yes, as the second sitting of D1. A run
   will be 25 to 35 minutes by v0.3, and Steam players expect to be able to stop.
2. **Does "Reset progress" also forget the reaction estimate?** I recommend no; offer it as a
   separate button, for a player who hands the laptop to a friend.

## Open questions
For the playtest: none. This one is judged by nobody noticing it.

## Tests
- Unit: defaults for a missing file; round trip of every field; `Migrate` on the version 1
  sample; unknown fields survive; a damaged file falls back to the backup; a newer version is
  refused; a run in progress restores the same room from the same seed and choices.
- Unit: moving in from a `settings.cfg` with both values set.
- Scenario `profile_persist`: win a run, restart the scene tree, the clear and the bank are there.
- Scenario `run_resume`: quit in the third room, continue, same room, same soul.
