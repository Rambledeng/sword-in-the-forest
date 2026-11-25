# Economy: 剑意 and 剑鸣 (roadmap items: D2, D3, D7, D9)

Status: draft, written 2026-11-15; Ranbao's answers of 2026-11-25 are in; one decision open

## Why
Nothing a soul does today outlasts it except the sword's closing line, and nothing in a run is
bought, so there is nothing to plan for. This page gives fights a yield, gives the yield two
uses, and lets the sword keep something when the soul falls.

## What the player sees
1. A defeated enemy lets go of a wisp of sword-intent (剑意) that flies to the blade. A small
   count sits by the sword on the HUD.
2. At an upgrade the soul still picks one of three for free. It may then pay 剑意 for a second
   one, or pay a little to have three new ones offered.
3. When the soul falls or wins, the summary shows the 剑意 it earned and the part the sword keeps.
4. Back in the grove, before the next soul arrives, the sword spends what it kept on its cry
   (剑鸣): permanent boosts that every later soul starts with.
5. The next soul starts at the grove with nothing but the sword's cry and its own perk.

## Rules
Numbers start in a new `game/tuning/economy.tres`; the rules live in `src/Combat/Run`.

1. **One currency.** 剑意 is the only one. The soul carries it during a run; the sword banks a
   share of it afterwards. No money.
2. **Yield per enemy:** saber 4, spear 5, archer 3, boss 30. Paid once, when the enemy is
   defeated: finished, spared, or killed.
3. **Posture pays.** An enemy whose health ran out without a posture break pays half, rounded
   down. Breaking posture comes from deflecting, so the core skill is also the best income.
4. **Mercy and cruelty pay the same.** A spared beggar and one cut down both pay the normal
   yield. Cruelty pays in safety, not in 剑意: a spared liar can come back (`mercy.md`).
5. A liar that comes back pays again when it is defeated, like any enemy.
6. **The free pick stays.** After it, one more of the remaining upgrades can be bought for 20,
   and each purchase in a run raises the price by 10. A reroll costs 5 and doubles each time on
   the same screen.
7. **A fall ends the run** (decided 2026-11-25). Upgrades, relics and carried 剑意 go with the soul.
   The next soul starts at level 1. The tutorial stays shortened for returning players.
8. **The sword's share.** At the end of a run, won or lost, 25% of all 剑意 earned in it
   (spent or not) is banked. Spending in a run therefore never costs anything later, and there
   is no reason to hoard.
9. **剑鸣, five boosts** (numbers in `game/tuning/meta.tres`; ranks bought one at a time):

   | Boost | Ranks | Price per rank | Effect |
   | --- | --- | --- | --- |
   | A louder cry | 1 | 40 | Two souls answer the cry; choose one (name, look, perk) |
   | Lingering intent | 3 | 15, 30, 60 | A new soul starts with 8, 16, 24 剑意 |
   | Tempered soul | 3 | 20, 40, 80 | +1 health per rank |
   | Full gourd | 1 | 50 | +1 sip |
   | Clear sight | 2 | 30, 60 | One free reroll per upgrade screen; then four choices instead of three |

10. A level-1 run yields about 75 剑意, so the bank grows by 8 to 19 a run and the first boost
    comes after one to three runs. All five, fully ranked, cost 425: about 25 to 35 runs.
11. 仇 (souls fallen) and 怨 (enemies killed) are lifetime counts in the profile. They are not
    spent; the endings use them.

## What it touches
- **Death and restart.** "The next soul continues where the last one fell and keeps its
  upgrades" is replaced by rule 7. `run_continue` is rewritten.
- **Upgrade screen:** a price row and a reroll. **HUD and summary:** the count, earned and kept.
- **Save file:** bank, boost ranks, 仇, 怨 (`save.md`). **Mercy:** rules 4 and 5, `mercy.md`.
- **Director:** none. Nothing here changes what an answer means.
- **Later:** shops (E4) and purses on the road (`run-map.md`) are more places to earn and spend.

## Not in this version
- A second currency, interest, or debt. Prices that depend on merit or karma.
- A bonus for clearing a room unhurt. It rewards the players who need help least.
- Recovering a fallen soul's 剑意 from its body. Seeds change between runs, so the room is gone.
- Boosts that unlock new upgrades or relics. Add them when the pools are larger (D7, D8).

## Decisions for Ranbao
Answered 2026-11-25: a fall ends the run; cutting down a beggar pays no extra.

1. **Names.** The five boosts and the banked share need Chinese names in the sword's voice.

## Open questions
For the playtest: does anyone buy the second pick, or only reroll? Is a boost every one to
three runs too slow at 12 minutes a run? Do players notice that a health kill pays less?

## Tests
- Unit: yield per kind and per way of defeat, the same for spared and cut down (rules 2 to 5); price steps (6); the share (8);
  boost prices and effects (9); a run's totals from a fixed seed.
- Scenario `run_intent`: kill, spare and cut down a beggar; check the count and the summary.
- Scenario `upgrade_buy`: free pick, paid pick, reroll, and a refusal when the soul is short.
- Scenario `grove_cry`: bank after a fall, buy a boost, the next soul has it.
