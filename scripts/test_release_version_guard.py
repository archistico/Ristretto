"""Regressioni del gate Windows: l'Alpha con più segmenti non va rifiutata.

Si controlla la regex effettivamente presente nel file PowerShell: il test evita
che una futura modifica torni a richiedere un numero fisso di componenti alpha.
Questo non esegue PowerShell; il risultato Windows resta da verificare.
"""
from pathlib import Path
import re
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
SCRIPT = ROOT / 'scripts' / 'verify-release.ps1'


def gate_pattern():
    contents = SCRIPT.read_text(encoding='utf-8-sig')
    match = re.search(r"\$expectedVersion\s+-notmatch\s+'([^']+)'", contents)
    if not match:
        raise AssertionError('Regola versione assente dallo script PowerShell')
    return re.compile(match.group(1))


class ReleaseVersionGuardTests(unittest.TestCase):
    def test_current_version_is_accepted_by_actual_powershell_pattern(self):
        version = ET.parse(ROOT / 'Directory.Build.props').findtext('.//Version')
        self.assertIsNotNone(version)
        self.assertIsNotNone(gate_pattern().fullmatch(version))
        self.assertEqual(f'Ristretto {version}', (ROOT / 'VERSION.txt').read_text(encoding='utf-8').strip())

    def test_three_or_more_alpha_identifiers_are_accepted(self):
        rule = gate_pattern()
        for version in ('3.9.0-alpha.20.19.1', '3.9.0-alpha.20.19.2',
                        '3.9.0-alpha.20.19.2.1', '3.9.0-alpha.21',
                        '3.9.0-alpha.7'):
            with self.subTest(version=version):
                self.assertIsNotNone(rule.fullmatch(version), version)

    def test_invalid_versions_remain_rejected(self):
        rule = gate_pattern()
        for version in ('', '3.9.0', '3.9-alpha.20', '3.9.0-alpha',
                        '3.9.0-alpha.', '3.9.0-alpha.20.',
                        '3.9.0-alpha.20..19', '3.9.0-alpha.20.x',
                        'v3.9.0-alpha.20', '3.9.0-alpha.20.19-extra'):
            with self.subTest(version=version):
                self.assertIsNone(rule.fullmatch(version), version)

    def test_error_is_attributed_to_source_format_not_msbuild(self):
        source = SCRIPT.read_text(encoding='utf-8-sig')
        self.assertNotIn("Stop-Validation 'Versione MSBuild non valida.'", source)
        self.assertIn('Formato versione sorgente non riconosciuto', source)
        self.assertNotIn('Estrai lo ZIP integrale della versione corrente in una cartella nuova;', source)


if __name__ == '__main__':
    unittest.main()
