# itch.io page draft

Draft for <https://rambledeng.itch.io/sword-in-the-forest>. Edit freely; the GIFs are in
`docs/gifs/` and the screenshots in `docs/screens/`.

**Paste [`itch_page.html`](itch_page.html), not this file.** itch.io's description editor does
not render Markdown; switch it to HTML (the `<>` source button) and paste the HTML version, which
holds the same text one line per paragraph. Keep the two in step when you edit one.

**Suggested page settings**

- Kind: Downloadable. Classification: Game. Release status: Prototype (or In development).
- Genre: Action. Tags: soulslike, wuxia, parry, pixel-art, roguelike, top-down, adaptive-difficulty, singleplayer.
- Platforms: Windows, Linux (butler pushes the `windows` and `linux` channels).
- Languages: English, Dutch, Chinese (Simplified). Inputs: keyboard, mouse, Xbox/PlayStation controller.
- Short description (tagline): *A wuxia soulslike where the sword learns how fast you react.*
- Cover: a crop of `docs/screens/title_en.png` or the boss GIF. Screenshots: deflect, F2 overlay, boss, title (zh_CN).

---

## Sword in the Forest · 林中剑

**The sword has waited sixty years in the bitter bamboo. It has learned patience. Now it wants
to learn you.**

You are a wandering soul who pulls 鸣不平 from its seal. The sword wants blood, and it will
talk you through every cut to get it. Fight through the bamboo forest, past the tavern and into
it, where the Landlady keeps a black inn.

**Deflect, don't flee.** Every enemy attack flashes a glint before it lands. Tap guard after the
glint to deflect it and wreck their posture; roll when the glint is red. Break an enemy and it
kneels for the finisher, or drops its weapon and begs. Spare it or don't. Not every beggar means
it.

**The sword reads your hand.** A small Bayesian model inside the sword estimates your reaction
time from every attack you face, then times each warning to the challenge you choose (Relaxed,
Standard or Hard). Fast or slow, the fight asks the same of you. Only the reaction window
adapts; health, damage and patterns are the same for everyone, and you can switch adaptive
timing off. Beat the boss once to unlock Detail mode and see what it learned about you.

**In this demo**

- One area in three parts: bamboo forest, outside the tavern, the tavern, and a two-phase boss.
- A 10–15 minute run; die and a new soul starts in the area where the last one fell, keeping its upgrades.
- Three enemy types (saber, spear with red thrusts, bow), sword-intent upgrades, mercy and karma.
- English, Nederlands and 简体中文, switchable in game. Keyboard and mouse or a gamepad.

**Controls:** WASD move · mouse aim · left click attack · right click guard / deflect ·
Space roll · R gourd · E spare · Esc pause. Gamepad: X attack, LB guard, A roll, Y gourd, B spare.

**How it works.** The director, its evaluation on 1,000 simulated players (it lands 93% of
players within 10 points of the Standard target, against 28% with fixed timing) and the full
source are on GitHub: <https://github.com/Rambledeng/sword-in-the-forest>.
The report: <https://github.com/Rambledeng/sword-in-the-forest/blob/main/docs/report.md>.

**Install:** unzip and run `SwordInTheForest.exe` (Windows) or `SwordInTheForest.x86_64`
(Linux). Keep the `data_SwordInTheForest_*` folder next to it. Windows may warn about an
unsigned app: More info → Run anyway.

**Credits:** art, music and sounds from the Ninja Adventure Asset Pack by Pixel-boy and AAA
(CC0); Fusion Pixel Font by TakWolf (OFL). Made with Godot. Code MIT.

### 中文简介

苦竹林中，一柄封印了六十年的剑在等它的主人。你是第一个游魂，拔出了鸣不平。它要血，也要读懂你的手。
每一次敌人出招前都有剑光：看清它，弹反它，红光则翻滚避开。剑会从每一招中估计你的反应速度，
并按你选的难度为每一次警示定时——快也好慢也好，挑战都恰如其分。击溃的敌人会跪地或求饶，饶不饶，由你，
但并非每个求饶的都真心。约 10–15 分钟的试玩，含竹林、酒馆外、酒馆与老板娘一战。支持简体中文、英文与荷兰文。
