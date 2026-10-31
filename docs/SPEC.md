# Sword in the Forest (林中剑): Demo Spec & Cloud Build Plan

Oct 31, 2026 · @Ranbao

## Summary

Build a polished vertical slice of **林中剑 / Sword in the Forest**, a top-down wuxia soulslike roguelike in Godot 4 with C#, on about $90 of the $100 cloud credit by Nov 3, 2026.

- **The slice:** the first level of your concept, 竹林 → 酒馆外 → 酒馆 (bamboo forest → outside the tavern → the tavern), ending in a boss fight. One run takes 10–15 minutes.
- **The hook:** the sword learns its wielder. A Bayesian director estimates each player's reaction time from about a dozen attacks, then times enemy attack warnings to a challenge level the player picks. It is your thesis problem, few samples and large differences between people, moved from fingertips to players.
- **Deliverables:** Windows and Linux builds on itch.io, a public GitHub repo with tests and CI, a short evaluation report with figures, and a README with GIFs.
- **Languages:** English, Dutch and Simplified Chinese, switchable in game.
- **Art and audio:** free CC0 packs now; original art later.
- **Deadline:** the credit expires at 23:59 Pacific on Nov 4, 2026 (08:59 on 5 November in Amsterdam). The demo should be complete by Nov 3, 2026, leaving 4 November for polish and as a buffer.

This doc doubles as the build brief. Export it as Markdown to `docs/SPEC.md` in the repo, and every cloud session builds against it.

## Goals, non-goals and definition of done

The demo is done when a stranger can download it from itch.io, play a 10–15 minute run in any of three languages, and see what the sword learned about them.

**Goals**

1. A complete loop: title → tutorial → three areas → boss → run summary → retry, with no dead ends.
2. Combat you are happy with: tight controls, readable warnings and satisfying deflects.
3. The adaptive director working live, visible in a debug overlay, and switchable off.
4. An evaluation on simulated players: three or four figures and a 2–4 page write-up.
5. Full localization in English, Dutch and Simplified Chinese, including fonts and layout.
6. Engineering a reviewer respects: tests, CI, a clean README, and credits for every asset.

**Not in this demo**

- Levels 2–4, the three endings, face customization (捏脸), equipment, food items, darts (飞镖), money, and the peace and madness states.
- Active skills (瞬步, 无懈, 乾坤). Flash step (瞬步) is first in line if budget remains.
- Original art, voice acting, achievements, a macOS build and a web build.

**Definition of done**

- [ ] Windows and Linux builds from itch.io run on a machine that has never seen the project.
- [ ] A full run is completable with keyboard and mouse, and with a gamepad.
- [ ] Every player-facing string reads correctly in all three languages, with no overflow at 1280×720 or 1920×1080.
- [ ] The game holds a steady 60 fps on a mid-range laptop.
- [ ] The overlay shows the director's estimate settling within the first area, and switching the director off restores fixed timings.
- [ ] `dotnet test` and the headless scenario tests pass in CI on every push.
- [ ] One command regenerates the evaluation figures and report.
- [ ] The README has 2–3 GIFs, controls, architecture and credits. Code is MIT-licensed, and `CREDITS.md` lists every asset with its licence.
- [ ] You have played three full runs and signed off on the feel.
- [ ] Credit spent stays at or under $95, with $90 as the target.

## Game design

The demo plays the opening of your concept: the first wandering soul (游魂) takes up the cursed sword 鸣不平 in the forest realm (林中境) and fights through to the tavern, deflecting more than dodging.

**Premise in the demo.** The sword has lain sealed in the bitter-bamboo forest (苦竹林) of Mount Xiuduan for sixty years. The Taoist 鹤箬子 lets the player in as the first wandering soul. The sword narrates and teaches; its backstory stays hidden, hinted at only in its lines.

**Pillars**

1. **Deflect, don't flee.** Sekiro-style posture combat seen from above. Reading the enemy's warning and deflecting it is the core skill.
2. **The sword knows its wielder.** The sword teaches you, comments on you and tunes its enemies to your reflexes (see the adaptive director).
3. **Mercy or karma.** A broken enemy can be spared or finished, and the sword remembers which (功德 / 业障).

**Core loop**

1. Enter a room; enemies engage on sight.
2. Read each warning, then deflect, block or roll; break the enemy's posture.
3. Finish the kneeling enemy, or spare one that surrenders.
4. Clear the room and move on. After each area, pick one sword-intent (剑意) upgrade.
5. Die, and the run summary shows what happened and what the sword learned. A new wandering soul starts a new layout.

**Player kit**

| Action | Keyboard and mouse | Gamepad | Rule |
| --- | --- | --- | --- |
| Move | WASD | Left stick | 8 directions; attacks aim at the cursor or the stick direction |
| Attack (攻击) | Left click | X / □ | 3-hit combo; damages health and posture |
| Guard (格挡) | Hold right click | LB / L1 | Blocks white attacks; each block fills your own posture bar |
| Deflect (弹反) | Tap guard after the glint, before impact | Same | No damage to you, heavy posture damage to the attacker |
| Roll (翻滚) | Space | A / ✕ | Invulnerable for most of the roll; the only answer to red attacks |
| Finisher (补刀) | Attack a kneeling enemy | Same | Kills the enemy |
| Spare | E | B / ○ | Lets a surrendering enemy go |
| Gourd (酒葫芦) | R | Y / △ | Heals; 3 sips per area; slow, so it can be punished |

Posture (架势) is the concept's stamina. It fills when you block or are hit and drains when you stop guarding. A full bar breaks your guard for about a second; on an enemy, it forces a kneel and opens the finisher.

**How every attack warns you.** All enemy attacks share four phases: a windup of random length, a glint (剑光) a fixed lead time before impact, the impact, then a recovery that is your opening. A white glint can be blocked, deflected or rolled. A red glint (杀招) is unblockable, so you must roll. The right response anywhere between the glint and the impact succeeds, so the lead time is exactly the player's reaction window. A guard raised before the glint only blocks. The director sets each attack's lead time; with it off, each attack uses its designed default.

**Enemies**

| Enemy | Weapon | Teaches | Attacks |
| --- | --- | --- | --- |
| Saber bandit (刀客) | Saber (刀) | Blocking and deflecting | Two-hit combo and a slow overhead chop, all white |
| Spearman (枪客) | Spear (枪) | Reading red warnings | Long thrust (red) and a sweep (white) |
| Archer (弓手) | Bow (弓) | Spacing and closing in | Aimed shot with a visible aim line (white, arrows can be deflected); backs off when you close in |
| Tavern boss | Twin cleavers | Everything above, faster | Phase 1: cleaver combos and a red charge. Phase 2 at half health: shorter windups, a three-hit flurry ending in red, and a thrown jar of drugged wine (蒙汗药) that slows you |

The boss is a placeholder; its name and story are yours to pick. A black-inn keeper (黑店) in the spirit of 孙二娘 from Water Margin would fit your wine-seller idea. Regular enemies get a random colour variant per spawn, as your concept's random looks.

**Mercy and karma (功德 / 业障).** A posture-broken enemy kneels for about two seconds, and attacking it is a finisher (+1 merit, since the realm's enemies are captured evil ghosts). One in four instead drops its weapon and begs (投降忏悔). Finish it for +1 karma, or spare it for +1 merit. The run summary counts merit, karma, kills (怨) and deaths (仇), and the sword's closing line leans toward its verdict.

**The sword speaks.** Short lines in a text box that never pause play: tutorial prompts, first deflect, first death, first spared enemy, area changes, the boss and the run summary. Plan on about 40 lines, all localized.

**Run structure**

1. **Tutorial**, the first bamboo-forest room. The sword talks you through each action while a weak ghost attacks with white warnings at varied timings. This doubles as the director's calibration.
2. **竹林 Bamboo forest**: two rooms of saber bandits.
3. **Upgrade**: pick one of three sword-intent upgrades.
4. **酒馆外 Outside the tavern**: two rooms mixing all three enemy types.
5. **Upgrade**: pick one of three again.
6. **酒馆 The tavern**: one room, then the boss.

Each room comes from 2–3 handmade templates per area, with random enemy sets and spawn points. The upgrade pool holds eight: +1 health, larger posture bar, faster posture recovery, +attack damage, +1 gourd sip, stronger deflects, healing on finishers, and flash step (瞬步) if built. The director's knowledge of you carries across runs; the layout and your soul's look do not.

**Title and intro (if budget allows).** Your title screen: the sword in the bamboo grove with a peeling seal and a skeleton beside it. On Start, the sword hums, the seal falls and a hand grips the hilt.

**Starting tuning values**, all editable in the live tuning panel:

- Response window: from the glint to the impact, as above.
- Roll: 400 ms long, invulnerable from 40 to 340 ms.
- Glint lead time: 350–550 ms by attack; the director may use 200–800 ms.
- Hit-stop: 60 ms on a hit, 90 ms on a deflect.
- Player health: 10; enemy hits deal 2–3.

## The adaptive director

The sword times every enemy warning to the player in front of it. It estimates the player's reaction time from each attack, then picks the lead time that gives the success rate they chose.

&#91;embedded content: director loop · four steps per attack, with its inputs and outputs\]

The loop runs once per attack. Calibration drives it during the tutorial; control takes over after that.

**The thesis link.** Your thesis predicts friction from small data that differs strongly between people: R² 0.91 on a random split, about 0.5 on unseen participants. The director faces the same gap with players. It answers the same way: a mechanistic model as the prior, a few calibration samples, then a personal estimate that keeps updating.

**What is measured.** For every attack aimed at the player: the lead time, whether they answered correctly, and when they pressed after the glint. A press after the impact still counts as a timed press; no press at all counts as a miss at the lead time. A guard raised before the glint is anticipation and carries no timing information.

**The model.** Response time is log-normal, a standard right-skewed reaction-time model, with median e^m and spread s. A lapse rate λ covers misses that have nothing to do with speed: looking away, the wrong button, being out of position. With d as the input delay of a frame or two:

```latex
P(\text{success} \mid L) = (1-\lambda)\,\Phi\!\left(\frac{\ln(L-d)-m}{s}\right)
```

**The filter.** The belief over m, s and λ lives on a grid of about 3,000 cells: medians from 150 to 900 ms, 12 spreads and 4 lapse rates. Each attack multiplies the grid by its likelihood and renormalizes, as a histogram filter does in robot localization. A slight blur along m before each update lets the estimate follow a player who improves or tires.

**The population prior.** A new player starts from a prior fitted to a training set of simulated players, refit from real playtest logs later. Their own evidence then takes over attack by attack, which is empirical Bayes: the hierarchical structure of your cross-participant problem.

**Calibration.** During the tutorial, the ghost's next lead time is the one expected to teach the most about the player's median, as the Psi method does in adaptive psychophysics. About a dozen attacks should narrow the estimate; M2's tests hold it to 40 ms after 15.

**Control.** After the tutorial, each attack gets the shortest lead time whose predicted success, averaged over the belief, reaches the target. The challenge setting sets that target: Relaxed 0.85, Standard 0.70, Hard 0.55. Lead times stay within 200–800 ms and within each attack's designed range.

**Fairness.** Only the reaction window adapts. Health, damage and attack patterns are the same for everyone, so reading the fight stays equally hard while the reflex demand fits the player. Switching adaptive timing off restores every designed default.

**What the player sees.**

- The attunement meter (人剑合一): how sure the sword is, from the width of its belief about the median.
- The sword's lines when its reading of the player changes.
- The run summary: the sword's reading of you, a median reaction time with its range.
- The F2 overlay for you and for interviews: the belief as a heat map, the estimate with its interval, predicted against observed success, and the latest lead time.

**Why not a neural network.** With a dozen samples per player and decisions made in real time that must be explainable, a small Bayesian model is the right tool. It is the same argument as your thesis's data-efficient models.

**Evaluation in M5**, on simulated players whose true values are known:

| Question | Experiment | Figure |
| --- | --- | --- |
| How fast does it learn a new player? | Estimate error against attacks seen: population prior only, player data only, and both; with and without calibration | Error against attacks |
| Does it hit the chosen challenge? | Success rate per player across 1,000 players, fixed against adaptive timing | Spread of success rates |
| Does it hold up on unfamiliar players? | A population shifted toward slower or faster players | Error against attacks, shifted |
| Does it survive a wrong model? | Bots drawing reaction times from an ex-Gaussian instead of a log-normal | Predicted against observed success |
| Does it work on a real person? | Your own play logs | Your belief after three runs |

## Technical architecture

Godot 4.7.2 with C#, plus a separate pure-C# Director library that the game, the simulator and the tests all share. Python only draws the report's figures.

&#91;embedded content: architecture · game, Director library, offline study, tests and CI\]

The game sends each attack's outcome to the library and gets the next lead time back. The simulator drives the same library offline, so the evaluation tests the exact code that ships.

**Toolchain**

| Part | Choice | Why |
| --- | --- | --- |
| Engine | Godot 4.7.2 .NET, Compatibility renderer | Scenes are text files Claude can edit, and the engine runs headless for tests |
| Language | C# on .NET 8 for the game; .NET Standard 2.1 for the Director library | The library drops into Unity unchanged |
| Unit tests | xUnit via `dotnet test` | Fast, and needs no engine |
| Scenario tests | Headless Godot scenes driven by bot players | Tests real combat without a screen |
| Analysis | Python with pandas and matplotlib | Your thesis tooling; Python is preinstalled in the cloud and the libraries install from PyPI |
| CI | GitHub Actions | Runs every check on each push and exports the release builds |

**Repo layout.** Paths named in the localization and art sections live inside `game/`.

```text
game/                  Godot project (C#)
  scenes/  scripts/  tuning/  localization/  assets/third_party/  assets/fonts/
src/Director/          Director library (.NET Standard 2.1, no Godot)
tests/Director.Tests/  xUnit tests for the library
sim/                   Simulator console app: simulated players to CSV
analysis/              Python: figures and report from the CSV
docs/                  SPEC.md, report.md, figures/, screens/
.github/workflows/     ci.yml on every push, release.yml on version tags
CLAUDE.md  PROGRESS.md  README.md  CREDITS.md  LICENSE
```

**Inside the game**

- **AttackRunner** drives every enemy attack through windup, glint, impact and recovery. It asks the director for the lead time and writes the telemetry line. Enemies differ only in their attack data and their state machine.
- **Player** is a state machine: idle, move, attack, guard, deflect, roll, hurt, broken, drink, dead. Inputs are buffered for about 100 ms so presses just before a state ends still count.
- **RunManager** owns the seed, the room templates, upgrades and the merit, karma, kill and death counters.
- **Autoloads** hold settings, the director service, telemetry, audio and the localization helper.
- **Tuning** numbers live in `game/tuning/*.tres` and change live in the F1 panel.

**Director interface**, a sketch that M2 may rename:

```csharp
var director = Director.FromPrior(prior);                  // belief on the grid
director.Observe(new ThreatEvent(leadMs: 420, pressMs: 365, correct: true));
int lead = director.NextLeadMs(attack, Mode.Control);      // or Mode.Calibrate
Estimate e = director.Estimate();                          // median, interval, attunement
```

**Telemetry.** One JSON line per attack aimed at the player, written to the game's user folder and never sent anywhere. A settings button opens the folder so a playtester can choose to share it.

```json
{"run":"r12","t":184.2,"attack":"saber_chop","glint":"white","lead_ms":420,"press_ms":365,"response":"deflect","outcome":"success"}
```

**CI.** `ci.yml` builds, runs the unit tests, the headless scenarios and the localization checks on every push. `release.yml` runs on a version tag: it exports Windows and Linux builds, attaches them to a GitHub Release and uploads them to itch.io.

## Localization

Every player-facing string goes through a translation key from the first milestone, and the Dutch and Chinese text lands in M4. Retrofitting keys late is what makes localization expensive.

**Pipeline**

- One file, `localization/strings.csv`, with columns `keys,en,nl,zh_CN`. Godot imports it as three translations.
- Code and scenes use keys only, such as `ui.menu.start` or `sword.first_deflect`, never literal text. Keys are grouped by prefix: `ui.`, `combat.`, `sword.`, `upgrade.`, `summary.`.
- Values with numbers use named placeholders like `{kills}`, never string concatenation, so each language can order words its own way. Labels such as "Kills: 3" avoid plural forms.
- First launch follows the operating system's language (Dutch, Chinese, otherwise English). A picker on the title screen and in settings switches instantly, without a restart.
- The developer overlay and tuning panel stay in English.

**Font.** [Fusion Pixel Font](https://github.com/TakWolf/fusion-pixel-font), 12 px proportional, Simplified Chinese variant. It is OFL-1.1 licensed and covers Latin, including Dutch accents, and Simplified Chinese in one pixel face, so all three languages keep the same look. Commit the font file and its licence under `assets/fonts/`.

**Layout.** Dutch strings often run longer than English, and Chinese needs taller lines. UI containers size to their text, with no fixed-width labels, and every screen is checked in its longest language.

**Who translates**

- Claude drafts Dutch and Chinese.
- You review the Chinese as a native speaker, above all the sword's lines, which should read as wuxia rather than as a translation.
- Dutch needs a native reader: a classmate can check about 40 sword lines and the UI in half an hour. Until then, `CREDITS.md` says the Dutch is machine-drafted and awaiting review.

**Checks**

1. A test fails if any key lacks one of the three languages, or if placeholders differ between languages.
2. A test fails if a scene or script shows text that is not a known key.
3. Godot's built-in pseudolocalization runs in debug builds to expose untranslated or overflowing text.
4. In M4 the session renders each menu and the run summary in all three languages, if the cloud machine can render; you look over the screenshots.

## Art and audio

The [Ninja Adventure](https://pixel-boy.itch.io/ninja-adventure-asset-pack) pack by Pixel-Boy is the base for everything visual and audible: it is CC0, 16 px, and its East Asian look suits a wuxia setting. Gaps are filled from other CC0 sources or drawn in code.

**What the pack gives.** 45+ characters with facesets, 20+ monsters, 6 bosses, exterior and interior tilesets, 30+ visual effects, a UI theme, 20+ sound effects and 25 music tracks. Its page doesn't list bamboo or a tavern by name, so check those first.

**Licence policy**

1. CC0 first. CC-BY is fine with credit in `CREDITS.md`.
2. Nothing whose licence forbids redistribution or commercial use. The repo is public, and committing such files counts as redistributing them.
3. Each pack lives under `assets/third_party/<pack>/` with its licence text and source link.
4. Commit only what the game uses. A lean asset tree keeps the repo small and keeps each session's context, and so its cost, down.

**Making the art fit the combat**

- Weapons are separate sprites that swing and rotate toward the aim direction, with a slash effect. This covers saber, spear and bow without per-weapon character animations.
- The glint, the deflect spark and the posture bars are simple shapes and particles drawn in code.
- The glint plays a sharp chime on the same frame as its flash. The director models reaction to the combined cue, and the sound also helps players who track the screen less well.

**Presentation.** A 480×270 base resolution with 16 px tiles, scaled by whole numbers (4× at 1080p, letterboxed below), with nearest-neighbour filtering, a pixel-snapped camera and Godot's Compatibility renderer.

**Audio.** The pack's sound effects first, plus Kenney's CC0 packs for impacts if needed. Three music cues: the bamboo forest, the tavern, and the boss. Master, music and effects volumes each get a slider.

**Your intake task, about 45 minutes before M1.** The cloud machine may not be able to download from itch.io or from other people's GitHub repos, so assets reach it through your repo.

- [ ] Download Ninja Adventure from itch.io.
- [ ] Pick the player, the saber bandit, the spearman, the archer and the boss, plus the tilesets, effects, UI, about 15 sound effects and 3 music tracks. Choosing the look is your call, not Claude's.
- [ ] Copy those files, with the licence, into `assets/third_party/ninja_adventure/`.
- [ ] Download the Fusion Pixel 12 px proportional SC font file and its OFL licence into `assets/fonts/`.
- [ ] Commit and push.

`CREDITS.md` lists each pack with its author, licence and link, and the in-game credits screen reads from it.

## Milestones

Six milestones, each one fresh cloud session except M3, which takes two. A session stops at its "Done when" list, pushes a branch and opens a pull request; you playtest the branch before merging.

### M0 · Toolchain and skeleton

Prove that a cloud session can build, test and run the game without a screen, so every later session can check its own work.

**Build**

- A cloud environment with the setup script from the appendix: the .NET SDK, Godot 4.7.2 .NET and a virtual display for screenshots. It must finish in under five minutes so the environment is cached.
- If downloading Godot from GitHub is refused, pull a Godot CI image from Docker Hub instead. If no Godot runs in the session at all, GitHub Actions becomes the build machine and the session reads results with `gh api`.
- The repo layout from the architecture section. The Godot project uses C#, the Compatibility renderer, 480×270 with integer scaling, and an input map for keyboard, mouse and gamepad.
- A scenario runner: a test scene loads, runs a set number of frames without a window and exits with 0 or 1.
- A screenshot command that writes PNGs to `docs/screens/`, if the machine can render.
- GitHub Actions running the build, unit tests and scenarios on every push.
- `CLAUDE.md`, `PROGRESS.md`, a `README.md` stub, an MIT `LICENSE`, `CREDITS.md`, and `localization/strings.csv` with one key in three languages.

**Done when**

- [ ] `dotnet build` and `dotnet test` pass in the session and in CI.
- [ ] A headless scenario passes in the session and in CI.
- [ ] One screenshot renders in the session, or `PROGRESS.md` records why not.
- [ ] One label shows its text in all three languages and under pseudolocalization.

**Your part.** Before: create the repo, install the Claude GitHub App on it, create the cloud environment with the setup script, install Godot 4.7.2 .NET and the .NET SDK on your laptop, and note your credit balance. After: check the balance again. That first number calibrates the whole budget.

### M1 · Combat core

One player against saber bandits in a test arena, with combat you enjoy.

**Build**

- The player: movement, three-hit combo, guard, deflect, roll, posture, health and gourd, aiming by cursor or stick.
- The shared attack anatomy as data: one resource per attack with its windup range, glint colour, default lead time, damage, posture damage and hit area.
- The saber bandit, with a state machine: approach, circle, attack, recover, stagger, kneel.
- Feedback: hit-stop, flash, knockback, a small camera shake, the deflect spark and sound hooks.
- Posture break, kneel and finisher.
- A live tuning panel on F1 for timings, speeds and damage; Save writes the tuning file.
- Telemetry: one JSON line per attack aimed at the player, with its lead time, the response, the press time after the glint and the outcome.
- Scripted bot players for tests, which respond to glints after a set delay.

**Done when**

- [ ] A bot giving the right response 150 ms after each glint is never hit when the lead time is 200 ms or more.
- [ ] A bot that never responds is hit by every attack, and a bot that guards against red attacks is hit by those.
- [ ] Unit tests cover the response window and roll invulnerability at frame boundaries.
- [ ] Telemetry lines validate against their schema.
- [ ] You can deflect, block, roll, break posture and finish a bandit, with keyboard and with gamepad.

**Your part, 30–45 minutes.** Play the arena, tune in F1 and save, then write your three to five most important notes for the next session.

### M2 · The director

The sword learns the player, live and tested.

**Build**

- The Director library, pure C# with no Godot types: reaction model, histogram filter, calibration picker, control law and save format.
- Wiring: telemetry events update the filter, and every attack asks the director for its lead time.
- Tutorial calibration: the ghost's lead times are chosen to learn the most from each attack.
- A debug overlay on F2: the estimate with its interval, the posterior, predicted against observed success, and the current lead time.
- The attunement meter (人剑合一) in the HUD, showing how sure the sword is.
- Settings: adaptive timing on or off, and challenge set to Relaxed, Standard or Hard.
- The estimate persists between runs.

**Done when**

- [ ] On 1,000 simulated players, the median reaction-time estimate is within 40 ms of the truth after 15 attacks for at least 80% of them.
- [ ] With the director on, at least 80% of simulated players finish 60 attacks within 10 points of the target success rate, a clearly tighter spread than fixed timing.
- [ ] One update plus one decision takes under 0.5 ms.
- [ ] With adaptive timing off, every attack uses its designed lead time exactly.

**Your part.** Play with the overlay on and check whether the estimate matches how you felt. Then play one room deliberately slowly and watch it adapt.

### M3 · The run, in two sessions

A complete run from the tutorial to the boss and back.

**Session A, rooms and flow**

- Room templates, two or three per area, with doors, transitions and spawn sets; each run is assembled from a seed.
- HUD: health, posture, gourd sips and attunement, plus the sword's text box.
- The upgrade choice after areas 1 and 2, the run summary, and retry with a new seed.
- Title and pause menus that work, with art later.

**Session B, enemies and choices**

- The spearman with its red thrust, the archer with its aim line and deflectable arrows, and the two-phase boss.
- Surrender, spare and finish, with the merit and karma counters.

**Done when**

- [ ] Twenty seeds produce valid runs: every room reachable, every spawn on open floor.
- [ ] An invulnerable bot finishes a seeded run without a window and reaches the summary screen.
- [ ] Both boss phases trigger, and beating the boss ends the run with a win summary.
- [ ] You finish a full run in 10–15 minutes.

**Your part.** Two full runs. Note the difficulty, whether red and white glints read clearly, and whether the boss feels fair.

### M4 · Languages, menus and polish

It looks, sounds and reads like a game, in three languages.

**Build**

- Dutch and Chinese for every key, including about 40 sword lines, plus the pixel font and the language picker.
- Settings for language, volumes, challenge, adaptive timing, overlay and controls, saved between launches.
- Audio: music per area, a sound on every action, and the glint chime.
- A feel pass: particles, shake, camera lead, and the death and boss-intro moments.
- The concept's title screen and intro, if the budget chart allows.
- Screenshots of every menu in all three languages.

**Done when**

- [ ] The localization tests pass.
- [ ] Screenshots show no overflow in any language at 1280×720 and 1920×1080.
- [ ] Settings survive a restart.

**Your part.** Review the Chinese lines (about 45 minutes), play once in Chinese and once in Dutch, and line up a Dutch reader.

### M5 · Evaluation and release

The research write-up and a public build.

**Build**

- The simulation study from the director section: a C# simulator over the Director library writes CSV, and a Python script turns it into figures.
- `docs/report.md`, two to four pages with its figures, regenerated by one command.
- The README: pitch, 2–3 GIFs, controls, how the director works, architecture, build steps and credits. GIFs come from Godot's Movie Maker mode if the cloud machine can render; otherwise you record them.
- A release workflow: a version tag makes CI export Windows and Linux builds to a GitHub Release, then upload them to itch.io with butler, using your itch.io API key stored as a repository secret.
- Cleanup: prune unused assets and update `CREDITS.md`.

**Done when**

- [ ] Release `v0.1.0` has Windows and Linux builds you have launched yourself.
- [ ] The itch.io page is live.
- [ ] The report and figures regenerate from one command.
- [ ] Every item in the definition of done is ticked.

**Your part.** Write the itch.io page from Claude's draft, do a final playthrough, make the repo public, and post a devlog on your site.

## Budget and session playbook

Plan to spend $90: $82 across the six milestones and $8 in reserve, checked against a cut line after every session.

**Is $100 enough?** Probably, if sessions stay focused. At the listed Opus 5.5 rates ($4 per million input tokens, $20 per million output, $0.20 per million cached reads), an agent step that re-reads about 70,000 tokens of context costs roughly 5 cents. $90 then buys about 1,800 steps. These are rough figures from a [third-party summary](https://www.eesel.ai/blog/claude-opus-5-5-pricing) of the price list; your M0 and M1 balances are the real test.

Overrunning costs no money. When the credit runs out, cloud sessions draw on your Pro plan's normal limits instead, with no extra charge unless you have turned on usage credits ([help center](https://support.claude.com/en/articles/17152539-cloud-sessions-bonus-credit-promotion)).

**Checkpoints.** After each session, read the balance in the usage menu, work out the total spent, and compare it with the cut line.

| Checkpoint | Envelope | Planned total | Cut if total is above | What to cut | Your total |
| --- | --- | --- | --- | --- | --- |
| After M0 · Toolchain | $6 | $6 | $10 | Stop fighting the sandbox; make GitHub Actions the build machine |  |
| After M1 · Combat core | $16 | $22 | $30 | The archer and the title intro; boss phase 2 keeps two attacks |  |
| After M2 · Director | $16 | $38 | $48 | Information-seeking calibration (use a fixed ladder of lead times); the report drops to two figures |  |
| After M3 · The run | $20 | $58 | $68 | M4's feel pass and title art; you record the GIFs |  |
| After M4 · Polish | $13 | $71 | $80 | M5 keeps the release and README; the report shrinks to one page |  |
| After M5 · Release | $11 | $82 | $90 | All feature work |  |
| Reserve | $8 | $90 | $95 | Everything except fixes |  |

**How to run the sessions**

1. **Plain cloud sessions only.** Projects, Routines and Remote Control sessions don't draw on this credit.
2. **One milestone per fresh session.** State lives in the repo, in `docs/SPEC.md`, `PROGRESS.md` and `CLAUDE.md`, not in chat history. A fresh session re-reads a few files instead of carrying hours of context into every step.
3. **Opus 5.5 at medium effort** for building. Raise the effort only for M2's maths. Sonnet 5.5, at about half the price, is enough for M4's translations and M5's README. Avoid Fable 5.1: the credit covers it, but it costs about 2.5 times as much as Opus per token.
4. **Plan, then build.** The kickoff message in the appendix asks for a plan and lets the session start without waiting for you.
5. **Batch your feedback** into one ranked list per playtest. Every message makes the session re-read its whole context.
6. **Tune in the F1 panel**, not through chat. Numbers you change yourself cost nothing.
7. **Run one session at a time** until the M1 checkpoint. After that, at most one extra session for separate work on its own branch, such as the simulation study while M4 polishes.
8. **Stop a session that circles** one error for 20 minutes. Write the error down and restart with a narrower instruction.

## Schedule

Seven sessions over four days put the demo together by Tuesday evening, with Wednesday as buffer before the credit expires on Thursday morning.

&#91;embedded content: schedule · Sat 31 Oct to Thu 5 Nov 2026, Amsterdam time\]

Sessions run while you do other work; your part is the right-hand column, about one to two hours a day. If a day slips, drop Wednesday's stretch goals first, then fold M4's polish into M5.

## Risks

The likeliest failure is combat that doesn't feel good yet when M1 ends, so the reserve goes there first. Risks are listed most likely first.

| Risk | Early sign | Response |
| --- | --- | --- |
| Combat doesn't feel good after M1 | Your M1 notes are mostly about feel | Tune in F1 first; then spend reserve on a second M1 session before M2; cut the archer before anything else |
| Thesis, drone and Battlecode squeeze your time | A playtest slips by a day | Sessions run while you work; each playtest takes 30–45 minutes; M4 and M5 can merge with lighter review |
| Spend runs ahead of plan | A checkpoint lands above its threshold | Apply that checkpoint's cut; stop feature work at $95 |
| Godot or .NET won't install in the cloud | M0 setup fails | Docker image, then GitHub Actions as the build machine |
| The cloud machine can't render | No screenshot in M0 | Keep the headless tests; you take screenshots and GIFs locally, for example with OBS |
| The director feels unfair, or invisible | "It's cheating" or "it does nothing" | Overlay and attunement meter, bounded lead times, the challenge setting and the off switch |
| The evaluation looks circular, testing the model on its own assumptions | You can't say how you know it works on people | Test against bots built on a different reaction-time model, a shifted population, and your own play logs |
| Asset gaps: bamboo, tavern interior, weapon poses | Found during intake | Effects drawn in code, other CC0 packs, recolours; placeholders are fine in a demo |
| Chinese text too small at 480×270 | M4 screenshots | Draw the UI layer at native resolution |
| The credit expires mid-session | Work still open on Wednesday evening | The plan ends Tuesday; the hard stop is 08:59 Thursday in Amsterdam |

## After the demo

If credit remains on Wednesday, spend it in this order, one item per session, and stop when the balance nears zero.

1. Flash step (瞬步), powered by a sword-qi (剑气) bar that deflects fill.
2. The concept's title intro, if M4 skipped it.
3. Backstab (背刺) on unaware enemies, earning karma.
4. The drugged-wine seller (卖酒人) outside the tavern.
5. A Chinese README.
6. A model comparison, log-normal against ex-Gaussian reaction times, echoing your thesis's comparison of model families.

**After the credit**

- A small pilot with friends playing, with their consent and an opt-in log export. Publishing it academically would need TU Delft ethics approval.
- Level 2 (树林 → 山间路 → 山顶庙), the merit and karma endings, and face customization.
- Original art once the design settles.
- Your Unity plans: the Director library targets .NET Standard 2.1, so it drops into Unity unchanged.

**How it reads in applications**

- **Paradox and CDPR:** game AI, player modelling and a shipped, localized demo, next to your CK3 mod.
- **Lely:** Bayesian state estimation with a histogram filter, test automation with bots, and CI.
- **PhD committees:** few-shot personalization under large differences between people, with an honest evaluation.

## Appendix

### A · Cloud environment setup script

Paste this into the environment's **Setup script** field before M0, with network access left at **Trusted**. M0 checks that Godot 4.7 accepts .NET 8 and adjusts the script if not.

```bash
#!/bin/bash
# Sword in the Forest: cloud setup (Ubuntu 24.04, runs as root).
# Must finish in about five minutes so the environment is cached.
GODOT_VERSION=4.7.2
GODOT_NAME="Godot_v${GODOT_VERSION}-stable_mono_linux_x86_64"

apt-get update -qq
apt-get install -y -qq dotnet-sdk-8.0 unzip xvfb libgl1 libegl1 libgl1-mesa-dri \
  libx11-6 libxcursor1 libxinerama1 libxrandr2 libxi6 libxext6 libxrender1 \
  libfontconfig1 > /dev/null || true

mkdir -p /opt/godot && cd /opt/godot || exit 0
for REPO in godot-builds godot; do
  curl -fsSL -o godot.zip \
    "https://github.com/godotengine/${REPO}/releases/download/${GODOT_VERSION}-stable/${GODOT_NAME}.zip" && break
done
if [ -s godot.zip ]; then
  unzip -q godot.zip && rm -f godot.zip
  ln -sf "/opt/godot/${GODOT_NAME}/Godot_v${GODOT_VERSION}-stable_mono_linux.x86_64" /usr/local/bin/godot
else
  echo "Godot download failed: use the M0 fallbacks"
fi
godot --version || true
exit 0
```

### B · CLAUDE.md for the repo

M0 writes this file and replaces the planned commands with the real ones.

```text
# Sword in the Forest: working rules

Read docs/SPEC.md and PROGRESS.md before anything else. Work only on the
milestone named in the kickoff message, and stop at its "Done when" list.

## Build and test (M0 makes these real)
- dotnet build and dotnet test pass before every commit.
- Scenario tests run headless: godot --headless --path game -- --scenario <name>
  Exit code 0 means pass.
- Screenshots, if the machine can render:
  xvfb-run -a godot --path game -- --screenshot <scene>  -> docs/screens/

## Conventions
- C# with nullable reference types on; one public type per file.
- src/Director targets .NET Standard 2.1 and never references Godot.
- Player-facing text is a key in game/localization/strings.csv (en, nl, zh_CN).
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
```

### C · Kickoff message for each session

```text
Milestone M1 · Combat core.
Read CLAUDE.md, the M1 section of docs/SPEC.md, and PROGRESS.md.
Reply first with a numbered plan mapped to M1's "Done when" items,
then start building without waiting for me.
Notes from my last playtest, most important first:
1. ...
2. ...
```

### D · Playtest routine

1. Play 10 minutes with keyboard and mouse and 5 with a gamepad.
2. Write the three to five things that most hurt the experience, most important first.
3. For each, say what you expected and what happened.
4. Tune numbers in F1, save, and list what you changed.
5. From M2 on, check whether the overlay's estimate matches how you felt.
6. Paste the notes into the next kickoff message.

### E · Glossary

| Term | Meaning |
| --- | --- |
| Vertical slice | A short section of a game built to final quality, standing in for the whole |
| Tell, or telegraph | The visible warning before an attack; here, the glint |
| Lead time | Time from the glint to the impact, which is the player's reaction window |
| Posture | Sekiro's guard meter; when it fills, the guard breaks |
| i-frames | Moments of invulnerability, here during a roll |
| Hit-stop | A freeze of a few frames on impact that makes hits feel heavy |
| Juice | Small feedback effects, such as shake, flash, particles and sound, that make actions feel good |
| State machine | Enemy logic that switches between named states such as approach, attack and recover |
| Integer scaling | Enlarging pixel art by whole numbers so pixels stay square |
| Headless | Running the engine without a window, for automated tests |
| Pseudolocalization | Fake translations that expose hard-coded text and overflow |
| Posterior | What the director believes about the player, given the evidence so far |
| Histogram filter | A Bayes filter over a grid of possible values, as in robot localization |
| Lapse | A miss unrelated to speed, such as looking away |

## Sources

- [Ninja Adventure asset pack](https://pixel-boy.itch.io/ninja-adventure-asset-pack), itch.io
- [Fusion Pixel 12px Proportional SC](https://fontsource.org/fonts/fusion-pixel-12px-proportional-sc/about), Fontsource, and its [GitHub repository](https://github.com/TakWolf/fusion-pixel-font)
- [Download Godot 4 for Linux](https://godotengine.org/download/linux), Godot Engine
- [Configure cloud environments](https://code.claude.com/docs/en/cloud-environments.md), Claude Code docs
- [Cloud sessions bonus credit promotion](https://support.claude.com/en/articles/17152539-cloud-sessions-bonus-credit-promotion), Claude Help Center
- [Claude Opus 5.5 pricing](https://www.eesel.ai/blog/claude-opus-5-5-pricing), eesel, a third-party summary of Anthropic's price list
