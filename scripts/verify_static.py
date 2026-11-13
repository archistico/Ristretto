#!/usr/bin/env python3
"""Controllo strutturale offline. NON compila C# e NON esegue xUnit."""
import re
import json
import sqlite3
import sys
from pathlib import Path
from urllib.parse import unquote
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
errors = []

for folder in ('archive', 'beeftext-screenshots'):
    if any(p.is_dir() and p.name == folder for p in root.rglob('*')):
        errors.append(f'Directory esclusa presente: {folder}')

for file in root.rglob('*'):
    if file.suffix in ('.resx', '.axaml', '.csproj', '.props'):
        try: ET.parse(file)
        except Exception as exc: errors.append(f'XML: {file.relative_to(root)}: {exc}')

loc = root/'src/TextExpander.Desktop/Localization'
sets = []
for file in sorted(loc.glob('Strings*.resx')):
    keys = [x.attrib['name'] for x in ET.parse(file).getroot().findall('data')]
    if len(keys) != len(set(keys)): errors.append(f'Duplicate translation keys in {file.name}')
    sets.append((file.name, set(keys)))
for name, keys in sets[1:]:
    if keys != sets[0][1]:
        errors.append(f'Incomplete translation {name}: {sorted(keys ^ sets[0][1])}')

sln = (root/'Ristretto.sln').read_text('utf-8')
for project in ('Core', 'Application', 'Infrastructure', 'Desktop'):
    if f'TextExpander.{project}' not in sln: errors.append(f'Missing project {project}')
if 'TextExpander.Tests' not in sln: errors.append('Missing tests project')

# Verifica DB SQLite senza .NET: NON verifica Microsoft.Data.Sqlite né il backup.
src = (root/'src/TextExpander.Infrastructure/Storage/SqliteLibraryStore.cs').read_text('utf-8')
m = re.search(r'using \(var schema = Cmd\(db, @"(.*?)", tx\)\)', src, re.S)
if not m: errors.append('DDL SQLite not found')
else:
    try:
        con = sqlite3.connect(':memory:')
        con.execute('PRAGMA foreign_keys=ON')
        con.executescript(m.group(1))
        group = '6d938a52-623f-423e-8b98-f312048c53df'
        con.execute('INSERT INTO groups(id,name,enabled) VALUES(?,?,1)', (group,'Gruppo 🌍'))
        con.execute('INSERT INTO snippets(id,group_id,title,trigger,content,description,enabled,revision) VALUES(?,?,?,?,?,?,1,1)',
            ('84c18c1d-87a4-4f96-8e2c-797f19999a63', group,'Café',';é','مرحبا 🧑🏽‍💻',''))
        assert con.execute('SELECT content FROM snippets').fetchone()[0] == 'مرحبا 🧑🏽‍💻'
        try:
            con.execute('INSERT INTO snippets(id,group_id,title,trigger,content,description,enabled,revision) VALUES(?,?,?,?,?,?,1,1)',
                ('7c18c1d1-87a4-4f96-8e2c-797f19999a63', 'missing','Invalid',';i','Text',''))
            errors.append('SQLite foreign key not enforced')
        except sqlite3.IntegrityError:
            pass
    except Exception as exc: errors.append(f'SQLite schema error: {exc}')
    finally: con.close()

# Controllo statico: ogni chiave caricata dal viewmodel deve essere presente in tutte le lingue.
strings_code = (loc/'UiStrings.cs').read_text('utf-8')
required_keys = set(re.findall(r'Get\("([^"]+)"\)', strings_code))
for name, keys in sets:
    missing = required_keys - keys
    if missing: errors.append(f'Localization {name} missing keys: {sorted(missing)}')

# Event handlers XAML -> code behind: non è una compilazione ma intercetta errori di battitura.
for view in ('MainWindow', 'PickerWindow'):
    xaml = (root/f'src/TextExpander.Desktop/{view}.axaml').read_text('utf-8')
    code = (root/f'src/TextExpander.Desktop/{view}.axaml.cs').read_text('utf-8')
    for handler in re.findall(r'(?:Click|SelectionChanged)="([A-Za-z_]\w*)"', xaml):
        if not re.search(r'\b' + re.escape(handler) + r'\s*\(', code):
            errors.append(f'{view}: missing handler {handler}')

# Alpha 20.3: il bottone Inserisci deve avere un handler reale, un percorso Windows
# separato e non deve essere scambiato per espansione automatica dei trigger.
picker_xaml = (root/'src/TextExpander.Desktop/PickerWindow.axaml').read_text('utf-8')
picker_code = (root/'src/TextExpander.Desktop/PickerWindow.axaml.cs').read_text('utf-8')
app_code = (root/'src/TextExpander.Desktop/App.axaml.cs').read_text('utf-8')
paste_code = root/'src/TextExpander.Infrastructure/Platform/Windows/WindowsPickerPasteService.cs'
if not all((
    'Click="Insert_Click"' in picker_xaml,
    'IsEnabled="{Binding CanInsert}"' in picker_xaml,
    'vm.InsertSelectedAsync(app.InsertFromPickerAsync)' in picker_code,
    'vm.InsertFirstAsync(app.InsertFromPickerAsync)' in picker_code,
    'AddHandler(KeyUpEvent, OnPickerEnterKeyUp, RoutingStrategies.Tunnel)' in picker_code,
    'vm.CopySelectedAsync(' in picker_code,
    'IsEnabled="{Binding CanCopy}"' in picker_xaml,
    'CaptureExternalForeground()' in app_code,
    'request.ExecuteAsync(' in app_code,
    'CancelPickerInsertion()' in app_code,
    'CancelPickerInsertion()' in picker_code,
    (root/'src/TextExpander.Infrastructure/Platform/Windows/WindowsPickerInsertRequest.cs').is_file(),
    paste_code.is_file(),
    (root/'tests/TextExpander.Tests/WindowsPickerPasteTests.cs').is_file(),
)):
    errors.append('Alpha 20.3: wiring Picker Inserisci mancante o incompleto')

# Il blocco JSON della specifica deve essere un documento valido.
json_doc = (root/'docs/JSON_PORTABILITY.md').read_text('utf-8')
example = re.search(r'```json\s*(.*?)```', json_doc, re.S)
if example is None:
    errors.append('Portable JSON documentation is missing its example')
else:
    try:
        parsed = json.loads(example.group(1))
        if parsed.get('format') != 'text-expander-studio/snippets' or parsed.get('version') != 1:
            errors.append('Unexpected JSON portability example version')
        groups = {g['id'] for g in parsed['groups']}
        if any(s['groupId'] not in groups for s in parsed['snippets']):
            errors.append('Portable JSON fixture has broken group references')
    except (ValueError, KeyError, TypeError) as exc:
        errors.append(f'Portable JSON example is invalid: {exc}')

# Valida riferimenti di file nei documenti Markdown.
count_links = 0
for file in root.rglob('*.md'):
    source = file.read_text('utf-8')
    for target in re.findall(r'\]\(([^)]+)\)', source):
        if target.startswith(('http:', 'https:', 'mailto:', '#', 'data:')): continue
        local = unquote(target.split('#', 1)[0].split(' ', 1)[0])
        if not local: continue
        count_links += 1
        resolved = (file.parent/local).resolve()
        if not resolved.exists(): errors.append(f'Missing link {file.relative_to(root)} -> {target}')

# In assenza di compilatore, almeno file e nomi dei controlli necessari devono esistere.
for name in ('App.axaml', 'App.axaml.cs', 'MainWindow.axaml', 'MainWindow.axaml.cs',
             'PickerWindow.axaml', 'PickerWindow.axaml.cs'):
    if not (root/'src/TextExpander.Desktop'/name).is_file(): errors.append(f'Missing UI file {name}')
picker = (root/'src/TextExpander.Desktop/PickerWindow.axaml').read_text('utf-8')
for name in ('SearchBox', 'ResultsList'):
    if f'Name="{name}"' not in picker: errors.append(f'Missing picker control {name}')
if not (root/'src/TextExpander.Desktop/Assets/app-icon.ico').is_file(): errors.append('Missing tray icon')

# Contratti v3.3: controllo solo strutturale, non sostituisce i test di comportamento.
review = root/'src/TextExpander.Desktop/ImportReviewWindow.cs'
if not review.is_file(): errors.append('Missing import review GUI')
else:
    r = review.read_text('utf-8')
    for pattern in ('PageSize = 60', 'SuggestedForImport', 'VisiblePage()', 'ShowDialog'):
        if pattern == 'ShowDialog': continue  # la chiamata e nel MainWindow
        if pattern not in r: errors.append(f'Import review missing {pattern}')
backend = (root/'src/TextExpander.Application/Features/Portability/ExternalLibraryImport.cs').read_text('utf-8')
for pattern in ('ImportItemRisk', 'LibraryRevision = snapshot.Revision',
                'selected.IsSubsetOf(available)', 'ImportPortableAsync(doc, ct, preview.LibraryRevision)'):
    if pattern not in backend: errors.append(f'Import service missing {pattern}')
storage = (root/'src/TextExpander.Infrastructure/Storage/SqliteLibraryStore.cs').read_text('utf-8')
for pattern in ('long? expectedLibraryRevision = null', 'revision.ExecuteScalar()) != expectedLibraryRevision.Value'):
    if pattern not in storage: errors.append(f'SQLite revision guard missing {pattern}')
if not (root/'tests/TextExpander.Tests/ImportReviewTests.cs').is_file():
    errors.append('Missing import review tests')


# v3.4 — verifiche puramente strutturali del backend Windows inattivo.
output_plan = root/'src/TextExpander.Application/Features/Output/UnicodeOutputPlan.cs'
windows_input = root/'src/TextExpander.Infrastructure/Platform/Windows/WindowsUnicodeTextOutput.cs'
output_tests = root/'tests/TextExpander.Tests/WindowsOutputContractTests.cs'
for file in (output_plan, windows_input, output_tests):
    if not file.is_file(): errors.append(f'Missing Windows output spike: {file.relative_to(root)}')
if output_plan.is_file() and windows_input.is_file():
    model = output_plan.read_text('utf-8')
    adapter = windows_input.read_text('utf-8')
    for marker in ('char.IsHighSurrogate', 'char.IsLowSurrogate', 'char.IsControl', 'SubmittedUnverified'):
        if marker not in model: errors.append(f'Unicode output plan missing {marker}')
    for marker in ('KeyEventUnicode | KeyEventKeyUp', 'IsAnyModifierPressed()', 'ForegroundWindow()', 'SendInput('):
        if marker not in adapter: errors.append(f'Windows backend missing {marker}')
    app_ui = (root/'src/TextExpander.Desktop/App.axaml.cs').read_text('utf-8')
    if 'WindowsUnicodeTextOutput' in app_ui:
        errors.append('Spike Windows should not be wired into the UI before runtime qualification')


# Regression guards from Windows v3.4 build report. These are static checks, not compilation.
main_xaml = (root/'src/TextExpander.Desktop/MainWindow.axaml').read_text('utf-8')
# G4 first pass: the content editor must remain editable, Unicode-preserving and
# multiline, but its height is flexible rather than the v3.7 fixed 165 pixels.
main_tree = ET.parse(root/'src/TextExpander.Desktop/MainWindow.axaml')
editors = [element for element in main_tree.iter()
           if element.tag.endswith('TextBox') and 'EditorContent' in element.attrib.get('Text', '')]
if len(editors) != 1 or editors[0].get('AcceptsReturn') != 'True' or editors[0].get('TextWrapping') != 'Wrap' or not (editors[0].get('MinHeight') or editors[0].get('Height')):
    errors.append('Multiline content editor must support wrapping and flexible height')
if re.search(r'<TextBox[^>]*VerticalScrollBarVisibility=', main_xaml):
    errors.append('TextBox uses unresolved unqualified scrollbar property')
review_source = (root/'src/TextExpander.Desktop/ImportReviewWindow.cs').read_text('utf-8')
if 'checkbox.IsCheckedChanged +=' not in review_source or 'checkbox.Checked +=' in review_source or 'checkbox.Unchecked +=' in review_source:
    errors.append('Import review checkbox still uses deprecated events')
product_project = ET.parse(root/'src/TextExpander.Desktop/TextExpander.Desktop.csproj').getroot()
if product_project.find('.//AssemblyName') is None or product_project.find('.//AssemblyName').text != 'Ristretto':
    errors.append('Desktop output must be Ristretto.exe')
if 'avares://Ristretto/Assets/app-icon.ico' not in (root/'src/TextExpander.Desktop/App.axaml.cs').read_text('utf-8'):
    errors.append('Icon URI must follow the Ristretto assembly name')
if 'Ristretto.sln' not in str((root/'Ristretto.sln').name):
    errors.append('Ristretto solution missing')

# v3.6 — guard statici per il warning loader e i dati di test del UTF-16.
picker_code = (root/'src/TextExpander.Desktop/PickerWindow.axaml.cs').read_text('utf-8')
if 'public PickerWindow()' not in picker_code or 'public PickerWindow(PickerViewModel viewModel)' not in picker_code:
    errors.append('PickerWindow must expose both public constructors')
output_test_source = (root/'tests/TextExpander.Tests/WindowsOutputContractTests.cs').read_text('utf-8')
if '[InlineData("\\uD83D")]' in output_test_source or '[InlineData("\\uDC4D")]' in output_test_source:
    errors.append('Lone surrogates must not be passed through InlineData(string)')
if 'new string(codeUnit, 1)' not in output_test_source:
    errors.append('Missing runtime construction for lone UTF-16 surrogates')
for unique_name in ('UnpairedHighSurrogateIsRejected', 'UnpairedLowSurrogateIsRejected'):
    if f'void {unique_name}()' not in output_test_source:
        errors.append(f'Missing unique surrogate regression test: {unique_name}')

if errors:
    print('STATIC CHECK FAILED:')
    for error in errors[:60]: print(' -', error)
    sys.exit(1)
print(f'STATIC CHECK OK — {len(sets)} localizations, {count_links} local Markdown links, XML and SQLite DDL.')
print('NOTE: C# not compiled, xUnit not executed, GUI not launched.')
