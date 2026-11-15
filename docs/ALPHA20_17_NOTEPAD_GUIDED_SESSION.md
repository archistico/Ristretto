# Alpha 20.17 — R2.05: guida diagnostica per la prova Notepad

**Data:** 14 novembre 2026. **Ambito:** esclusivamente strumenti di qualificazione e documentazione. **Non cambia** il picker, l'invio, la hotkey, Avalonia o lo schema SQLite.

## Problema osservato

Durante una prova reale sono stati eseguiti `prepare --open`, `seed-picker-notepad.cmd TOKEN` e subito `verify --session TOKEN`. Il qualificatore ha restituito `NOT_RUN | file_unchanged_no_saved_insertion`.

Questo risultato è atteso quando il documento sintetico non è stato modificato **e salvato**. `prepare` crea il file, `seed` crea il database di prova, ma nessuno di questi comandi avvia Ristretto con il profilo creato o inserisce testo in Notepad. Il risultato non dimostra un malfunzionamento di Invio/Inserisci e non equivale a un test fallito.

## Procedura guidata Windows

Eseguire i comandi dalla radice `Ristretto/`, **in una cartella estratta nuova**, senza usare documenti o profili personali.

1. Preparare una sessione e prendere nota del token:

   ```bat
   scripts\validate-picker-notepad.cmd prepare --open
   ```

2. Preparare il database SQLite di quella sessione, senza sovrascriverlo:

   ```bat
   scripts\seed-picker-notepad.cmd TOKEN
   ```

3. Controllare lo stato del profilo/documento **senza creare nuove ricevute**:

   ```bat
   scripts\validate-picker-notepad.cmd status --session TOKEN
   ```

   `Synthetic SQLite profile: READY` dimostra che **in quel database isolato** esiste uno snippet attivo `;ristretto-test` col contenuto della fixture. `Synthetic saved document: UNCHANGED` significa che il file non è ancora stato modificato e salvato, **non** che Ristretto abbia fallito l'inserimento.

4. **Chiudere le altre istanze di Ristretto** se occupano la hotkey globale. Avviare la GUI *esclusivamente col profilo della sessione*:

   ```bat
   scripts\validate-picker-notepad.cmd launch --session TOKEN
   ```

   Il comando avvia l'app con `--data-dir` impostato alla sola cartella `profile` della sessione; non imposta il profilo predefinito. Lasciare aperta la GUI. **Il comando resta in esecuzione fino alla chiusura dell'app.** Se si preferisce usare una seconda console:

   ```bat
   dotnet run -c Release --project src\TextExpander.Desktop\TextExpander.Desktop.csproj -- --data-dir ".local-cache\notepad-qualification\TOKEN\profile"
   ```

5. In Notepad verificare di avere aperto **il file sintetico creato da questa sessione**; non usare documenti personali. Posizionarsi alla fine con `Ctrl+End`, senza aggiungere spazi o righe. Richiamare il picker con la hotkey configurata, cercare `;ristretto-test` e premere **Invio** o **Inserisci**, **non** Copia. Verificare visivamente l'inserimento e **salvare in Notepad con Ctrl+S**.

6. Controllare il file:

   ```bat
   scripts\validate-picker-notepad.cmd status --session TOKEN
   scripts\validate-picker-notepad.cmd verify --session TOKEN
   ```

## Diagnostica

| Stato | Significato | Azione |
| --- | --- | --- |
| `profile=NOT_CREATED` | Seeder non eseguito | Eseguire `seed-picker-notepad.cmd TOKEN` |
| `profile=INVALID` | Profilo SQLite mancante, illeggibile o diverso dalla fixture | Creare una **nuova** sessione; non sovrascrivere il profilo esistente |
| `profile=READY`, `document=UNCHANGED` | Profilo sintetico pronto; file salvato identico alla preparazione | Avviare GUI col profilo, usare Inserisci/Invio nel documento, salvare |
| `document=MATCHES_EXPECTED` | Contenuto persistito uguale alla fixture | Eseguire `verify` per ottenere ricevuta `PASS` |
| `document=CHANGED_DIFFERENT` | Documento modificato ma non identico alla fixture | Rivedere la prova e il contenuto senza aggiunte manuali |
| `document=MISSING` | File sintetico non disponibile | Nuova sessione; evitare di ricostruire manualmente il file |

La verifica dello stato legge **solo** la fixture e il database sintetico in modalità SQLite read-only; non registra il contenuto dei campi nei log. `launch` rifiuta profili assenti/non corrispondenti, token non validi e sistemi diversi da Windows. Il controllo non osserva il processo Notepad né prova la provenienza degli eventi di input.

**Sicurezza delle evidenze:** `verify` continua a restituire `NOT_RUN`, `PASS` o `FAIL` basandosi unicamente sul file salvato, con una ricevuta JSON priva di contenuto; non attiva alcuna espansione globale. Il salvataggio potrebbe anche essere stato prodotto mediante un'altra azione: `PASS` non equivale alla verifica crittografica della provenienza. In caso di `NOT_RUN` dopo un tentativo visibile, verificare **documento aperto**, **profilo selezionato**, **pulsante Inserisci**, **focus** e **Ctrl+S**, prima di aprire una segnalazione di bug.

## Verifiche e confini

I test Python esercitano stati, controlli SQLite read-only, sessioni alterate e costruzione degli argomenti di avvio Windows tramite un runner simulato. Non avviano la GUI né Notepad. Per Alpha 20.17 compilazione .NET, xUnit e test interattivi Windows restano `NOT_RUN`; l'ultima baseline desktop confermata dall'utente è la Alpha 20.15 (compilazione, test, Invio in Notepad).

**Prossimo passo dopo la prova:** qualificare su Windows il percorso manuale Unicode multilinea, completare casi negativi e chiudere R2 solo con evidenze desktop. In seguito, adapter host reale VS Code e primo automatismo qualificato R5/R6.
