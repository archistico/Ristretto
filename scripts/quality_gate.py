#!/usr/bin/env python3
"""Ristretto R1.04-R1.06: fail-closed quality gates and evidence report.

This is an orchestrator, not a substitute for the compiler. It never reads user
profiles and never emits snippet, clipboard or typed content into its JSON.
"""
from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
import platform
import re
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Callable

ROOT = Path(__file__).resolve().parent.parent
TOOLS = [
    'tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj',
    'tools/Ristretto.CoreBenchmark/Ristretto.CoreBenchmark.csproj',
    'tools/Ristretto.ReplacementLab/Ristretto.ReplacementLab.csproj',
    'tools/Ristretto.UiReview/Ristretto.UiReview.csproj',
]
STEPS = (
    'source_audit', 'python_regressions', 'static_checks',
    'dotnet_restore', 'solution_build', 'xunit',
    'windows_lab_build', 'benchmark_build', 'replacement_lab_build', 'ui_review_build',
    'startup_cli', 'node_tests', 'export_contract', 'native_windows',
)


@dataclass(frozen=True)
class Executable:
    argv: tuple[str, ...]
    version: str


def discover(name: str, names: tuple[str, ...], marker: str, minimum: int,
             run: Callable = subprocess.run, which: Callable = shutil.which,
             environ: dict | None = None) -> Executable | None:
    """Resolve executable and check it *runs* (not just that its file exists).

    Environment override is a literal executable path, including paths with
    spaces; no shell parsing or splitting of user-controlled strings.
    """
    env = os.environ if environ is None else environ
    override = env.get('RISTRETTO_' + name.upper())
    candidates: list[tuple[str, ...]] = []
    if override:
        candidates.append((override,))
    else:
        for candidate in names:
            path = which(candidate)
            if path:
                candidates.append((path, '-3') if candidate == 'py' and name == 'python' else (path,))
    for candidate in candidates:
        try:
            result = run([*candidate, '--version'], capture_output=True, text=True,
                         timeout=8, stdin=subprocess.DEVNULL, check=False)
        except (OSError, subprocess.TimeoutExpired):
            continue
        version = ((result.stdout or '') + ' ' + (result.stderr or '')).strip()
        match = re.search(marker, version, re.IGNORECASE)
        if (result.returncode == 0 and match and int(match.group(1)) >= minimum
                and not (name == 'python' and int(match.group(1)) == 3
                         and int(version.split('.')[1].split()[0]) < 10)):
            return Executable(candidate, version[:120])
    return None


def source_digest(root: Path) -> str:
    """Deterministic provenance fingerprint, excluding reports, data and build outputs."""
    h = hashlib.sha256()
    paths: list[Path] = []
    for name in ('src', 'tests', 'tools', 'scripts', '.github'):
        paths.extend(p for p in (root / name).rglob('*') if p.is_file() and
                     '.git' not in p.parts and not any(x in p.parts for x in
                     ('bin', 'obj', '__pycache__', 'node_modules', '.local-cache', 'TestResults'))
                     and p.suffix in ('.cs', '.csproj', '.py', '.cmd', '.sh', '.ps1', '.js', '.json', '.yml', '.yaml'))
    paths += [root / 'VERSION.txt', root / 'Directory.Build.props', root / 'global.json']
    for p in sorted(paths, key=lambda p: p.relative_to(root).as_posix()):
        if p.exists():
            h.update(p.relative_to(root).as_posix().encode('utf-8') + b'\0')
            h.update(hashlib.sha256(p.read_bytes()).digest())
    return h.hexdigest()


def plan(mode: str) -> list[str]:
    if mode == 'source':
        return list(STEPS[:3])
    if mode == 'core':
        return list(STEPS[:11])
    if mode == 'ci':
        # CI exercises the Node/export contract without claiming an interactive desktop.
        return list(STEPS[:-1])
    if mode == 'full':
        return list(STEPS)
    raise ValueError(f'unknown gate: {mode}')


def record_not_run(name: str, reason: str) -> dict:
    return {'name': name, 'status': 'NOT_RUN', 'detail': reason}


def run_gate(mode: str, root: Path = ROOT, *, reporter: Path | None = None,
             runner: Callable = subprocess.run, resolver: Callable = discover,
             os_name: str | None = None, restore_source: str = 'unknown',
             verbose: bool = True) -> dict:
    root = root.resolve()
    os_name = platform.system() if os_name is None else os_name
    version = (root / 'VERSION.txt').read_text(encoding='utf-8').strip().removeprefix('Ristretto ')
    report: dict = {
        'schema': 'ristretto/quality-gate/1', 'source_version': version,
        'git_commit': (os.environ.get('GITHUB_SHA', '').lower() if
                       re.fullmatch(r'[a-fA-F0-9]{40}', os.environ.get('GITHUB_SHA', '')) else None),
        'source_sha256': source_digest(root),
        'utc': dt.datetime.now(dt.timezone.utc).isoformat(timespec='seconds'),
        'mode': mode, 'platform': os_name,
        'steps': [record_not_run(step, 'Not in gate') for step in STEPS],
        'restore': {'source': restore_source, 'status': 'NOT_RUN'},
        'nuget_vulnerability_audit': {'status': 'NOT_RUN',
                                     'detail': 'Not inferred from package restore; run separate online audit'},
        'vs_code_real_host': 'NOT_RUN',
        'desktop_picker_real_io': 'NOT_RUN',
        'result': 'FAIL',
    }
    selected = plan(mode)
    index = {step: i for i, step in enumerate(STEPS)}
    for name in selected:
        report['steps'][index[name]] = record_not_run(name, 'Blocked by previous step')

    py = resolver('python', ('py', 'python', 'python3'), r'Python\s+(\d+)\.', 3)
    dotnet = resolver('dotnet', ('dotnet',), r'\b(\d+)\.\d+', 10) if mode != 'source' else None
    node = resolver('node', ('node',), r'v?(\d+)\.\d+', 22) if mode in ('ci', 'full') else None
    npm = resolver('npm', ('npm.cmd', 'npm'), r'\b(\d+)\.\d+', 10) if mode in ('ci', 'full') else None
    if verbose:
        print(f'Ristretto {version}: gate={mode} OS={os_name}; hash={report["source_sha256"][:12]}')
        for name, tool in (('Python',py), ('dotnet',dotnet), ('Node',node), ('npm',npm)):
            if tool:
                print(f'  {name}: {tool.version}')

    def execute(name: str, args: list[str] | None, *, cwd: Path = root,
                reason: str = '') -> bool:
        if name not in selected:
            return True
        if args is None:
            report['steps'][index[name]] = {'name': name, 'status': 'FAIL',
                                             'detail': reason or 'Required tool or platform missing'}
            if verbose: print(f'FAIL {name}: {reason}')
            return False
        if verbose: print(f'RUN {name}', flush=True)
        try:
            result = runner(args, cwd=cwd, timeout=600, check=False)
            ok = result.returncode == 0
            detail = 'Command returned 0' if ok else f'Exit code {result.returncode}'
        except subprocess.TimeoutExpired:
            ok, detail = False, 'Timeout (600 seconds)'
        except OSError as exc:
            ok, detail = False, f'Unable to start: {type(exc).__name__}'
        report['steps'][index[name]] = {'name': name, 'status': 'PASS' if ok else 'FAIL', 'detail': detail}
        if verbose: print(f'{"PASS" if ok else "FAIL"} {name}: {detail}', flush=True)
        return ok

    try:
        if not execute('source_audit', [*py.argv,'scripts/audit_source.py'] if py else None,
                       reason='Python >=3 required'): return report
        if not execute('python_regressions', [*py.argv,'-m','unittest','discover','-s','scripts',
                                              '-p','test_*.py','-v'] if py else None): return report
        if not execute('static_checks', [*py.argv,'scripts/verify_all_static.py'] if py else None): return report
        if mode == 'source':
            report['result'] = 'PASS'
            return report
        if not execute('dotnet_restore', [*dotnet.argv,'restore','Ristretto.sln'] if dotnet else None,
                       reason='.NET 10 SDK required'):
            report['restore']['status'] = 'FAIL'
            return report
        report['restore']['status'] = 'PASS'
        if not execute('solution_build', [*dotnet.argv,'build','Ristretto.sln','-c','Release',
                                           '--no-restore']): return report
        if not execute('xunit', [*dotnet.argv,'test','Ristretto.sln','-c','Release',
                                  '--no-build','--logger','console;verbosity=normal']): return report
        for label, path in zip(('windows_lab_build','benchmark_build','replacement_lab_build','ui_review_build'),TOOLS):
            if not execute(label, [*dotnet.argv,'build',path,'-c','Release']): return report
        if not execute('startup_cli', [*py.argv, 'scripts/check_startup_profile.py',
                         '--assembly','src/TextExpander.Desktop/bin/Release/net10.0/Ristretto.dll',
                         '--output', '.local-cache/gates/startup-cli.json',
                         '--dotnet-executable', dotnet.argv[0]]): return report
        if mode in ('ci', 'full'):
            if not execute('node_tests', [*npm.argv,'run','check'] if npm and node else None,
                           cwd=root/'tools/ristretto-vscode-extension',
                           reason='Node >=22 and npm >=10 required'): return report
            # Keep the real contract between .NET serialization and the Node reader.
            export = root / '.local-cache/gates/export-from-dotnet.json'
            export.parent.mkdir(parents=True, exist_ok=True)
            export.unlink(missing_ok=True)
            # Execute the actual .NET test fixture with an isolated output path.
            try:
                env = os.environ.copy()
                env['RISTRETTO_VSCODE_EXPORT_OUTPUT'] = str(export)
                rc = runner([*dotnet.argv, 'test', 'tests/TextExpander.Tests/TextExpander.Tests.csproj',
                             '-c', 'Release', '--no-build', '--filter',
                             'FullyQualifiedName~VscodeExportContractTests'],
                            cwd=root, env=env, timeout=600, check=False)
                contract_ok = rc.returncode == 0 and export.is_file()
                if contract_ok:
                    rc = runner([*node.argv,'tools/ristretto-vscode-extension/cli/verify-dotnet-export.js',
                                 str(export)], cwd=root, timeout=600, check=False)
                    contract_ok = rc.returncode == 0
                report['steps'][index['export_contract']] = {'name':'export_contract',
                    'status':'PASS' if contract_ok else 'FAIL',
                    'detail':'Exporter and Node verifier agree' if contract_ok else 'Missing export or contract failed'}
                if not contract_ok: return report
            except (OSError, subprocess.TimeoutExpired):
                report['steps'][index['export_contract']] = {'name':'export_contract', 'status':'FAIL',
                                                              'detail':'Export command failed'}
                return report
            finally:
                export.unlink(missing_ok=True)
            if mode == 'ci':
                # Deliberately NOT_RUN even on Windows CI: a runner is not a qualified user desktop.
                report['result'] = 'PASS'
                return report
            if os_name != 'Windows':
                execute('native_windows', None, reason='Windows interactive desktop required')
                return report
            if not execute('native_windows', [*dotnet.argv,'run','-c','Release', '--no-build',
                                               '--project', TOOLS[0], '--', '--replacement-test']): return report
            # A complete gate additionally requires both cross-process smoke tests.
            for arg in ('--cooperating-smoke', '--remote-owned-smoke'):
                try:
                    rc = runner([*dotnet.argv,'run','-c','Release','--no-build',
                                 '--project',TOOLS[0], '--',arg], cwd=root, timeout=600, check=False)
                    if rc.returncode != 0:
                        report['steps'][index['native_windows']]['status'] = 'FAIL'
                        report['steps'][index['native_windows']]['detail'] = f'Native smoke failed: {arg}'
                        return report
                except (OSError, subprocess.TimeoutExpired):
                    report['steps'][index['native_windows']]['status'] = 'FAIL'
                    report['steps'][index['native_windows']]['detail'] = f'Native smoke not executed: {arg}'
                    return report
        report['result'] = 'PASS'
        return report
    finally:
        if reporter is not None:
            reporter.parent.mkdir(parents=True, exist_ok=True)
            # Write even on early return or missing tools: NOT_RUN is never a PASS.
            reporter.write_text(json.dumps(report, indent=2, ensure_ascii=False)+'\n',encoding='utf-8')
            if verbose: print(f'Report: {reporter}')


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--mode', choices=('source','core','ci','full'), default='core')
    parser.add_argument('--report', type=Path, default=None)
    parser.add_argument('--restore-source', choices=('local','online','unknown'), default='unknown',
                        help='Provenance of NuGet restore; NOT an online audit result')
    args = parser.parse_args()
    output = (args.report or ROOT/'.local-cache/gates'/f'{args.mode}.json').resolve()
    report = run_gate(args.mode, reporter=output, restore_source=args.restore_source)
    print('\n' + ' | '.join(f'{x["name"]}={x["status"]}' for x in report['steps']))
    print(f'GATE {args.mode.upper()}: {report["result"]} (NOT_RUN is never a PASS)')
    return 0 if report['result'] == 'PASS' else 1


if __name__ == '__main__':
    raise SystemExit(main())
