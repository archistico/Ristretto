#!/usr/bin/env python3
"""Run both the general static checks and the current GUI contract.

These are structural checks only. They do not compile Avalonia or launch a GUI.
"""
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parent.parent
for name in ('verify_static.py', 'verify_gui_redesign.py'):
    outcome = subprocess.run([sys.executable, str(root / 'scripts' / name)], cwd=root,
                             stdin=subprocess.DEVNULL, check=False)
    if outcome.returncode != 0:
        print(f'STATIC GATE FAIL: {name}', file=sys.stderr)
        sys.exit(1)
print('STATIC GATE PASS: structural source, SQLite, localization and GUI contract')
