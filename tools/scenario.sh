#!/usr/bin/env bash
# Run headless scenario tests. Usage: tools/scenario.sh [name ...]  (default: all)
# A scenario passes only if Godot exits 0 AND prints "SCENARIO PASS <name>".
set -uo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
GODOT="${GODOT:-godot}"
LOGDIR="${LOGDIR:-$ROOT/.logs}"
mkdir -p "$LOGDIR"
cd "$ROOT"
dotnet build game/SwordInTheForest.csproj -nologo -v q > "$LOGDIR/build.log" 2>&1 || { tail -20 "$LOGDIR/build.log"; exit 1; }
# First run imports resources (translations etc.) into game/.godot.
[ -d game/.godot/imported ] || timeout 300 "$GODOT" --headless --path game --import > "$LOGDIR/import.log" 2>&1
names=("$@")
[ ${#names[@]} -gt 0 ] || names=($(ls game/scenes/scenarios/*.tscn | xargs -n1 basename | sed 's/\.tscn$//'))
failed=0
for n in "${names[@]}"; do
  log="$LOGDIR/scenario-$n.log"
  timeout 120 "$GODOT" --headless --path game -- --scenario "$n" > "$log" 2>&1
  code=$?
  if [ $code -eq 0 ] && grep -q "^SCENARIO PASS $n" "$log"; then
    echo "PASS $n"
  else
    echo "FAIL $n (exit $code), log: $log"; tail -15 "$log"; failed=1
  fi
done
exit $failed
