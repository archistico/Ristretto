# Alpha 20.15 — Regressione Invio nel picker (R2.02 / R2.03)

**Segnalazione utente, 14 novembre 2026:** la Alpha 20.14 compila e supera i test sul PC Windows, ma il gesto **apri picker → Invio → inserimento in Notepad** non produce più il testo, pur funzionando nelle versioni precedenti.

## Cosa abbiamo confrontato

Il confronto dei sorgenti Alpha 20.13 e 20.14 mostra che `PickerWindow.axaml.cs`, `PickerViewModel` e `App.axaml.cs` erano identici: nella 20.14 è cambiata soltanto l'autorizzazione finale al dispatch in `WindowsPickerInsertRequest` e `WindowsPickerPasteService`. La causa precisa del mancato inserimento nel desktop utente non è confermata senza riprodurre la prova. Non abbiamo rimosso il controllo di annullamento atomico del 20.14.

## Correzioni e protezioni Alpha 20.15

1. **Invio senza dipendenza dalla velocità di caricamento:** il gesto viene acquisito a `KeyDown` e completato a `KeyUp`. Se la ricerca è ancora in caricamento, il gesto può restare in sospeso. Il risultato viene usato solo dopo che il caricamento è completato e solo se il picker ha ancora il focus, il filtro è identico e `CanInsert` è vero. `Esc`, perdita focus, chiusura e nuova sessione azzerano il gesto. Il rilascio senza pressione e l'autoripetizione non creano invii duplicati.
2. **Sessione di inserimento protetta:** `PickerSessionController.TryBeginInsertion` riserva la sessione PRIMA di nascondere il picker. Una callback tardiva della hotkey non ricattura l'editor mentre il paste è in corso: `Open` restituisce `InsertionInProgress` e non annulla la richiesta. Un annullamento *esplicito* conserva la priorità e ferma `SendInput` se è intervenuto prima della prenotazione nativa. Una vecchia completion non può cancellare lo stato della sessione nuova.
3. **Diagnosi limitata e priva di contenuti:** quando il picker deve ripresentarsi dopo un rifiuto nativo, il messaggio mostra un codice tecnico fisso (per esempio `[request_cancelled]` o `[focus_changed]`). Non vengono mostrati testo, processi, appunti né identificativi delle finestre.
4. **Regressioni xUnit:** gesto Enter, risultati inizialmente in caricamento, query/focus mutati, pressione prolungata, richiamo hotkey durante il paste, annullamento esplicito, completamento di una sessione obsoleta.

## Cosa verificare su Windows

- Aprire un **Notepad non elevato**, posizionare il cursore e aprire il picker con `Ctrl+Shift+Space`; quando è visibile un risultato, premere **Invio** una sola volta. Il testo dello snippet dovrebbe comparire in Notepad. Ripetere premendo **Inserisci**: le due azioni utilizzano lo stesso servizio di paste, ma Invio sceglie il primo risultato filtrato e il pulsante la riga selezionata.
- Ripetere immediatamente dopo l'apertura, mentre il picker sta ancora caricando: se la query/focus non cambiano, il gesto in attesa deve essere completato una sola volta.
- Se il picker rimane/torna visibile, annotare **soltanto** il codice finale fra parentesi quadre nel messaggio di stato. Non inviare contenuti degli appunti o file personali. Un errore con `[focus_changed]` indica un problema nel controllo del destinatario, non nella tastiera.
- Verificare che `Esc`, cambio finestra o hotkey ripetuta non causino un inserimento tardivo o in destinatario diverso.
- Eseguire `scripts\validate.cmd`, `scripts\validate-picker.cmd` e, in sessione desktop, `scripts\validate-picker-desktop.cmd`. La suite .NET/Windows **non è stata eseguita in questo ambiente** per assenza del SDK .NET 10 e desktop Windows.

## Dove siamo / obiettivo / cosa manca

**Stato:** codice corretto e controlli statici/Node/Python eseguiti; validazione Windows di Alpha 20.15 aperta. La Alpha 20.14 è stata dichiarata compilabile e con test passati dall'utente, ma aveva la regressione funzionale qui trattata.

**Obiettivo:** ripristinare apertura picker + Invio in Notepad, mantenendo il destinatario corretto e il percorso di annullamento sicuro. Successivamente qualificare inserimento effettivo in Notepad e nel laboratorio, poi un automatismo limitato per editor supportati.

**Prossimo lavoro:** confermare la correzione sul PC dell'utente; continuare R2.05, la matrice Notepad/Unicode e le qualificazioni native, senza confondere `SendInput` riuscito con testo incollato verificato.

## Conferma successiva (utente, 14 novembre 2026)

La Alpha 20.15 **compila, supera i test e ripristina l’inserimento con Invio nel picker in Notepad**, secondo la prova effettuata dall’utente. Questa conferma costituisce la baseline per l’Alpha 20.16; non qualifica ancora le altre combinazioni Unicode/editor della matrice R2.
