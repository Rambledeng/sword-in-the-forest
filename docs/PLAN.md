# Plan

How Sword in the Forest gets from a 15-minute demo to a game on Steam, made part-time by one
student on a tight budget. The ordered work is in `docs/ROADMAP.md`; this page is the reasoning,
the budget and everything that is not code. It is for Ranbao. Claude sessions do not need to
read it unless asked.

Written 11 November 2026. Facts about prices, store rules and law were true to my knowledge
then; check each one at the source before acting on it.

## Things you did not ask about

Each is short here and explained in its section.

1. **A game you can stop at any rung.** The roadmap is a ladder of small releases, not one long
   march to 1.0 (section 1).
2. **Your right to earn money as a student, and tax.** Check it before the first euro arrives,
   not after (section 10).
3. **AI art is not free of copyright questions**, and stores make you declare it (section 5).
4. **Keeping the code public the way Barotrauma does means leaving MIT** before the first
   sale (section 5).
5. **A Steam page early is worth more than a better game late** (section 8).
6. **A save file with a version number** before anything persists between runs (roadmap D1).
7. **Watching a stranger play in silence** teaches more than any form (section 7).
8. **Your Chinese is an advantage**: wuxia has its largest audience among the players who read
   Simplified Chinese (section 8).

## 1. What we are aiming at

**The game:** a top-down wuxia roguelike where deflecting is the core skill, the sword learns
how fast you react, and mercy or cruelty changes how the story ends. Those three are the pillars.
A feature that serves none of them goes to the parking lot.

**Who it is for:** people who like Sekiro's deflects or Hades' runs and read English or Chinese.
They will forgive plain art; they will not forgive combat that feels unfair or a run with
nothing to decide.

**What "good enough for Steam" means here:** a first session of 30–45 minutes that ends with
the player wanting another run, 5 or more hours before the content repeats, and three endings
that make a second and third clear feel different. The demo covers the first minutes of that.

**Rungs, not a march.** Each version in the roadmap's ladder is a whole thing you could leave
the project at. That matters because the thesis, a job search or a move will interrupt this,
and an interrupted march leaves nothing while an interrupted ladder leaves the last rung.

## 2. Time

- **Until the thesis is in (around March 2027): 3–4 hours a week, 8 in a good week.** Two
  sittings, for example one weekday evening and one weekend morning. Phases A to C are sized
  for this: setup, playtests, and design pages that are thinking more than building.
- **In a crunch week, zero is fine.** The loop survives gaps because the state lives in the
  repo, not in your head.
- **After the thesis: at least 8 hours a week, up to 16.** Phase D starts in earnest then. The
  job search in the orientation year comes first when the two collide.
- **One rule against drift:** every sitting ends with something merged or something written
  down. Half-finished branches are what kill part-time projects.

## 3. How we work

**The loop for one roadmap item**

1. Open a fresh local session in the repo. Say: "Do roadmap item D2. Plan first."
2. Read the plan. Correct it. Only then does Claude write code, on a branch, and run the tests.
3. Play it in the Godot editor straight away and give feedback in the same session.
4. Commit, push, open a pull request, let CI pass, merge. Claude adds a few lines to PROGRESS.md
   and ticks the roadmap.
5. Tag a release when a batch is worth showing. itch.io updates from the tag.

**For a big system**, the first sitting is a design chat that ends in a page in `docs/design/`.
The build sitting starts from that page.

**Keeping sessions cheap**

- One task per session. A new task gets a new session.
- Batch feedback into one ranked list instead of five messages.
- Tune numbers yourself in the F1 panel; that costs nothing.
- A mid-tier model for routine items (S and most M). A stronger model for design chats, the
  director's maths and bugs that survive two attempts.
- If a session circles one error three times, stop it, write the error into PROGRESS.md and
  start fresh with a narrower instruction.

**When to use the cloud:** long jobs with a clear test that do not need your eyes. On this
roadmap those are B3 (the report), D11 (balance simulation) and large refactors such as D1.
Everything you need to feel with your hands stays local.

## 4. Budget

Assumption: as close to zero as possible, with money spent only where nothing free does the job.

| Item | Cost | When | Verdict |
| --- | --- | --- | --- |
| Claude subscription | what you pay now | ongoing | The main tool. Enough at 3–8 h a week. |
| Godot, .NET, GitHub, itch.io | free | now | itch.io lets you set its share, even to zero. |
| Steam Direct fee | US$100 per game, once | at G1 | **Necessary.** Returned by Valve after the game earns US$1,000. |
| Capsule and key art by an artist | roughly €150–500 | at G1 | **The one purchase I would argue for.** It is the first and often only thing a Steam visitor sees. |
| A pixel editor | free (Pixelorama, LibreSprite) | at E1 | Aseprite is about €20 and also free if you compile it. |
| Domain | already yours | now | Use ranbaodeng.com; no second domain. |
| Steam Deck for testing | — | at G2 | Borrow one. |
| Code signing for Windows | hundreds a year | never | Steam does not need it. |
| Paid asset packs | varies | not before E1 | They would be thrown away if the look changes. |
| Ads, PR agencies, trademark | — | not before 1.0 | Wishlists come from the demo and the page. |

**Total to reach a live Steam page: about US$100**, or about €350–600 with commissioned key art.

## 5. Art, music and licences

**Now.** Everything is CC0 (Ninja Adventure), OFL (the font) or drawn in code. That is clean,
free and legal to sell. Its weakness is that other games use the same pack, so the game does
not yet look like itself.

**The decision comes at roadmap item E1, by experiment.** Restyle one small slice three ways
and look at the screenshots: a palette and lighting pass on the current pack, pixel art edited
by hand, and AI-assisted art cleaned by hand. Until then, keep drawing effects in code and keep
every image behind its file name so a swap is a file copy.

**What you should know about AI-generated art and music before relying on it**

- It is allowed on Steam, but Valve's content survey asks you to declare it and shows the
  declaration on the store page. Kickstarter asks the same of projects. itch.io has its own
  tagging rule. Read each before publishing.
- "No copyright issues" is not quite right. In several countries, including the United States,
  an image made by a model with no meaningful human authorship may not be protected by
  copyright at all. You could sell the game, but you might not be able to stop someone copying
  the art. Work you edit substantially by hand stands on firmer ground.
- A model can produce something close to an existing work. For characters and key art, check
  by reverse image search.
- Each service's terms decide whether its output may be used commercially; some allow it only
  on a paid tier. Keep a note per asset of the tool, the date and the terms. CREDITS.md is the
  place.
- Part of the indie audience reacts badly to AI art, and it shows in reviews. For a wuxia game
  aimed largely at Chinese-reading players this may matter less than on English-language
  forums, but I am guessing. The E1 screenshots are a chance to ask your playtesters.

**A middle path I would recommend:** keep CC0 and code-drawn art for the bulk, use AI as a
sketching tool that you then redraw or clean by hand in a pixel editor, and pay an artist for
the few pieces that sell the game (capsule, title, boss portraits) once there is money.

**Music and sound.** The pack's tracks are fine through v0.3. After that: CC0 and CC-BY music
(credit in CREDITS.md), your own in LMMS, or a commissioned theme. Sound effects from jsfxr,
Audacity and CC0 libraries. Traditional instruments (guzheng, dizi, xiao) exist in free sample
libraries and would do more for the wuxia feel than any amount of new art.

**The repo.** The code is public under MIT and the current art is CC0, so everything is in one
public repository. The day a commissioned, purchased or carefully made asset goes in, anyone
may take it unless you decide first (roadmap A5). The usual answers: keep art in a private
repository pulled in as a submodule, or state in LICENSE that the MIT licence covers code only
and the art is all rights reserved. Either works; the second is simpler but leaves the files
downloadable.

**Decided (13 November 2026): the code stays public when the game is sold, in the way
Barotrauma does it.** The source is there to read and to mod, not to copy or resell the game.
What that means in practice:

- That model is "source-available", not open source. MIT allows anyone to sell the game, so
  the licence has to change before the first paid release (roadmap A5).
- You are the only copyright holder, so you may relicense future versions. The versions already
  published under MIT stay MIT for whoever has them. The demo is small, so this costs little,
  but it is a reason to switch before the roguelike core lands and not after.
- Options, simplest first: an existing source-available licence such as PolyForm Noncommercial
  (reading, modding and non-commercial sharing allowed, selling not); or a custom licence
  modelled on Barotrauma's, which also says mods may be shared and that a bought copy is needed
  to play. Do not copy their text without reading what it promises.
- The `src/Director` library is the part with value for your applications. Consider leaving
  it under MIT as its own piece, and putting the game under the new licence.
- Art, music and the sword's text are "all rights reserved" either way, stated in LICENSE and
  CREDITS.md, with CC0 pieces still marked CC0.

## 6. Funding

In order of realism for a first game with no audience yet.

1. **itch.io "pay what you want"** on the demo. Small sums and no cost, but it is income, so
   switch it on only after the check in section 10.
2. **Dutch and European cultural funds.** The Creative Industries Fund NL (Stimuleringsfonds)
   has funded games under its digital culture grants. An application wants a playable prototype
   and a plan, both of which you have. Check the current rounds and whether a student or a
   one-person company can apply. This is the most realistic source of real money.
3. **Kickstarter**, only after a Steam page with a few thousand wishlists and a demo people have
   played. A campaign does not create an audience; it converts one. Launching without one is
   the usual way they fail, and a failed campaign is public.
4. **Patreon or similar**: for creators with a steady following. Not before a devlog has
   readers.
5. **A publisher**: possible after a strong Next Fest. They pay for art, localisation and
   marketing against a share of revenue. Do not sign anything without someone who has read such
   a contract before.

## 7. Playtesting

One friend liked it and could not say much because it is short. That is normal: people cannot
tell you what is missing, only what they did. So collect what they did.

- **Watch, silently.** Sit beside one player, or have them share their screen, and say nothing
  for the whole run. Note where they die, where they stop to read, what they never try.
- **The log folder.** Every run already writes its attacks. With roadmap A3 they can send it.
- **Who.** Five people: a Dutch reader, a Chinese reader who is not you, someone who plays
  soulslikes, someone who does not, and someone who has never heard you describe the game.
- **Eight questions for the form** (roadmap A3):
  1. How far did you get, and how many runs did you play?
  2. What did you do in the first minute that did not work?
  3. When did you feel clever? When did you feel cheated?
  4. Did you notice the sword adapting to you? Did it feel fair?
  5. Did you spare anyone? Why, or why not?
  6. What did you want to do that the game did not let you?
  7. Would you play another run right now? What would make you?
  8. Anything in the text that read wrong in your language?
- **Later, strangers.** itch.io comments, a post on r/godot or r/roguelites, and a Bilibili or
  Xiaohongshu clip reach people who owe you no kindness.

## 8. Steam and being found

- **Wishlists are the currency.** Steam shows a new game to more people when it launches with
  many wishlists, and they only accumulate while a page exists. So the page (G1) goes up as soon
  as the look is settled, long before the game is finished.
- **The capsule image and the first six seconds of the trailer** do most of the work. Show a
  deflect and the director's overlay learning a player; nobody else has the second one.
- **Next Fest** runs three times a year and a game may take part once. Use it shortly before
  release with the most polished demo you can make.
- **Languages.** Simplified Chinese readers are among the largest groups on Steam and the
  natural audience for wuxia. Your native Chinese for the sword's voice is the game's unfair
  advantage; spend your own writing time there and let English and Dutch follow.
- **A devlog, lightly.** One post per release, in English and Chinese, with one GIF. On the
  itch.io page, your site and one Chinese platform. Do not start a schedule you cannot keep
  during the thesis.
- **The hook in one sentence** for every page and post: "A wuxia roguelike where the sword
  learns how fast you react."
- **Comparables.** Your first playtester described it unprompted as Sekiro, Undertale and Dead
  Cells merged well. That is the store page's "for fans of" line: Sekiro for the deflects,
  Undertale for sparing, Dead Cells for the runs. It also says which promise is thinnest today:
  the Dead Cells part, which is phases D and E.
- **Steam's waiting periods.** A new developer account waits about 30 days after paying the fee
  before a first release, and a page must be public for two weeks before launch. Neither is a
  problem if the page goes up early.

## 9. Mobile later

Not now, but do not close the door. Godot can export C# projects to Android and iOS, with less
maturity than desktop. The design question is larger than the technical one: deflecting on a
touch screen. The director helps, because it measures the player's reaction including the
device's delay and adapts to it.

Habits that keep the port possible: all input goes through `IPlayerControls`; no feature
depends on a mouse hover; the 480×270 layout stays; nothing needs more than a few buttons at
once.

## 10. Admin and legal

- **Your right to earn.** Your path is study permit, then the orientation year (zoekjaar),
  then a highly skilled migrant (kennismigrant) permit through a job. Each treats side income
  differently, so check these three with the IND or the university's international office
  before the first payment. What I believe, without certainty:
  - *Study permit:* self-employment beside your studies is allowed if you register with the
    Chamber of Commerce (KVK).
  - *Orientation year:* work is free, including for yourself. This is the easiest year to open
    the Steam account and take first revenue.
  - *Highly skilled migrant:* the permit rests on the job with your sponsor. Whether a small
    business beside it is allowed, and on what terms, is the one to ask about explicitly.
- **Your future employment contract.** Many Dutch contracts ask permission for side activities
  (nevenwerkzaamheden), and software employers often claim what you make. Name the game when
  you negotiate, and get a line in writing that it stays yours. This matters most if the job
  is at a game studio.
- **Free until then.** While the game earns nothing (free demo, no donations switched on),
  none of the above bites. Pay-what-you-want on itch.io counts as income, so leave it off
  until you have checked.
- **Tax.** Selling a game is a business activity. Below a turnover threshold, Dutch small
  businesses can be exempt from charging VAT, and on Steam Valve is the seller to the customer
  anyway. Steam asks for tax and bank details when you open the account, including a form for
  non-US persons. A free first consult with a bookkeeper who knows freelancers is worth an hour.
- **The name.** Search Steam, itch.io and the app stores for "Sword in the Forest" and 林中剑
  before the page goes up. Changing a name later loses wishlists.
- **The university.** The game shares an idea with your thesis, not code or data. If you ever
  publish results from real players, that needs ethics approval; playing with friends and
  looking at their logs with consent does not.
- **Privacy.** The logs stay on the player's machine and are sent only by the player. Keep it
  that way and say so on the store page; it removes a whole class of obligations.
- **Licences of what you use.** Godot (MIT), the font (OFL) and CC0 art all allow selling.
  Every new asset goes in CREDITS.md with its licence on the day it is added.
- **Backups.** The code is on GitHub. Source files for art and music that are not in the repo
  need a second home.

## 11. Risks, most likely first

| Risk | Early sign | Response |
| --- | --- | --- |
| The thesis takes every hour | Two weeks with no sitting | Expected. Do one design page a fortnight, nothing else. |
| Scope grows past what one person can finish | Items appear in phases without removing others | The parking lot and the scope rule in the roadmap. |
| Systems pile up but the run is not more fun | Playtesters say "more stuff" but do not replay | Stop building, run five playtests, cut. |
| The look stays generic | Store visitors bounce at the capsule | E1 early, key art by an artist. |
| A backlash against AI art | Comments on the first devlog that shows it | Declare it plainly, keep it hand-finished, be ready to replace pieces. |
| Windows-only bugs now that CI tests on Linux | A scenario passes in CI and fails locally | A1 makes local runs routine; CI stays the referee. |
| Save files break between versions | A playtester loses progress | D1's version number and migration test. |
| The director feels like cheating to some players | "The game slows down for me" in reviews | Detail mode, the off switch, and saying honestly what adapts. |
