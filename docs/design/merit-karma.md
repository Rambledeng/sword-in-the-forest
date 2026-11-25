# Merit, karma and the three clears (roadmap items: F1, F2, F3, and the trigger for F7 to F9)

Status: draft, written 2026-11-15; Ranbao's answers of 2026-11-25 are in; three decisions open

## Why
Merit (功德) and karma (业障) change one closing line. The game promises that mercy and
cruelty change how the story ends, and the player should feel the world lean long before then.

## What the player sees
1. Gold glitter for merit and dark smoke for karma, as now. No meter outside Detail mode.
2. **A soul that keeps its hand:** more enemies give up, and some that would have died only
   fall senseless. Now and then a whole room rises, turns, and salutes (和平, peace).
3. **A player who cuts down the kneeling:** the sword learns it, across souls. Even with little
   karma it starts to finish beggars by itself, with a red shiver first that the soul can
   answer by sparing. With high karma, enemies near a death may run. Now and then it takes the soul's hands for a few kills (疯魔,
   madness), and hands them back with the bar full.
4. A soul that does some of both sees a little of both, and neither peace nor madness.
5. Each won run leaves a seal: gold, grey or dark. Once three are set, the next win does not
   end at the last boss: the realm breaks (境破) and the road leads to the place of the ending
   the seals call for. Only three gold seals lead home to the Bitter Bamboo (苦竹林), where the
   journey began. That is the true ending, and most players will not reach it first.

## Rules
Numbers start in a new `game/tuning/karma.tres`; the rules replace `Lean` in `RunStats`.

1. **Points.** Merit: finishing a kneeling enemy 1, sparing a genuine beggar 3. Karma: cutting
   down a genuine beggar 3, a backstab 2, killing a bystander 5, striking an honest wine seller 5.
2. **A liar is not a beggar.** Cutting down a false beggar before it drops the act gives
   neither, and sparing one gives no merit (`mercy.md`). Reading the tell is rewarded by the
   smoke that does not come, and by the knife that does not.
3. **A run's verdict** comes from its karma share, karma ÷ (merit + karma): 0.20 or less is
   merit, 0.50 or more is karma, between is mortal (凡人). With no points at all it is mortal.
   Today's play fits: spare every beggar and it is merit, cut down every one and it is karma.
4. **Tiers inside a run.** Merit tier 1 at 8 points, tier 2 at 16. Karma tier 1 at 6, tier 2
   at 12. Both can be held at once.
5. **The sword remembers.** Each earlier clear with a merit seal starts a soul with 4 merit;
   each with a dark seal, with 4 karma.
6. **Merit tier 1:** the chance to surrender rises from 0.25 to 0.35. **Tier 2:** to 0.45, and
   an enemy whose health runs out is knocked senseless instead of killed with chance 0.5. It
   counts as defeated and pays in full, but adds nothing to 怨.
7. **The sword learns the hand** (decided 2026-11-25). The profile keeps the player's last 20
   beggars: cut down or spared, across all souls. The habit is the share cut down. Once 5 are
   known and the habit is 0.4 or more, a beggar within reach is taken by the sword with chance
   habit × 0.5 (plus 0.2 at karma tier 2). No karma tier is needed: it learns from the
   player, not from the run.
8. **The shiver.** Before it strikes, the blade shivers red for 400 ms. Sparing during the
   shiver holds it back; that counts as a spare and the habit learns it too. Otherwise the
   sword finishes the beggar, and it counts as the soul's deed: karma if it was genuine.
9. **Karma tier 1:** when an enemy dies, each other enemy within 80 px flees with chance 0.25
   for 3000 ms. One that reaches the room's edge is gone and pays half.
10. **Peace** needs merit tier 2 and a merit verdict so far. On entering a fight room (never an
   elite or a boss) the chance is 0.15. The enemies salute and do not fight. The room is
   cleared, pays in full and gives 2 merit. Striking one ends peace for the run and every
   enemy there counts as a bystander.
11. **Madness** needs karma tier 2 and a karma verdict so far. On a kill the chance is 0.2, at
   most once a room. For 3 kills or 6000 ms the soul moves and strikes by itself, uses skills
   without cost, and turns every attack aside. Then control returns and the bar is full.
12. **The ending run.** The seals are the last three clears. When three are set, the next run is
    the ending run. It plays as usual up to the last level's boss; then the realm breaks and
    one more stage follows, chosen by the seals:
    - **Three gold: home to the Bitter Bamboo (功德, the true ending).** The waves of 怨, then
      风源虹·怨, with the first soul holding the sword.
    - **Two or three dark: Mount Xiuduan (修断山, 业障).** The fallen souls, the first soul,
      then 鹤箬子.
    - **Anything else: the burnt farmstead (旧居, 凡人).** Where it happened. 鹤箬子 waits
      there and asks for the vow.
    One gold seal short of the true ending is therefore a mortal ending, not a merit one. That
    is on purpose: the true ending asks for a whole journey of mercy.
13. After an ending the three seals are cleared. Three new clears can lead to another ending.
    Souls, 仇, 怨, the habit and the sword's cry stay.

## What it touches
- **Director.** Madness turns attacks aside for the player, so those are recorded as assisted
  and leave the estimate alone, like 无懈. The sword's own finish is a short loss of control:
  the director is told, and no attack starts during the shiver. Peace has no attacks. Fleeing
  enemies drop theirs.
- **Mercy rules:** the surrender chance now depends on the tier; liars follow `mercy.md`.
  **Economy:** rules 6, 9 and 10.
- **Story:** the three ending places are new levels of one or two rooms each (F7 to F9). The
  journey's map and the meaning of each place are in `level-2.md`, "The journey".
- **Save file:** each clear's points and seal, and the last 20 beggars. **Summary and title:** the seals.
- **Verdict lines:** the six existing keys stay; only the rule that picks them changes.
- **Awareness (C6)** and the wine seller (E7) supply the backstab and bystander points.

## Not in this version
- Bosses that can be spared, and the larger points the first design gave "certain great
  enemies". That belongs to each boss's own page.
- A visible alignment meter. Shop prices or relic offers that depend on the lean.
- The endings as scenes. Those are `story.md`, which you write first.

## Decisions for Ranbao
Answered 2026-11-25: the sword may finish beggars by itself, at low karma, because it
learns from the player (rules 7 and 8); the bamboo ending is the true good ending (rule 12).

1. **Seals or a running total?** The first design counts the share of merit over three
   clears. I recommend one seal per clear and a majority, because a player can see and remember
   three seals. A running total is the alternative. Either way the true ending needs all
   three clears merciful.
2. **Can a second ending be earned on the same save** (rule 11), or should the first be final
   until progress is reset?
3. **What does a fall inside an ending cost?** I recommend that the oldest seal is lost and
   one more clear is needed. The first design sends the soul back to the grove with no more said.

## Open questions
For the playtest: does anyone connect the salute to their own mercy? Is madness a thrill or
an insult? Does the shiver read as the sword's will, and does anyone learn to stop it? Is the mortal verdict where most players land, and is that right?

## Tests
- Unit: points per deed (rules 1, 2); the verdict at each boundary (3); tiers and the head
  start (4, 5); surrender chance per tier (6); the habit over 20 beggars and its chance (7);
  the ending place for every combination of three seals (12).
- Unit: the evidence filter ignores attacks turned aside in madness.
- Scenario `merit_peace`: at merit tier 2 a forced peace room salutes, pays and counts as
  cleared; striking one enemy gives karma.
- Scenario `karma_madness`: a forced madness kills without input, returns control, fills the
  bar, and leaves the director's belief as it was.
- Scenario `sword_takes`: with a forced habit of 1.0, a beggar is finished after the shiver;
  sparing during the shiver holds it back and counts as a spare.
- Scenario `three_seals`: three forced clears send the next win to the place the seals call for.
