#!/usr/bin/env python3
"""Create an integrally self-contained Ristretto ZIP with a stable Ristretto/ root.

This is packaging only: it does not build .NET or assert the tests are passing.
"""
from __future__ import annotations

import argparse
import hashlib
import zipfile
from pathlib import Path

PROJECT = Path(__file__).resolve().parent.parent
EXCLUDED_DIRS = {'.git', '.vs', 'bin', 'obj', '__pycache__', 'archive', 'beeftext-screenshots', '.local-cache', 'TestResults'}
EXCLUDED_SUFFIXES = {'.pyc', '.pyo'}
MANIFEST = 'MANIFEST_SHA256.txt'


def source_files() -> list[Path]:
    return sorted(
        (p for p in PROJECT.rglob('*') if p.is_file()
         and p.name != MANIFEST
         and not any(part in EXCLUDED_DIRS for part in p.relative_to(PROJECT).parts)
         and p.suffix not in EXCLUDED_SUFFIXES),
        key=lambda p: p.relative_to(PROJECT).as_posix()
    )


def build(out: Path) -> tuple[int, str]:
    files = source_files()
    lines = [f'{hashlib.sha256(p.read_bytes()).hexdigest()}  {p.relative_to(PROJECT).as_posix()}' for p in files]
    (PROJECT / MANIFEST).write_text('\n'.join(lines) + '\n', encoding='utf-8')
    out.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(out, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for p in [*files, PROJECT / MANIFEST]:
            zf.write(p, 'Ristretto/' + p.relative_to(PROJECT).as_posix())
    with zipfile.ZipFile(out) as zf:
        assert zf.testzip() is None, 'ZIP CRC verification failed'
        names = zf.namelist()
        assert all(n.startswith('Ristretto/') and len(n) > len('Ristretto/') for n in names)
        assert len(names) == len(files) + 1
        for record in lines:
            expected, name = record.split('  ', 1)
            assert hashlib.sha256(zf.read('Ristretto/' + name)).hexdigest() == expected, name
    return len(files) + 1, hashlib.sha256(out.read_bytes()).hexdigest()


if __name__ == '__main__':
    p = argparse.ArgumentParser(description='Create a ZIP with one versionless Ristretto/ folder')
    p.add_argument('--output', type=Path, default=PROJECT / '.local-cache' / ('Ristretto_' + (PROJECT / 'VERSION.txt').read_text(encoding='utf-8').strip().removeprefix('Ristretto ').replace('.', '_') + '_COMPLETO.zip'))
    args = p.parse_args()
    count, digest = build(args.output.resolve())
    print(f'{args.output.resolve()} | {count} files | SHA-256 {digest}')
