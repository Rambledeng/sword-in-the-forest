# Loot: relics and food (roadmap items: D8, later E3 and E4)

Status: draft, written 2026-11-15, waiting for Ranbao's answers below

## Why
Upgrades change numbers. Nothing a soul finds changes how it plays, and nothing found says
anything about the sword's past. Relics (遗物) do both, a few per run.

## What the player sees
1. Sometimes a fight leaves a thing behind: a bell, a string of beads, a pair of sandals. Two
   are shown; the soul takes one.
2. The sword says one line about it, as if it half remembers whose it was.
3. The relic changes one rule for the rest of the run. Its icon sits in a row on the pause
   screen and the summary.
4. Food works as it does now: a rare drop, eaten by walking over it.

## Rules
Numbers start in a new `game/tuning/relics.tres`. Relics are an enum and a rules class in
`src/Combat/Run`, beside upgrades.

1. **Upgrades are numbers, relics are rules.** An upgrade can be taken twice. A relic is held
   once and is never offered again in that run.
2. **No slots, no bag.** A soul holds every relic it finds. Relics are not sold or swapped.
3. **Where they come from.** In v0.2 one offer per run: clearing the last room before the boss
   shows two relics. From v0.3: every elite room, the boss of each level but the last, and the
   shop.
4. **Offers come from their own seeded stream**, so adding a relic never moves rooms or upgrades.
5. **The first five**, one for each thing the game is about. Names are working names.

   | Relic | Rule it changes |
   | --- | --- |
   | Bronze bell 铜铃 | Deflects give half again as much qi |
   | Wine banner 酒旗 | A sip from the gourd also empties the soul's posture bar |
   | Straw sandals 草鞋 | The roll recovers 80 ms sooner |
   | Prayer beads 佛珠 | A spared beggar leaves food behind |
   | Blood jade 血玉 | Cutting down a beggar heals 2 |

6. The beads and the jade are a pair on purpose: a relic may lean a soul toward mercy or away
   from it, and the soul chose it.
7. **Each relic has one line from the sword** (`relic.<id>.line`), said when it is taken.
   You write these first in Chinese; they are a cheap place for the story's hints.
8. **Food.** Unchanged: chance 0.12, rice, beef and chicken heal 1, 2 and 3. Food is not
   picked up at full health. Food left behind when the soul leaves the room is gone; that
   becomes a rule, not a known issue. The shop sells food later.

## What it touches
- **Run plan:** a relic offer after the last room before the boss (v0.2), then elite rooms.
- **Summary and pause screens:** the relic row. **Save file:** relics in the run in progress.
- **Qi, mercy, food, roll:** each of the first five reaches into one of them through one hook.
- **Director:** none of the five changes what an answer means. A later relic that does (an
  automatic deflect, a slower world) must report it, as 无懈 does.

## Not in this version
- **The three equipment slots (冠, 衣, 履).** They would be a second set of passives with its
  own drops, screen and art. Come back to them when there are fifteen or more relics and a
  reason to make the soul give one up for another.
- Rarity tiers, cursed relics, relic combinations with their own names.
- A fourth food (炒菜), carrying food, food that restores posture.
- Other weapons. Darts. Both are in the roadmap's parking lot.

## Decisions for Ranbao
1. **Whose things are the relics?** I recommend they belonged to earlier sword-bearers and to
   风源虹's own life, so each line is a piece of memory the sword cannot place. The plain
   alternative is that they are just things the bandits carried.
2. **Drop the equipment slots for now?** I recommend yes, as above.
3. **The five names and lines** are yours to write. The rules can change to fit a better object.

## Open questions
For the playtest: is one relic a run enough to be remembered? Does anyone take the jade? Is
"not picked up at full health" understood without a word of text?

## Tests
- Unit: an offer never repeats a held relic; offers are the same for the same seed and are
  unmoved by a longer upgrade list; each relic's hook (qi gain, posture on a sip, roll
  recovery, food on a spare, heal on a cut-down beggar).
- Scenario `relic_offer`: clear the room before the boss, take one of two, see it on the summary.
- Scenario `relic_mercy`: with the beads, spare a beggar and find food; with the jade, cut one
  down and gain health and karma.
- `food_drop` gains a check that food is left alone at full health.
