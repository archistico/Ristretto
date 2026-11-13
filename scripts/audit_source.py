#!/usr/bin/env python3
"""Offline structural audit for C# project boundaries.

This is NOT a C# compiler: implicit usings, overloaded methods, generated sources,
external package symbols and type resolution must still be checked with dotnet build.
It detects internal namespace and project reference drift before shipping a ZIP.
"""
from __future__ import annotations

import argparse
from collections import defaultdict
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

NAMESPACE = re.compile(r"^\s*namespace\s+([\w.]+)\s*[;{]", re.M)
USING = re.compile(r"^\s*(?:global\s+)?using\s+(?!static\s)([\w.]+)\s*;", re.M)
TEST_ATTRIBUTE = re.compile(r"\[(?:Fact|Theory|InlineData)\b")
SOLUTION_PROJECT = re.compile(r'Project\([^\r\n]+?\)\s*=\s*"[^"]+",\s*"([^"]+\.csproj)"')
# Conservative check for explicit Type.InternalMethod(...) calls across project boundaries.
# This is a source-pattern guard, NOT a replacement for Roslyn binding/compilation.
CLASS_DECL = re.compile(r'\b(?:public|internal|private|protected)\s+(?:(?:abstract|sealed|static|partial)\s+)*class\s+(\w+)')
INTERNAL_STATIC_METHOD = re.compile(r'\binternal\s+static\s+[\w?<>\[\],]+\s+(\w+)\s*\(')



def audit(root: Path) -> list[str]:
    root = root.resolve()
    issues: list[str] = []
    version_path = root / 'VERSION.txt'
    if not version_path.is_file():
        return ['VERSION.txt mancante']
    raw_version = version_path.read_text(encoding='utf-8').strip()
    if not raw_version.startswith('Ristretto '):
        issues.append('VERSION.txt: formato versione non riconosciuto')
    version = raw_version.removeprefix('Ristretto ')
    projects = sorted(root.glob('src/**/*.csproj')) + sorted(root.glob('tests/**/*.csproj')) + sorted(root.glob('tools/**/*.csproj'))
    project_files: dict[Path, list[Path]] = {}
    references: dict[Path, set[Path]] = {}
    all_namespaces: dict[str, set[Path]] = defaultdict(set)
    each_usings: dict[Path, list[tuple[Path, str]]] = defaultdict(list)
    internal_static_members: list[tuple[Path, str, str]] = []
    for project in projects:
        project = project.resolve()
        try:
            tree = ET.parse(project)
        except ET.ParseError as error:
            issues.append(f'{project.relative_to(root)}: XML non valido: {error}')
            continue
        refs = set()
        for element in tree.findall('.//ProjectReference'):
            relative = element.attrib.get('Include', '')
            target = (project.parent / relative.replace('\\', '/')).resolve()
            if not target.is_file():
                issues.append(f'{project.relative_to(root)}: riferimento progetto mancante {relative}')
            else:
                refs.add(target)
        references[project] = refs
        for element in tree.findall('.//Version'):
            if element.text and element.text.strip() != version:
                issues.append(f'{project.relative_to(root)}: versione {element.text.strip()} diversa da {version}')
        sources = sorted(f for f in project.parent.rglob('*.cs') if not any(part in ('bin', 'obj') for part in f.relative_to(project.parent).parts))
        project_files[project] = sources
        for source in sources:
            text = source.read_text(encoding='utf-8-sig')
            found = NAMESPACE.findall(text)
            if len(found) != 1:
                issues.append(f'{source.relative_to(root)}: attesa una dichiarazione namespace, trovate {len(found)}')
            for namespace in found:
                all_namespaces[namespace].add(project)
            # Detect cross-assembly calls to internal static members. This catches the
            # CommittedInputSession.IsValidText CS0117 error missed in Alpha 20.1.
            declared_class = CLASS_DECL.search(text)
            if declared_class:
                for member in INTERNAL_STATIC_METHOD.findall(text):
                    internal_static_members.append((project, declared_class.group(1), member))
            for name in USING.findall(text):
                if name.startswith(('TextExpander.', 'Ristretto.')):
                    each_usings[project].append((source, name))
            if project.parent.name.endswith('.Tests') and TEST_ATTRIBUTE.search(text) and 'using Xunit;' not in text:
                issues.append(f'{source.relative_to(root)}: manca using Xunit per attributi di test')
    def accessible(project: Path) -> set[Path]:
        result = {project}
        stack = [project]
        while stack:
            current = stack.pop()
            for dependency in references.get(current, ()):
                if dependency not in result:
                    result.add(dependency)
                    stack.append(dependency)
        return result
    for project, usings in each_usings.items():
        visible = accessible(project)
        for source, name in usings:
            declaring = {decl_project for declared, owners in all_namespaces.items()
                         if declared == name or declared.startswith(name + '.')
                         for decl_project in owners}
            if not declaring:
                issues.append(f'{source.relative_to(root)}: using interno sconosciuto: {name}')
            elif not (declaring & visible):
                issues.append(f'{source.relative_to(root)}: using {name} senza ProjectReference raggiungibile')
    for caller_project, sources in project_files.items():
        for source in sources:
            contents = source.read_text(encoding='utf-8-sig')
            for declaring_project, type_name, member in internal_static_members:
                if caller_project != declaring_project and re.search(
                    rf'\b{re.escape(type_name)}\s*\.\s*{re.escape(member)}\s*\(', contents
                ):
                    issues.append(
                        f'{source.relative_to(root)}: chiamata cross-assembly a membro internal '
                        f'{type_name}.{member} dichiarato in {declaring_project.relative_to(root)}'
                    )
    solution = root / 'Ristretto.sln'
    if not solution.is_file():
        issues.append('Ristretto.sln mancante')
    else:
        found = {(root / path.replace('\\', '/')).resolve() for path in SOLUTION_PROJECT.findall(solution.read_text(encoding='utf-8-sig'))}
        for missing in sorted(found - set(projects)):
            issues.append(f'Ristretto.sln: riferimento assente {missing}')
        for project in sorted(set(root.glob('src/**/*.csproj')) | set(root.glob('tests/**/*.csproj'))):
            if project.resolve() not in found:
                issues.append(f'Ristretto.sln: manca progetto {project.relative_to(root)}')
    return sorted(set(issues))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--root', type=Path, default=Path(__file__).resolve().parent.parent)
    args = parser.parse_args()
    issues = audit(args.root)
    if issues:
        for issue in issues:
            print('ERRORE:', issue, file=sys.stderr)
        return 1
    print('AUDIT SOURCE OK — using, namespace, riferimenti, versioni, accessi static internal, solution, xUnit (strutturale)')
    print('NOTA: questo controllo NON sostituisce dotnet build, dotnet test né l’host VS Code')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
