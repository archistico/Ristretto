## Revisione documentale — 15 novembre 2026

[README e roadmap](DOCS_ROADMAP_REVIEW.md): revisione dei soli documenti sulla baseline Alpha 20.26. I nuovi controlli documentali sono separati dai risultati funzionali già conservati qui sotto.

## Alpha 20.26 — ultimo rapporto funzionale

[Verifiche](ALPHA20_26_CHECKS.md) · [JSON](ALPHA20_26_VALIDATION.json) · [log](alpha20-26-validation.log). 846 xUnit passati, inclusi 26 nuovi casi. Conferma utente del richiamo in Blocco note della 20.25; nuova form ancora da qualificare sul desktop. Le sezioni successive conservano gli esiti storici.

## Alpha 20.25 — rapporto precedente

[Verifiche](ALPHA20_25_CHECKS.md) · [JSON](ALPHA20_25_VALIDATION.json) · [log](alpha20-25-validation.log). CS0246 riprodotti e corretti; 820 xUnit passati, inclusi 41 nuovi casi. Hotkey reali, clipboard e GUI desktop ancora da qualificare. Le sezioni successive sono storiche.

## Alpha 20.23 — rapporto precedente

[Verifiche Alpha 20.23](ALPHA20_23_CHECKS.md) — osservatore WinEvent, controlli disponibili e test Windows ancora aperti.

**Hotfix Alpha 20.19.5:** [stato verifica](ALPHA20_19_5_VALIDATION_STATUS.json) · [correzione CS9174](../docs/ALPHA20_19_5_CS9174_FIX.md). Due errori di compilazione confermati dall’utente sulla 20.19.4; corretti nei sorgenti 20.19.5, build/tool C# della nuova versione ancora NOT_RUN.

**Stato corrente Alpha 20.19.3:** [rapporto strutturato](ALPHA20_19_3_VALIDATION_STATUS.json) · [correzione startup CLI Windows](../docs/ALPHA20_19_3_STARTUP_CLI_ENCODING.md). I PASS della build/xUnit Alpha 20.19.2 sono confermati dall'utente; il test CLI della 20.19.2 era FAIL per la decodifica. Non trasferire questi esiti alla 20.19.3.

**Stato corrente Alpha 20.19.2:** [rapporto strutturato](ALPHA20_19_2_VALIDATION_STATUS.json) · [correzione gate PowerShell](../docs/ALPHA20_19_2_VERSION_GATE_FIX.md). I controlli Python/Node/statici risultano PASS; PowerShell Windows, build .NET, xUnit e prove native restano NOT_RUN in questa consegna.

**Stato corrente Alpha 20.19:** [rapporto strutturato](ALPHA20_19_VALIDATION_STATUS.json), [dettagli R5](../docs/ALPHA20_19_FOCUS_BOUNDARY.md). Nuovo codice e prove Windows non ancora compilati/eseguiti con .NET in questa consegna.

**Stato corrente Alpha 20.18.1:** [rapporto strutturato](ALPHA20_18_1_VALIDATION_STATUS.json), [dettagli correzione](../docs/ALPHA20_18_1_CLIPBOARD_CS0618.md). Il test manuale Notepad Alpha 20.18 è PASS sul documento sintetico salvato (conferma dell'utente), ma gli xUnit e la compilazione della nuova Alpha sono NOT_RUN.

**Alpha 20.12:** [rapporto corrente](ALPHA20_12_VALIDATION_STATUS.json) — controlli Python/Node/statici, .NET e Notepad reale `NOT_RUN`. Risultati storici delle Alpha precedenti rimangono documentati ma non vengono trasferiti.

# Evidenze di verifica — Ristretto

**Versione corrente Alpha 20.15:** [rapporto strutturato](ALPHA20_15_VALIDATION_STATUS.json) · [metodo](../docs/ALPHA20_15_ENTER_REGRESSION_FIX.md). Controlli Python, Node e statici eseguiti nella preparazione; **compilazione .NET, nuovi xUnit e prove desktop Windows NON ESEGUITI**. I risultati delle Alpha precedenti, riportati qui sotto, restano evidenze storiche e non sono stati rieseguiti sulla versione corrente.

## Archivio storico

**Alpha 20.7:** gate `ci` predisposto, non ancora eseguito su GitHub/Windows. Esiti locali: 26 Python, 32 Node e audit strutturale passati; nessuna nuova prova .NET o desktop. [Rapporto](../docs/ALPHA20_7_CI.md). I report generati dai gate rimangono in `.local-cache/gates` e non sono spediti nello ZIP.



**Nuova verifica Alpha 20.6:** i rapporti runtime del gate finiscono in `.local-cache/gates` e non sono inclusi nel pacchetto; consulta [la specifica dei gate](../docs/ALPHA20_6_QUALITY_GATES.md). I rapporti storici della 20.5 non sono prove della 20.6.

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
