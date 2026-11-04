#!/usr/bin/env bash
# Screenshot every screen in every language: docs/screens/<screen>_<locale>.png.
# Usage: tools/screens_all.sh [locale ...]   (default: en nl zh_CN)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
locales=("$@")
[ ${#locales[@]} -gt 0 ] || locales=(en nl zh_CN)
shots=(
  "Title 20 title"
  "screens/MenuTitleSettings 20 title_settings"
  "screens/RunBamboo 150 run_bamboo"
  "screens/RunOutside 150 run_outside"
  "screens/RunUpgrade 60 run_upgrade"
  "screens/RunBoss 120 run_boss"
  "screens/MenuPause 30 pause"
  "screens/MenuPauseSettings 30 pause_settings"
  "screens/MenuSummaryLoss 200 summary_loss"
  "screens/MenuSummaryWin 200 summary_win"
)
for loc in "${locales[@]}"; do
  for s in "${shots[@]}"; do
    read -r scene frames name <<< "$s"
    tools/screenshot.sh "$scene" "$loc" "$frames" "${name}_${loc}" > /dev/null || echo "FAILED $name $loc"
  done
done
echo "screens done"
