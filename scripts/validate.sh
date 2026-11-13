#!/usr/bin/env bash
set -euo pipefail
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export AVALONIA_TELEMETRY_OPTOUT=1
cd "$(dirname "$0")/.."
echo "Ristretto v3.9-alpha.20.5 - sorgenti: $PWD"
if [[ "$(cat VERSION.txt 2>/dev/null)" != "Ristretto 3.9.0-alpha.20.5" ]]; then
  echo 'Versione inattesa: utilizzare una nuova estrazione dello ZIP v3.9 alpha.20.5' >&2; exit 3
fi
if grep -Fq '[InlineData("\uD83D")]' tests/TextExpander.Tests/WindowsOutputContractTests.cs ||
   grep -Fq '[InlineData("\uDC4D")]' tests/TextExpander.Tests/WindowsOutputContractTests.cs; then
  echo 'Vecchi surrogate InlineData presenti: fonte non aggiornata' >&2; exit 3
fi
for test_name in UnpairedHighSurrogateIsRejected UnpairedLowSurrogateIsRejected; do
  grep -q "void $test_name()" tests/TextExpander.Tests/WindowsOutputContractTests.cs || { echo "Manca test $test_name" >&2; exit 3; }
done
grep -Fq 'HasEligiblePrefix(_suffix, exactLength)' src/TextExpander.Core/Matching/TriggerActivationSession.cs || { echo 'Hotfix F1b Immediate mancante' >&2; exit 3; }
for required in src/TextExpander.Core/Matching/TriggerActivationSession.cs tests/TextExpander.Tests/TriggerActivationSessionTests.cs docs/F1B_ACTIVATION_POLICY.md; do
  [[ -f "$required" ]] || { echo "F1b incompleta: manca $required" >&2; exit 3; }
done
if command -v python3 >/dev/null; then
  ./scripts/validate-source.sh
else
  echo 'AVVISO: Python3 non disponibile, audit statico facoltativo omesso; il compilatore resta obbligatorio.'
fi
command -v dotnet >/dev/null || { echo "ERRORE: installare .NET 10 SDK" >&2; exit 2; }
dotnet restore Ristretto.sln
dotnet build Ristretto.sln -c Release --no-restore
dotnet test Ristretto.sln -c Release --no-build --logger "console;verbosity=normal"
dotnet restore tools/Ristretto.CoreBenchmark/Ristretto.CoreBenchmark.csproj
dotnet build tools/Ristretto.CoreBenchmark/Ristretto.CoreBenchmark.csproj -c Release --no-restore
echo 'Benchmark Core compilato ma non eseguito; laboratorio Win32 escluso su Linux/macOS.'
dotnet restore tools/Ristretto.ReplacementLab/Ristretto.ReplacementLab.csproj
dotnet build tools/Ristretto.ReplacementLab/Ristretto.ReplacementLab.csproj -c Release --no-restore
echo 'Laboratorio F2b simulato compilato ma non eseguito.'
dotnet restore tools/Ristretto.UiReview/Ristretto.UiReview.csproj
dotnet build tools/Ristretto.UiReview/Ristretto.UiReview.csproj -c Release --no-restore
echo 'Revisione UI compilata ma non eseguita (richiede sessione desktop).'

if command -v node >/dev/null 2>&1 && command -v npm >/dev/null 2>&1; then
  ./scripts/validate-vscode.sh
  ./scripts/validate-export-contract.sh
else
  echo 'AVVISO: Node/npm non disponibili: test adapter VS Code NON ESEGUITI.'
fi
