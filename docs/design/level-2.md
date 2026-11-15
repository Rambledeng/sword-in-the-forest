# Level 2: 树林 → 山间路 → 山顶庙 (roadmap item: E10, with E6 and E9)

Status: draft, written 2026-11-15, waiting for Ranbao's answers below

## Why
A second level that is level 1 in new colours would double the length of a run and add
nothing to learn. Level 2 has to ask a new question of each thing the game is about: the
deflect, the sword's skills, and mercy.

## What the player sees
1. **The woods (树林).** Wide rooms among trunks. Not everyone here is an enemy: a woodcutter
   keeps working, a pilgrim walks past. The first staff fighters, whose blows come in a rhythm.
2. **The mountain path (山间路).** Long, narrow rooms with a drop on one side. Throwers of
   hidden weapons stand far down the path. Whoever is pushed over the edge is gone.
3. **The summit temple (山顶庙).** Courtyards with pillars. Taoists who seal the sword's qi.
   The hammer, whose slow red blow covers ground that must be left.
4. The boss at the top. It gets its own page.

## Rules
The road follows `run-map.md`: 3, 3 and 1 steps, then the boss. Numbers go in the enemy files
in `game/tuning/` and a new `level2.tres`.

1. **Each area teaches one thing.** Woods: who to leave alone, and deflecting in rhythm.
   Path: space and edges. Temple: fighting without qi.
2. **Bystanders (woods).** Neutral and passive figures follow `awareness.md` (C6). Killing one
   is karma. They never appear in level 1.
3. **The staff (棍)** attacks in chains of 3 quick whites with even gaps. Each one deflected
   breaks more posture than the last. The director sets the lead of the first; the gaps are fixed.
4. **Edges (path).** A marked edge tile is a fall. An enemy knocked onto it is defeated and
   pays 剑意 as a health kill. The soul cannot roll or be knocked over an edge: it stops there.
5. **Hidden weapons (暗器)** are thrown in fans of 3 from mid range. Each can be deflected
   or stepped past.
6. **The Taoist (道士)** casts a seal with a visible wind-up of 1200 ms. If it finishes, the
   qi bar is locked for 6000 ms: nothing is gained and no skill can be used. Hitting the
   Taoist during the wind-up breaks the seal. At most one Taoist in a room.
7. **The hammer (锤)** marks a red circle and strikes it after the director's lead. The answer
   is to be outside it. It cannot be deflected.
8. **Enemy mixes** grow as in level 1: woods rooms hold 2 or 3, path rooms 3, temple rooms 3
   or 4 with at most one Taoist and one hammer.
9. **Room templates:** 3 woods, 3 path and 2 temple to start, 8 in all.

## What it touches
- **Qi:** the lock in rule 6. **Economy:** the fall in rule 4. **Mercy:** bystanders in rule 2.
- **Director:** the staff's later blows and the fan's later blades follow the first by fixed
  gaps, so only the first of each is evidence. The hammer is a new kind of answer (leaving an
  area) and needs its own entry in the evidence rules before it is built.
- **Room validator:** edge tiles, and a check that the soul can always walk to every exit.
- **Art:** three tile sets and four enemies. Not drawn before E1 settles the look.

## Not in this version
- The other enemy weapons (剑, 斧, 弩, 鞭). Weather. Rooms with several floors.
- The soul falling off an edge. Bystanders who fight back in groups.
- Levels 3 and 4, which wait for the roadmap's scope rule.

## Decisions for Ranbao
1. **Who waits in the temple?** The sword remembers the faces of a few great enemies. Which one
   is this, and what did they do? I can design the fight once I know who it is. For the
   mechanics I would suggest a staff master who seals qi in the second phase.
2. **Are the Taoists a hint?** A Taoist sealed the sword in the grove. I would let the sword
   hate these ones without knowing why. Say if that gives away too much too early.
3. **Who are the bystanders?** Ordinary wandering souls, as the first design had it, or
   something else? Their look and their two or three lines are yours.

## Open questions
For the playtest: is the fall satisfying or cheap? Do players learn to stop the Taoist, or
just wait the seal out? Is the staff's rhythm readable at the slowest director setting?

## Tests
- Unit: the seal locks gain and use and ends on time; a fall pays as a health kill; the staff's
  posture steps; only the first blow of a chain reaches the evidence filter; every level 2
  template passes the validator with edges.
- Scenario `edge_fall`: 乾坤 throws an enemy over the edge and the soul stops at it.
- Scenario `taoist_seal`: the seal lands, a skill is refused, the bar opens again; a hit
  during the wind-up prevents it.
- Scenario `staff_chain`: a bot deflects all three and the staff fighter kneels.
