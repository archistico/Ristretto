# Alpha 20.23 — osservatore WinEvent per editor `Edit` esterni (R5)

## Cosa stiamo facendo

Passiamo dalla prova R5 con **due fotografie** di un campo `Edit` esterno a un osservatore **temporaneo e volontario** delle notifiche Windows `EVENT_OBJECT_VALUECHANGE`. È un incremento del laboratorio Windows, non dell'applicazione desktop.

Un controllo Win32 classico può comunicare che il proprio valore è cambiato tramite WinEvent. L'osservatore usa la notifica come **segnale** per acquisire una nuova fotografia, con le verifiche già presenti su focus, proprietà del controllo, processo, caret, dimensioni e Unicode. Il testo non viene stampato o salvato.

## Nuovi componenti

- `ExternalClassicEditLiveSession` (`Infrastructure`): conserva una sola fotografia precedente in memoria; dopo un cambio di controllo, errore di lettura o barriera focus riparte da una nuova baseline. `CandidateInsertion` **non** autorizza modifiche.
- `WindowsExternalClassicEditTextReader.CaptureForControl`: impone la corrispondenza con l'HWND notificato **prima** di leggere il contenuto.
- `ExternalClassicEditLiveCommand` (`WindowsOutputLab`): sottoscrive temporaneamente `EVENT_OBJECT_VALUECHANGE`, `EVENT_OBJECT_FOCUS` e `EVENT_SYSTEM_FOREGROUND` tramite `SetWinEventHook(..., WINEVENT_OUTOFCONTEXT | WINEVENT_SKIPOWNPROCESS)`.
- `scripts/observe-classic-edit-live.cmd`: avviatore Windows facoltativo; durata da 5 a 30 secondi.
- `ExternalClassicEditLiveSessionTests`: test C# per Unicode, cambio contesto, cancellazione, lettura fallita, selezione e non autorizzazione.
- Contratti `Ristretto.DevTools` per verificare che il nuovo percorso rimanga diagnostico e che i file richiesti siano inclusi.

## Privacy, consenso e limiti

**Importante:** durante la prova il laboratorio legge temporaneamente in RAM fino a **4096 unità UTF-16** del campo `Edit` attivo. Non usarlo con password, documenti personali, conversazioni o informazioni riservate; prepara soltanto testo sintetico. La classe `Edit` viene qualificata e i campi password/sola lettura sono esclusi. Non si usa `WH_KEYBOARD_LL`, non vengono intercettati tasti, non vengono salvati contenuti né inviati tasti o incollamenti. Le API possono fallire per policy Windows, UIPI e cambi concorrenti.

`EVENT_OBJECT_VALUECHANGE` **non viene necessariamente emesso da tutti gli editor**, nemmeno da tutti i controlli testuali. Inoltre una modifica plausibile può derivare da incollamento, IME o codice. I risultati contano **candidati**, non attestano digitazione umana, non alimentano `TriggerActivationSession` e **non abilitano R6**.

## Prova guidata su Windows — facoltativa

1. **Compilazione.** Estrai l'archivio in una cartella nuova. Apri PowerShell nella directory con `Ristretto.sln`, chiudi Ristretto e avvia ` .\scripts\validate.cmd `. Atteso: `GATE CORE: PASS` senza errori o warning nuovi. Questo non esegue la prova interattiva.
2. **Prepara il destinatario.** Apri un programma di prova dotato di un controllo Win32 `Edit` classico **vuoto**, diverso da Ristretto. Non utilizzare dati personali. Notepad moderno, browser e Word potrebbero essere incompatibili.
3. **Avvia l'osservatore.** Nel terminale esegui:

   ```powershell
   .\scripts\observe-classic-edit-live.cmd 15
   ```

   Il comando prepara un'osservazione limitata a 15 secondi. Durante i primi 5 secondi porta il focus nel campo `Edit`, ma non digitare ancora.
4. **Genera una modifica.** Quando viene mostrato «Osservazione attiva», digita nel campo sintetico ad esempio `;firma` o `caffè ☕`, senza spostare focus o cursore. Non premere Invio nel terminale.
5. **Interpreta il risultato.** Alla scadenza l'osservatore mostra solo contatori: notifiche, candidati, letture non disponibili e barriere. `PASS` significa che è stata vista almeno una modifica plausibile; `NOT_RUN` significa che le prove non erano sufficienti o la tecnologia dell'editor non è compatibile. In nessun caso vengono sostituiti trigger.

## Obiettivo finale e prossimi lavori

**Obiettivo:** digitare `;firma` in un editor compatibile ed espandere automaticamente lo snippet senza aprire il picker.

**Cosa resta:** dimostrare la provenienza e l'ordine dell'input Unicode (IME, incollamento, input sintetico), associare i singoli eventi allo stesso documento e qualificare la sostituzione atomica verificabile. Solo dopo è ammessa un'attivazione opt-in per editor specificamente supportati. L'adapter VS Code resta facoltativo e sospeso.

**Stato test:** la Alpha 20.20 è stata verificata dall'utente; la Alpha 20.22 non ha ancora conferma esplicita di build/test Windows. La Alpha 20.23 introduce nuovi test C# e una prova WinEvent da eseguire sul desktop Windows; il loro esito rimane `NOT_RUN` finché non vengono eseguiti.
