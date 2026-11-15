# Alpha 20.6 — versione unica e gate di validazione R1

**Data:** 14 novembre 2026. **Base:** archivio `Ristretto 3.9.0-alpha.20.5.zip` fornito dall'utente. **Ambito:** R1.03–R1.05, senza modifica a GUI, SQLite, picker, hotkey o backend nativo. Il repository originale non è identificabile con certezza da questa estrazione senza `.git`: non attribuire un commit o tag inventato.

## Risultato

- `Directory.Build.props` contiene la **versione canonica MSBuild** (`3.9.0-alpha.20.6`). I cinque `.csproj` che prima avevano un `<Version>` privato ora la ereditano. Il file `VERSION.txt` è uno specchio leggibile da script, che l'audit confronta con la versione canonica. Il comando `--version` legge l'attributo dell'assembly compilato. La versione inclusa nei report benchmark deriva anch'essa dall'assembly, non da un vecchio literal.
- `scripts/quality_gate.py` registra ogni fase come `PASS`, `FAIL` o `NOT_RUN` con hash SHA-256 dei sorgenti e timestamp UTC. Rifiuta i gate incompleti. Il report va in `.local-cache/gates/<mode>.json` (escluso dal pacchetto) salvo `--report` esplicito.
- Il rilevamento degli strumenti **esegue realmente** `--version`: Python 3.10+, .NET SDK 10+, Node 22+ e npm 10+. Accetta percorsi con spazi, installazioni Scoop presenti in `PATH` e override del percorso eseguibile tramite `RISTRETTO_PYTHON`, `RISTRETTO_DOTNET`, `RISTRETTO_NODE`, `RISTRETTO_NPM` (valore = **solo percorso del binario**, non argomenti di shell). Un alias Windows non eseguibile non conta come tool disponibile.
- Il gate `source` richiede audit, test Python e controlli statici (incluso il contratto GUI Preview 9). Il gate `core` aggiunge restore/build della solution, xUnit, build dei quattro tool e controllo CLI di startup senza GUI. Il gate `full` richiede in più Node, contratto JSON .NET→Node e **tre prove native** sul desktop interattivo Windows.

## Comandi Windows

```bat
.\scripts\validate-source.cmd
.\scripts\validate.cmd
.\scripts\validate-gate.cmd full
```

Il normale `validate.cmd` usa il **gate CORE** e conserva il controllo preventivo PowerShell di provenienza dei file. I test nativi sono eseguibili anche separatamente con `validate-native.cmd`. Il gate `full` esige una sessione Windows interattiva: non va lanciato su macOS/Linux o in una CI headless.

## Comandi Linux / macOS

```bash
./scripts/validate-gate.sh source
./scripts/validate.sh
```

La suite Windows nativa non può risultare `PASS` su un sistema diverso da Windows. Non confondere la compilabilità dell'app multipiattaforma con il funzionamento della hotkey/uscita Windows.

## Semantica dei risultati

- `PASS` = il comando corrispondente è terminato correttamente **in quella esecuzione**. Il riepilogo non eredita automaticamente risultati da report storici.
- `FAIL` = prerequisito mancante, comando con esito errato o prova impossibile nel gate selezionato.
- `NOT_RUN` = fase non inclusa nel gate o non iniziata per errore precedente. Il gate `core` può passare lasciando `node_tests`, `export_contract` e `native_windows` come `NOT_RUN`, **senza dichiararli validati**.
- `restore.source` è un'etichetta dichiarata dall'operatore (`local|online|unknown`, predefinito `unknown`). Non è rilevamento automatico e non dimostra che NuGet abbia raggiunto la rete. `nuget_vulnerability_audit` resta `NOT_RUN`: il restore da cache non è prova di assenza vulnerabilità.
- `vs_code_real_host` e `desktop_picker_real_io` restano `NOT_RUN` anche se il gate `full` supera il laboratorio Win32. Questi flussi hanno le proprie matrici R2/R3.
- Il report registra hash, versioni e stati, **non** contenuti degli snippet, testo digitato, appunti, percorsi personali né PID.

## Test ed evidenze

Controlli eseguiti in questa sessione: audit sorgenti, **20 unit test Python** (9 precedenti + 11 nuovi), validazione statica e **32 test Node**. Non è installato il .NET SDK nell'ambiente: build Release, xUnit, controllo CLI reale e prove native **non eseguiti** sulla 20.6. I **588 xUnit** e dieci prove CLI della 20.5 sono evidenze della versione caricata, non della 20.6.

I test del nuovo orchestratore coprono: percorsi con spazi, alias non eseguibili, versioni Node incompatibili, un possibile path Scoop, stop fail-closed, `NOT_RUN` esplicito, incompatibilità del gate `full` fuori Windows, non inferenza di GUI/audit online e hash sorgenti sensibile alle modifiche. Due nuovi test sull'audit garantiscono la coerenza della versione canonica.

## Limiti aperti

- R1.02: conflitto reale hotkey globale e alias symlink/junction da verificare su Windows con profili sintetici.
- R1.03: repository, commit e tag da collegare **quando saranno disponibili**, senza inventare provenienza Git.
- R1.04/1.05: rilevamento e gate implementati, ma esecuzione da Windows reale ancora da confermare; `validate-gate.cmd` va qualificato su installazioni reali diverse.
- R1.06: CI da collegare solo dopo scelta del repository, mantenendo il gate nativo separato.
- R2: picker, appunti, hotkey e Inserisci su un editor esterno restano da verificare graficamente. L'espansione automatica globale **non è implementata**.

## Regressione preesistente del controllore GUI

Il vecchio `verify_gui_redesign.py` richiedeva il binding `HasSelection` direttamente nel picker, ma Alpha 20.5 usa correttamente `CanCopy` e `CanInsert` per coordinare gli stati di caricamento e degli appunti. Il controllo era obsoleto, non la GUI. Aggiornato il contratto strutturale senza cambiare XAML/ViewModel e incluso nel gate `source/core/full`. Questo controllo non sostituisce un test visivo.
