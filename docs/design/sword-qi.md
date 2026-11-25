# Sword qi: 剑气 and the three skills (roadmap items: D4, D5, D6)

Status: draft, written 2026-11-15, waiting for Ranbao's answers below

## Why
A deflect today protects and wears down posture, and that is all it buys. The flash step sits
on a timer that has nothing to do with how well the player fights. Qi makes the deflect the
source of every strong move.

## What the player sees
1. Once a soul knows a skill, a thin bar appears beside its posture. Every deflect fills it a
   little, with a brighter ring on the blade.
2. 瞬步 (flash step): a short dash-strike through enemies. Cheap, used several times a room.
3. 乾坤 (heaven and earth): after a short wind-up, one cut all around that throws enemies back
   and sometimes leaves one dazed.
4. 无懈 (no opening): for a few seconds the sword turns every attack aside by itself. It takes
   a full bar.
5. Qi stays between rooms and leaves with the soul.

## Rules
Numbers start in a new `game/tuning/qi.tres`; the meter lives in `src/Combat` beside posture.

1. The bar holds 100. A new soul starts at 0. Qi does not drain over time.
2. **Gain:** a deflect (a right answer to a white attack) gives 10. A roll through a red
   attack inside its window gives 5. A held guard, an early roll and a hit taken give nothing.
3. **瞬步 costs 30** and loses its cooldown. Distance, damage and posture stay as they are in
   `run.tres`. It still passes guards and still does not dodge.
4. **乾坤 costs 60.** Wind-up 250 ms, during which the soul can be hit. Radius 72 px, damage
   2, posture 40, knockback 48 px. Each enemy hit is dazed for 1500 ms with chance 0.25, from
   its own seeded stream. An enemy in wind-up loses its attack.
5. 乾坤 does not touch an enemy that is kneeling or begging. A skill never decides mercy.
6. **无懈 costs 100** and lasts 3000 ms. Every white, red and arrow that would land is
   deflected with normal posture damage. These deflects give no qi.
7. **Learning.** The three skills are upgrades of their own kind (招式). 瞬步 stays in the
   pool and the other two join it. The first upgrade screen of a run always offers one skill
   the soul lacks. Taking a skill a second time lowers its cost by 20%.
8. With too little qi a skill does nothing but a dull sound and a flicker of the bar.
9. Each skill has its own input action. 瞬步 keeps `flash_step`; `skill_cut` and
   `skill_guard` are new and get default keys with rebinding (A4).

## What it touches
- **Director.** Attacks deflected by 无懈 are recorded as assisted and leave the estimate
  alone, the way a guard raised before the glint already does. An attack cancelled by 乾坤
  writes no record. 瞬步 never counts as an answer. Each gets a unit test on the evidence filter.
- **Flash step.** `FlashStepCooldownMs` and its repeat factor go; the upgrade text changes.
- **HUD.** The bar follows the hidden-values rule: no number until Detail mode.
- **Later.** The Taoist (level 2) locks the bar. Madness (F3) fills it. Relics may change gains.

## Not in this version
- A chance for 乾坤 to kill outright, as the first design had. A daze is enough, and it sets
  up the backstab later.
- Qi as a second health bar, qi that drains, skills that cost health.
- A fourth skill, or skill upgrades beyond the cheaper second copy.

## Decisions for Ranbao
1. **Found or known?** I recommend skills are found as upgrades (rule 7), so a first run stays
   simple and each soul fights differently. The alternative is that every soul knows all three
   from the start, which needs a longer tutorial.
2. **Is 无懈 allowed against bosses?** I recommend yes. It costs ten deflects.
3. **The skill names** are yours already. Their one-line descriptions need your Chinese.

## Open questions
For the playtest: is ten deflects for a full bar reached about once a room? Do players hold
qi for 无懈 or spend it all on 瞬步? Are three more buttons too many on a gamepad?

## Tests
- Unit: gains per outcome (rule 2); costs and refusals (3, 4, 6, 8); the cheaper second copy;
  the daze stream is seeded; the evidence filter ignores assisted and cancelled attacks.
- Scenario `qi_fill`: a bot deflects ten times and the bar is full; a held guard adds nothing.
- Scenario `skill_cut`: three enemies are thrown back, a beggar among them is untouched.
- Scenario `skill_guard`: a white, a red and an arrow are all turned aside, the director's
  belief is unchanged afterwards, and the bar is empty.
- `flash_step` is updated: no cooldown, refused at 20 qi.
