#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export AVALONIA_TELEMETRY_OPTOUT=1
mode="${1:-core}"
python="${RISTRETTO_PYTHON:-}"
if [[ -z "$python" ]]; then
  if command -v python3 >/dev/null 2>&1; then python="$(command -v python3)"
  elif command -v python >/dev/null 2>&1; then python="$(command -v python)"
  else echo 'Python 3.10+ richiesto' >&2; exit 2; fi
fi
"$python" scripts/quality_gate.py --mode "$mode"
