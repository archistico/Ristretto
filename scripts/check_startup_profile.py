#!/usr/bin/env python3
"""Verifica il vero punto d'ingresso desktop senza avviare la GUI o aprire SQLite.

Gli errori diagnostici su Windows possono essere emessi nella codifica della
console (es. CP1252/CP850), mentre l'output di successo JSON deve essere UTF-8.
Catturare byte, senza decoder nel thread di subprocess, evita crash spurii.
"""
from __future__ import annotations

import argparse
import json
import subprocess
import tempfile
from pathlib import Path
from typing import Callable


def invoke_cli(command: list[str], name: str, expected_code: int, cwd: Path,
               *, runner: Callable = subprocess.run) -> str:
    """Verifica codice e canale d'uscita; decodifica solo stdout dei successi.

    Non occorre decodificare stderr nei casi di errore attesi: e' sufficiente
    attestare che non e' vuoto. I messaggi possono usare codepage Windows.
    L'output JSON di successo resta invece rigorosamente UTF-8: niente replace
    che potrebbe nascondere percorsi Unicode danneggiati.
    """
    result = runner(command, cwd=cwd, stdin=subprocess.DEVNULL,
                    stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                    timeout=15, check=False)
    stdout, stderr = result.stdout, result.stderr
    if not isinstance(stdout, bytes) or not isinstance(stderr, bytes):
        raise AssertionError(f'{name}: subprocess non ha restituito byte per stdout/stderr')
    if result.returncode != expected_code:
        raise AssertionError(f'{name}: codice di uscita {result.returncode}, atteso {expected_code}; '
                             f'stdout={len(stdout)} byte, stderr={len(stderr)} byte')
    if expected_code != 0:
        if not stderr.strip() or stdout.strip():
            raise AssertionError(f'{name}: errore diagnostico deve usare solo stderr')
        return ''
    if stderr.strip():
        raise AssertionError(f'{name}: successo con output inatteso su stderr ({len(stderr)} byte)')
    try:
        return stdout.decode('utf-8', errors='strict')
    except UnicodeDecodeError as exc:
        raise AssertionError(f'{name}: stdout del comando riuscito non e\' UTF-8 valido') from exc


def check(assembly: Path, version: str, dotnet: str = 'dotnet') -> dict:
    assembly = assembly.resolve(strict=True)
    results = []
    with tempfile.TemporaryDirectory(prefix='ristretto-startup-') as temporary:
        root = Path(temporary).resolve()

        def invoke(name: str, args: list[str], code: int) -> str:
            stdout = invoke_cli([dotnet, str(assembly), *args], name, code, root)
            results.append({'case': name, 'status': 'PASS', 'exit_code': code})
            return stdout

        help_result = invoke('help', ['--help'], 0)
        assert '--data-dir' in help_result
        version_result = invoke('version', ['--version'], 0)
        assert version_result.strip() == 'Ristretto ' + version
        assert not list(root.iterdir()), 'Information commands created files'

        relative = 'prove con spazi/日本語 🌍'
        expected = root / relative
        for name, args in [
            ('relative-unicode-directory', ['--check-profile', '--data-dir', relative]),
            ('absolute-equals-directory', ['--data-dir=' + str(expected), '--check-profile']),
        ]:
            payload = json.loads(invoke(name, args, 0))
            assert payload['status'] == 'profile_writable'
            assert payload['version'] == version
            assert Path(payload['data_directory']) == expected
            assert Path(payload['database_path']) == expected / 'library.db'
            assert Path(payload['backup_directory']) == expected / 'Backups'
            assert payload['database_opened'] is False and payload['desktop_started'] is False
            assert not list(expected.iterdir()), 'Preflight left files or opened storage'

        for name, args in [
            ('missing-directory-value', ['--data-dir', '--check-profile']),
            ('empty-directory', ['--check-profile', '--data-dir=']),
            ('duplicate-directory', ['--check-profile', '--data-dir=a', '--data-dir=b']),
            ('unknown-option', ['--check-profile', '--unknown']),
            ('conflicting-actions', ['--check-profile', '--version']),
        ]:
            invoke(name, args, 2)
        assert not (root / 'a').exists() and not (root / 'b').exists()

        occupied = root / 'occupied'
        occupied.write_bytes(b'synthetic sentinel')
        invoke('file-as-directory', ['--check-profile', '--data-dir', str(occupied)], 2)
        assert occupied.read_bytes() == b'synthetic sentinel'
        assert not list(expected.iterdir())
        assert not list(root.rglob('*.db')) and not list(root.rglob('.ristretto.profile.lock'))
    return {'schema': 'ristretto/startup-cli-check/1', 'source_version': version,
            'status': 'PASS', 'passed': len(results), 'cases': results,
            'gui_started': False, 'database_opened': False,
            'scope': 'Real entry point, diagnostic commands, temporary synthetic directories only'}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--assembly', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--dotnet-executable', default='dotnet', help='Exact dotnet executable path, spaces allowed')
    arguments = parser.parse_args()
    source_version = (Path(__file__).resolve().parent.parent / 'VERSION.txt').read_text(encoding='utf-8').strip()
    report = check(arguments.assembly, source_version.removeprefix('Ristretto '),
                   arguments.dotnet_executable)
    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    arguments.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(f"Startup CLI: {report['passed']}/{report['passed']} PASS; nessuna GUI o database aperto")
