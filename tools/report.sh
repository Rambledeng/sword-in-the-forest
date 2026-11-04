#!/usr/bin/env bash
# Regenerates the M5 evaluation: simulator CSV -> docs/figures/*.png and docs/report.md.
# Usage: tools/report.sh [--players N]   (default 1000; needs .NET 8 and analysis/requirements.txt)
set -euo pipefail
cd "$(dirname "$0")/.."
players=1000
if [[ "${1:-}" == "--players" ]]; then players="$2"; fi
mkdir -p .logs
echo "simulating ($players players per run)..."
dotnet run --project sim -c Release -- --study m5 --players "$players" --out analysis/out > .logs/report-sim.log 2>&1 \
  || { tail -20 .logs/report-sim.log; exit 1; }
tail -1 .logs/report-sim.log
python3 analysis/evaluate.py --data analysis/out --docs docs
