#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../tools/ristretto-vscode-extension"
command -v node >/dev/null || { echo 'Node.js necessario per il test VS Code' >&2; exit 2; }
command -v npm >/dev/null || { echo 'npm necessario per il test VS Code' >&2; exit 2; }
npm run check
