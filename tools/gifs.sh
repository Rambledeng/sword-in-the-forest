#!/usr/bin/env bash
# Records the README GIFs with Godot's Movie Maker (seeded, bot-played runs) -> docs/gifs/*.gif.
# Needs xvfb-run (or a display) and ffmpeg. About a minute.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GODOT="${GODOT:-godot}"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
cd "$ROOT"
dotnet build game/SwordInTheForest.csproj -nologo -v q > /dev/null
"$GODOT" --headless --path game --import > /dev/null 2>&1 || true
record() { # scene frames out
  timeout 600 xvfb-run -a "$GODOT" --path game --fixed-fps 30 --write-movie "$TMP/$3.avi" \
    --quit-after "$2" "res://scenes/screens/$1.tscn" -- --locale en > "$TMP/$3.log" 2>&1
}
gif() { # in start seconds filter out colours
  ffmpeg -loglevel error -y -ss "$2" -t "$3" -i "$TMP/$1.avi" \
    -vf "$4,split[a][b];[a]palettegen=max_colors=$6:stats_mode=diff[p];[b][p]paletteuse=dither=none:diff_mode=rectangle" \
    -loop 0 "docs/gifs/$5.gif"
}
mkdir -p docs/gifs
record MovieBamboo 600 bamboo      # the first bamboo room, no overlay
record MovieDirector 600 director  # the same room with the F2 overlay and a fresh belief
record RunBoss 750 boss            # the Landlady, both phases
gif bamboo 3.5 9 "crop=320:180:80:45,fps=20,scale=640:360:flags=neighbor" deflect 128
gif director 0 19 "setpts=PTS/1.5,fps=10,scale=720:405:flags=neighbor" director 96
gif boss 1.5 21 "setpts=PTS/1.5,fps=15,scale=720:405:flags=neighbor" boss 128
ls -l docs/gifs
