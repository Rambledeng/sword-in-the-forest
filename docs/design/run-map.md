# The road: forks, room kinds and run length (roadmap items: E2, E3, E4, E5, E7, E11)

Status: draft, written 2026-11-15, waiting for Ranbao's answers below

## Why
A run today is one fixed line of rooms, so the only decisions are inside fights. A player who
is hurt, rich or one upgrade short of a plan has no way to act on it.

## What the player sees
1. When a room is cleared the road forks. Two ways are shown, each with a sign for what lies
   that way and a sign for what it gives.
2. The sword has an opinion, in a short line, now and then. It likes the way with more blood.
3. Ways lead to a fight, a harder fight with a relic, a shop, a shrine to rest at, or
   something unknown.
4. Before a level's boss the choice is always the same: rest, or one more hard fight for a relic.
5. A level takes 12 to 15 minutes. A run of two levels takes 25 to 35.

## Rules
Numbers start in a new `game/tuning/map.tres`; the plan stays in `src/Combat/Run/RunPlan`.

1. **A road with forks, not a map.** A level is 7 steps and then its boss. Each step after
   the first offers 2 options, and either can be taken whatever was chosen before. There is
   no overview screen.
2. **A level has three areas**, of 3, 3 and 1 steps. Level 1: bamboo forest, outside the
   tavern, the tavern. The tutorial room comes before step 1 and is not a step.
3. **Step 1 is a single fight.** Steps 2 to 6 are forks. **Step 7 is always shrine or elite.**
4. **Room kinds and their weights at a fork:** fight 60, elite 12, shop 10, shrine 8, event 10.
   - Fight (战): as now. It shows its reward: an upgrade pick (悟), a purse of 15 剑意, or food.
   - Elite (精): one stronger enemy with one extra move, and company. Gives a relic.
   - Shop (市): spend 剑意 on food, an upgrade or a relic.
   - Shrine (祠): heal 4 or refill the gourd, not both.
   - Event (奇): shown as unknown. The first one is the wine seller (E7).
5. **What a fork may not do:**
   - Show the same kind with the same reward on both sides.
   - Offer an elite more than twice in a level, apart from step 7.
   - Offer the same kind, other than a fight, at two forks in a row.
6. **What a level must do:**
   - At steps 2 and 5 both ways are fights that give an upgrade pick. That keeps today's two
     picks a level.
   - A shop is offered at least once in steps 4 to 6. If none has come up by step 6, one side
     of step 6 is a shop.
7. **Fixed by the seed, not by the path.** What stands behind an option depends on the seed,
   the level, the step and the side only. The road uses its own seeded stream.
8. **The gourd** refills at the start of each area, as now. Health does not.
9. **Length.** A fight room should take about 90 seconds and a boss 2 to 3 minutes. If the
   balance bot (D11) measures a level outside 12 to 15 minutes, change the number of steps,
   not the fights.

## What it touches
- **RunPlan** becomes a list of levels, each a list of steps with one or two options.
  `RunStepKind.Upgrade` goes: an upgrade is now a fight's reward.
- **Room templates.** A level needs about 5 fight rooms per area so a soul rarely meets one
  twice (E8), and one small room each for shop, shrine and event.
- **Save file:** the choices made on the road, so a run in progress can be restored.
- **Economy:** purses and shops. **Loot:** elites and shops give relics.
- **Scenarios:** `run_seeds`, `run_bot` and `run_continue` all walk the old line and change.

## Not in this version
- A full map with branching paths that close behind the player.
- Three options at a fork, seeing two steps ahead, secret rooms, going back.
- Exits as places in the room. The fork is a small overlay first; doors can come with new art.
- Starting a run at level 2. Modifiers that make a whole run harder.

## Decisions for Ranbao
1. **Fork overlay or real exits?** I recommend the overlay: it needs no change to the ten
   handmade rooms and works on a phone. Real exits look better and cost a sitting per area.
2. **Does the sword comment on the choice?** I recommend yes, rarely, and only to pull toward
   the fight. That needs about six lines from you.
3. **The signs.** I used 战, 精, 市, 祠, 奇 and 悟 as working signs. Change any of them.

## Open questions
For the playtest: do players read the signs without being taught? Does anyone ever choose the
shrine before a boss? Is 25 to 35 minutes right for one sitting?

## Tests
- Unit: every seed from 1 to 500 gives a level that obeys rules 3, 5 and 6; an option is the
  same whatever was chosen before it (rule 7); adding an upgrade or a relic moves no room.
- Unit: a path of only fights and a path of every shop and shrine both end at the boss.
- Scenario `road_fork`: clear a room, see two different signs, take the second, arrive in it.
- Scenario `road_shrine`: heal or refill, and the other choice is gone.
- `run_bot` plays a whole level along random choices and reports its length.
