# Alpha 20.18 — Diagnostica Notepad e istruzioni in italiano

**Data:** 14 novembre 2026. **Ambito:** script Python `qualify_notepad.py`, test e documentazione. Nessuna modifica a GUI Preview 9, hotkey, Core, SQLite o al percorso di Inserisci/Invio di produzione.

## Cosa stiamo facendo

Consolidiamo **R2.05**: la prova con un documento sintetico in Notepad deve produrre messaggi comprensibili in italiano e distinguere problemi di utilizzo da differenze effettive del testo salvato. Non leggiamo documenti personali, non intercettiamo tastiere e non inviamo Ctrl+V dal verificatore.

## Modifiche

1. **Un solo classificatore** alimenta `status` e `verify`, per evitare diagnosi discordanti sulla stessa sessione.
2. Un documento identico alla base rimane `NOT_RUN`, senza fingere che il picker non funzioni. Un documento corretto genera `PASS`. I documenti modificati non corrispondenti generano `FAIL` con codice diagnostico specifico.
3. Possibili diagnosi: inserimento doppio, inserimento in posizione errata, intestazione modificata, snippet mancante/incompleto, riga finale aggiunta, Unicode NFC/NFD alterato, file non UTF-8.
4. **Messaggi umani in italiano** nei comandi `prepare`, `status`, `verify`. Le parole tecniche `PASS`, `FAIL`, `NOT_RUN` e i nomi dei sottocomandi sono mantenuti stabili per compatibilità con script e ricevute esistenti.
5. `prepare --open` crea e mostra sempre la sessione, anche se Notepad non può essere avviato. Il token non viene perso; l'avvio fallito è distinto dalla preparazione riuscita.
6. Le ricevute contengono solo codici, identificativi di sessione e metadati: mai il contenuto Unicode salvato.

## Procedura Windows dettagliata

Prima di iniziare: estrarre lo ZIP in una cartella nuova e **chiudere altre istanze di Ristretto**, affinché la scorciatoia globale non sia già occupata. Aprire PowerShell nella cartella `Ristretto`. Non usare documenti personali.

1. **Preparare il documento sintetico** (viene tentata l'apertura in Blocco note):

   ```powershell
   .\scripts\validate-picker-notepad.cmd prepare --open
   ```

   Annotare il token di 32 caratteri stampato alla voce **Sessione di prova**. La preparazione non inserisce testo e non crea un profilo personale.

2. **Preparare il profilo SQLite separato** mediante il servizio .NET reale:

   ```powershell
   .\scripts\seed-picker-notepad.cmd TOKEN
   ```

   Sostituire `TOKEN` col valore effettivo; aspettarsi la stampa del percorso `profile` e del trigger `;ristretto-test`.

3. **Controllare** che lo stato del profilo sia `READY` e quello del documento `UNCHANGED`:

   ```powershell
   .\scripts\validate-picker-notepad.cmd status --session TOKEN
   ```

   `UNCHANGED` prima della prova è corretto: non significa errore.

4. **Avviare Ristretto con il profilo sintetico**, lasciando aperta questa console finché non si chiude l'applicazione:

   ```powershell
   .\scripts\validate-picker-notepad.cmd launch --session TOKEN
   ```

   Non usare una normale istanza di Ristretto collegata alla tua libreria personale.

5. Nel **Blocco note** verificare il nome del file sintetico della sessione; posizionare il cursore alla fine con `Ctrl+End`. Richiamare il picker con la scorciatoia configurata, cercare `;ristretto-test` e usare **Invio** oppure **Inserisci**. Non premere Copia. Attendere che il contenuto appaia nel documento, quindi salvare in Blocco note con `Ctrl+S`.

6. **Controllare e registrare il risultato** aprendo un'altra console, se quella della GUI è occupata:

   ```powershell
   .\scripts\validate-picker-notepad.cmd status --session TOKEN
   .\scripts\validate-picker-notepad.cmd verify --session TOKEN
   ```

   `verify` esce con codice **0** per `PASS`, **1** per `FAIL` e **3** per `NOT_RUN`. Anche `FAIL` è riferito soltanto al **file sintetico salvato**: leggere la descrizione italiana del codice e **preparare una nuova sessione** prima di ripetere l'inserimento, per evitare un doppio incolla sullo stesso documento.

## Interpretazione dei risultati

| Stato | Significato | Come procedere |
| --- | --- | --- |
| `NOT_RUN` | Il documento non esiste o è ancora quello originale | Eseguire il passaggio manuale e salvare con `Ctrl+S` |
| `PASS` | I byte del file salvato rappresentano esattamente il testo Unicode atteso (solo CRLF/LF equivalenti) | Conservare la ricevuta, ma non attribuirle la prova della provenienza dell'input |
| `FAIL` / `snippet_inserted_twice` | Testo presente due volte | Creare una nuova sessione e premere Invio una volta sola |
| `FAIL` / `snippet_inserted_at_unexpected_position` | Testo presente nel punto sbagliato | Ricontrollare `Ctrl+End` e la posizione del cursore |
| `FAIL` / `unicode_normalization_changed` | Unicode NFC/NFD diverso | Non normalizzare né modificare la fixture |
| `FAIL` / `synthetic_header_changed` | Modificata la riga iniziale della prova | Ripartire con un documento sintetico nuovo |
| `FAIL` / `extra_trailing_newline` | Aggiunto un Invio dopo lo snippet | Non aggiungere caratteri dopo Inserisci |
| `FAIL` / `snippet_missing_or_incomplete` | Modifica parziale o non pertinente | Controllare il profilo usato e il salvataggio del documento |

La ricevuta non contiene il testo e **non dimostra** la provenienza dell'inserimento, il comportamento su altri editor o l'espansione automatica globale. La diagnosi si basa esclusivamente sul file salvato, non su un ascoltatore di tastiera.

## Evidenze e cosa resta

- Regressioni Python del qualificatore ampliate con **12 casi nuovi** (20 precedenti → 32): classificazione, coerenza `status/verify`, uscita prudente e recupero del token dopo errore di avvio.
- Test di compilazione e xUnit .NET Alpha 20.18: **NOT_RUN** qui (SDK .NET non disponibile).
- Prova interattiva Notepad Alpha 20.18: **NOT_RUN** qui.
- Solo una prova Windows salvata e confermata può contribuire alla chiusura di R2; non confondere un controllo statico con una prova desktop.

**Obiettivo finale:** prima il picker manuale qualificato, poi un adapter di espansione automatica per un editor con garanzie dimostrate, quindi le altre piattaforme.

**Prossimo lavoro:** una volta disponibili le evidenze Windows del picker, chiudere R2.05/R2.07; nel frattempo si può avanzare sul percorso R3 di VS Code in host reale senza toccare il flusso Invio/Inserisci già ripristinato.
