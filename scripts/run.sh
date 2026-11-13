#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
dotnet run --project src/TextExpander.Desktop/TextExpander.Desktop.csproj
