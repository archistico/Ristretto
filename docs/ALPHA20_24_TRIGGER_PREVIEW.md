# Ristretto 3.9 — Alpha 20.24: anteprima di trigger in un Edit esterno

## Cosa stiamo facendo

R5 prosegue con una **simulazione in sola lettura** che collega le modifiche Unicode osservate tramite WinEvent al motore di attivazione dei trigger. La nuova modalità cerca esclusivamente il trigger **sintetico** `;firma`, seguito da uno spazio, all'interno di un controllo Win32 `Edit` classico di prova.

È volutamente un'**anteprima non attendibile**: una notifica `EVENT_OBJECT_VALUECHANGE` e una differenza testuale non provano che il testo sia stato digitato. Anche incollamenti, IME e modifiche programmatiche possono produrre una corrispondenza. **Non viene mai autorizzata o eseguita una sostituzione**, né viene letto SQLite personale.

## Componenti

- `ExternalClassicEditTriggerPreviewSession` (`Infrastructure`): usa la sessione di confronto Unicode dell'Alpha 20.23 e una simulazione privata di `TriggerActivationSession`. Dopo cambi di contesto, errori di lettura, selezioni, modifiche incompatibili e invalidazioni azzera il riconoscimento; non interpreta retroattivamente il testo precedente all'avvio.
- `ExternalClassicEditTriggerPreviewStep`: restituisce solo uno stato, mai testo o snippet. `CanAuthorizeExpansion` rimane sempre `false`.
- `ExternalClassicEditLiveCommand` (`WindowsOutputLab`): la modalità anteprima conta i trigger potenziali osservati, senza mostrare contenuti, handle, PID o posizioni del cursore.
- `scripts/observe-classic-edit-preview.cmd`: comando Windows facoltativo, con intervallo di 5–30 secondi.
- `ExternalClassicEditTriggerPreviewSessionTests`: regressioni C# su confini, Unicode, contesti e non autorizzazione.

## Come eseguire la prova su Windows (facoltativa)

**Prima:** la build/test della Alpha 20.24 deve superare `scripts\validate.cmd`. Per la prova serve **un programma esterno** con un controllo **Win32 Edit classico**, contenente soltanto testo di prova. Notepad moderno, browser, Word e RichEdit potrebbero non essere qualificabili. La prova legge temporaneamente fino a 4096 unità UTF-16 del controllo in RAM; non usarla con documenti personali, password o informazioni riservate.

1. Apri PowerShell nella cartella `Ristretto` estratta, con il file `Ristretto.sln`.
2. Prepara un campo `Edit` classico vuoto in un'altra applicazione di prova. **Non digitare ancora**.
3. Esegui:

   ```powershell
   .\scripts\observe-classic-edit-preview.cmd 15
   ```

4. Durante i primi **5 secondi**, porta il focus nel campo sintetico. Quando la console indica che l'osservazione è attiva, digita **`;firma` e uno spazio** (non occorre premere Invio).
5. Alla fine, leggi soltanto i contatori. Se il laboratorio ha osservato un trigger completo, verrà mostrato un `PASS` riferito alla **simulazione**. Se non è arrivata una sequenza osservabile, apparirà `NOT_RUN`. In nessun caso Ristretto cancella `;firma` o incolla uno snippet.

**Importante:** anche un incollamento di `;firma ` potrebbe comparire nei contatori come potenziale trigger. Il risultato **non certifica la provenienza** e non abilita l'espansione automatica in editor esterni.

## Obiettivo finale e cosa resta

**Obiettivo finale:** digitare un trigger Unicode e ottenere automaticamente lo snippet in un editor compatibile, senza aprire il picker.

**Ancora da sviluppare:** origine verificabile dell'input, gestione IME e appunti nei destinatari esterni, identificazione atomica di documento/focus/revisione, sostituzione condizionale, controllo dell'esito e attivazione opt-in. Il picker funzionante rimane il fallback per le applicazioni non qualificabili.

**Stato verifiche:** Alpha 20.20 validata dall'utente. Le novità 20.22–20.24 e i test nativi Windows sono ancora da confermare. I soli controlli statici e Node eseguiti sull'archivio non sostituiscono la compilazione .NET e gli xUnit.
