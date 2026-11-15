"""Cross-platform contract tests: never start Notepad or touch clipboard."""
import json
import sqlite3
from contextlib import closing
from unittest.mock import Mock
import tempfile
import unittest
from pathlib import Path
from qualify_notepad import BASE_TEXT, prepare, verify, normalize_newlines, fixture, status, launch_picker, classify_saved_document, REASONS_IT


class NotepadQualificationTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        (self.root / 'data').mkdir()
        (self.root / 'data' / 'PICKER_INSERT_SYNTHETIC_FIXTURE.txt').write_text(
            'Caffè cafe\u0301 👩🏽\u200d💻 العربية 日本語\nseconda riga', encoding='utf-8')
        (self.root / 'VERSION.txt').write_text('Ristretto 3.9.0-alpha.20.12', encoding='utf-8')
        self.session = prepare(self.root)

    def _save(self, text):
        self.session['document'].write_bytes(b'\xef\xbb\xbf' + text.encode('utf-8'))

    def _seed_synthetic_db(self, text=None):
        profile = self.session['document'].parent / 'profile'
        profile.mkdir()
        db = profile / 'library.db'
        with closing(sqlite3.connect(db)) as conn:
            conn.executescript("""CREATE TABLE groups(id TEXT, enabled INTEGER);
            CREATE TABLE snippets(id TEXT, group_id TEXT, trigger TEXT, enabled INTEGER, content TEXT);
            INSERT INTO groups VALUES ('g',1);""")
            conn.execute('INSERT INTO snippets VALUES (?, ?, ?, ?, ?)',
                         ('s', 'g', ';ristretto-test', 1, fixture(self.root) if text is None else text))
            conn.commit()
        return db

    def test_status_requires_profile_but_does_not_write_receipt(self):
        info = status(self.session['session'], self.root)
        self.assertEqual('NOT_CREATED', info['profile'])
        self.assertEqual('UNCHANGED', info['document'])
        self.assertEqual([], list(self.session['document'].parent.glob('receipt-*.json')))

    def test_status_validates_isolated_database_without_mutating_it(self):
        db = self._seed_synthetic_db()
        before = db.read_bytes()
        info = status(self.session['session'], self.root)
        self.assertEqual('READY', info['profile'])
        self.assertEqual('UNCHANGED', info['document'])
        self.assertFalse(info['desktop_input_observed'])
        self.assertEqual(before, db.read_bytes())

    def test_status_rejects_wrong_fixture_in_synthetic_profile(self):
        self._seed_synthetic_db('wrong snippet')
        self.assertEqual('INVALID', status(self.session['session'], self.root)['profile'])

    def test_status_rejects_broken_database(self):
        profile = self.session['document'].parent / 'profile'
        profile.mkdir()
        (profile / 'library.db').write_text('not a real SQLite database', encoding='utf-8')
        self.assertEqual('INVALID', status(self.session['session'], self.root)['profile'])

    def test_status_rejects_linked_profile(self):
        profile = self.session['document'].parent / 'profile'
        alternate = self.root / 'other'
        alternate.mkdir()
        try:
            profile.symlink_to(alternate, target_is_directory=True)
        except (OSError, NotImplementedError):
            self.skipTest('symlinks not available')
        self.assertEqual('INVALID', status(self.session['session'], self.root)['profile'])

    def test_status_after_explicit_saved_fixture(self):
        self._seed_synthetic_db()
        self._save(BASE_TEXT + fixture(self.root))
        info = status(self.session['session'], self.root)
        self.assertEqual('MATCHES_EXPECTED', info['document'])
        self.assertEqual('READY', info['profile'])
        self.assertEqual([], list(self.session['document'].parent.glob('receipt-*.json')))

    def test_launcher_refuses_unseeded_profile_without_starting(self):
        mocked = Mock()
        with self.assertRaises(ValueError):
            launch_picker(self.session['session'], self.root, run=mocked, platform='nt')
        mocked.assert_not_called()

    def test_launcher_rejects_non_windows(self):
        mocked = Mock()
        with self.assertRaises(OSError):
            launch_picker(self.session['session'], self.root, run=mocked, platform='posix')
        mocked.assert_not_called()

    def test_launcher_uses_exact_isolated_data_directory(self):
        self._seed_synthetic_db()
        project = self.root / 'src' / 'TextExpander.Desktop' / 'TextExpander.Desktop.csproj'
        project.parent.mkdir(parents=True)
        project.write_text('<Project />', encoding='utf-8')
        mocked = Mock(return_value=Mock(returncode=0))
        code = launch_picker(self.session['session'], self.root, run=mocked, platform='nt')
        self.assertEqual(0, code)
        args = mocked.call_args.args[0]
        self.assertEqual(['--', '--data-dir', str(self.session['document'].parent / 'profile')], args[-3:])
        self.assertEqual(self.root.resolve(), mocked.call_args.kwargs['cwd'])
        self.assertNotIn('--seed-picker-profile', args)

    def test_original_document_means_not_run(self):
        result = verify(self.session['session'], self.root)
        self.assertEqual('NOT_RUN', result['status'])

    def test_exact_multiline_unicode_matches_when_saved(self):
        self._save(BASE_TEXT.replace('\n', '\r\n') + fixture(self.root).replace('\n', '\r\n'))
        result = verify(self.session['session'], self.root)
        self.assertEqual('PASS', result['status'])
        report = json.loads(result['receipt'].read_text(encoding='utf-8'))
        self.assertFalse(report['global_expansion_proven'])
        self.assertFalse(report['desktop_action_proven'])
        self.assertNotIn('Caffè', result['receipt'].read_text(encoding='utf-8'))
        self.assertNotIn('👩', result['receipt'].read_text(encoding='utf-8'))

    def test_nfd_is_not_silently_normalized(self):
        self._save(BASE_TEXT + fixture(self.root).replace('cafe\u0301', 'café'))
        self.assertEqual('FAIL', verify(self.session['session'], self.root)['status'])

    def test_wrong_text_is_failure(self):
        self._save(BASE_TEXT + 'non è il contenuto richiesto')
        self.assertEqual('FAIL', verify(self.session['session'], self.root)['status'])

    def test_utf8_decoding_failure_is_not_success(self):
        self.session['document'].write_bytes(b'\xff\xfe\xff')
        self.assertEqual('FAIL', verify(self.session['session'], self.root)['status'])

    def test_missing_document_is_not_run(self):
        self.session['document'].unlink()
        self.assertEqual('NOT_RUN', verify(self.session['session'], self.root)['status'])

    def test_session_reuse_never_overwrites_receipt(self):
        self._save(BASE_TEXT + fixture(self.root))
        one = verify(self.session['session'], self.root)
        two = verify(self.session['session'], self.root)
        self.assertNotEqual(one['receipt'], two['receipt'])
        self.assertEqual('PASS', one['status'])
        self.assertTrue(one['receipt'].exists())

    def test_session_id_rejects_paths_and_other_sessions(self):
        for token in ('../test', 'A' * 32, '1' * 31, 'a' * 33):
            with self.assertRaises(ValueError):
                verify(token, self.root)
        with self.assertRaises(ValueError):
            verify('a' * 32, self.root)

    def test_modified_fixture_fails_closed(self):
        (self.root / 'data' / 'PICKER_INSERT_SYNTHETIC_FIXTURE.txt').write_text('changed', encoding='utf-8')
        with self.assertRaises(ValueError):
            verify(self.session['session'], self.root)

    def test_modified_version_fails_closed(self):
        (self.root / 'VERSION.txt').write_text('Ristretto 3.9.0-alpha.20.99')
        with self.assertRaises(ValueError):
            verify(self.session['session'], self.root)

    def test_inserimento_doppio_riconosciuto_senza_esporre_testo(self):
        self._save(BASE_TEXT + fixture(self.root) * 2)
        result = verify(self.session['session'], self.root)
        self.assertEqual('FAIL', result['status'])
        self.assertEqual('snippet_inserted_twice', result['reason'])
        self.assertEqual(result['reason'], status(self.session['session'], self.root)['reason'])
        receipt = result['receipt'].read_text(encoding='utf-8')
        self.assertNotIn('Caffè', receipt)
        self.assertNotIn('👩', receipt)
        self.assertIn('snippet_inserted_twice', receipt)

    def test_inserimento_nel_punto_sbagliato(self):
        self._save(fixture(self.root) + BASE_TEXT)
        result = verify(self.session['session'], self.root)
        self.assertEqual('snippet_inserted_at_unexpected_position', result['reason'])
        self.assertEqual('CHANGED_DIFFERENT', status(self.session['session'], self.root)['document'])

    def test_normalizzazione_unicode_non_accettata(self):
        self._save(BASE_TEXT + fixture(self.root).replace('cafe\u0301', 'café'))
        result = verify(self.session['session'], self.root)
        self.assertEqual('FAIL', result['status'])
        self.assertEqual('unicode_normalization_changed', result['reason'])

    def test_riga_finale_aggiunta_non_accettata(self):
        self._save(BASE_TEXT + fixture(self.root) + '\n')
        self.assertEqual('extra_trailing_newline', verify(self.session['session'], self.root)['reason'])

    def test_testata_sintetica_modificata(self):
        self._save('CAMBIATO\n' + fixture(self.root))
        self.assertEqual('synthetic_header_changed', verify(self.session['session'], self.root)['reason'])

    def test_snippet_parziale_mancante(self):
        self._save(BASE_TEXT + 'contenuto incompleto')
        self.assertEqual('snippet_missing_or_incomplete', verify(self.session['session'], self.root)['reason'])

    def test_status_verify_condividono_una_classificazione(self):
        expected = fixture(self.root)
        cases = (
            (BASE_TEXT + expected, 'MATCHES_EXPECTED'),
            (BASE_TEXT + expected + '\n', 'CHANGED_DIFFERENT'),
            (BASE_TEXT + 'altro', 'CHANGED_DIFFERENT'),
        )
        for text, expected_state in cases:
            with self.subTest(expected_state=expected_state, suffix=text[-8:]):
                self._save(text)
                info = status(self.session['session'], self.root)
                self.assertEqual(expected_state, info['document'])
                self.assertEqual(info['reason'], verify(self.session['session'], self.root)['reason'])

    def test_preparazione_senza_notepad_disponibile_mantiene_sessione(self):
        mocked = Mock(side_effect=OSError('notepad.exe non disponibile'))
        new_session = prepare(self.root, open_editor=True, platform='nt', opener=mocked)
        self.assertEqual('LAUNCH_FAILED', new_session['editor_state'])
        self.assertTrue(new_session['document'].is_file())
        self.assertTrue((new_session['document'].parent / 'session.json').is_file())
        self.assertEqual('NOT_CREATED', status(new_session['session'], self.root)['profile'])
        mocked.assert_called_once()
        self.assertEqual('notepad.exe', mocked.call_args.args[0][0])
        self.assertEqual(str(new_session['document']), mocked.call_args.args[0][1])

    def test_preparazione_fuori_windows_mantiene_sessione_senza_avvio(self):
        mocked = Mock()
        new_session = prepare(self.root, open_editor=True, platform='posix', opener=mocked)
        self.assertEqual('WINDOWS_REQUIRED', new_session['editor_state'])
        self.assertTrue(new_session['document'].exists())
        mocked.assert_not_called()

    def test_preparazione_windows_chiede_apertura_del_solo_file_sintetico(self):
        mocked = Mock(return_value=Mock())
        new_session = prepare(self.root, open_editor=True, platform='nt', opener=mocked)
        self.assertEqual('LAUNCH_REQUESTED', new_session['editor_state'])
        mocked.assert_called_once_with(['notepad.exe', str(new_session['document'])], close_fds=True)

    def test_esiti_tradotti_senza_testo_sensibile(self):
        self.assertIn('Ctrl+S', REASONS_IT['file_unchanged_no_saved_insertion'])
        self.assertIn('Unicode', REASONS_IT['unicode_normalization_changed'])
        for key, message in REASONS_IT.items():
            self.assertTrue(message.strip(), key)
            self.assertNotIn('Caffè', message)

    def test_classificatore_senza_file_non_dichiara_pass(self):
        result = classify_saved_document(None, 'a' * 64, fixture(self.root))
        self.assertEqual(('NOT_RUN', 'synthetic_document_unavailable', 'MISSING'), result)

    def test_normalize_line_endings_only(self):
        self.assertEqual('a\nb', normalize_newlines('a\r\nb'))
        self.assertNotEqual(normalize_newlines('cafe\u0301'), normalize_newlines('café'))
        self.assertNotEqual(normalize_newlines('a\r'), normalize_newlines('a\n'))


if __name__ == '__main__':
    unittest.main()
