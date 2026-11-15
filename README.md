# Ristretto

**Versione sorgenti: 3.9.0-alpha.20.19.3** · 15 novembre 2026 · C#/.NET 10, Avalonia, SQLite.

Ristretto è un **text expander open source multipiattaforma in sviluppo**. La GUI Preview 9 rimane congelata. Questa consegna non modifica il comportamento del picker confermato nell'Alpha 20.15.

## Cosa è già disponibile

- **Gestione libreria:** gruppi e snippet salvati in SQLite, import/export JSON versionato, import Beeftext/Espanso, backup.
- **Picker Windows:** ricerca Unicode, **Copia**, **Inserisci** e **Invio** per il primo risultato filtrato.
- **Hotkey globale Windows:** `Ctrl+Shift+Space` come valore iniziale, configurabile/disattivabile; utilizza `RegisterHotKey`.
- **Profili isolati:** `--data-dir` permette prove con un database differente da quello personale.
- **Adapter VS Code sperimentale:** espansione **su comando esplicito** e da export JSON, con suite Node; prove nel vero host non ancora concluse.

**Non è ancora presente l'espansione automatica globale dei trigger in Notepad/Word/browser.** Anche se il motore riconosce Unicode e delimitatori, non è collegato a un adapter globale qualificato. Su Linux/macOS mancano ancora gli adapter per hotkey e output.

### Come funziona Inserisci

Il picker conserva il destinatario originale, controlla focus/processo/controllo, scrive lo snippet negli appunti e chiede un singolo `Ctrl+V`. Gli appunti rimangono contenenti lo snippet. Un invio di tasti non prova che l'editor abbia ricevuto il testo: non ci sono ritentativi automatici quando l'esito è incerto. Il trigger eventualmente già digitato nell'editor non viene cancellato.

## Cosa stiamo facendo ora — Alpha 20.19.3 (diagnostica CLI Windows)

**Corretto il blocco `startup_cli` del gate `CORE`.** L'utente ha confermato su Windows `source_audit`, regressioni Python, controlli statici, restore, build della solution, xUnit e quattro build di laboratorio **PASS** nella Alpha 20.19.2. Il test CLI falliva invece in Python: la lettura forzata UTF-8 di `stderr` non tollerava messaggi diagnostici di errore codificati con una codepage Windows (`UnicodeDecodeError`), causando anche un `AttributeError` secondario.

Nella Alpha 20.19.3 lo strumento legge i canali del processo figlio **come byte**. Per un errore atteso verifica codice e presenza di `stderr` senza decodificarne i contenuti; per un comando riuscito richiede ancora UTF-8 esatto in `stdout`, fondamentale per percorsi e JSON Unicode. Nove regressioni riproducono realmente subprocess con byte CP1252/CP850 e controllano che i casi non validi restino errori.

**L'applicazione C#, il picker e il motore R5 sono invariati.** I test Windows della nuova versione, compreso `startup_cli`, richiedono ancora la tua conferma. [Dettagli](docs/ALPHA20_19_3_STARTUP_CLI_ENCODING.md).

## Funzionalità R5 conservata — Alpha 20.19

**R5 — preparazione del primo automatismo Windows.** Abbiamo aggiunto una barriera di continuità che azzera i trigger temporanei quando cambia finestra o controllo attivo, e un osservatore Win32 **facoltativo, di sola lettura, limitato a 3–30 secondi**. Quest'ultimo riceve soltanto notifiche di foreground/focus: **non legge tasti, caratteri, documenti o appunti**, non modifica altri programmi e non funziona come text expander automatico.

La GUI, la hotkey e il percorso **Copia / Inserisci / Invio** sono identici alla Alpha 20.18.1; l'API Avalonia `TryGetTextAsync` resta conservata. La prova Notepad dell'Alpha 20.18 ha ottenuto `PASS` sul testo Unicode salvato e l'utente conferma di aver usato il picker; il risultato non certifica altri editor né l'automatismo.

**Baseline Windows confermata:** Alpha 20.15 compilata/testata con Invio funzionante e prova Notepad Alpha 20.18 positiva. **Alpha 20.19.2:** verifica PowerShell reale, compilazione .NET, nuovi test xUnit e osservazione nativa **NOT_RUN** fino a verifica. [Rapporto della milestone](docs/ALPHA20_19_FOCUS_BOUNDARY.md).

### Avvio dei test

Da PowerShell, nella cartella `Ristretto` estratta:

```powershell
.\scripts\validate.cmd
.\scripts\validate-picker.cmd
```

Per la procedura in Blocco note, che usa solo dati sintetici e **richiede un inserimento volontario**:

```powershell
.\scripts\validate-picker-notepad.cmd prepare --open
```

Il comando stampa il token e tutti i passaggi in italiano. La guida completa è [qui](docs/ALPHA20_18_NOTEPAD_DIAGNOSTICS.md). Per pulire le directory di build senza toccare i dati utilizzare `scripts\clean-build.cmd -Preview` e, solo dopo aver controllato l'anteprima, `scripts\clean-build.cmd`.

## Dove siamo e cosa resta

**Ora:** corretto il blocco dello startup CLI su Windows; primo blocco R5 di continuità del focus invariato e test Windows della Alpha 20.19.1 aperti. Picker e prova Notepad Unicode confermati nella baseline precedente.

**Obiettivo finale:** espansione automatica Unicode affidabile, inizialmente in un editor qualificato e in seguito sulle piattaforme supportate.

**Prossimi passi:** verificare la nuova barriera focus su Windows, scegliere una sorgente realmente qualificabile di testo Unicode committato per R5 e solo allora collegare un adapter automatico R6. R2 e distribuzione R4 proseguono; VS Code R3 rimane facoltativo.

Documentazione autorevole: [stato corrente](docs/CURRENT_STATE.md) · [roadmap unica](docs/ROADMAP.md) · [istruzioni di compilazione](docs/BUILD_AND_TEST.md) · [handoff](docs/HANDOFF.md) · [indice tecnico](docs/INDEX.md) · [cronologia](docs/CHANGELOG.md).
