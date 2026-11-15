# Ristretto

**Versione sorgenti: 3.9.0-alpha.20.18** · 14 novembre 2026 · C#/.NET 10, Avalonia, SQLite.

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

## Cosa stiamo facendo ora — Alpha 20.18

Stiamo qualificando **R2.05, inserimento manuale Unicode in Notepad**. Lo strumento di prova ora fornisce messaggi in italiano, distingue diversi errori reali del documento (inserimento doppio, posizione, normalizzazione Unicode) e non perde la sessione quando Blocco note non si apre.

**La baseline Windows confermata dall'utente è l'Alpha 20.15** (compilazione, test e inserimento tramite Invio dal picker). Per Alpha 20.18 gli xUnit .NET e la prova desktop restano `NOT_RUN`; i controlli Python/Node della consegna sono riepilogati nel [rapporto corrente](docs/ALPHA20_18_NOTEPAD_DIAGNOSTICS.md).

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

**Ora:** picker manuale funzionante nella baseline testata; diagnostica Notepad e regressioni ampliate in Alpha 20.18.

**Obiettivo finale:** espansione automatica Unicode affidabile, inizialmente in un editor qualificato e in seguito sulle piattaforme supportate.

**Prossimi passi:** completare i test desktop R2, qualificare l'adapter VS Code in un host reale (R3), preparare la prima distribuzione Windows (R4) e poi sviluppare il primo automatismo sicuro (R5–R6).

Documentazione autorevole: [stato corrente](docs/CURRENT_STATE.md) · [roadmap unica](docs/ROADMAP.md) · [istruzioni di compilazione](docs/BUILD_AND_TEST.md) · [handoff](docs/HANDOFF.md) · [indice tecnico](docs/INDEX.md) · [cronologia](docs/CHANGELOG.md).
