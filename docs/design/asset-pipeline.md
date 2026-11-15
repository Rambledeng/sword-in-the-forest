# Asset pipeline (roadmap items: S1–S5, E1, C9)

Status: draft, decisions of 15 November 2026 recorded. The numbers are guesses until the test (E1).

## Why
The demo uses a free pack that other games use too. New art, music and sound will be made with
AI as a drafting tool and finished by hand, on a small budget and in few hours a week.

## What the player sees
One look: pixel art in play, ink-wash (水墨) portraits and cutscene stills reduced to the same
palette. Cutscenes are stills moved by code. The sword keeps its blip; there is no generated voice.

## Rules
1. **Ranbao is the author.** AI output is a draft. Every shipped piece is chosen, then redrawn,
   cleaned or arranged by hand. Illustrations start from Ranbao's sketch or written brief.
2. **Never prompt with an artist's or a game's name.** References are own sketches, own photos
   and public-domain works.
3. **One palette (about 32 colours) and one pixel grid.** Pure white and cinnabar red belong to
   the glints, gold to merit, purple smoke to karma; no sprite, tile or still may use them.
   `tools/art/conform` reduces an image to the palette and grid; a test fails on a file that
   leaves either. The palette lives in `game/tuning/`.
4. **Characters keep the sheet the game reads** (4 across, 7 down) with weapons as separate
   sprites. Swings, trails, sparks and shakes stay in code. Frame-by-frame AI animation is for
   walk cycles and a few boss poses only.
5. **Signal sounds are designed, not generated** (glints, deflect, block): made in jsfxr or
   synthesised, identical every time, and tested for no silence at the start, because the
   director measures reaction time from them. Texture sounds come from CC0 libraries, own
   recordings, or AI for gaps. Never CC-BY-NC.
6. **Stills are the default for cutscenes.** A generated clip ships only if it clearly beats the
   moved still for the opening, a boss entrance or an ending, and comes from a paid plan or an
   open model.
7. **Tools must allow selling the output.** Open models with Apache-2.0 or MIT terms first
   (Z-Image, Qwen-Image, ACE-Step 1.5); paid services by the single month or per use. Free tiers
   are for judging only. Not allowed: FLUX.1 dev, MusicGen weights, Udio.
8. **Every file is recorded.** Final files go in `game/assets/own/`, with one row each in
   `game/assets/own/ASSETS.csv`: file, tool and model version, date, link to the terms, what went
   in, what was changed by hand. A test fails on a file without a row. CREDITS.md summarises.
9. **Sources stay out of this repository:** sketches, layered files, chosen raw generations,
   prompts and a saved copy of each tool's terms go in a private folder, or a private repository
   if they grow. Nothing new goes into `own/` before the licence change (A5).
10. **Disclosure is honest and not advertised.** AI use in art and in code is declared wherever
    a store asks and in the credits, written from `ASSETS.csv`. It is not a selling point and is
    never denied.
11. **Spending:** free tools first, small amounts at the start, and Ranbao approves each payment.

## Machines
- OMEN 17 (Windows 11, RTX 3080 16 GB): the workbench. Godot, pixel editor, Krita, ComfyUI,
  ACE-Step's larger model, the video trial.
- OMEN 15 (Ubuntu, GTX 1070 8 GB, on Tailscale): queued batches overnight, audio first.

## What it touches
CREDITS.md and LICENSE (A5), `game/assets/` layout, the art-direction page (C9), the E1 spike,
the title and intro (a scene player for moved stills), the Steam page text (G1).

## Not in this version
Generated voice acting. Content generated while the game runs. A generated capsule image: the
capsule is an artist's work or Ranbao's own composite with brush calligraphy. Replacing the
demo's pack art before level 2 looks right.

## Open questions
- Characters at 16 px, or 24 to 32 px on the same tiles?
- Does any music model play guzheng, dizi, xiao and erhu convincingly, and loop cleanly?
- Minutes of hand cleanup per character, tileset, still and track (measured in E1).
- Does a video model run usefully in 16 GB, and does a clip beat a moved still?

## Tests
A unit test for the palette and grid check; a unit test that every file in `own/` has a record;
a unit test for leading silence in signal sounds; a scenario for the scene player.
