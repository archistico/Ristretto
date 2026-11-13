# Alpha 16 — osservazione per messaggio e attesa eventi nel laboratorio Win32

**Sorgenti:** `3.9.0-alpha.16` · **GUI Preview 9 / hotkey picker / SQLite:** invariati rispetto ad Alpha 15 · **Build, xUnit, test Windows:** `NOT_RUN` qui.

## Problema riproducibile

Nella demo precedente il controllo Win32 riceveva i messaggi mediante `PeekMessageW`, poi `ObserveCurrent` leggeva il documento una volta ogni 10 ms. Con caratteri ravvicinati già in coda (`;firma `), potevano avvenire più revisioni prima dello snapshot; il verificatore del delta, correttamente, le rifiutava. Il polling rendeva quindi il percorso umano inutilmente dipendente dalla velocità di digitazione.

## Modifica

1. `OwnedEditTarget.PumpPendingMessages(Action? afterOwnTurn)` esegue la callback **sul thread che possiede il controllo**, dopo il dispatch di ogni modifica o invalidazione dell'epoch, **prima di elaborare il messaggio successivo**. Nessuna callback da thread esterni, nessun nuovo hook o osservatore globale. Il confronto/sostituzione avviene in una singola iterazione del thread senza pumping annidato.
2. `RunDemo` non usa più `Thread.Sleep(10)`. Svuota tutti i messaggi del thread, poi `WaitMessage()` blocca il thread fino a un evento Windows. La callback fa `ObserveCurrent`, conta l'esito e non invia alcun testo o tasto al processo padre. Anche `--cooperating-demo` usa questo percorso perché avvia lo stesso `RunDemo` nel figlio.
3. Una prima metà alta di coppia surrogate UTF-16 ricevuta come `WM_CHAR` è temporaneamente differita: non viene letta come se fosse un carattere committato. Una metà bassa **immediatamente successiva** consente una singola osservazione coerente e correla le due notifiche di cambiamento a una revisione logica. Interruzioni o altre sequenze invalidano l'epoch; non vengono ricostruiti tasti e non si può dedurre una digitazione fisica.
4. Il pump drena tutti i messaggi della **sola thread-queue del laboratorio** e classifica soltanto messaggi appartenenti alla propria finestra `EDIT` o alla finestra padre. Interazioni con altri HWND o processi non vengono considerate input qualificato.
5. `PostOwnedCharacterSequenceFixture` invia una sequenza valida di unità UTF-16 **solo** al proprio EDIT per i test; rifiuta stringhe vuote, caratteri di controllo, NUL, surrogate isolate e payload eccessivi. Non costituisce input umano.

## Regressioni native previste

| Caso | Risultato atteso |
|---|---|
| Tutta `;firma ` in coda prima di avviare il pump | espansione verificata una sola volta; nessuna perdita per burst |
| `;fir` poi freccia e infine `ma ` nella stessa coda | nessuna espansione retroattiva |
| `;😀 ` come messaggi UTF-16, high/low separati | nessuno snapshot su metà alta; match Unicode corretto dopo metà bassa |
| Ritorno del focus, messaggi non fidati, clipboard/IME | invariati i rifiuti della Alpha 15 |
| Nessun messaggio in coda | attesa `WaitMessage`, nessun polling fisso |

I tre nuovi casi sono aggiunti a `--replacement-test`, portando i **28 scenari già previsti a 31 scenari previsti**. Anche `--cooperating-smoke` rimane nel validatore. La prova interattiva umana è separata.

## Limiti espliciti

- La nuova callback è *event-driven* per il nostro `EDIT` Win32 controllato; non è una sorgente affidabile di testo per applicazioni terze, browser, Word o controlli personalizzati.
- `WM_CHAR` può essere sintetico. La provenienza viene validata soltanto secondo il contratto limitato del laboratorio. Composizione IME generale e focus esterno non sono qualificati.
- Un `SendMessage` diretto o una mutazione non osservata da questo pump continua a richiedere i gate di revisione/delta, e in caso dubbio viene rifiutata.
- Nessuna validazione reale su Windows viene dichiarata in questa consegna: né l'esecuzione xUnit né le 31 prove native né la demo umana.

## Test da eseguire su Windows

Estrarre lo ZIP in una **nuova cartella**; non unire versioni diverse:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

Demo interattiva opzionale:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab -- --cooperating-demo
```

**Gate prossimo:** eseguire test Windows complessivi ancora aperti, poi studiare un adapter per un editor esterno individuato con capacità documentate e una fonte credibile del testo committato. Non attivare l'espansione esterna sulla sola base della nuova pompa di messaggi.
