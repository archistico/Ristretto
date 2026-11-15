"""Regressioni CLI: diagnostica Windows non UTF-8 non deve bloccare il gate.

Non serve .NET: i processi figli sintetici emettono veri byte di stdout/stderr.
"""
import sys
import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from check_startup_profile import invoke_cli


class StartupCliEncodingTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='ristretto-cli-test-')
        self.addCleanup(self.temporary.cleanup)
        self.cwd = Path(self.temporary.name)

    def _process(self, code: str, expected_exit: int) -> str:
        return invoke_cli([sys.executable, '-c', code], 'fixture-sintetica', expected_exit, self.cwd)

    def test_windows_cp1252_diagnostic_byte_on_stderr_is_accepted(self):
        script = 'import os,sys;os.write(2,bytes([0x95,0x20,0xe8]));sys.exit(2)'
        self.assertEqual('', self._process(script, 2))

    def test_windows_cp850_diagnostic_byte_on_stderr_is_accepted(self):
        script = 'import os,sys;os.write(2,bytes([0x82,0x20,0x9b]));sys.exit(2)'
        self.assertEqual('', self._process(script, 2))

    def test_success_keeps_unicode_stdout_exact(self):
        script = "import os;os.write(1, '日本語 🌍 e\\u0301'.encode('utf-8'))"
        self.assertEqual('日本語 🌍 e\u0301', self._process(script, 0))

    def test_non_utf8_success_is_rejected_instead_of_replacing_characters(self):
        script = 'import os;os.write(1,bytes([0x95]))'
        with self.assertRaisesRegex(AssertionError, 'non .*UTF-8 valido'):
            self._process(script, 0)

    def test_nonzero_exit_requires_nonempty_stderr(self):
        script = 'import sys;sys.exit(2)'
        with self.assertRaisesRegex(AssertionError, 'solo stderr'):
            self._process(script, 2)

    def test_nonzero_exit_rejects_stdout_even_with_stderr(self):
        script = "import os,sys;os.write(1,b'wrong');os.write(2,b'error');sys.exit(2)"
        with self.assertRaisesRegex(AssertionError, 'solo stderr'):
            self._process(script, 2)

    def test_success_rejects_stderr(self):
        script = "import os;os.write(2,b'warning')"
        with self.assertRaisesRegex(AssertionError, 'stderr'):
            self._process(script, 0)

    def test_wrong_exit_code_is_a_failure_without_decoding_bytes(self):
        script = 'import os,sys;os.write(2,bytes([0x95]));sys.exit(3)'
        with self.assertRaisesRegex(AssertionError, 'atteso 2'):
            self._process(script, 2)

    def test_subprocess_contract_requires_raw_bytes(self):
        def wrong_runner(*_args, **_kwargs):
            return SimpleNamespace(returncode=0, stdout='test', stderr='')
        with self.assertRaisesRegex(AssertionError, 'non ha restituito byte'):
            invoke_cli(['ignored'], 'incorrect-runner', 0, self.cwd, runner=wrong_runner)


if __name__ == '__main__':
    unittest.main()
