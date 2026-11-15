# Alpha 20.21 — osservazione Unicode esterna controllata (R5)

## Cosa stiamo facendo

Per la prima volta il laboratorio Ristretto può **tentare di confrontare due stati di testo** appartenenti a un controllo Win32 `Edit` classico in un processo Windows *esterno*. La prova è avviata **esclusivamente su richiesta dell'utente** e non fa parte del normale avvio di Ristretto.

Questa funzione serve a capire quali evidenze sono disponibili fuori dal nostro processo, senza abilitare alcuna espansione automatica e senza installare hook globali.

## Modifiche implementate

- `ExternalClassicEditTextDeltaPolicy` (`Application`): confronta due fotografie temporanee, verifica testo Unicode valido, posizione del cursore, continuità prima/dopo l'inserimento, limiti di lunghezza, confini dei grafemi e identità del destinatario fornita dal chiamante. Restituisce soltanto una classificazione, mai il testo.
- `WindowsExternalClassicEditTextReader` (`Infrastructure`): usa `WM_GETTEXTLENGTH`, `WM_GETTEXT` e `EM_GETSEL` con timeout di 150 ms per tentare una lettura *limitata* di un `Edit` classico. Ricontrolla focus e processo e non legge campi password, sola lettura o non qualificati.
- `ExternalClassicEditTextCommand` (`WindowsOutputLab`): esegue una prova esplicita in due momenti, senza salvare o mostrare i caratteri acquisiti.
- `scripts/observe-classic-edit-text.cmd`: avviatore Windows in italiano.
- `ExternalClassicEditTextDeltaTests`: regressioni C# su emoji, Unicode combinato, modifiche non contigue, caret, selezioni, target mutato, testo troppo lungo e diagnostica senza contenuto.

## Privacy e limiti di sicurezza

**Attenzione:** la sonda legge realmente, anche se solo in RAM e solo dopo avvio esplicito, fino a **4096 unità UTF-16 del campo esterno attivo**. Usarla esclusivamente con un documento sintetico, mai con password, conversazioni o informazioni riservate. Non vengono emessi log del testo, titoli finestra, PID, HWND o offset, né creati file di report.

Il lettore è limitato ai controlli con classe Windows `Edit` riconosciuti dai controlli preesistenti. **Notepad moderno, browser, Word, RichEdit e controlli personalizzati possono essere rifiutati**. Non si forza un fallback per aggirare il rifiuto. I messaggi Win32 possono fallire per restrizioni di integrità del processo, UIPI o modifiche concorrenti.

Un risultato `CandidateInsertion` dimostra soltanto che il testo del controllo è cambiato in modo compatibile con un inserimento al cursore tra due osservazioni. **Non dimostra digitazione umana**: lo stesso risultato può derivare da incollamento, IME o codice. **Non autorizza una sostituzione**, non passa testo a `OwnedCommittedInputBridge` e non alimenta direttamente `TriggerActivationSession`.

## Come provare la nuova funzione su Windows

Questa verifica è *facoltativa*. Per prima cosa esegui la compilazione ordinaria, poi la prova nativa, quando puoi.

1. **Prepara un campo di prova vuoto.** Usa un'applicazione di test con un controllo Windows `Edit` classico, senza dati personali. Non usare un documento di lavoro: il programma legge temporaneamente il contenuto del campo. Notepad moderno potrebbe non essere compatibile con questa sonda.
2. **Apri PowerShell** nella cartella `Ristretto` contenente `Ristretto.sln` e avvia:

   ```powershell
   .\scripts\observe-classic-edit-text.cmd
   ```

3. **Nei primi 5 secondi** passa al controllo `Edit` nella seconda applicazione e porta il cursore in posizione di scrittura. Non digitare ancora.
4. **Nei successivi 5 secondi**, quando il terminale lo indica, digita per esempio `;firma` oppure `caffè ☕` nel campo di test. Mantieni la stessa finestra e non spostare il cursore.
5. **Leggi l'esito nel terminale**. `CandidateInsertion` segnala una differenza Unicode coerente con l'inserimento. `NOT_RUN`, `NotQualified` o `FocusChanged` indicano che il test non è riuscito a raccogliere informazioni sufficienti. Il comando non invia tasti e non modifica il documento.

Il valore restituito è **0** soltanto per un candidato osservato; **3** per osservazioni assenti/non qualificate. Questo è un risultato di ricerca tecnica, non una certificazione di espansione automatica.

## Verifiche e stato

- **Baseline 20.20:** compilazione e test superati sul PC dell'utente, come confermato nella conversazione.
- **Alpha 20.21:** codice e nuovi xUnit preparati. Se il presente ambiente non dispone di .NET 10 e desktop Windows, build e test nativi restano `NOT_RUN`.
- **Picker, hotkey, GUI e SQLite:** nessuna modifica.

## Obiettivo finale e cosa resta

Vogliamo **digitare un trigger Unicode in un editor compatibile e ottenere lo snippet senza aprire il picker**. Per arrivarci serve ancora una sorgente attendibile di testo committato, capace di stabilire la provenienza dell'evento, e un adapter che garantisca una sostituzione condizionale e verificabile nel destinatario.

La Alpha 20.21 non risolve questi requisiti. Il prossimo lavoro deve analizzare le condizioni di input e di consenso/abilitazione per applicazione, senza abilitare un hook globale sulla sola base di questi snapshot.
