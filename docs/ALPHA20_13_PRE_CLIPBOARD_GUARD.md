# Alpha 20.13 — Controlli prima di modificare gli appunti

**Versione:** `3.9.0-alpha.20.13` · **Area:** R2.03–R2.05 · **Data:** 14 novembre 2026

## Problema concreto

Nell’Alpha 20.12 `WindowsPickerInsertRequest.ExecuteAsync` chiamava
`prepareClipboard` **prima** di `settleFocus`. Quando il focus non tornava
all’editor (finestra chiusa, nuovo foreground, cambio controllo) il successivo
preflight impediva l’invio, ma gli appunti erano già stati modificati.
La sola protezione pre-invio non preservava quindi gli appunti in quei casi.

## Correzione nel percorso reale

1. Una richiesta rimane **monouso** e viene rifiutata se non è più valida.
2. `TryActivate` valida il destinatario prima di tentare il ripristino del foreground.
3. Il servizio legge la versione corrente della clipboard e attende il focus (`settleFocus`);
   se la richiesta viene annullata durante l’attesa, non scrive negli appunti.
4. `Preflight` controlla che esistano ancora root e child, che entrambi abbiano il PID
   catturato, che foreground e focus siano quelli previsti, e che editor e modificatori
   non risultino protetti/attivi. **Tutto prima di SetTextAsync**.
5. `ClipboardStillAt` rifiuta i cambi della sequenza osservati durante l’attesa;
   non sovrascrive gli appunti modificati da un altro processo in quel periodo.
6. Solo allora `prepareClipboard` scrive lo snippet e verifica contenuto esatto
   (con la sola normalizzazione CRLF/LF) e stabilità della sequenza.
7. `Paste` ripete il preflight e verifica la sequenza immediatamente prima di
   `SendInput`. L’esito resta `SubmittedUnverified`, `Rejected` oppure
   `PartialOrUnknown`. Nessun retry automatico.

**Garanzie limitate:** questi controlli non sono un lock sul focus o sulla clipboard.
Un cambiamento *dopo* il controllo finale e *prima* dell’input può ancora sfuggire.
Se `SetTextAsync` è stato già eseguito quando avviene un nuovo cambio focus,
la clipboard può restare modificata anche senza incolla. Non viene ripristinata
in modo automatico per non cancellare una modifica concorrente dell’utente.
L’invio di quattro eventi `SendInput` non prova la ricezione del testo.

## Test

Le nuove regressioni C# in `WindowsPickerPasteTests.cs` verificano, tramite
API Win32 fittizia e senza scrivere appunti reali:

- focus di un’altra app e cambio di controllo durante l’attesa;
- finestra figlia eliminata, PID cambiato, editor protetto, tasti modificatori;
- appunti cambiati prima della preparazione;
- perdita del focus o degli appunti **durante** la preparazione;
- annullamento e richiesta duplicata, ordine focus → clipboard → dispatch.

Gli script sono gli stessi: `scripts\validate.cmd`, `scripts\validate-picker.cmd`
e `scripts\validate-native.cmd` (quest’ultimo solo su Windows interattivo).
La prova Notepad sintetica è descritta in
[Alpha 20.12](ALPHA20_12_NOTEPAD_QUALIFICATION.md).

La compilazione C#/.NET, xUnit e l'inserimento reale su Windows non sono
eseguibili dall'ambiente Linux usato per questa consegna e vanno marcati `NOT_RUN`.
GUI Preview 9, hotkey, schema SQLite e automazione dei trigger non sono stati modificati.
