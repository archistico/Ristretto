# Alpha 20.19 — Confini di focus Windows (R5)

**Data:** 15 novembre 2026. **Base:** Alpha 20.18.1. **Tipo:** incremento sperimentale sul percorso verso l'espansione automatica. **Nessuna modifica a GUI, hotkey, SQLite, Copia, Inserisci o Invio.**

## 1. Cosa stiamo facendo

Il motore sa già riconoscere trigger Unicode quando riceve **testo effettivamente committato e verificato**. Non abbiamo ancora una sorgente affidabile di tale testo nelle applicazioni Windows arbitrarie. Prima di aprire quel canale, è necessario assicurarsi che un trigger iniziato in una finestra **non possa proseguire in un'altra**.

Alpha 20.19 aggiunge una barriera riutilizzabile ai cambi di finestra e focus e una prova nativa di sola osservazione.

## 2. Modifiche ai sorgenti

1. `src/TextExpander.Application/Features/Input/ExternalFocusBoundarySession.cs`: quattro segnali (`ForegroundChanged`, `ControlFocusChanged`, `ObservationUnavailable`, `ObserverStopped`) invalidano immediatamente `TriggerActivationSession` e `CommittedInputSession`. Il risultato non contiene testo, dati di finestra o permessi di sostituzione.
2. `tools/Ristretto.WindowsOutputLab/WindowsFocusBoundaryProbe.cs`: osservatore WinEvent con `SetWinEventHook` in modalità `WINEVENT_OUTOFCONTEXT`, limitato agli eventi di foreground e focus. Si avvia **soltanto da comando esplicito**, su thread dedicato con message loop, e viene arrestato entro 3–30 secondi. Il callback non elabora né registra HWND, PID, titoli, tasti, clipboard o testo.
3. `tools/Ristretto.WindowsOutputLab/Program.cs`: nuova opzione `--observe-focus N`.
4. `scripts/observe-focus.cmd`: avvio semplificato da PowerShell.
5. `tests/TextExpander.Tests/ExternalFocusBoundarySessionTests.cs`: regressioni per quattro barriere, ripresa con testo **nuovo e verificato**, duplicati, segnale non valido e rifiuto di input non qualificato.

## 3. Che cosa NON dimostra

Il segnale WinEvent **non è** un evento di testo committato. Può essere perso, duplicato o osservato in ritardo. La prova non permette di distinguere digitazione, incollamento, automazioni, IME, modifiche programmatiche o revisioni concorrenti. **Non va utilizzata per cancellare trigger negli editor esterni**, né per costruire parole traducendo tasti virtuali. Nessuna espansione automatica è attivata in questa versione.

La privacy è intenzionale: si contano soltanto le variazioni. Non esiste log di contenuti, né un hook tastiera. Anche una conta positiva non abilita un adapter per Notepad.

## 4. Verifiche eseguite e aperte

I risultati esatti della versione corrente sono nel [rapporto di validazione](../reports/ALPHA20_19_VALIDATION_STATUS.json). I controlli statici e i test disponibili nel laboratorio Linux vengono eseguiti al confezionamento. **Compilazione .NET, nuovi xUnit e osservazione WinEvent reale su Windows restano NOT_RUN** finché non eseguiti.

**Evidenza storica separata:** l'utente ha confermato build/test e inserimento con Invio per Alpha 20.15; Alpha 20.18 ha prodotto `PASS` sul documento sintetico Unicode salvato in Blocco note con inserimento manuale dal picker. Queste verifiche non certificano la build di questa Alpha né l'espansione automatica.

## 5. Come testare su Windows, passo dopo passo

Non occorre farlo immediatamente. Quando sarà possibile:

1. **Preparazione:** estrarre lo ZIP in una **cartella nuova**. Chiudere eventuali istanze precedenti di Ristretto; non usare documenti personali. Verificare la presenza di `.NET SDK 10`.
2. **Compilazione e test:** aprire PowerShell nella cartella che contiene `Ristretto.sln` ed eseguire:

   ```powershell
   .\scripts\validate.cmd
   ```

   Il risultato atteso è una build senza errori e la suite xUnit completata, compresi i nuovi test `ExternalFocusBoundarySessionTests`. Se fallisce, conservare il primo errore completo.

3. **Prova facoltativa del focus:** aprire un Blocco note con un file **di prova** vuoto; nello stesso PowerShell eseguire:

   ```powershell
   .\scripts\observe-focus.cmd 15
   ```

   Nei 15 secondi seguenti spostarsi tra Blocco note e un'altra finestra, poi cliccare in due campi diversi. **Non è necessario digitare** e non bisogna inserire password.

4. **Risultato atteso:** al termine compaiono soltanto tre conteggi (variazioni finestra, variazioni focus, barriere) e un messaggio `PASS` solo se sono state osservate notifiche e la sottoscrizione è stata arrestata. Contatori tutti zero producono `NOT_RUN`, perché nessuna notifica è stata realmente dimostrata. `NOT_RUN` indica anche una sessione non interattiva o l’impossibilità di registrare le notifiche.
5. **Limite da ricordare:** questa prova non genera testo, non usa `SendInput`, non apre il picker e non modifica documenti. Per la prova manuale del picker/Notepad usare la [procedura R2.05](ALPHA20_18_NOTEPAD_DIAGNOSTICS.md).

## 6. Obiettivo e attività successive

**Obiettivo principale:** espansione automatica Unicode realmente sicura su un insieme dichiarato di editor Windows, con attivazione/disattivazione configurabile e fallback tramite picker.

**Passo successivo R5:** scegliere una sorgente verificabile di testo Unicode committato, qualificare focus/IME/revisione/caret e una sostituzione condizionale nel target. Non riutilizzare in automatico le garanzie del laboratorio Win32 in un'applicazione arbitraria. VS Code è un ramo facoltativo.
