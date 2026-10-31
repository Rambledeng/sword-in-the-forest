#!/usr/bin/env bash
# Render a scene and save docs/screens/<scene>.png. Usage: tools/screenshot.sh <scene> [locale]
# <scene> is relative to game/scenes without .tscn, e.g. Title.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
mkdir -p .logs
dotnet build game/SwordInTheForest.csproj -nologo -v q > .logs/build.log 2>&1 || { tail -20 .logs/build.log; exit 1; }
args=(--screenshot "$1")
[ $# -ge 2 ] && args+=(--locale "$2")
timeout 120 xvfb-run -a "${GODOT:-godot}" --path game -- "${args[@]}" > .logs/screenshot.log 2>&1 || { tail -15 .logs/screenshot.log; exit 1; }
grep '^SCREENSHOT' .logs/screenshot.log
