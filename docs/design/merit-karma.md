# Merit, karma and the three clears (roadmap items: F1, F2, F3, and the trigger for F7 to F9)

Status: draft, written 2026-11-15, waiting for Ranbao's answers below

## Why
Merit (功德) and karma (业障) change one closing line. The game promises that mercy and
cruelty change how the story ends, and the player should feel the world lean long before then.

## What the player sees
1. Gold glitter for merit and dark smoke for karma, as now. No meter outside Detail mode.
2. **A soul that keeps its hand:** more enemies give up, and some that would have died only
   fall senseless. Now and then a whole room rises, turns, and salutes (和平, peace).
3. **A soul that cuts down the kneeling:** enemies near a death may run. The sword starts to
   finish the broken by itself. Now and then it takes the soul's hands for a few kills (疯魔,
   madness), and hands them back with the bar full.
4. A soul that does some of both sees a little of both, and neither peace nor madness.
5. Each won run leaves a seal: gold, grey or dark. After the third seal the realm breaks and
   the ending that the seals call for begins.

## Rules
Numbers start in a new `game/tuning/karma.tres`; the rules replace `Lean` in `RunStats`.

1. **Points.** Merit: finishing a kneeling enemy 1, sparing a genuine beggar 3. Karma: cutting
   down a genuine beggar 3, a backstab 2, killing a bystander 5, striking an honest wine seller 5.
2. **A liar is not a beggar.** Cutting down a false beggar before it drops the act gives
   neither. Reading the tell is rewarded by the smoke that does not come.
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
7. **Karma tier 1:** when an enemy dies, each other enemy within 80 px flees with chance 0.25
   for 3000 ms. One that reaches the room's edge is gone and pays half. **Tier 2:** a broken
   enemy in reach is finished by the sword with chance 0.3, before the soul can choose. If it
   was begging, that is karma.
8. **Peace** needs merit tier 2 and a merit verdict so far. On entering a fight room (never an
   elite or a boss) the chance is 0.15. The enemies salute and do not fight. The room is
   cleared, pays in full and gives 2 merit. Striking one ends peace for the run and every
   enemy there counts as a bystander.
9. **Madness** needs karma tier 2 and a karma verdict so far. On a kill the chance is 0.2, at
   most once a room. For 3 kills or 6000 ms the soul moves and strikes by itself, uses skills
   without cost, and turns every attack aside. Then control returns and the bar is full.
10. **The ending** is decided by the three seals. Two or more alike decide it; three different
    ones give the mortal ending. It begins after the third clear's boss.
11. After an ending the three seals are cleared. Three new clears can lead to another ending.
    Souls, 仇, 怨 and the sword's cry stay.

## What it touches
- **Director.** Madness turns attacks aside for the player, so those are recorded as assisted
  and leave the estimate alone, like 无懈. Peace has no attacks. Fleeing enemies drop theirs.
- **Mercy rules:** the surrender chance now depends on the tier. **Economy:** rules 6 to 8.
- **Save file:** each clear's points and seal. **Summary and title:** the seals.
- **Verdict lines:** the six existing keys stay; only the rule that picks them changes.
- **Awareness (C6)** and the wine seller (E7) supply the backstab and bystander points.

## Not in this version
- Bosses that can be spared, and the larger points the first design gave "certain great
  enemies". That belongs to each boss's own page.
- A visible alignment meter. Shop prices or relic offers that depend on the lean.
- The endings as scenes. Those are `story.md`, which you write first.

## Decisions for Ranbao
1. **May the sword take the choice away?** At karma tier 2 it sometimes finishes a beggar by
   itself (rule 7). I recommend yes: it is what madness means, and it makes karma feed itself.
   It is also the rule most likely to anger a player, so it is yours to allow.
2. **Seals or a running total?** The first design counts the share of merit over three
   clears. I recommend one seal per clear and a majority, because a player can see and remember
   three seals. A running total is the alternative.
3. **Can a second ending be earned on the same save** (rule 11), or should the first be final
   until progress is reset?
4. **What does a fall inside an ending cost?** I recommend that the oldest seal is lost and
   one more clear is needed. The first design sends the soul back to the grove with no more said.

## Open questions
For the playtest: does anyone connect the salute to their own mercy? Is madness a thrill or
an insult? Is the mortal verdict where most players land, and is that right?

## Tests
- Unit: points per deed (rules 1, 2); the verdict at each boundary (3); tiers and the head
  start (4, 5); surrender chance per tier (6); the seals' majority with every combination (10).
- Unit: the evidence filter ignores attacks turned aside in madness.
- Scenario `merit_peace`: at merit tier 2 a forced peace room salutes, pays and counts as
  cleared; striking one enemy gives karma.
- Scenario `karma_madness`: a forced madness kills without input, returns control, fills the
  bar, and leaves the director's belief as it was.
- Scenario `three_seals`: three forced clears set the ending flag that the seals call for.
