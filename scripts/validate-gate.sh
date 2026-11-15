#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export AVALONIA_TELEMETRY_OPTOUT=1
command -v dotnet >/dev/null || { echo '.NET SDK 10 richiesto; Python non necessario' >&2; exit 2; }
dotnet run -c Release --project tools/Ristretto.DevTools/Ristretto.DevTools.csproj -- gate "${1:-core}"
