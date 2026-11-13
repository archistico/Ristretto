# Evidenze conservate

## Alpha 20.5 — Profili dati

[STARTUP_PROFILE_VALIDATION.json](STARTUP_PROFILE_VALIDATION.json), [esiti CLI](STARTUP_PROFILE_CLI.json) e [log build/test](startup-profile-validation.log): 588 xUnit passati (30 nuovi casi), dieci controlli del vero entry point, solution/quattro strumenti compilati, audit e sette Python passati. Database e pipe sintetici; nessun avvio grafico o clipboard reale. [Metodo e limiti](../docs/ALPHA20_5_STARTUP_PROFILES.md).

## Alpha 20.4 — Invio nel picker

[ENTER_INSERT_VALIDATION.json](ENTER_INSERT_VALIDATION.json) e [log build/test](enter-insert-validation.log): 558 xUnit passati (50 ViewModel, otto nuovi casi), solution e quattro strumenti compilati; audit e sette Python passati. Nessun avvio grafico o clipboard reale. [Metodo](../docs/ALPHA20_4_ENTER_INSERT.md).

## Incremento comandi appunti picker

[PICKER_CLIPBOARD_VALIDATION.json](PICKER_CLIPBOARD_VALIDATION.json) e [log build/test](picker-clipboard-validation.log): solution e quattro strumenti compilati, 550 xUnit passati (42 ViewModel, 22 nuovi casi), audit e sette Python passati. Callback sintetici, nessun avvio grafico o clipboard reale. [Metodo](../docs/ALPHA20_3_PICKER_CLIPBOARD.md).

## Incremento caricamento picker

[PICKER_LOADING_VALIDATION.json](PICKER_LOADING_VALIDATION.json) e [log build/test](picker-loading-validation.log): solution e quattro strumenti compilati, 528 xUnit passati (20 ViewModel, 22 nuovi casi complessivi), audit statici e sette Python passati. Nessun avvio di Avalonia o altri processi grafici; UiReview soltanto compilato. [Metodo](../docs/ALPHA20_3_PICKER_LOADING.md).

## Incremento ricerca picker

[PICKER_SEARCH_VALIDATION.json](PICKER_SEARCH_VALIDATION.json) e [log build/test](picker-search-validation.log): solution e quattro strumenti compilati, 506 xUnit passati (30 ricerca, 22 nuovi), audit statici e sette Python passati. Nessun avvio grafico né nuovo benchmark. [Metodo](../docs/ALPHA20_3_PICKER_SEARCH.md).

## Incremento ciclo di vita picker

[PICKER_INSERT_SESSION_VALIDATION.json](PICKER_INSERT_SESSION_VALIDATION.json) e [log build/test](picker-insert-session-validation.log): solution e quattro strumenti compilati, 484 xUnit passati (36 picker), audit statici e sette test Python passati. Adapter simulati; nessun nuovo avvio grafico o test nativo. [Metodo](../docs/ALPHA20_3_PICKER_SESSION.md).

## Suite host VS Code

[VSCODE_HOST_VALIDATION.json](VSCODE_HOST_VALIDATION.json): runner isolato eseguito, renderer di VS Code 1.127.0 non avviato (codice 49), **0 scenari host verificati**. Implementati 14 scenari; sintassi e 32 test Node passati. [Metodo](../docs/ALPHA20_3_VSCODE_HOST_TESTS.md).

## Baseline corrente Alpha 20.3

Build Release di solution e quattro strumenti, 466 xUnit, 31 scenari nativi EDIT posseduto, smoke cooperante/remoto, 32 Node e contratto .NET→Node verificati con restore locale. [Metodo, correzioni e limiti](../docs/ALPHA20_3_VALIDATION.md).

- [Riepilogo JSON](WINDOWS_ALPHA20_3_VALIDATION.json)
- [Risultati xUnit](windows-alpha20-3-tests.trx)
- [Log build e test](windows-alpha20-3-build.log)
- [Log test nativi](windows-alpha20-3-native.log)

La prova manuale Inserisci del picker e il vero host VS Code restano da eseguire. I paragrafi successivi descrivono evidenze e limiti storici delle consegne precedenti.

## Baseline storica Alpha 7

**Le versioni alpha.8 e alpha.9 non sostituiscono le evidenze Windows alpha.7.** Il nuovo laboratorio in secondo processo è nei sorgenti, ma non è stato provato su Windows. Seguire [Alpha 9](../docs/ALPHA9_COOPERATING_PROCESS.md) per registrare un rapporto *nuovo*, privo di testo digitato.

| File | Significato |
|---|---|
| [CLEANUP_VALIDATION.json](CLEANUP_VALIDATION.json) | Verifica del riordino: sorgenti, anteprime ed evidenze invariati; collegamenti, XML/JSON e token documentati controllati |
| [WINDOWS_ALPHA7_VALIDATION.json](WINDOWS_ALPHA7_VALIDATION.json) | Riepilogo Windows: solution e quattro tool, 180/180 xUnit e 21/21 scenari nativi |
| [windows-alpha7-validation.log](windows-alpha7-validation.log) | Log completo di restore locale, build e test |
| [windows-alpha7-tests.trx](windows-alpha7-tests.trx) | Risultati xUnit della alpha.7 |
| [windows-alpha7-native.json](windows-alpha7-native.json) | 21 scenari su EDIT Win32 posseduto, con hash assembly |
| [windows-alpha7-native.log](windows-alpha7-native.log) | Esiti sintetici della prova nativa |
| [alpha5-replacement-lab.log](alpha5-replacement-lab.log) | 5/5 scenari del laboratorio in memoria, eseguiti nella alpha.5 |
| [core-benchmark-alpha3.json](core-benchmark-alpha3.json) | Baseline del benchmark Core |
| [core-benchmark-alpha4.json](core-benchmark-alpha4.json) | Stessa misura dopo ottimizzazione |
| [core-benchmark-comparison.json](core-benchmark-comparison.json) | Confronto delle allocazioni e latenze |

I rapporti precedenti, i log di build duplicati e la prova nativa intermedia da 20 casi sono stati spostati in `.local-cache/cleanup-recovery`, esclusa dal progetto distribuito. I risultati storici restano nel [changelog](../docs/CHANGELOG.md). Il tentativo F2a SendInput della alpha.3 era stato bloccato da `target_changed` prima dell'invio: non è una prova superata di consegna.

Per la GUI sono conservati separatamente [32 render e registro originali](../design/current/review.json) della alpha.6 e il [CSV dei contrasti](../data/UI_PREVIEW9_CONTRAST.csv). Il benchmark risale alla alpha.4; né benchmark né render sono stati rieseguiti nella alpha.7 o durante la pulizia.

Le evidenze native riguardano soltanto il controllo posseduto: non qualificano SendInput, editor esterni o IME generale. Prova umana, controlli GUI manuali e runtime macOS/Linux restano da completare. La verifica offline non include audit NuGet online. [Comandi e limiti](../docs/BUILD_AND_TEST.md).

La verifica del riordino usa PowerShell/.NET per hash, collegamenti, XML/JSON e corrispondenza dei 22 token di palette, oltre al gate dei sorgenti e alla sintassi PowerShell/Bash. I tre script statici Python non sono stati eseguiti: in questo ambiente sono disponibili solo alias Windows senza interprete utilizzabile. Le prove funzionali non sono state ripetute perché i 64 file di `src`, `tests` e `tools` sono rimasti identici.

## Alpha 11 — verifiche della consegna (statiche, non .NET)

- [ALPHA11_SOURCE_AUDIT.json](ALPHA11_SOURCE_AUDIT.json): confronto hash con Alpha 10 di GUI, Core e SQLite; nessuna build/xUnit/native effettuata nella consegna.
- I rapporti Alpha 7 sono **storici** e non documentano esecuzioni della Alpha 11.

## Alpha 14 — incremento di provenienza (da validare)

La suite xUnit `OwnedEditProvenanceTests` e due nuovi scenari nativi (`synthetic_fixture_provenance_is_revision_correlated`, `queued_character_message_is_not_human_proof`) sono presenti nei sorgenti Alpha 14. I **25 scenari nativi sono previsti, non eseguiti**. Il controllo statico corrente, se presente in `STATIC_VALIDATION.json`, non attesta una compilazione C# o una prova Win32. L'ultima evidenza Windows effettiva continua a essere Alpha 7.
