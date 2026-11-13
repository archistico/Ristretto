#!/usr/bin/env python3
"""Check the real desktop entry point using diagnostic commands only; never request GUI startup."""
from __future__ import annotations

import argparse
import json
import subprocess
import tempfile
from pathlib import Path


def check(assembly: Path, version: str) -> dict:
    assembly = assembly.resolve(strict=True)
    results = []
    with tempfile.TemporaryDirectory(prefix='ristretto-startup-') as temporary:
        root = Path(temporary).resolve()

        def invoke(name: str, args: list[str], code: int) -> subprocess.CompletedProcess:
            result = subprocess.run(['dotnet', str(assembly), *args], cwd=root,
                                    stdin=subprocess.DEVNULL, capture_output=True,
                                    encoding='utf-8', timeout=15, check=False)
            assert result.returncode == code, (name, result.returncode, result.stderr)
            if code:
                assert result.stderr.strip() and not result.stdout.strip(), name
            else:
                assert not result.stderr.strip(), (name, result.stderr)
            results.append({'case': name, 'status': 'PASS', 'exit_code': code})
            return result

        help_result = invoke('help', ['--help'], 0)
        assert '--data-dir' in help_result.stdout
        version_result = invoke('version', ['--version'], 0)
        assert version_result.stdout.strip() == 'Ristretto ' + version
        assert not list(root.iterdir()), 'Information commands created files'

        relative = 'prove con spazi/日本語 🌍'
        expected = root / relative
        for name, args in [
            ('relative-unicode-directory', ['--check-profile', '--data-dir', relative]),
            ('absolute-equals-directory', ['--data-dir=' + str(expected), '--check-profile']),
        ]:
            payload = json.loads(invoke(name, args, 0).stdout)
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
    arguments = parser.parse_args()
    source_version = (Path(__file__).resolve().parent.parent / 'VERSION.txt').read_text(encoding='utf-8').strip()
    report = check(arguments.assembly, source_version.removeprefix('Ristretto '))
    arguments.output.parent.mkdir(parents=True, exist_ok=True)
    arguments.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(f"Startup CLI: {report['passed']}/{report['passed']} PASS; no GUI or database opened")
