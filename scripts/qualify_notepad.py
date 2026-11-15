#!/usr/bin/env python3
"""R2.05: opt-in, file-backed acceptance test for the real Windows Notepad.

This helper never sends input, reads clipboard, or opens the user's documents.
It creates one uniquely named synthetic file, optionally opens it in Notepad,
and compares the file *after the user explicitly saves it* with the expected
snippet. The result attests only persisted bytes, not which UI action created
those bytes or when they were pasted. No text is included in receipts.
"""
from __future__ import annotations

import argparse
from contextlib import closing
import hashlib
import json
import os
import re
import sqlite3
import subprocess
import unicodedata
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SCHEMA = 'ristretto/notepad-qualification-session/1'
RESULT_SCHEMA = 'ristretto/notepad-qualification-result/1'
BASENAME = 'notepad-synthetic.txt'
BASE_TEXT = 'RISTRETTO NOTEPAD TEST\n'
TOKEN = re.compile(r'^[0-9a-f]{32}$')


def digest(value: bytes) -> str:
    return hashlib.sha256(value).hexdigest()


def normalize_newlines(value: str) -> str:
    # The platform is allowed to convert line endings, not Unicode normalization,
    # white space, tabs, emoji, or the embedded text itself.
    return value.replace('\r\n', '\n')


def fixture(root: Path) -> str:
    raw = (root / 'data' / 'PICKER_INSERT_SYNTHETIC_FIXTURE.txt').read_bytes()
    result = raw.decode('utf-8-sig')
    if not result or '\x00' in result:
        raise ValueError('fixture missing or invalid')
    return result


def session_base(root: Path) -> Path:
    root = root.resolve()
    candidate = root / '.local-cache' / 'notepad-qualification'
    # Do not follow a pre-existing symlink/junction to outside the project.
    if not candidate.resolve().is_relative_to(root):
        raise ValueError('session storage points outside Ristretto')
    return candidate


def prepare(root: Path = ROOT, *, open_editor: bool = False,
            platform: str = os.name, opener=subprocess.Popen) -> dict:
    root = root.resolve()
    snippet = fixture(root)
    version = (root / 'VERSION.txt').read_text(encoding='utf-8').strip()
    base = session_base(root)
    base.mkdir(parents=True, exist_ok=True)
    token = uuid.uuid4().hex
    folder = base / token
    folder.mkdir(mode=0o700)
    document = folder / BASENAME
    document_bytes = b'\xef\xbb\xbf' + BASE_TEXT.encode('utf-8')
    with document.open('xb') as output:
        output.write(document_bytes)
    metadata = {
        'schema': SCHEMA, 'session': token, 'version': version,
        'baseline_sha256': digest(document_bytes),
        'fixture_sha256': digest(snippet.encode('utf-8')),
        'document_name': BASENAME,
    }
    with (folder / 'session.json').open('x', encoding='utf-8') as output:
        json.dump(metadata, output, ensure_ascii=False, indent=2)
        output.write('\n')
    # Preparare una sessione non deve mai perdere il token soltanto perché
    # Notepad non è disponibile o l'avvio viene rifiutato da Windows.
    editor_state = 'NOT_REQUESTED'
    if open_editor:
        if platform != 'nt':
            editor_state = 'WINDOWS_REQUIRED'
        else:
            try:
                opener(['notepad.exe', str(document)], close_fds=True)
                editor_state = 'LAUNCH_REQUESTED'
            except OSError:
                editor_state = 'LAUNCH_FAILED'
    return {'session': token, 'document': document,
            'fixture': root / 'data' / 'PICKER_INSERT_SYNTHETIC_FIXTURE.txt',
            'editor_state': editor_state}


def session_details(token: str, root: Path = ROOT) -> tuple[Path, dict, str, Path]:
    """Validate an existing synthetic session without touching any personal profile."""
    if TOKEN.fullmatch(token) is None:
        raise ValueError('Invalid session identifier')
    root = root.resolve()
    base = session_base(root)
    for part in (root / '.local-cache', base):
        if part.is_symlink():
            raise ValueError('Unsafe linked synthetic session storage')
    folder = base / token
    if not folder.is_dir() or folder.is_symlink():
        raise ValueError('Unknown session')
    metadata_path = folder / 'session.json'
    if metadata_path.is_symlink():
        raise ValueError('Unsafe session metadata')
    metadata = json.loads(metadata_path.read_text(encoding='utf-8'))
    version = (root / 'VERSION.txt').read_text(encoding='utf-8').strip()
    expected_fixture = fixture(root)
    if not (metadata.get('schema') == SCHEMA and metadata.get('session') == token and
            metadata.get('version') == version and metadata.get('document_name') == BASENAME and
            metadata.get('fixture_sha256') == digest(expected_fixture.encode('utf-8'))):
        raise ValueError('Session metadata/version/fixture mismatch')
    doc = folder / BASENAME
    if doc.is_symlink():
        raise ValueError('Unsafe synthetic document')
    return folder, metadata, expected_fixture, doc


def synthetic_profile_status(folder: Path, expected_fixture: str) -> str:
    """Check the isolated SQLite fixture read-only, never output stored content."""
    profile = folder / 'profile'
    if not profile.exists():
        return 'NOT_CREATED'
    if not profile.is_dir() or profile.is_symlink():
        return 'INVALID'
    db = profile / 'library.db'
    if not db.is_file() or db.is_symlink():
        return 'INVALID'
    try:
        # SQLite opens read-only (including any WAL), without creating a database.
        with closing(sqlite3.connect(db.resolve().as_uri() + '?mode=ro', uri=True, timeout=1)) as conn:
            rows = conn.execute('''SELECT s.content FROM snippets s JOIN groups g ON g.id=s.group_id
                WHERE s.trigger=? AND s.enabled=1 AND g.enabled=1''', (';ristretto-test',)).fetchall()
            count = conn.execute('SELECT COUNT(*) FROM snippets').fetchone()[0]
            if count == 1 and len(rows) == 1 and rows[0][0] == expected_fixture:
                return 'READY'
    except (sqlite3.Error, OSError, ValueError):
        return 'INVALID'
    return 'INVALID'


# Esiti fissi e senza dati di contenuto. Non serializzare mai il testo.
REASONS_IT = {
    'synthetic_document_unavailable': 'Il documento sintetico non esiste più: prepara una nuova sessione.',
    'file_unchanged_no_saved_insertion': 'Il file non è cambiato: usa Inserisci/Invio in Notepad e salva con Ctrl+S.',
    'saved_document_not_utf8': 'Il file salvato non è un testo UTF-8 valido.',
    'saved_synthetic_document_matches_unicode_fixture': 'Il file salvato corrisponde esattamente alla prova Unicode.',
    'snippet_inserted_twice': 'Lo snippet risulta presente due volte nel documento.',
    'snippet_inserted_at_unexpected_position': 'Il testo è presente, ma non nel punto richiesto (fine documento).',
    'unicode_normalization_changed': 'Il testo è simile ma è cambiata la normalizzazione Unicode (NFC/NFD).',
    'extra_trailing_newline': 'È stata aggiunta una riga vuota in fondo al documento.',
    'synthetic_header_changed': 'La riga iniziale del documento di prova è stata modificata.',
    'snippet_missing_or_incomplete': 'Il file è cambiato, ma lo snippet risulta mancante o incompleto.',
    'saved_document_differs_from_fixture': 'Il contenuto salvato non coincide con quello atteso.',
}


def classify_saved_document(raw: bytes | None, baseline_sha256: str,
                            expected_fixture: str) -> tuple[str, str, str]:
    """Restituisce (PASS/FAIL/NOT_RUN, motivo fisso, stato sintetico).

    Confronta solo il documento artificiale della sessione; mai testo utente.
    Non modifica file, normalizzazione Unicode né spazi bianchi.
    """
    if raw is None:
        return 'NOT_RUN', 'synthetic_document_unavailable', 'MISSING'
    if digest(raw) == baseline_sha256:
        return 'NOT_RUN', 'file_unchanged_no_saved_insertion', 'UNCHANGED'
    try:
        saved = normalize_newlines(raw.decode('utf-8-sig'))
    except UnicodeDecodeError:
        return 'FAIL', 'saved_document_not_utf8', 'CHANGED_INVALID'
    expected = normalize_newlines(BASE_TEXT + expected_fixture)
    fixture_text = normalize_newlines(expected_fixture)
    prefix = normalize_newlines(BASE_TEXT)
    if saved == expected:
        return 'PASS', 'saved_synthetic_document_matches_unicode_fixture', 'MATCHES_EXPECTED'
    if saved == prefix + fixture_text + fixture_text:
        reason = 'snippet_inserted_twice'
    elif saved == expected + '\n':
        reason = 'extra_trailing_newline'
    elif unicodedata.normalize('NFC', saved) == unicodedata.normalize('NFC', expected):
        reason = 'unicode_normalization_changed'
    elif not saved.startswith(prefix) and saved.endswith(fixture_text):
        # Intestazione modificata ma contenuto completo al fondo.
        reason = 'synthetic_header_changed'
    elif fixture_text and fixture_text in saved:
        reason = 'snippet_inserted_at_unexpected_position'
    elif not saved.startswith(prefix):
        reason = 'synthetic_header_changed'
    elif saved.startswith(prefix):
        reason = 'snippet_missing_or_incomplete'
    else:
        reason = 'saved_document_differs_from_fixture'
    return 'FAIL', reason, 'CHANGED_DIFFERENT'


def status(token: str, root: Path = ROOT) -> dict:
    """Diagnostica read-only, senza ricevute o avvio di processi."""
    folder, metadata, expected_fixture, doc = session_details(token, root)
    profile_state = synthetic_profile_status(folder, expected_fixture)
    raw = doc.read_bytes() if doc.is_file() else None
    _, reason, file_state = classify_saved_document(raw, metadata['baseline_sha256'], expected_fixture)
    return {'session': token, 'profile': profile_state, 'document': file_state,
            'reason': reason, 'desktop_input_observed': False,
            'profile_path': folder / 'profile', 'document_path': doc}


def launch_picker(token: str, root: Path = ROOT, *, run=subprocess.run,
                  platform: str = os.name) -> int:
    """Opt-in desktop launcher for the single validated synthetic data directory."""
    if platform != 'nt':
        raise OSError('Ristretto desktop qualification launcher requires Windows')
    details = status(token, root)
    if details['profile'] != 'READY' or details['document'] == 'MISSING':
        raise ValueError('Synthetic session/profile is not ready. Run seed-picker-notepad.cmd TOKEN.')
    project = root.resolve() / 'src' / 'TextExpander.Desktop' / 'TextExpander.Desktop.csproj'
    if not project.is_file() or project.is_symlink():
        raise ValueError('Ristretto desktop project not found')
    args = ['dotnet', 'run', '-c', 'Release', '--project', str(project),
            '--', '--data-dir', str(details['profile_path'])]
    print('Avvio Ristretto con SOLO il profilo sintetico:', details['profile_path'], flush=True)
    print('Lancio desktop manuale: il comando non esegue alcun incolla.', flush=True)
    result = run(args, cwd=root.resolve(), check=False)
    return result.returncode


def verify(token: str, root: Path = ROOT) -> dict:
    root = root.resolve()
    folder, metadata, expected_fixture, doc = session_details(token, root)
    version = (root / 'VERSION.txt').read_text(encoding='utf-8').strip()
    document_bytes = doc.read_bytes() if doc.is_file() else None
    result_status, reason, _ = classify_saved_document(
        document_bytes, metadata['baseline_sha256'], expected_fixture)
    receipt = {
        'schema': RESULT_SCHEMA, 'version': version, 'session': token,
        'timestamp_utc': datetime.now(timezone.utc).isoformat(timespec='seconds'),
        'status': result_status, 'reason': reason,
        'evidence': 'saved_synthetic_file_only',
        'desktop_action_proven': False,
        'global_expansion_proven': False,
        'editor_version_verified': False,
        'contains_snippet_text': False,
        'contains_clipboard_text': False,
        'desktop_automation_used': False,
    }
    # Le ricevute sono immutabili: una nuova prova non sovrascrive lo storico.
    path = folder / ('receipt-' + uuid.uuid4().hex + '.json')
    with path.open('x', encoding='utf-8') as output:
        json.dump(receipt, output, ensure_ascii=False, indent=2)
        output.write('\n')
    return {'status': result_status, 'reason': reason, 'receipt': path}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description='Ristretto R2.05 — prova guidata in Blocco note, solo con file sintetici')
    subs = parser.add_subparsers(dest='command', required=True)
    prep = subs.add_parser('prepare', help='Prepara una sessione e un documento di prova isolati')
    prep.add_argument('--open', action='store_true', help='Prova ad aprire il documento in Blocco note')
    state = subs.add_parser('status', help='Mostra cosa manca, senza modificare la sessione')
    state.add_argument('--session', required=True, help='Identificativo stampato da prepare')
    launch = subs.add_parser('launch', help='Avvia Ristretto con il solo profilo di prova (Windows)')
    launch.add_argument('--session', required=True, help='Identificativo della sessione')
    check = subs.add_parser('verify', help='Confronta il documento salvato e genera una ricevuta')
    check.add_argument('--session', required=True, help='Identificativo della sessione')
    args = parser.parse_args(argv)
    try:
        if args.command == 'prepare':
            session = prepare(open_editor=args.open)
            token = session['session']
            print('Sessione di prova:', token)
            print('Documento sintetico:', session['document'])
            print('Contenuto dello snippet:', session['fixture'])
            if args.open:
                message = {
                    'LAUNCH_REQUESTED': 'Apertura richiesta: controlla che Blocco note sia visibile.',
                    'LAUNCH_FAILED': 'ATTENZIONE: apertura di Blocco note non riuscita; la sessione è comunque pronta.',
                    'WINDOWS_REQUIRED': 'ATTENZIONE: apertura di Blocco note disponibile soltanto su Windows.'
                }
                print(message[session['editor_state']])
            print('PASSO 1 — Prepara il database SQLite ISOLATO, senza toccare la tua libreria:')
            print('  scripts\\seed-picker-notepad.cmd ' + token)
            print('PASSO 2 — Chiudi le altre istanze di Ristretto, poi avvia il profilo di prova:')
            print('  scripts\\validate-picker-notepad.cmd launch --session ' + token)
            print('PASSO 3 — In Blocco note apri il documento sintetico; vai in fondo con Ctrl+End.')
            print('PASSO 4 — Richiama il picker, cerca ;ristretto-test e premi Invio o Inserisci (non Copia).')
            print('PASSO 5 — Salva il documento sintetico da Blocco note con Ctrl+S.')
            print('PASSO 6 — Controlla lo stato e genera la ricevuta:')
            print('  scripts\\validate-picker-notepad.cmd status --session ' + token)
            print('  scripts\\validate-picker-notepad.cmd verify --session ' + token)
            print('IMPORTANTE: questo strumento non invia tasti. File invariato = NOT_RUN, non FAIL.')
            return 0
        if args.command == 'status':
            info = status(args.session)
            print('Sessione:', info['session'])
            print('Profilo SQLite sintetico:', info['profile'])
            print('Documento sintetico salvato:', info['document'])
            print('Spiegazione:', REASONS_IT.get(info['reason'], 'Controlla il documento sintetico.'))
            print('Non è stato osservato né registrato alcun tasto digitato.')
            if info['profile'] == 'NOT_CREATED':
                print('PROSSIMO PASSO — Genera il profilo isolato: scripts\\seed-picker-notepad.cmd ' + info['session'])
            elif info['profile'] != 'READY':
                print('PROSSIMO PASSO — Profilo non valido; crea una NUOVA sessione, non sovrascrivere il database.')
            elif info['document'] == 'UNCHANGED':
                print('PROSSIMO PASSO — Apri Ristretto col profilo sintetico usando:')
                print('  scripts\\validate-picker-notepad.cmd launch --session ' + info['session'])
                print('In Blocco note usa Invio/Inserisci, salva con Ctrl+S, quindi esegui verify.')
            elif info['document'] == 'MATCHES_EXPECTED':
                print('PROSSIMO PASSO — Esegui verify per creare una ricevuta PASS.')
            else:
                print('PROSSIMO PASSO — Controlla il file sintetico e ripeti manualmente la prova;')
                print('non usare documenti personali e non ripetere l’inserimento su file già alterati.')
            return 0
        if args.command == 'launch':
            return launch_picker(args.session)
        result = verify(args.session)
        print('Esito:', result['status'], '| codice:', result['reason'])
        print('Spiegazione:', REASONS_IT.get(result['reason'], 'Verifica il documento sintetico.'))
        print('Ricevuta senza contenuto digitato:', result['receipt'])
        print('PASS dimostra solo il file salvato, non la provenienza dell’inserimento o l’espansione globale.')
        return {'PASS': 0, 'FAIL': 1, 'NOT_RUN': 3}[result['status']]
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        print('PROVA NON COMPLETATA (' + type(exc).__name__ + '): ' + str(exc), file=sys.stderr)
        return 3


if __name__ == '__main__':
    sys.exit(main())
