# Progress

## M1 polish · playtest fixes (2026-11-01)

Your six notes, in order. Each has a test.

1. **Attack spam no longer beats the bandit.** Stamina is posture:
   - The bandit **blocks** your strikes while it approaches or circles: no health damage, and
     posture damage × `BlockPostureFactor` 0.35, with a block spark and a guard pose. After
     `BlocksBeforeCounter` 3 blocks (within `BlockChainResetMs` 1500 of each other) it
     **counterattacks** with the new fast `saber_counter` (windup 100–180 ms, its own glint, lead
     380 ms). A chain of 3 staggers also ends in that counter.
   - Each of your swings costs `AttackPostureCost` 6 posture; endless spam breaks your own guard,
     and the bandit punishes a broken player at once with the counter.
   - To close the remaining leaks I also made three changes you didn't ask for. Each is in F1:
     a hit during its windup makes it strike at once (`HitInWindupStrikesNow`, same lead, so
     still fair) and does posture × `ArmoredPostureFactor` 0.5; extra hits during a stagger don't
     extend it; and after its attack **hits you**, its recovery is × `RecoveryAfterHitFactor` 0.35,
     so the full opening is earned by defending.
   - Still open, as the spec wants: the recovery after an attack you defended, and staggers.
   - Tests: `bot_spam` (a bot that only mashes attack dies; the bandit keeps 9 of 12 health
     after blocking 10 swings and countering 3 times). `deflect_break`: deflects alone break
     it in **5** (accepted range 4–6). `duel` (deflect, then cut when it is open) still finishes
     it. Slash deflect posture went from 22/24 to 25/27 to land on 5.
2. **Aim.** The mouse aims only while it moved or clicked within `MouseAimIdleMs` (2000).
   Otherwise, and on a gamepad with the right stick at rest, you aim along your movement, else
   your facing. Aim assist turns toward the nearest enemy in front: a 120° cone
   (`AimAssistDeg`) for movement/facing aim, 40° (`AimAssistExplicitDeg`) for mouse or stick,
   within `AimAssistRangePx` 56. The same assisted aim sets the guard and deflect facing. Test:
   `input_devices` (mouse aims after a motion and stops after the idle time).
3. **Diagonals.** Movement and attacks are free-angle (8-way on keys); sprites still show the
   nearest of 4 facings. The 4-way feel came from the mouse cursor still aiming. Test:
   `input_devices` (a diagonal stick and W+A both move and attack on the diagonal).
4. **White floor: found and fixed.** `ArenaFloor` loaded its texture into a local variable
   inside `_Draw`. Draw commands keep only the texture's ID, so when .NET's garbage collector
   freed the C# wrapper, the texture was freed and the floor drew white. The bamboo didn't,
   because sprites hold their textures; Esc→title rebuilt the floor. The floor now keeps its
   texture in a field. `Tunables` had the same weakness (a GC could silently throw away unsaved
   F1 edits); it now caches its resources too.
   - Test: `screens/FloorSoak` runs the arena for 2,400 frames, forcing a GC every second, then
     checks the grass pixels. Before the fix it failed (99% white); now it passes. It runs in
     CI. `tuning_panel` checks that an F1 edit survives a forced GC.
5. **Title:** left click (and J, X/RB, E, Enter) now starts the game. The full-screen controls
   swallowed mouse clicks before `_UnhandledInput` saw them. Test: `title_start`.
6. **Directional guard.** Guard and deflect only catch attacks within `GuardArcDeg` 220° (full
   angle) of your facing; rolls still evade from any side. Telemetry gains `angle_deg` (0 =
   facing the attacker, 180 = back turned; schema updated, now required). A deflect from
   behind logs `response: deflect, outcome: hit, angle_deg: ~180`, a positioning miss rather
   than a timing miss. Tests: `bot_guard_back`, unit tests in `ResponseWindowTests` and
   `TelemetryTests`.

### Numbers to tune in F1 (new)
- `player.tres`: AttackPostureCost 6, GuardArcDeg 220, MouseAimIdleMs 2000, AimAssistDeg 120,
  AimAssistExplicitDeg 40, AimAssistRangePx 56.
- `saber_bandit.tres`: BlockPostureFactor 0.35, BlocksBeforeCounter 3, BlockChainResetMs 1500,
  ArmoredPostureFactor 0.5, HitInWindupStrikesNow on, RecoveryAfterHitFactor 0.35.
- `attacks/saber_counter.tres`: the counter's windup, lead, damage and reach.

### What to test
1. Mash attack at a circling bandit: you should hear blocks, see it counter, and feel your
   posture bar fill.
2. Beat it properly: about 5 deflects should make it kneel, plus cuts in its openings.
3. Keyboard only (no mouse): move diagonally and attack; then touch the mouse and attack toward
   the cursor; leave it for 2 s and the aim returns to your movement.
4. Turn your back on a bandit and deflect: it should hit you. Roll instead: it should evade.
5. Play for a few minutes: the floor should stay green. Click to start from the title.

### Questions for the playtest
- Does the bandit now block too much, or counter too fast (`saber_counter` windup 100–180 ms)?
- Is a 220° guard arc right, or should it be narrower?
- Is "a hit during its windup makes it strike at once" readable, or does it feel like the bandit
  cheats?

### Known issues
- Recover (right after its attack) is unguarded by design; Approach and Circle block.
- An exported build still can't save F1 edits (unchanged from M1).

## M1 · Combat core (2026-10-31)

### Done
- **Assets.** Player: `NinjaGray` (grey, reads against the grass and as a pale wandering
  soul). Saber bandit: `CamouflageRed` (straw hat, 斗笠, the classic 刀客 look). Katana and big
  sword "in hand" sprites, slash/cut/smoke effects, grass and bamboo tiles, hearts and a gourd
  icon, 12 sounds and the "Dark Forest" track. Copies live in
  `game/assets/third_party/ninja_adventure/` (5.5 MB); `CREDITS.md` maps each one to its path in
  the pack. The full pack arrived as `Ninja Adventure - Asset Pack/` (≈2,200 files); it now has a
  `.gdignore`, so Godot no longer imports it.
- **Font.** Fusion Pixel 12 px `zh_hans` is the default font (`game/ui/theme.tres`), with
  antialiasing, hinting and subpixel positioning off. All seven variants are kept. Chinese is
  crisp at 12 px and 24 px: `docs/screens/title_zh_CN.png`, `arena_zh_CN.png`. CREDITS lists
  its OFL licence and the Ark Pixel, Cubic 11 and Galmuri licences.
- **Combat rules in `src/Combat`** (pure C#, netstandard2.1, 95 unit tests): attack timeline,
  `ThreatWindow` (the response window), `RollTimeline`, `PostureMeter`, `InputBuffer`,
  `AttackRunner`, telemetry record, JSON format and a small schema validator.
  `AttackRunner` gets lead times from `ILeadTimeSource`; `FixedLeadTimeSource` is the M1 version.
  M2 swaps in the director at `CombatServices.LeadTimes`.
- **Response window, as the spec says.** Any right response between the glint and the impact
  (both ends included) succeeds. A guard tap enters a deflect stance that catches the blow at
  the impact. A guard raised before the glint only blocks. A roll started in the window evades
  red or white. Red attacks hit a guard. The glint is stamped with the frame it shows on, and the
  impact lands on the first frame at or after glint + lead, so the window is never shorter than
  the lead.
- **Player:** an 8-direction state machine (free, attack, guard, deflect, roll, hurt, broken,
  drink, dead) with a 100 ms input buffer. It has a 3-hit combo with a small lunge and light aim
  assist, roll i-frames from 40 to 340 ms, posture, health and gourd. Aim follows the mouse or
  the right stick, switching with the last device used.
- **Saber bandit:** approach, circle, attack (a two-hit combo and an overhead chop, all white),
  recover, stagger (up to 3 in a row, then it counterattacks), kneel and dead. It tracks you
  through the windup and locks its aim at the glint, so spacing and rolls matter. Attack data
  lives in `game/tuning/attacks/*.tres`.
- **Feel:** hit-stop (60 ms hit, 90 ms deflect, 40 ms block, 180 ms finisher) freezes combat
  time. Also white or red hit flashes, knockback, trauma-based camera shake, a code-drawn glint
  (star and ring at the blade tip, with a chime on the same frame), the deflect spark, a block
  spark, sword trails, roll dust and afterimages. Enemy health and posture bars sit above the
  head, with a pulsing red finisher mark on a kneeling bandit. The HUD shows hearts, gourd sips
  and a centre-out posture bar.
- **Anti-mash:** a guard press within 200 ms of the previous one only blocks, so mashing guard
  never deflects (`bot_mash` scenario).
- **F1 tuning panel:** every number in `game/tuning/*.tres`, edited live, with Save writing
  the files. The game pauses while the panel is open.
- **Telemetry:** one JSON line per attack in `user://telemetry/<run>.jsonl`. The schema is
  `docs/telemetry.schema.json`. Unit tests validate every format combination, and the bot
  scenarios validate every line they write.
- **Scenarios (headless, in CI):** `bot_right_response`, `bot_never`, `bot_guard_red`, `bot_mash`,
  `duel`, `input_devices`, `tuning_panel`, `boot`, `localization`, run at a fixed 60 fps (the
  M1 polish adds `bot_spam`, `deflect_break`, `bot_guard_back` and `title_start`: 13 in all).
- **Screenshots:** `tools/screenshot.sh screens/Showcase "" 6000 x` stages a seeded fight and
  saves `docs/screens/combat_{glint,glint_red,deflect,cut,kneel,finisher,roll,hurt}.png`.
- Gamepad bindings now follow the spec table: X attack (RB too), LB guard, A roll, B spare,
  Y gourd, Start back. M0 had attack on RB, roll on B and interact on A.

### Status of "Done when"
- [x] A bot giving the right response 150 ms after each glint is never hit when the lead is
  200 ms or more. `bot_right_response` uses leads of 200, 217, 250, 300, 350, 420, 550, 650 and
  800 ms against the slash combo, the chop and a red thrust: 36 attacks, 0 hits. The unit test
  `AttackRunnerTests` repeats this frame by frame.
- [x] A bot that never responds is hit by every attack (`bot_never`: 12 of 12). A bot that
  guards against red attacks is hit by them (`bot_guard_red`: tapping and holding guard, 6 of 6).
- [x] Unit tests cover the response window and roll invulnerability at frame boundaries:
  `ResponseWindowTests`, `RollTimelineTests` and `AttackTimelineTests` (60 Hz frames,
  inclusive and exclusive edges).
- [x] Telemetry lines validate against their schema (`TelemetryTests` and every drill scenario).
- [ ] You can deflect, block, roll, break posture and finish a bandit with keyboard and with
  gamepad. Covered in CI by `duel` (bot breaks posture and finishes) and `input_devices`
  (simulated pad and keyboard/mouse events, including a deflect with each). **Needs your hands
  on a real gamepad.**

### What to test (30–45 min)
1. Title → press attack. Fight the bandit with keyboard and mouse: deflect (tap right click
   when the star flashes), block (hold before the flash), roll, break its posture, and finish
   it while the red dot shows.
2. The same with a gamepad: X attack, LB guard, A roll, Y gourd, right stick aim.
3. F1: in `combat.tres` set `FixedLeadOverrideMs` to 250, then to 600, and feel the difference.
   Set `ArenaEnemies` to 2 or 3 for a crowd. Save if you like the numbers.
4. Let a bandit kill you once (respawn after 2 s). Drink the gourd in a fight.
5. Run once with `--locale zh_CN` (or switch your OS language) to see the pixel font in game.
6. Open `user://telemetry/` (Godot: Project → Open User Data Folder) and check the lines.

### Numbers to tune in F1
- `player.tres`: **MoveSpeed 82**, **RollDistancePx 60**, **RollDurationMs 400**, RollInvuln
  40–340, Attack1–3 Windup 70/70/140 and Recovery 230/230/380, Damage 1/1/2, Posture 9/9/16,
  ComboCancelFromMs 90, AttackReachPx 26, **DeflectPostureFactor 0.25**, MaxPosture 100,
  PostureDrainPerSec 28, BrokenMs 1000, HurtMs 260, InputBufferMs 100,
  **DeflectMashLockoutMs 200**, MaxHealth 10, GourdHeal 4, DrinkMs 1100.
- `saber_bandit.tres`: **MaxHealth 12**, MaxPosture 100, PostureDrainPerSec 10, MoveSpeed 52,
  **CircleMinMs/MaxMs 500/1400** (aggression), **ChopChance 0.35**, AttackRangePx 30,
  StaggerMs 240, MaxStaggerChain 3, DeflectedRecoilMs 160, KneelMs 2200.
- `feel.tres`: **HitStopHitMs 60, HitStopDeflectMs 90**, HitStopBlockMs 40,
  HitStopFinisherMs 180, Shake* (0–1), ShakeMaxPx 4, Knockback*, FlashMs 90,
  **GlintVisibleMs 220**, SfxVolume 0.8, MusicVolume 0.35.
- `combat.tres`: **FixedLeadOverrideMs 0** (0 = designed leads), LatePressGraceMs 400,
  ArenaEnemies 1, RespawnDelayMs 2000.
- `attacks/*.tres` (per attack): WindupMin/MaxMs, **LeadDefaultMs** (slash 420/380, chop 520),
  SwingMs, RecoveryMs, Damage, PostureDamage, **DeflectPostureDamage** (22/24/40), ReachPx,
  ArcDeg, LungePx.

### Questions for the playtest
1. Does the glint read in time: the size and colour of the star, and the chime? Is the red one
   different enough?
2. The spec's window rule makes any guard tap between glint and impact a deflect, and the stance
   holds until the blow lands. Does that feel generous or sticky at long leads?
3. Hit-stop and shake: too much, too little?
4. Bandit pressure: circle time, chop chance, and how many deflects it takes to break it.
5. Sprites and sounds: are the NinjaGray player and CamouflageRed bandit right? I picked the
   sounds by analysing them, not by ear. The swing (`Grass2`) and glint (`Alert3`/`Alert2`)
   matter most; swap any file in `ninja_adventure/sounds/` under the same name.
6. Should the full `Ninja Adventure - Asset Pack/` folder stay in the repo? The spec says to
   commit only what the game uses, but the folder is handy for picking M3 assets.

### Known issues
- Combat time freezes during hit-stop. A glint that appears during another freeze gets up to
  90 ms of extra real time; telemetry records game time.
- Presses are timed at 60 Hz physics ticks (±17 ms).
- A guard press inside the window that the anti-mash rule turns into a block is logged with
  `press_ms` but `response: block`. M2 should decide how the director counts it.
- A roll in the window evades even when its i-frames end before a late impact. That is the spec's
  window rule; the roll usually carries you out of reach anyway.
- F1 Save writes to `res://`, so it works when running from the project, not from an export.
- Headless runs print "resources still in use at exit" warnings; they are harmless.
- Not yet done: per-spawn colour variants for enemies (M3), the surrender variant (M3) and a
  locale picker (M4).

### Next (M2 · The director)
- Implement `ILeadTimeSource` with the Director library and set `CombatServices.LeadTimes`.
- Feed `ThreatWindow` results (lead, press, response, outcome) into the filter.

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
- [x] `dotnet build` and `dotnet test` pass in the session (7 tests) and in CI.
- [x] Headless scenarios pass in the session and in CI.
- [x] One screenshot renders in the session (and in CI, uploaded as an artifact).
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
