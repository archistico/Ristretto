#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
command -v dotnet >/dev/null || { echo '.NET 10 SDK necessario' >&2; exit 2; }
command -v node >/dev/null || { echo 'Node.js 22 necessario' >&2; exit 2; }
mkdir -p .local-cache/vscode-contract
export RISTRETTO_VSCODE_EXPORT_OUTPUT="$PWD/.local-cache/vscode-contract/export-from-dotnet.json"
rm -f "$RISTRETTO_VSCODE_EXPORT_OUTPUT"
trap 'rm -f "$RISTRETTO_VSCODE_EXPORT_OUTPUT"' EXIT
dotnet test tests/TextExpander.Tests/TextExpander.Tests.csproj -c Release --filter 'FullyQualifiedName~VscodeExportContractTests'
test -f "$RISTRETTO_VSCODE_EXPORT_OUTPUT"
node tools/ristretto-vscode-extension/cli/verify-dotnet-export.js "$RISTRETTO_VSCODE_EXPORT_OUTPUT"
echo 'CONTRATTO INCROCIATO SUPERATO'
