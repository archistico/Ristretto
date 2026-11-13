# Alpha 18 — richiesta di sostituzione a un editor cooperante in un altro processo

**13 novembre 2026 · sorgenti 3.9.0-alpha.18 · non ancora compilati né provati su Windows.** GUI Preview 9, hotkey del picker, SQLite e i vecchi adapter invariati.

## Obiettivo e differenza rispetto all'Alpha 9

L'Alpha 9 mostrava un processo figlio che decideva e applicava localmente una propria espansione: il genitore **riceveva soltanto gli esiti**. L'Alpha 18 prova, in un **processo separato e isolato**, anche il verso opposto: il controller chiede una singola sostituzione al destinatario **cooperante**. È il destinatario a verificare e sostituire sul thread UI che possiede il controllo. La richiesta e le risposte contengono **solo identificatori di sessione, sequenza ed esiti**, mai testo, snapshot, intervalli, PID o HWND nel report.

È un laboratorio con fixture **sintetiche** e un `EDIT` di proprietà del processo figlio appena creato, non un adapter per Notepad, Word, browser o altre app esterne. Nessun supporto alla modifica di documenti dell'utente è stato aggiunto. Non si intercetta la tastiera globale, non si legge la clipboard e non si registrano tasti.

## Implementazione

- `RemoteOwnedEditProtocol`: protocollo di ricerca versionato e senza testo, con `READY`, `APPLY` e `RESULT`. Sessione GUID e PID atteso limitano la correlazione al processo **lanciato dal lab**. Non sono un'autenticazione di applicazioni esterne.
- `RemoteOwnedEditOneShotGate`: una sola richiesta può arrivare a `TriggerReplacementExecutor`; anche una prima richiesta malformata consuma il permesso. Nessun retry dopo esito sconosciuto.
- `RemoteOwnedEditLab`: il processo padre richiede `APPLY`, il figlio ricontrolla `TargetToken`, epoch, revisione, range, stato del campo e testo atteso sul **proprio** `EDIT`. La sostituzione usa il già esistente `CompareAndReplace` proprietario, senza pompare messaggi tra confronto e scrittura; legge il risultato e invia il solo stato.
- Dopo un esito ricevuto il controller **ripete intenzionalmente** il messaggio esclusivamente come test negativo: deve essere rifiutato senza una seconda scrittura. Questa non è una strategia di retry di un'operazione non confermata.

Due prove native isolate: (1) `;firma` + spazio diventa `Caffè ☕ 漢字 ` nella fixture; (2) la revisione cambia **dopo** la preparazione e **prima** dell'`APPLY`, dunque la sostituzione deve essere respinta senza alcuna modifica. Entrambe richiedono risposta del figlio e uscita pulita. Non viene acquisito testo dell'utente.

## Esecuzione

Da `Ristretto/` su Windows con .NET 10 SDK:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

Solo il nuovo esperimento, dopo il build:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab -- --remote-owned-smoke
```

Report JSON facoltativo (creato solo se non esiste):

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab -- --remote-owned-smoke --report reports/alpha18-remote-test.json
```

**Non** usare `--remote-owned-child`: è un comando interno avviato soltanto dal controller del test. Le due verifiche Win32 possono richiedere una sessione desktop interattiva. Le suite xUnit del protocollo sono indipendenti da Windows.

## Contratto di sicurezza e limiti

L'esempio prova la possibilità tecnica di una decisione condizionale **nel destinatario cooperante**, non la possibilità di imporre la stessa garanzia a un HWND di un'applicazione non cooperante. Metadati e `EM_GETSEL` dell'Alpha 17 non autorizzano ancora alcuna sostituzione. In caso di timeout o risposta incerta il risultato è **non verificato**; non si tenta nuovamente di modificare il testo, non si invia Backspace, non si esegue rollback automatico. L'esito `Verified` è affidabile soltanto all'interno di questa fixture locale e dopo exit code 0 del figlio.

Per un editor vero servirà un adapter nativo o una sua estensione **supportata**, con contratto transazionale equivalente e test reali: provenienza del testo Unicode committato, controllo del focus, protezioni IME/password, snapshot e revisione atomiche, comportamento dei limiti di lunghezza, esito verificabile e consenso/esclusioni.

## Verifica e gate

- **Controlli statici:** da eseguire al confezionamento dell'archivio.
- **Compilazione .NET 10, xUnit e native:** **NOT_RUN** qui, non deducibili dal solo codice.
- **Gate della milestone successiva:** validare l'Alpha 18 su Windows e selezionare un editor di terze parti con una vera API/estensione in grado di sostenere il contratto, oppure documentare il fallback al picker. Non collegare un semplice `SendInput` ai controlli esterni come scorciatoia.
