#!/usr/bin/env python3
"""Static neutral UI contract; does NOT compile or render Avalonia."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent.parent
ui = root / 'src/TextExpander.Desktop'
errors = []
paths = [ui / f for f in ('App.axaml','MainWindow.axaml','PickerWindow.axaml')]
for p in paths:
    try:
        ET.parse(p)
    except ET.ParseError as exc:
        errors.append(f'{p.name}: XML invalido: {exc}')
app, main, picker = (p.read_text(encoding='utf-8') for p in paths)
for key in ('RCanvas','RSidebar','RSurface','RInput','REditorInset','RSubtle','RHover',
            'RSelection','RSelectionMarker','RBorder','RSoftBorder','RText','RMuted',
            'RFaint','RPrimary','RPrimaryHover','RPrimaryPressed','RPrimaryForeground',
            'RFocus','RDanger','RWarning','RSuccess'):
    if app.count(f'x:Key="{key}"') != 2:
        errors.append(f'{key}: attese risorse Light/Dark')
for key in ('Button','ListBoxItem'):
    if f'x:Key="{{x:Type {key}}}" TargetType="{key}"' not in app:
        errors.append(f'{key}: tema del controllo autonomo assente')
for token in ('Button.primary:pointerover','Button.primary:pressed','Button.subtle:pointerover',
              'Button.ghost:pointerover','Button:focus-visible','Button:disabled',
              'ListBoxItem:selected','ListBoxItem:selected:pointerover','TextBox:focus'):
    if f'Selector="{token}"' not in app:
        errors.append(f'Stato controllo non definito: {token}')
if 'Style Selector="TextBlock"' in app:
    errors.append('Global TextBlock.Foreground impedirebbe ereditarieta foreground dei pulsanti')
for name in ('PART_ContentPresenter','TemplateBinding Background','TemplateBinding BorderBrush'):
    if app.count(name) < 2:
        errors.append('I template non seguono le risorse dei controlli: '+name)
if main.count('<GridSplitter') != 1:
    errors.append('Manca un unico divisore navigazione/editor')
for name in ('GroupsList','SnippetsList'):
    if f'Name="{name}"' not in main:
        errors.append(f'Selezione {name} persa')
for handler in ('Picker_Click','NewSnippet_Click','AddGroup_Click','RenameGroup_Click',
                'ToggleGroup_Click','DeleteGroup_Click','Import_Click','Export_Click',
                'Restore_Click','Delete_Click'):
    if f'Click="{handler}"' not in main:
        errors.append(f'Handler UI mancante: {handler}')
cs=(ui/'MainWindow.axaml.cs').read_text(encoding='utf-8')
for handler in re.findall(r'Click="([A-Za-z_][A-Za-z_0-9]*)"',main):
    if f'{handler}(' not in cs:
        errors.append(f'Handler non implementato: {handler}')
if 'SizeToContent="Manual"' not in picker or 'CanResize="True"' not in picker:
    errors.append('Picker deve essere ridimensionabile')
# Picker Preview 8 reserves three compact (~40 DIP) rows within a smaller
# viewport; the first result and manual-copy controls remain always visible.
main_root = ET.fromstring(main)
picker_root = ET.fromstring(picker)
def list_min_height(tree, target):
    return next((int(el.get('MinHeight')) for el in tree.iter()
                 if el.get('Name') == target and el.get('MinHeight')), 0)
if int(picker_root.get('MinHeight', '0')) < 360 or list_min_height(picker_root, 'ResultsList') < 120:
    errors.append('Picker: tre risultati completi devono avere viewport minimo')
if int(picker_root.get('Width', '9999')) > 620 or int(picker_root.get('Height', '9999')) > 420:
    errors.append('Picker: dimensione iniziale non compatta')
if int(main_root.get('Width', '9999')) > 1020 or int(main_root.get('Height', '9999')) > 665:
    errors.append('Editor: dimensione iniziale eccessiva')
if main_root.get('Classes') != 'compact' or picker_root.get('Classes') != 'compact':
    errors.append('Le finestre principali devono applicare la densita compatta')
for selector in ('Window.compact Button', 'Window.compact Button.icon',
                 'Window.compact TextBox', 'Window.compact TextBox.editor-field',
                 'Window.compact ListBoxItem', 'Window.compact TextBlock.section'):
    if f'Selector="{selector}"' not in app:
        errors.append(f'Densita ottica mancante: {selector}')
# Preview 9 keeps the compact geometry and owns the Fluent watermark states.
for token in ('TextBox /template/ TextBlock#PART_Watermark',
              'TextBox:disabled /template/ Border#PART_BorderElement',
              'Window.compact TextBox:focus', 'Border.trigger-chip'):
    if token not in app:
        errors.append('Preview 9: stato condiviso mancante: '+token)
if 'Classes="trigger-chip"' not in main:
    errors.append('Preview 9: trigger separato dal nome snippet assente')
if 'ColumnDefinitions="*,245"' in picker or 'Grid.Row="3" Background="{DynamicResource RSidebar}"' not in picker:
    errors.append('Picker: anteprima sotto risultati non verificata')
# Preview 8: reject regression to oversized editor and disappearing actions.
content_editors = [e for e in main_root.iter() if e.get('Name') == 'ContentEditor']
if len(content_editors) != 1 or not 185 <= int(content_editors[0].get('Height', '0')) <= 250:
    errors.append('Editor Preview 8: contenuto deve essere un canvas moderato e scrollabile')
if 'ScrollViewer Grid.Row="1" VerticalScrollBarVisibility="Auto"' not in main:
    errors.append('Editor Preview 8: mancano lo scroll form e i campi accessibili al resize')
if 'Border Grid.Row="2" BorderBrush="{DynamicResource RSoftBorder}"' not in main:
    errors.append('Editor Preview 8: barra azioni fissa fuori dallo scroll')
if 'ColumnDefinitions="226,4,*"' not in main:
    errors.append('Editor Preview 8: larghezza sidebar/spacer inattesa')
if int(picker_root.get('Height', '9999')) < 365:
    errors.append('Picker Preview 8: spazio insufficiente per anteprima e tasti')
for binding in ('EditorTitle', 'EditorTrigger', 'EditorContent', 'EditorDescription',
                'HasUnsavedChanges', 'EditorEnabled', 'SaveCommand'):
    if '{Binding ' + binding not in main:
        errors.append(f'Editor: binding preservato mancante: {binding}')
for binding in ('Results', 'Selected.Title', 'Selected.Trigger', 'Selected.Content',
                'HasSelection', 'Texts.PickerInstructions'):
    if '{Binding ' + binding not in picker:
        errors.append(f'Picker: binding preservato mancante: {binding}')
if 'SelectedItem="{Binding Selected, Mode=TwoWay}"' not in picker:
    errors.append('Picker: binding selezione perso')
if 'Texts.PickerCopy' not in picker:
    errors.append('Picker: azione copia manuale persa')
if 'WindowsUnicodeTextOutput' in (ui/'App.axaml.cs').read_text(encoding='utf-8'):
    errors.append('Iniezione globale non deve essere connessa')
if main.count('<LinearGradientBrush') or picker.count('<LinearGradientBrush'):
    errors.append('Superfici colorate/gradienti non ammessi nel neutral reset')
# No color literals (except SVG path coordinates) may bypass the design tokens.
for filename, txt in [('MainWindow.axaml',main),('PickerWindow.axaml',picker)]:
    if re.search(r'#[0-9A-Fa-f]{6,8}',txt):
        errors.append(f'{filename}: colore hardcoded al di fuori dei token')
# Also check a single theme token for content and icon brushes referenced by views.
keys=set(re.findall(r'x:Key="(R[A-Za-z]+)"',app))
for filename,txt in [('MainWindow.axaml',main),('PickerWindow.axaml',picker)]:
    for key in set(re.findall(r'DynamicResource (R[A-Za-z]+)',txt)):
        if key not in keys:
            errors.append(f'{filename}: token inesistente {key}')
if 'SelectedSnippet = FilteredSnippets.FirstOrDefault();' not in (ui/'ViewModels/MainViewModel.cs').read_text():
    errors.append('Selezione automatica primo snippet da conservare')
review=(ui/'ImportReviewWindow.cs').read_text(encoding='utf-8')
for token in ('ristretto-dialog','dialog-hero','dialog-card','dialog-footer','review-row',
              'review-row-risk','ImportReviewSelected','checkbox.IsCheckedChanged','Key.Escape'):
    if token not in review and token not in app:
        errors.append('Contratto finestra import mancante: '+token)
for token in ('dialog-title','dialog-trigger-text','dialog-risk-text','dialog-warning','dialog-toolbar'):
    if token not in app:
        errors.append('Stile dialogo mancante: '+token)
print('GUI_NEUTRAL_RESET_STATIC:', 'PASS' if not errors else 'FAIL')
for e in errors:
    print(' -',e)
print('Nota: controllo strutturale; non compila, esegue o renderizza Avalonia.')
sys.exit(bool(errors))
