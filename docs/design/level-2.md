# Level 2: 树林 → 山间路 → 山顶庙 (roadmap item: E10, with E6 and E9)

Status: draft, written 2026-11-15; story proposed on 2026-11-25 at Ranbao's invitation; waiting for Ranbao to accept or change it

## Why
A second level that is level 1 in new colours would double the length of a run and add
nothing to learn. Level 2 has to ask a new question of each thing the game is about: the
deflect, the sword's skills, and mercy.

## The journey
A proposal, since Ranbao left level 2's story open on 2026-11-25. The levels can change.

- The realm in the forest (林中境) is 鹤箬子's work, and he built it from the sword's own memory.
  Each level is a place on the road 风源虹 once walked; going deeper is remembering more.
- **Start:** the Bitter Bamboo (苦竹林), where the sword is sealed. English "the Bitter
  Bamboo", as the game already says; 苦竹 is a real plant, bitter bamboo, so it keeps the
  bitterness. Dutch "Het Bittere Bamboe". The plain bamboo forest after it stays 竹林.
- **Level 1: bamboo forest → outside the tavern → the black inn.** As in the demo.
- **Level 2: the woods → the mountain path → Broken Bell Temple (断钟寺).** The road the
  couple took when they left the jianghu. The sword recalls a woman's voice on this path
  and cannot place it: one line per area, from you.
- **Levels 3 and 4** (by the scope rule): the manor and the yamen of the first design, or other
  enemies' places. Nothing below depends on them.
- **The end** comes only on an ending run (`merit-karma.md`, rule 12): home to the Bitter
  Bamboo for the true ending, to Mount Xiuduan (修断山) for karma, or to the burnt farmstead
  (旧居) for the mortal ending. Every other run ends at the last level's boss.

**Broken Bell Temple and its abbot.** The abbot 了因 (Liaoyin) was 风源虹's sworn brother and
the only one who knew where the couple went to live. The court offered to make his temple
the first in the land, and he wrote one letter. The realm's abbot is an evil ghost wearing his
face, as every enemy is, but the sword does not know that. The temple bell is cracked: it
was rung the day the letter was sent, and never since.

- **The fight.** Phase 1: a staff master, chains of four, the staff's rhythm at its hardest.
  Phase 2: he strikes the cracked bell. Each toll is a seal on the soul's qi for 4 s, and
  hitting the bell while he winds up breaks it for the rest of the fight.
- **The first boss who begs.** Broken the second time, he kneels and confesses. Cutting him
  down: 5 karma, and the sword is satisfied. Sparing him: 5 merit, the sword is enraged for
  the rest of the run, and he says where the farmstead was. That is the first time the
  player hears of the mortal ending's place.
- **The Taoists** in the temple are shadows of 鹤箬子's disciples, the jailers. The sword hates
  them and does not know why. That is the first hint that someone sealed it.
- **The bystanders** in the woods are other wandering souls (游魂) that 鹤箬子 sent in and that
  lost their way, still in the shape of their last life: a woodcutter, a pilgrim. Killing one
  is karma, as the first design had it.

## What the player sees
1. **The woods (树林).** Wide rooms among trunks. Not everyone here is an enemy: a woodcutter
   keeps working, a pilgrim walks past. The first staff fighters, whose blows come in a rhythm.
2. **The mountain path (山间路).** Long, narrow rooms with a drop on one side. Throwers of
   hidden weapons stand far down the path. Whoever is pushed over the edge is gone.
3. **The summit temple (山顶庙).** Courtyards with pillars. Taoists who seal the sword's qi.
   The hammer, whose slow red blow covers ground that must be left.
4. The abbot at the top (above). His fight gets its own page before it is built.

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
Answered 2026-11-25: level 2's story is mine to propose; levels can change; the journey starts
and ends in the Bitter Bamboo, and ending there is the true good ending.

1. **Accept or change the story above:** the abbot 了因 and his letter, Broken Bell Temple, the
   Taoists as jailers' shadows, the lost souls as bystanders. Names are placeholders for yours.
2. **May the abbot be spared?** I recommend yes: a boss who begs makes level 2's mercy
   question the sharpest in the game, and it points to the farmstead.
3. **The ending places.** Is the burnt farmstead right for the mortal ending, and Mount
   Xiuduan for the karma ending? The first design had no place for either.

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
