# Mercy: beggars, true and false (roadmap item: E7b)

Status: draft, written 2026-11-25 from Ranbao's answers on 2026-11-25

## Why
Sparing costs nothing today except the merit it does not earn back, so cutting a beggar down is
just being cruel. Ranbao's rule: cruelty pays because mercy is a risk. A spared liar can come
back for the soul, now or later, and the player has to decide whether the risk is worth it.

## What the player sees
1. A broken enemy kneels, or drops its weapon and begs, as now. The first beggar of a run is
   always genuine, as now.
2. A false beggar may still give itself away while begging: it grabs its weapon when the soul
   turns its back, as now.
3. **New: the knife on the way out.** A spared liar walks to the door like a genuine one. If the
   soul turns its back on it before it is gone, it snatches its weapon and strikes from behind.
   A scrape of steel on stone comes first.
4. **New: the friends it brings.** A spared liar leaves the room for real. Some rooms later it
   comes back through the door with two others, in the same colours, laughing.
5. A genuine beggar never does either. The soul cannot be sure which one it let go.

## Rules
Numbers in `game/tuning/run.tres`; the rules stay in `src/Combat/Run/MercyRules`.

1. A false beggar that is spared picks one of three ends from the mercy stream:
   strike at once (today's behaviour) 0.2, the knife on the way out 0.5, the friends 0.3.
2. **The knife.** While leaving, it strikes if the soul is within 90 px and has faced away
   from it (more than 110 degrees) for 400 ms. That is the same test as the begging tell.
   It reaches the door after at most 12 s; after that it is gone and does nothing.
3. The strike from behind is a red attack, the director's lead plus 200 ms, with the scrape
   sound at the start of the lead. Before it, the enemy grabs its weapon (the first time in a
   run, 1000 ms; later, 380 ms, as today).
4. **The friends.** The liar is remembered with its kind and colour. Two to four rooms later,
   chosen by the stream, a fight room gets it and two extra enemies of the area's mix, entering
   through the door after the room's own enemies are placed. At most one return per area.
   Never in an elite, a boss, a shop or a shrine room.
5. A returning liar does not beg again. Finishing it is merit, as for any enemy.
6. **Karma only for the true.** Cutting down a genuine beggar is karma, as now. A liar cut
   down while it still begs gives no karma: the sword knows, and says so the first time.
   Sparing a liar gives no merit either. The soul never sees which it was until later.
7. Cutting down a beggar pays the normal 剑意 yield, not more. The payment is safety.
8. With merit tier 2 (`merit-karma.md`) the friends never come: word of the soul's mercy has
   spread. The knife still can.

## What it touches
- **Director.** A strike from behind is attacked out of the soul's sight, so its answer is not
  evidence about reaction time and the filter ignores it. Its lead still comes from the
  director, so it is fair.
- **Run plan:** a room can gain enemies after it was placed. The extra enemies come from the
  mercy stream, never the room stream, so seeds keep their layouts.
- **Save file:** the remembered liar is part of the run in progress.
- **Merit and karma:** rule 6 replaces "a liar cut down gives neither" in `merit-karma.md`.
- **Scenario `run_fake`:** extended for both new ends.

## Not in this version
- A fake that begs twice. Beggars who come back grateful. Telling true from false by its look.
- A liar who follows the soul into the next level.

## Open questions
For the playtest: does anyone still spare after the first knife in the back? Is the scrape
enough warning? Do players recognise the one who returns?

## Tests
- Unit: the three ends follow their weights over 10 000 rolls and the first beggar is always
  true; a remembered liar returns two to four rooms later, never in the wrong kind of room,
  at most once per area; the room stream is unchanged by a return; karma per rule 6.
- Unit: the evidence filter ignores a strike from behind.
- Scenario `fake_knife`: spare a forced liar, turn away, hear the scrape, roll through the
  strike; then again without turning away and it leaves.
- Scenario `fake_friends`: spare a forced liar, it returns with two others in the right room.
