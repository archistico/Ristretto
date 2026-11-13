#!/usr/bin/env python3
"""Controlli statici indipendenti da .NET. Non sostituiscono dotnet build/test."""
from pathlib import Path
from urllib.parse import unquote
import xml.etree.ElementTree as ET
import re, sqlite3, json, sys
root=Path(__file__).resolve().parents[1]
checks=[]
def check(name, ok, details=''):
 checks.append({'name':name,'passed':bool(ok),'details':details})
 print(('OK' if ok else 'FAIL'), name, details)

xmls=list(root.rglob('*.csproj'))+list(root.rglob('*.props'))+list(root.rglob('*.resx'))+list(root.rglob('*.axaml'))
errs=[]
for p in xmls:
 try: ET.parse(p)
 except Exception as ex: errs.append(f'{p.relative_to(root)}: {ex}')
check('XML progetti, XAML e localizzazioni',not errs,f'{len(xmls)} file; '+str(errs[:2]))

sln=(root/'Ristretto.sln').read_text()
csprojs=[p for p in root.rglob('*.csproj') if 'tools' not in p.relative_to(root).parts]
optional_lab=root/'tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj'
check('Progetti della solution + lab isolato',len(csprojs)==5 and all(p.name in sln for p in csprojs) and optional_lab.is_file() and optional_lab.name not in sln,str(len(csprojs))+' in solution + laboratorio separato')

name='http://www.w3.org/XML/1998/namespace'
resources={}
for p in sorted((root/'src/TextExpander.Desktop/Localization').glob('*.resx')):
 tree=ET.parse(p)
 resources[p.name]={d.attrib['name'] for d in tree.findall('data')}
base=resources.get('Strings.resx',set())
check('Chiavi di traduzione allineate',len(resources)==5 and all(v==base for v in resources.values()),f'{len(resources)} lingue, {len(base)} chiavi')
ui=(root/'src/TextExpander.Desktop/Localization/UiStrings.cs').read_text()
xaml=(root/'src/TextExpander.Desktop/MainWindow.axaml').read_text()
refs=set(re.findall(r'Texts\.([A-Za-z_]+)',xaml))
check('Binding testi GUI presenti',all(re.search(r'public string '+k+r'\b',ui) for k in refs),str(sorted(refs)))

store=(root/'src/TextExpander.Infrastructure/Storage/SqliteLibraryStore.cs').read_text()
m=re.search(r'using \(var schema = Cmd\(db, @"(.*?)", tx\)\) schema.ExecuteNonQuery\(\);',store,re.S)
if m:
 conn=sqlite3.connect(':memory:')
 try:
  conn.execute('PRAGMA foreign_keys=ON')
  conn.executescript(m.group(1))
  conn.execute('INSERT INTO groups(id,name,enabled) VALUES (?,?,?)',('1','Prove 🌍',1))
  conn.execute('INSERT INTO snippets(id,group_id,title,trigger,content,description,enabled,revision) VALUES (?,?,?,?,?,?,?,?)',
      ('2','1','Accenti',';café','cafe\u0301 👍🏽 مرحبا','',1,1))
  ok=conn.execute('PRAGMA user_version').fetchone()[0]==1 and conn.execute('PRAGMA integrity_check').fetchone()[0]=='ok'
  ok=ok and '👍🏽' in conn.execute('SELECT content FROM snippets').fetchone()[0]
  try:
   conn.execute("INSERT INTO snippets VALUES ('3','nonexistent','No',';none','x','',1,1)")
   ok=False
  except sqlite3.IntegrityError: pass
  check('Schema SQLite + unicode + FK',ok,'SQLite di Python, NON il provider .NET')
 except Exception as ex: check('Schema SQLite + unicode + FK',False,str(ex))
 finally: conn.close()
else: check('Schema SQLite + unicode + FK',False,'DDL non trovato')

broken=[]; links=0
for md in root.rglob('*.md'):
 txt=md.read_text(encoding='utf-8')
 for dest in re.findall(r'\]\(([^)]+)\)',txt):
  if dest.startswith(('http:','https:','mailto:','sandbox:','#','data:')): continue
  dest=unquote(dest.split('#')[0].split(' ')[0])
  if not dest: continue
  links+=1
  if not (md.parent/dest).exists(): broken.append((str(md.relative_to(root)),dest))
check('Link Markdown interni',not broken,f'{links} link; {len(broken)} errori, esempi={broken[:4]}')

# Alpha 20 optional VS Code adapter: structural contract, not a live VS Code run.
try:
 package=json.loads((root/'tools/ristretto-vscode-extension/package.json').read_text(encoding='utf-8'))
 props=package['contributes']['configuration']['properties']
 ids={x['command'] for x in package['contributes']['commands']}
 allowed={'ristrettoVscode.expandAtCursor','ristrettoVscode.chooseExport'}
 js=(root/'tools/ristretto-vscode-extension/src/extension.js').read_text(encoding='utf-8')
 ctl=(root/'tools/ristretto-vscode-extension/src/controller.js').read_text(encoding='utf-8')
 fixture=json.loads((root/'tools/ristretto-vscode-extension/fixtures/synthetic-export.json').read_text(encoding='utf-8'))
 ok=(props['ristrettoVscode.enabled']['default'] is False and
     props['ristrettoVscode.exportFile']['default']=='' and
     allowed==ids and package['activationEvents']==['onCommand:ristrettoVscode.expandAtCursor','onCommand:ristrettoVscode.chooseExport'] and
     'onDidChangeTextDocument' not in js and 'onDidChangeTextDocument' not in ctl and
     'editor.edit(' in ctl and fixture['format']=='text-expander-studio/snippets' and fixture['version']==1)
 check('Adapter VS Code opt-in, export v1 e comando esplicito',ok,'Controllo statico, NON prova in VS Code')
 check('Gate Alpha 20 verso export .NET',
       (root/'tests/TextExpander.Tests/VscodeExportContractTests.cs').exists() and
       (root/'tools/ristretto-vscode-extension/cli/verify-dotnet-export.js').exists() and
       (root/'scripts/validate-export-contract.cmd').exists(),
       'La prova .NET -> Node richiede .NET SDK + Node, NON viene eseguita qui')
except (OSError,ValueError,KeyError,TypeError) as ex:
 check('Adapter VS Code opt-in, export v1 e comando esplicito',False,str(ex))
for forbidden in ('archive','beeftext-screenshots'):
 check(f'Cartella {forbidden} assente',not any(x.is_dir() and x.name==forbidden for x in root.rglob('*')))

result={'status':'STATIC_ONLY','checks':checks,'passed':sum(x['passed'] for x in checks),
        'failed':sum(not x['passed'] for x in checks),'dotnet_build':'NOT_RUN','dotnet_test':'NOT_RUN','gui_runtime':'NOT_RUN'}
(root/'reports').mkdir(exist_ok=True)
(root/'reports/STATIC_VALIDATION.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')
if result['failed']: sys.exit(1)
