# Credits

Code: MIT, see LICENSE.

## Assets

### Ninja Adventure Asset Pack

- Authors: [Pixel-boy](https://pixel-boy.itch.io/) and [AAA](https://www.instagram.com/challenger.aaa/)
- Licence: CC0 1.0 (public domain), text in
  `game/assets/third_party/ninja_adventure/LICENSE.txt`
- Source: <https://pixel-boy.itch.io/ninja-adventure-asset-pack>
- The full pack is at `game/assets/third_party/Ninja Adventure - Asset Pack/` (it has a
  `.gdignore`, so Godot does not import it). The game uses only the copies listed below, renamed
  and placed under `game/assets/third_party/ninja_adventure/`.

| File in `ninja_adventure/` | Original path in the pack | Used for |
| --- | --- | --- |
| `actors/ninja_gray.png` | `Actor/Character/NinjaGray/SpriteSheet.png` | The player (the wandering soul) |
| `actors/ninja_gray_faceset.png` | `Actor/Character/NinjaGray/Faceset.png` | Player portrait (later) |
| `actors/camouflage_red.png` | `Actor/Character/CamouflageRed/SpriteSheet.png` | Saber bandit (刀客) |
| `actors/camouflage_red_faceset.png` | `Actor/Character/CamouflageRed/Faceset.png` | Bandit portrait (later) |
| `weapons/katana_in_hand.png` | `Items/Weapons/Katana/SpriteInHand.png` | The player's sword (鸣不平) |
| `weapons/big_sword_in_hand.png` | `Items/Weapons/BigSword/SpriteInHand.png` | The bandit's saber |
| `fx/slash_arc.png` | `FX/Slash/SpriteSheetArc.png` | Player sword trail |
| `fx/slash_curved.png` | `FX/Attack/SlashCurved/SpriteSheet.png` | Enemy strike trail |
| `fx/cut_x.png` | `FX/Attack/CutX/SpriteSheet.png` | Finisher cut |
| `fx/smoke.png` | `FX/Smoke/Smoke/SpriteSheet.png` | Posture break puff |
| `fx/smoke_circular.png` | `FX/Smoke/SmokeCircular/SpriteSheet.png` | Roll dust |
| `fx/spark.png`, `fx/leaf.png` | `FX/Particle/Spark.png`, `FX/Particle/Leaf.png` | Reserved for M3/M4 polish |
| `tiles/tileset_floor.png` | `Backgrounds/Tilesets/TilesetFloor.png` | Arena grass |
| `tiles/tileset_nature.png` | `Backgrounds/Tilesets/TilesetNature.png` | Bamboo fence and bushes |
| `ui/heart.png` | `Ui/Receptacle/Heart.png` | Health hearts |
| `ui/gourd.png` | `Items/Object/Gourd.png` | Gourd (酒葫芦) icon |
| `sounds/glint_white.wav` | `Audio/Sounds/Alert/Alert3.wav` | White glint chime |
| `sounds/glint_red.wav` | `Audio/Sounds/Alert/Alert2.wav` | Red glint (杀招) cue |
| `sounds/deflect.wav` | `Audio/Sounds/Hit & Impact/Hit7.wav` | Deflect |
| `sounds/block.wav` | `Audio/Sounds/Hit & Impact/Impact.wav` | Block |
| `sounds/player_hurt.wav` | `Audio/Sounds/Hit & Impact/Hit9.wav` | Player hit |
| `sounds/enemy_hit.wav` | `Audio/Sounds/Hit & Impact/Hit3.wav` | Enemy hit |
| `sounds/posture_break.wav` | `Audio/Sounds/Hit & Impact/Impact5.wav` | Posture break, kneel |
| `sounds/finisher.wav` | `Audio/Sounds/Hit & Impact/Impact3.wav` | Finisher (补刀) |
| `sounds/roll.wav` | `Audio/Sounds/Jump & Bounce/Jump.wav` | Roll |
| `sounds/swing.wav` | `Audio/Sounds/Elemental/Grass2.wav` | Sword swing |
| `sounds/drink.wav` | `Audio/Sounds/Elemental/Bubble.wav` | Gourd sip |
| `sounds/heal.wav` | `Audio/Sounds/Magic & Skill/Heal.wav` | Healing |
| `music/dark_forest.ogg` | `Audio/Musics/37 - Dark Forest.ogg` | Test arena music |

### Fusion Pixel Font (缝合像素字体), 12 px proportional

- Author: TakWolf, <https://github.com/TakWolf/fusion-pixel-font>
- Version: 2026.10.25, from `fusion-pixel-font-12px-proportional-ttf-v2026.10.25`
- Licence: SIL Open Font License 1.1, text in
  `game/assets/fonts/fusion-pixel-font-12px-proportional-ttf-v2026.10.25/OFL.txt`
- In use: `fusion-pixel-12px-proportional-zh_hans.ttf` (Simplified Chinese glyph forms), the
  default UI font for English, Dutch and Chinese. The `latin`, `ja`, `ko`, `zh_hant`, `zh_hk` and
  `zh_tw` variants are kept for future languages.

Fusion Pixel combines glyphs from these fonts, whose licences ship with it in `LICENSES/`:

| Font | Author | Licence | Licence file |
| --- | --- | --- | --- |
| [Ark Pixel Font](https://github.com/TakWolf/ark-pixel-font) | TakWolf | OFL 1.1 | `LICENSES/ark-pixel/OFL.txt` |
| [Cubic 11 (俐方體11號)](https://github.com/ACh-K/Cubic-11) | ACh-K; includes JF Dot M+H 12 (M+ FONTS PROJECT) and M+ BITMAP FONTS (COZ), both free licences | OFL 1.1 | `LICENSES/cubic-11/OFL.txt` |
| [Galmuri](https://github.com/quiple/galmuri) | Lee Minseo | OFL 1.1 | `LICENSES/galmuri/LICENSE.txt` |

## Drawn in code

The glint (剑光), deflect, block, hit and finisher sparks, the shadows, the posture bars and the
finisher mark are drawn in code (`game/scripts/Fx/`, `game/scripts/Actors/EnemyBars.cs`,
`game/scripts/Ui/Hud.cs`).

## Translations

- English: original.
- Nederlands: machine-drafted, awaiting review by a native speaker.
- 简体中文: drafted by Claude, to be reviewed by the author.

## Tools

Built with [Godot Engine](https://godotengine.org) (MIT).
