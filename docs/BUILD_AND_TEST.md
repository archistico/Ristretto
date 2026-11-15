# Compilazione e verifiche — Ristretto Alpha 20.28

Per installare, aggiornare e recuperare i dati partire dalla [guida utente](INSTALLATION_AND_UPDATE.md). Questo documento descrive i controlli per lo sviluppo.

**Baseline già eseguita:** CI locale e da ZIP nuovo, 870 xUnit, 32 Node, 23 regressioni degli strumenti, 10 CLI e contratto .NET→Node passati. Inoltre 21 casi nativi sulle registrazioni, eseguiti separatamente da CI e ripetuti anche dall'estrazione. Consegna e rebuild nel [rapporto](../reports/ALPHA20_28_CHECKS.md).

## Requisiti

- .NET SDK 10 e dipendenze NuGet disponibili. Python non è richiesto.
- Node.js 22 per CI/FULL e il contratto dell'estensione, non per SOURCE/CORE.
- Sessione desktop Windows soltanto per i controlli nativi e manuali che la richiedono.

Eseguire i comandi dalla cartella contenente `Ristretto.sln`.

## 1. Scegliere il gate

| Gate | Comando PowerShell | Perimetro |
| --- | --- | --- |
| SOURCE | `.\scripts\validate-source.cmd` | Audit C#, regressioni degli strumenti, XML/XAML, lingue, link e DDL SQLite |
| CORE | `.\scripts\validate.cmd` | SOURCE, restore/build solution, xUnit, quattro laboratori e DevTools, dieci CLI senza GUI |
| CI | `.\scripts\validate-gate.cmd ci` | CORE, Node e contratto export .NET→Node |
| FULL | `.\scripts\validate-gate.cmd full` | CI e laboratori nativi Windows controllati, incluse registrazioni hotkey; richiede desktop interattivo |

Il risultato è `GATE <NOME>: PASS` oppure `FAIL`. Ogni passo registra anche `NOT_RUN`. Un tool compilato non è un tool eseguito.

**Nessuno di questi nomi certifica automaticamente** il picker e la form sul desktop, l'incolla in un editor esterno, il vero host VS Code o la provenienza dell'input WinEvent. Sono prove separate, con un rapporto proprio.

Per soli documenti usare SOURCE e controllare le immagini/link modificati. Per codice applicativo eseguire i gate pertinenti e ripetere la validazione del pacchetto da cartella nuova. I gate non eseguono un audit vulnerabilità online completo: va registrato separatamente.

Linux/macOS dispongono degli avviatori `validate-source.sh`, `validate.sh` e `validate-gate.sh`; la disponibilità degli script non attesta una qualificazione runtime di quei sistemi.

## 2. Test mirati e avvio di prova

~~~powershell
.\scripts\validate-picker.cmd
dotnet test tests/TextExpander.Tests/TextExpander.Tests.csproj -c Release --filter FullyQualifiedName~HotkeyCaptureTests
dotnet test tests/TextExpander.Tests/TextExpander.Tests.csproj -c Release --filter FullyQualifiedName~HotkeyCaptureWindowTests
~~~

Il secondo filtro apre i controlli reali su backend Avalonia headless: 22 casi, risorse del prodotto e database sintetico, senza finestre del sistema operativo, registrazioni native o profilo personale. Sono inclusi anche in CORE/CI. La dipendenza solo test `Avalonia.Headless.XUnit` deve restare alla stessa patch di Avalonia, attualmente 11.3.22. [Copertura e limiti](ALPHA20_27_CAPTURE_INTEGRATION.md).

Per avviare una libreria isolata, senza usare quella personale:

~~~powershell
dotnet run --project src/TextExpander.Desktop/TextExpander.Desktop.csproj -c Release -- --data-dir "$env:LOCALAPPDATA\Ristretto-Qualifica-20-28"
~~~

Per riutilizzare il profilo nelle versioni successive conservare lo stesso percorso assoluto. Un secondo profilo separa i dati, ma può entrare in conflitto sulle combinazioni globali.

## 3. Form Registra… e hotkey reali

Per provare soltanto i worker di registrazione e le collisioni fra processi, senza aprire finestre o inviare input:

~~~powershell
.\scripts\validate-snippet-hotkeys.cmd
~~~

Registra temporaneamente tre combinazioni disponibili fra Ctrl+Alt+Shift+F6…F11, produce un JSON nuovo per caso e rilascia le assegnazioni. Nessun uso di appunti o profili personali. La prova è inclusa anche in FULL/validate-native; CORE e CI non la eseguono. [Perimetro, prerequisiti ed esiti](ALPHA20_28_NATIVE_HOTKEY_REGISTRATIONS.md).

Usare la [matrice di qualificazione Windows](WINDOWS_QUALIFICATION_PLAN.md). Prima sequenza:

1. Aprire uno snippet sintetico e premere Registra….
2. Premere `ALT + CTRL + SHIFT + B`, rilasciare e confrontare il display.
3. Confermare e salvare; provare il richiamo in un documento vuoto.
4. Ripetere con annullamento, Esc, chiusura, rimozione e combinazione già assegnata.
5. Controllare che picker e snippet tornino utilizzabili dopo ogni chiusura della form.

Il richiamo base in Blocco note e quello dopo la 20.27 sono confermati dall'utente. I test headless verificano UI/SQLite e il laboratorio 20.28 verifica worker/coordinatore nativi; l'orchestrazione Desktop.App e i casi avanzati del desktop restano aperti.

## 4. Prova guidata del picker in Blocco note

Gli script usano un documento e un profilo sintetici:

~~~powershell
.\scripts\validate-picker-notepad.cmd prepare --open
.\scripts\seed-picker-notepad.cmd TOKEN
.\scripts\validate-picker-notepad.cmd launch --session TOKEN
~~~

Sostituire `TOKEN` con quello prodotto da prepare e seguire le istruzioni del runner. Nell'editor portare il cursore in fondo, richiamare il picker, scegliere lo snippet di prova, inserire e salvare il documento.

~~~powershell
.\scripts\validate-picker-notepad.cmd status --session TOKEN
.\scripts\validate-picker-notepad.cmd verify --session TOKEN
~~~

Il PASS del verificatore riguarda i byte del documento salvato rispetto alla fixture Unicode. Non dimostra espansione automatica o provenienza umana. `NOT_RUN` indica una prova non conclusa, non un successo.

## 5. Esperimenti R5, sempre separati

Prima compilare. Preparare un controllo Win32 Edit classico esterno, vuoto e contenente solo dati sintetici. I comandi possono leggere temporaneamente in RAM fino a 4096 unità UTF-16: non usarli su documenti personali o campi protetti.

~~~powershell
.\scripts\observe-classic-edit-text.cmd
.\scripts\observe-classic-edit-live.cmd 15
.\scripts\observe-classic-edit-preview.cmd 15
~~~

Scegliere **un comando per volta**, seguendone i tempi di preparazione e osservazione. Nella modalità preview digitare `;firma` seguito da spazio dopo l'avviso di attivazione.

Gli esiti sono osservazioni o trigger potenziali: paste, IME e modifiche programmatiche possono produrre lo stesso testo. Nessuno dei comandi autorizza la sostituzione. Notepad moderno, RichEdit e altri editor possono non appartenere al tipo di controllo supportato.

[Snapshot esterni](ALPHA20_21_EXTERNAL_EDIT_OBSERVATION.md) · [WinEvent](ALPHA20_23_LIVE_EDIT_OBSERVER.md) · [anteprima](ALPHA20_24_TRIGGER_PREVIEW.md).

## 6. VS Code e anteprime grafiche

Il ramo host VS Code è sospeso. Prima di riprenderlo diagnosticare l'avvio normale; il precedente crash `0x80000003` non ha una causa confermata. Il runner supporta `-PrepareOnly` per preparare senza avviare l'editor; le istruzioni sono in [host VS Code](ALPHA20_3_VSCODE_HOST_TESTS.md).

Il tool UiReview apre finestre proprie: è compilato dai gate, ma non avviato automaticamente. Le immagini attuali sono render della Alpha 7. La rigenerazione del nuovo dialogo è pianificata in R2.06; un render non sostituisce tastiera, focus, DPI e registrazioni reali. [Anteprime](../design/README.md) · [accettazione visuale](GUI_VISUAL_ACCEPTANCE.md).

## 7. Pacchetto e cartella nuova

~~~powershell
dotnet run -c Release --project tools/Ristretto.DevTools/Ristretto.DevTools.csproj -- package --output .local-cache/Ristretto_COMPLETO.zip
~~~

Il tool include i file sorgenti e i documenti, esclude output/cache e produce `MANIFEST_SHA256.txt`, verificando gli hash dell'archivio. Questo comando crea il pacchetto: non esegue i test dell'applicazione.

Estrarre in una cartella nuova, verificare il manifest e l'assenza iniziale di `bin`/`obj`, poi eseguire il gate dichiarato. Per una consegna funzionale ripetere build/test pertinenti; per una revisione documentale verificare SOURCE e che l'hash dei sorgenti funzionali corrisponda alla baseline.

Versione del codice, revisione dei documenti e risultato delle prove devono essere dichiarati separatamente. Non pubblicare un manifest riferito a file precedenti all'ultimo aggiornamento.

## 8. Rapporti, restore ed errori

I rapporti dei gate sono in `.local-cache/gates/` e contengono versione, hash e risultati per passo. Conservare nella consegna solo le evidenze scelte, con il loro perimetro.

Il tool C# accetta `--report <file>` e `--restore-source <etichetta>` quando invocato direttamente. L'etichetta descrive la sorgente: non configura NuGet. Registrare onestamente restore online/cache e audit non eseguito. Non disabilitare TLS o controlli delle firme.

Se un comando fallisce, leggere il primo FAIL e la prima eccezione/errore di compilazione; i passi successivi restano NOT_RUN. Condividere versione e diagnostica senza contenuto degli snippet.

La pulizia di output è facoltativa: `scripts/clean-build.cmd -Preview` mostra i percorsi prima della cancellazione. [Regole di pulizia](CLEAN_BUILD.md).
