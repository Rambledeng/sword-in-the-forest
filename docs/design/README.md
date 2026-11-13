# Design pages

One page per system, written before the code. A page is the output of a design chat and the
input of a build session: "Read docs/design/economy.md and do roadmap item D2."

The pages to write, in order, are items C1–C9 in `docs/ROADMAP.md`.

## Rules

- One page, about 60 lines. If it needs more, it is two systems.
- Chinese terms keep their characters (剑意, 剑气) with the English beside them once.
- Numbers are starting values and say which tuning file they will live in.
- When the code and the page disagree after playtesting, fix the page in the same pull request.

## Template

```markdown
# <System> (roadmap items: D2, D3)

Status: draft | approved on <date> | built in <version>

## Why
What the player lacks today, in two sentences.

## What the player sees
The system from the player's side, step by step. No code words.

## Rules
Numbered, each one testable. Starting numbers with their tuning file.

## What it touches
Existing systems that change (director, mercy, save file, HUD, summary) and how.

## Not in this version
What was discussed and left out, so it is not built by accident.

## Open questions
For the playtest to answer.

## Tests
The unit tests and scenarios that will prove the rules.
```
