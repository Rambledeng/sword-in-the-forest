#!/usr/bin/env bash
# Render a scene and save docs/screens/<name>.png.
# Usage: tools/screenshot.sh <scene> [locale] [frames] [name]
# <scene> is relative to game/scenes without .tscn, e.g. Title. Frames run at a fixed 60 fps.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
mkdir -p .logs
dotnet build game/SwordInTheForest.csproj -nologo -v q > .logs/build.log 2>&1 || { tail -20 .logs/build.log; exit 1; }
timeout 600 "${GODOT:-godot}" --headless --path game --import > .logs/import.log 2>&1
args=(--screenshot "$1")
[ $# -ge 2 ] && [ -n "$2" ] && args+=(--locale "$2")
[ $# -ge 3 ] && args+=(--frames "$3")
[ $# -ge 4 ] && args+=(--shot-name "$4")
timeout 300 xvfb-run -a "${GODOT:-godot}" --path game --fixed-fps 60 -- "${args[@]}" > .logs/screenshot.log 2>&1 || { tail -15 .logs/screenshot.log; exit 1; }
# Godot's exit code decides pass/fail (checks such as FloorSoak exit 1 on failure); then
# require at least one result line, so a run that silently saved nothing also fails.
grep -E '^(SCREENSHOT|SOAK) ' .logs/screenshot.log || { echo "no SCREENSHOT or SOAK line in .logs/screenshot.log"; tail -15 .logs/screenshot.log; exit 1; }
