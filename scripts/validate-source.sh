#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
python3 scripts/audit_source.py
python3 -m unittest discover -s scripts -p 'test_audit_source.py' -v
python3 scripts/verify_static.py
