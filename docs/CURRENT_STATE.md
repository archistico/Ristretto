# Stato corrente — Ristretto 3.9.0-alpha.20.19.3

**Data:** 15 novembre 2026 · **Hotfix corrente:** [Alpha 20.19.3](ALPHA20_19_3_STARTUP_CLI_ENCODING.md) · **Piano unico:** [ROADMAP.md](ROADMAP.md) · **Storico:** [Alpha 20.19.1](ALPHA20_19_1_CRLF_REGRESSION_FIX.md).

## Cosa stiamo facendo

**Alpha 20.19.3 — correzione portabile del gate `startup_cli`.** L'utente ha confermato nella Alpha 20.19.2: audit sorgenti, regressioni Python, controlli statici, restore .NET, build solution, xUnit e build dei quattro tool **PASS**. Il gate si è fermato nel controllo CLI senza GUI, perché uno dei messaggi di errore su `stderr` conteneva un byte della codepage Windows non decodificabile come UTF-8. Il reader thread Python ha generato `UnicodeDecodeError` e in seguito `.strip()` su `None` ha causato `AttributeError`. Non è un errore di compilazione né del picker.

Lo script `check_startup_profile.py` cattura ora **byte grezzi**: per gli errori attesi controlla codice di uscita, assenza di stdout e presenza di stderr senza decodificare; per i successi richiede UTF-8 valido ed esatto in stdout, preservando l'identità dei percorsi giapponesi/emoji e dell'output JSON. Nove test di regressione su veri subprocess sintetici coprono CP1252, CP850, UTF-8 Unicode e violazioni del contratto. [Report](ALPHA20_19_3_STARTUP_CLI_ENCODING.md).

**Sorgenti C#, GUI, hotkey, SQLite, picker e motore R5 invariati.** La compilazione .NET, gli xUnit e il `startup_cli` della **nuova** Alpha 20.19.3 restano `NOT_RUN` nell'ambiente di preparazione; i passaggi .NET su Windows della Alpha 20.19.2 sono evidenza dell'utente e non vengono trasferiti come test della 20.19.3.

**R5 — prima preparazione dell'espansione automatica Windows.** Il nuovo `ExternalFocusBoundarySession` (Application) invalida la memoria temporanea di riconoscimento quando cambia finestra, controllo attivo o la qualità dell'osservazione. Il nuovo esperimento `--observe-focus N` (laboratorio Windows) sottoscrive eventi WinEvent foreground/focus **soltanto su richiesta**, per massimo 30 secondi, senza osservare caratteri o modificare applicazioni. Non è collegato al picker né abilita l'automatismo.

## Funzionalità ed evidenze

| Area | Stato | Evidenza |
| --- | --- | --- |
| Snippet, gruppi, SQLite, import/export e backup | Implementati | Test storici, non rieseguiti in questa Alpha |
| Picker, ricerca Unicode, Copia, Inserisci, Invio | Implementati | Alpha 20.15 build/test e Invio confermati dall'utente |
| Hotkey globale configurabile Windows | Implementata | Matrice tray/conflitti ancora aperta |
| Inserimento Unicode in Notepad | Prima prova positiva | Alpha 20.18: file sintetico `MATCHES_EXPECTED` e `PASS`; uso del picker confermato dall'utente |
| Barriera cambio focus R5 | Implementata in Core/Application | Nuovi xUnit scritti, esecuzione .NET **NOT_RUN** |
| Osservatore WinEvent R5 | Esperimento opt-in, non in produzione | Windows desktop **NOT_RUN** |
| Testo committato in editor esterni | Non qualificato | Eventi focus/keycode non provano il testo inserito |
| Espansione automatica Windows | Non implementata globalmente | Nessuna autorizzazione alla sostituzione in editor arbitrari |
| VS Code | Adapter manuale sperimentale | 32 test Node eseguiti; host reale aperto |
| Linux/macOS | GUI/Core predisposti | Adapter di input/output nativi da sviluppare |

## Stato verifiche della versione corrente

I controlli Python, Node e statici di questa consegna sono riportati in [ALPHA20_19_2_VALIDATION_STATUS.json](../reports/ALPHA20_19_2_VALIDATION_STATUS.json). L’utente ha confermato 64 test Python superati con la 20.19.1; il gate PowerShell rifiutava erroneamente il formato versione prima di avviare .NET. La correzione è stata testata staticamente, non eseguita in PowerShell in questa sessione. Compilazione .NET, nuovi xUnit, prova WinEvent Windows e nuovo desktop picker sono **NOT_RUN** in questo ambiente. Non trasferire automaticamente i PASS delle versioni precedenti.

## Obiettivo e prossimi passi

**Obiettivo:** digitare un trigger Unicode in un'applicazione compatibile e ottenere lo snippet automaticamente, con verifica del destinatario, gestione IME e possibilità di disattivazione. Il picker rimane alternativa manuale.

**Prossimo lavoro:** R5.02–R5.04, identificare una **sorgente di testo effettivamente committato** con evidenze affidabili, non tradurre arbitrariamente eventi tastiera in caratteri; documentare le capacità che mancano e testare i cambi di focus. Successivamente R6 (adapter esplicito), R2 (ulteriori editor), R4 (packaging), R7–R8 (funzioni/OS).

Le decisioni e i task di dettaglio si trovano soltanto in [ROADMAP.md](ROADMAP.md); i rapporti delle vecchie Alpha rimangono storici in [INDEX.md](INDEX.md).
