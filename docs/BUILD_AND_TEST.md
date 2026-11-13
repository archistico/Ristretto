# Build e test — Ristretto 3.9.0-alpha.20.5

**Alpha 20.5:** [profili dati separati](ALPHA20_5_STARTUP_PROFILES.md); solution/quattro strumenti compilati in Release, **588 xUnit**, 30 nuovi casi profilo, dieci controlli del vero entry point, audit e sette Python passati. Nessuna prova grafica. Dopo la build: `python scripts/check_startup_profile.py --assembly src/TextExpander.Desktop/bin/Release/net10.0/Ristretto.dll --output .local-cache/startup-cli.json`. Per le prove desktop usare sempre `--data-dir` con una cartella sintetica separata.

**Alpha 20.4:** [Invio inserisce il primo risultato](ALPHA20_4_ENTER_INSERT.md); solution/quattro strumenti compilati in Release e **558 xUnit**, inclusi 50 ViewModel, passati. Audit sorgenti/statici e sette Python passati. Nessuna prova grafica o clipboard reale eseguita.

**Hotfix build:** in Alpha 20.1 `validate-native.cmd` era bloccato da CS0117 nella chiamata Core→Application a `CommittedInputSession.IsValidText`. Alpha 20.2 espone il metodo come `public static`, aggiunge audit cross-assembly e richiede una nuova compilazione Windows. [Report](ALPHA20_2_CS0117_FIX.md).

**Stato delle prove:** Alpha 20.3 verificata su Windows con .NET SDK 10.0.201 e restore locale: solution e quattro tool compilati in Release, 466 xUnit, 31 scenari nativi, smoke cooperante/remoto, 32 test Node, contratto .NET→Node e sette test Python superati. **Inserisci in editor esterni, vero host VS Code e audit NuGet online restano da eseguire.** Vedere [rapporto corrente](ALPHA20_3_VALIDATION.md); Alpha 7 conserva soltanto evidenze storiche.

**Incremento successivo:** [richiesta picker monouso e annullabile](ALPHA20_3_PICKER_SESSION.md), solution e quattro strumenti ricompilati e **484 xUnit** superati. Prove native e Node della baseline precedente non ripetute; nessun avvio grafico effettuato per questo incremento.

**Incremento ricerca:** [indice riutilizzabile e Unicode](ALPHA20_3_PICKER_SEARCH.md), solution e quattro tool ricompilati e **506 xUnit** superati (30 ricerca); controlli statici e sette test Python passati. Nessuna nuova prova desktop, nativa o Node.

**Incremento caricamento:** [annullamento e indice su worker](ALPHA20_3_PICKER_LOADING.md), solution e quattro strumenti ricompilati e **528 xUnit** passati. Il progetto test referenzia Desktop per verificare il ViewModel reale e le risorse, senza inizializzare Avalonia o aprire finestre. UiReview compilato ma non eseguito; controlli statici e sette Python passati.

**Incremento comandi appunti:** [Copia/Inserisci coordinati](ALPHA20_3_PICKER_CLIPBOARD.md), solution e quattro strumenti ricompilati e **550 xUnit** passati, inclusi 42 ViewModel con callback sintetici. Audit sorgenti/statici e sette Python passati; nessun avvio grafico, appunto reale o nuova prova nativa/Node.

## Preflight indipendente da .NET

Per rilevare errori di `using`/namespace interni, riferimenti tra progetti, versioni discordanti, attributi xUnit senza import e link documentali prima della compilazione:

```bat
scripts\validate-source.cmd
```

Su Linux/macOS: `./scripts/validate-source.sh`. Richiede Python 3; questi controlli **non sono un compilatore C#**. Le cinque regressioni dell'audit vengono eseguite insieme ai controlli statici.

## Requisiti

- .NET SDK 10; pacchetti NuGet disponibili per restore iniziale.
- Windows desktop per l'esperimento reale F2a; gli xUnit e i controlli statici non inviano tasti.
- Estrarre sempre lo ZIP in cartella **nuova**, senza unire cartelle `bin`/`obj` preesistenti.

## Gate separato del vero host VS Code

Su Windows con VS Code già installato: `scripts\validate-vscode-host.cmd -CodeExecutable "C:\percorso\Code.exe"`. Crea un profilo isolato e documenti sintetici per 14 scenari reali; non scarica né installa estensioni nel profilo personale. In questa sessione il renderer non si avvia (codice 49): suite implementata ma **test host non eseguiti**. [Istruzioni e limiti](ALPHA20_3_VSCODE_HOST_TESTS.md).

## Validazione principale su Windows

Da `Ristretto/`:

```bat
scripts\validate.cmd
```

Lo script esegue, in ordine (incluso l'adapter VS Code quando Node/npm sono disponibili):

1. Gate PowerShell versione e sorgenti anti-cartella obsoleta, incluse le versioni dei tool; se Python 3 è disponibile, audit statico e suoi test.
2. `dotnet restore`, `dotnet build -c Release`, `dotnet test` della solution (cinque progetti, inclusi i test).
3. `dotnet restore` e `dotnet build` del **laboratorio F2a separato**.
4. **Non** avvia l'esperimento Win32: per eseguirlo serve il comando esplicito seguente.
5. Ripristina e compila il benchmark Core separato, senza eseguirlo.
6. Ripristina e compila il laboratorio F2b simulato, senza eseguirlo.
7. Ripristina e compila `Ristretto.UiReview`, senza aprire finestre o generare immagini automaticamente.
8. Con Node/npm disponibili: esegue i 32 test VS Code e il contratto incrociato .NET → Node. Con Node/npm assenti il passaggio è indicato esplicitamente come non eseguito.

Gli script Windows e Bash disattivano la telemetria di build .NET/Avalonia. Non servono modifiche ai permessi del profilo per il log della telemetria Avalonia.

## Restore da sorgente locale

Se la rete non è disponibile, usare una sorgente NuGet locale completa e verificata. In PowerShell, limitando le impostazioni alla sessione corrente:

```powershell
$env:RestoreSources = 'C:\percorso\pacchetti-nuget'
$env:RestorePackagesPath = Join-Path $env:TEMP 'Ristretto-nuget-packages'
$env:NuGetAudit = 'false'
.\scripts\validate.cmd
```

La sorgente deve includere tutte le dipendenze e le versioni richieste. La validazione conservata ha usato restore locale e non comprende l'audit online delle vulnerabilità; non disabilitare TLS o verifiche delle firme. Per tornare al restore ordinario aprire una nuova sessione.

## Laboratorio F2b simulato, senza finestre o dati utente

```powershell
dotnet run -c Release --project tools/Ristretto.ReplacementLab/Ristretto.ReplacementLab.csproj -- --self-test
```

Esegue in memoria il percorso riconoscimento → piano → sostituzione e quattro scenari di rifiuto/incertezza. [Metodo e limiti](F2B_CONDITIONAL_REPLACEMENT.md). Non è un test di editor esterni.

## Esperimento Windows F2a facoltativo

```powershell
dotnet run --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --self-test
```

Cliccare nel campo di prova bianco del target aperto dal tool. Attendere l'esito stampato dal programma. È un test nativo separato, con output verso il **solo** controllo EDIT del processo figlio. Leggere [istruzioni e limiti F2a](F2A_WINDOWS_CONTROLLED_LAB.md) prima di eseguire. `--self-test` non serve per validare i test unitari.

## Sostituzione nativa in EDIT cooperante — alpha.7

Prova automatica esplicita, senza SendInput o necessità di cliccare durante il test:

```powershell
dotnet run -c Release --no-build --no-restore --project tools/Ristretto.WindowsOutputLab -- --replacement-test
```

Demo interattiva: digitare `;firma` + spazio nel campo del laboratorio, poi chiudere la finestra per terminare:

```powershell
dotnet run -c Release --no-build --no-restore --project tools/Ristretto.WindowsOutputLab -- --replacement-demo
```

Il primo comando è stato verificato su Windows con 21/21 scenari. La demo con input umano è disponibile ma non è stata provata in questa sessione. Entrambi operano soltanto su controlli creati dal laboratorio; non qualificano altre app o IME. [Dettagli e rapporto](F2B_WINDOWS_OWNED_EDIT.md).

### Gate Alpha 8 e demo verificabile

Per eseguire i test nativi in una sessione Windows interattiva:

```bat
scripts\validate-native.cmd
```

La nuova demo richiede anche il focus sul proprio campo e riporta un esito `PASS` soltanto quando è stata osservata almeno una sostituzione nativa verificata senza tentativi bloccati/incerti. Per conservarne un rapporto **senza testo digitato**:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --replacement-demo --report reports/alpha8-demo-nuova-prova.json
```

Il file JSON deve essere nuovo. La prova deve essere eseguita con vera digitazione dall'operatore; il programma non è in grado di certificare l'origine fisica dei caratteri, e il PASS vale solo per il proprio EDIT. [Contratto](F3_ADMISSION_GATE.md) · [Matrice manuale](ALPHA8_MANUAL_TEST_MATRIX.md).

## Alpha 9: prova cooperante in secondo processo

Il primo processo avvia un figlio che possiede il proprio campo EDIT, ne osserva i commit e applica localmente la sostituzione. Al primo processo arrivano soltanto READY e i contatori (nessun testo digitato). Questa prova **non** modifica editor di terze parti. `scripts\validate-native.cmd` esegue ora anche uno smoke automatico in secondo processo, distinto dai 21 scenari storici del proprio EDIT.

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --cooperating-smoke
```

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --cooperating-demo --report reports/alpha9-cooperating-prima-prova.json
```

Digitare `;firma` + spazio nella finestra figlia e chiuderla. Il nuovo rapporto deve indicare `VerifiedInCooperatingChildOnly`; senza un tentativo verificato la prova non passa. [Guida e limiti](ALPHA9_COOPERATING_PROCESS.md).

## Alpha 10: ricognizione esterna in sola lettura (manuale)

Eseguire **solo quando desiderato**, su Windows. Il programma lascia cinque secondi per dare focus a un editor; non legge né modifica il suo testo. Il risultato è una classificazione **non qualificante**. Nessuno script di validazione la avvia automaticamente.

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab -- --probe-external
```

Per creare facoltativamente un JSON **nuovo e privo di contenuto utente**: `--probe-external --report reports/alpha10-mia-prova.json`. [Contratto e limitazioni](ALPHA10_EXTERNAL_EDITOR_PROBE.md).

## Alpha 11: interrogazione read-only della selezione (manuale)

La ricognizione alpha.10 **non** legge la selezione. La modalità aggiuntiva prova `EM_GETSEL` su un `Edit` esterno (150 ms, focus ricontrollato); scarta gli offset. Non legge testo e non autorizza la sostituzione. **Non è in alcuno script di prova automatica**:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab/Ristretto.WindowsOutputLab.csproj -- --inspect-classic-edit
```

Report facoltativo con nuovo percorso: `--inspect-classic-edit --report reports/alpha11-prova-mia.json`. [Limiti e matrice di capacità](ALPHA11_CLASSIC_EDIT_SELECTION.md).

## Verifiche strutturali indipendenti dallo SDK

```shell
python3 scripts/verify_static.py
python3 scripts/validate_static.py
python3 scripts/verify_gui_redesign.py
```

Queste verifiche controllano XML/XAML, risorse, SQLite Python, riferimenti e struttura dei file: **NON** sostituiscono compilazione C#, xUnit né la prova Windows. L'esecuzione su Linux/macOS con .NET può usare `bash scripts/validate.sh` per solution, benchmark Core e laboratorio simulato; il laboratorio Windows è escluso. L'esecuzione Linux/macOS non è ancora stata verificata.

Lo script Bash compila anche il benchmark Core. Per misurare esplicitamente, senza debugger:

```shell
dotnet run -c Release --project tools/Ristretto.CoreBenchmark/Ristretto.CoreBenchmark.csproj -- --output reports/core-benchmark-nuova-misura.json
```

Usare un nuovo nome per il JSON. Il [manuale del benchmark](CORE_BENCHMARK.md) precisa scope, metodo e limiti.

## Smoke test manuale GUI (invariata)

Verificare gruppi/snippet, picker Invio = copia, Esc = chiudi, Unicode, import/backup. Preview 9: confrontare chiaro/scuro, focus Tab, menu e DPI 125/150%. Nessuna espansione globale deve attivarsi.

Le anteprime con dati sintetici possono essere ripetute da una sessione desktop con un nuovo nome di cartella:

```shell
dotnet run -c Release --project tools/Ristretto.UiReview/Ristretto.UiReview.csproj -- --output .local-cache/ui-review-nuova
```

Il tool apre brevemente finestre proprie, renderizza i client a 96 DPI e controlla i limiti dei pulsanti applicativi. Non carica il DB personale. [Metodo e limiti](GUI.md).

## Archivio integrale

```shell
python3 scripts/package.py --output .local-cache/Ristretto_v3.9_ALPHA19_VSCODE_ADAPTER_COMPLETO.zip
```

Radice `Ristretto/`, sorgenti, test, quattro tool separati, documentazione, script, misure, render correnti e manifest SHA-256. Cache locale, bin/obj e risultati temporanei sono esclusi. I vecchi materiali grafici e i rapporti duplicati sono stati rimossi anche dal workspace. L'integrità del pacchetto **non** dimostra che build o API native funzionino.

## Alpha 19 — adapter Visual Studio Code (separato da .NET)

L'estensione è in [`tools/ristretto-vscode-extension`](../tools/ristretto-vscode-extension/README.md). Non è caricata dal Desktop e non influisce sulle normali build .NET. Per i test puri con Node 22+ non serve installare dipendenze:

```bat
scripts\validate-vscode.cmd
```

```bash
./scripts/validate-vscode.sh
```

`validate.cmd` esegue anche questi test se Node/npm sono presenti; se assenti segnala esplicitamente **NON ESEGUITI** senza bloccare la compilazione .NET. L'avvio in una finestra reale VS Code è **manuale** e non equivale al test Node. [Guida per l'Extension Development Host](ALPHA19_VSCODE_ADAPTER.md).
