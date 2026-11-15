# Alpha 20.14 — Concorrenza tra annullamento e invio Win32

**Versione:** `3.9.0-alpha.20.14` · **Area:** R2.02–R2.05 · **Data:** 14 novembre 2026

## Cosa stiamo facendo

Rendere esplicito il limite tra una richiesta **ancora annullabile** e il suo
**primo tentativo di invio**. Il picker già annullava le operazioni ai confini
asincroni, ma nell'Alpha 20.13 rimaneva una finestra fra l'ultimo controllo
`IsCancelled` in `WindowsPickerInsertRequest` e il `SendInput` effettuato da
`WindowsPickerPasteService`.

## Modifica effettiva

- `WindowsPickerInsertRequest` conserva una decisione atomica monouso:
  `0 = pending`, `1 = cancelled`, `2 = dispatch reserved`.
- `Cancel()` marca la richiesta e tenta di vincere questa decisione con
  `Interlocked.CompareExchange`. Non può revocare un dispatch già autorizzato.
- Dopo i controlli finali di finestra, PID, focus, campo, modificatori e
  sequenza clipboard, il servizio invoca la decisione di autorizzazione
  **immediatamente prima** di `SendInput`.
- Se l'annullamento ha vinto, l'esito è `Rejected/request_cancelled`, con
  **zero** invii di scorciatoia. Se vince l'invio, un annullamento successivo
  non annulla gli effetti: `SubmittedUnverified` oppure `PartialOrUnknown`.
- Il servizio mantiene l'API pubblica precedente `Paste(target, sequence)`;
  la nuova sovraccarica `internal` è riservata alla richiesta monouso. Il
  percorso `App.InsertFromPickerAsync` resta collegato alla stessa richiesta.
- Il controllo `PickerSessionController.IsCurrent(generation, request)` continua
  a impedire che l'esito tardivo di una richiesta precedente modifichi la nuova
  sessione del picker.

## Casi aggiunti

In `WindowsPickerPasteTests.cs` sono presenti cinque esecuzioni di regressione
(Fact, Theory 2 casi) e una verifica aggiuntiva del motivo di rifiuto; in
`PickerSessionControllerTests.cs` è presente una regressione d'integrazione
per il cambio di sessione durante il preflight finale.

I test verificano: annullamento durante il controllo dei modificatori, durante
l'ultimo controllo della clipboard, apertura di una nuova sessione mentre il
vecchio inserimento è in preparazione, e annullamento mentre `SendPasteShortcut`
è già in corso. Usano soltanto un adapter simulato: **non inviano tasti veri**.

## Cosa NON garantisce

- La decisione atomica ordina annullamento e tentativo di invio nel processo.
  **Non blocca il sistema operativo**: il focus può ancora cambiare dopo il
  controllo finale, o la destinazione può non accettare il comando.
- `SendInput` può riportare input parziale; non è la prova di un incolla nel
  documento. In caso di incertezza non viene tentato un retry.
- `SetTextAsync` già iniziato non può essere annullato in modo affidabile;
  gli appunti possono rimanere modificati anche con richiesta annullata.
- Niente letture di documenti esterni, espansione automatica, monitoraggio
  globale dei tasti o prova generale di compatibilità con Notepad/Word/browser.

## Obiettivo e cosa resta

**Obiettivo R2:** qualificare il picker per un flusso manuale affidabile
su Windows, usando prima un controllo sintetico e poi Notepad su file di prova.

1. Compilare `scripts\validate.cmd` ed eseguire `scripts\validate-picker.cmd`.
2. Eseguire `scripts\validate-native.cmd` e il laboratorio interattivo
   `scripts\validate-picker-desktop.cmd` su un Windows desktop.
3. Completare la matrice R2 in Notepad (contenuto salvato, focus, clipboard,
   modificatori, riaperture). Nessun risultato sintetico equivale a una prova
   desktop in un editor arbitrario.
4. Solo dopo la qualificazione manuale scegliere un adapter per il primo
   automatismo e completare i successivi obiettivi R3–R6.

## Evidenze

Consulta [il report strutturato](../reports/ALPHA20_14_VALIDATION_STATUS.json).
Le verifiche Python/Node/statiche possono essere eseguite qui. Compilazione
.NET, nuovi xUnit e prove Windows restano **NOT_RUN** finché non vengono
eseguiti sul sistema appropriato. GUI Preview 9, hotkey, schema SQLite e
comandi Copia/Inserisci/Invio invariati.
