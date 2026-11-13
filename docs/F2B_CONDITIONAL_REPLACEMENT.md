# F2b — contratto di sostituzione condizionale

**Aggiornamento alpha.7:** il medesimo percorso è ora verificato anche in un [EDIT Win32 cooperante creato dal laboratorio](F2B_WINDOWS_OWNED_EDIT.md), 21/21 scenari passati. Il contratto applicativo è condiviso dalla simulazione e dalla prova nativa. Non esiste ancora un adapter per editor esterni arbitrari.

Il planner e l’esecutore collegano le decisioni del Core a un target qualificato. L’implementazione originale alpha.5 usa la simulazione; la alpha.7 aggiunge un backend confinato al proprio EDIT. Il prodotto non esegue sostituzioni in app esterne.

## Comportamento

Il planner riceve una decisione `Ready` con un solo snippet, l'identità registrata insieme al match, una snapshot corrente del destinatario, il trigger originale committato e l'eventuale delimitatore già digitato. Prepara l'intero risultato prima di qualunque effetto.

Esempio sintetico: trigger della libreria `;café`, testo realmente digitato `;cafe\u0301`, delimitatore NBSP. Viene sostituito il range originale NFD, conservando NBSP, testo circostante e contenuto dello snippet senza normalizzarli. Il nuovo contenuto può includere newline, tab, emoji ZWJ, CJK e arabo. Non si calcola un numero di Backspace.

Il flusso completo è ripetibile senza interagire con finestre o dati personali:

```powershell
dotnet run -c Release --project tools/Ristretto.ReplacementLab/Ristretto.ReplacementLab.csproj -- --self-test
```

Il laboratorio verifica cinque scenari: riconoscimento → sostituzione Unicode/multiriga, piano già consumato, modifica intervenuta, ricevuta mancante ed errore dopo l'effetto. Stampa soltanto esiti; non legge DB, tastiera o clipboard.

## Contratti implementati

| Componente | Responsabilità |
|---|---|
| `ReplacementOrigin` | token, epoch, revisione del testo e della libreria registrati con il match originale |
| `ReplacementTargetSnapshot` | testo effimero esatto, selezione, revisione, protezione e capacità osservate |
| `TriggerReplacementPlanner` | confronto evidenze, preparazione del range e risultato atteso senza effetti |
| `TriggerReplacementPlan` | proposta immutabile, associata a un solo tentativo |
| `IConditionalReplacementTarget` | confronto completo e modifica indivisibili, con revisione incrementata di uno |
| `TriggerReplacementExecutor` | consumo unico, controllo libreria/cancellazione, classificazione prudente della ricevuta |
| `SimulatedReplacementTarget` | editor di test gestito, protetto da lock; nessuna API di sistema |

File applicativi: [planner](../src/TextExpander.Application/Features/Output/TriggerReplacementPlan.cs), [esecutore](../src/TextExpander.Application/Features/Output/TriggerReplacementExecutor.cs), [target simulato](../src/TextExpander.Infrastructure/Simulation/SimulatedReplacementTarget.cs).

Il port richiede una capacità forte: osservazione del target, confronto di identità/revisione/testo/selezione/stato e applicazione dell'edit nella stessa operazione. Una verifica del foreground seguita da `SendInput` non soddisfa il contratto. La simulazione realizza questa garanzia tramite lock; il proprio EDIT del laboratorio usa il thread UI proprietario senza message pump tra confronto ed edit. Nessuna API disponibile in un editor esterno viene presunta equivalente.

Questo port rappresenta una strategia di sostituzione condizionale, non un requisito di transazionalità universale. Futuri adapter OS non atomici richiederanno un contratto distinto con controlli per fase ed esiti prudenti; non possono dichiarare di implementare questa garanzia semplicemente effettuando preflight e invio separati.

## Rifiuti prima dell'effetto

- Attivazione non `Ready`, ambiguità o snippet invalido/disabilitato.
- Origine mancante o non valida; token, epoch o revisione del testo diversi; libreria aggiornata.
- Stato protetto, ignoto, readonly, disabilitato o in composizione; capacità ignota/non supportata; modificatori in sospeso.
- Selezione attiva o fuori dal testo, UTF-16 malformato, NUL, testo oltre i limiti.
- Testo originale non presente esattamente prima del caret: l'equivalenza NFC non sostituisce l'evidenza del testo originale.
- Range o caret che dividono un cluster grafemico, incluso un caret finale dentro una nuova sequenza ZWJ.

NFC viene usata soltanto per confrontare raw trigger e trigger della libreria, tramite [String.Normalize](https://learn.microsoft.com/en-us/dotnet/api/system.string.normalize). I confini di testo sono controllati con [StringInfo.ParseCombiningCharacters](https://learn.microsoft.com/en-us/dotnet/api/system.globalization.stringinfo.parsecombiningcharacters?view=net-10.0). Gli indici UTF-16 descrivono una snapshot esatta e non il comportamento di cancellazione di un editor esterno.

Limiti espliciti di questa tranche: snapshot e risultato massimo 65.536 unità UTF-16; raw match massimo 512; delimitatore vuoto oppure un solo elemento testuale whitespace/punteggiatura ammesso, entro il limite di osservazione Core. Tab, Invio e separatori di riga non sono accettati come delimitatori di attivazione, ma restano testo valido nel contenuto dello snippet. Uno snippet più grande resta salvabile/copiabile secondo le regole esistenti: questo percorso di sostituzione restituisce un rifiuto.

## Esecuzione ed esiti

Il piano viene reclamato con `Interlocked` prima di entrare nel backend; anche un tentativo cancellato o obsoleto viene consumato. Chiamate concorrenti sullo stesso piano entrano nel backend al massimo una volta. Piani distinti sulla medesima snapshot sono comunque condizionati alla revisione e al testo correnti del target simulato.

| Esito | Significato |
|---|---|
| `BlockedNoChange` | questa invocazione non ha modificato il target; cancellata, obsoleta, già tentata o rifiutata dal backend senza effetti |
| `VerifiedComplete` | ricevuta esatta con identità, epoch, revisione, testo, selezione e stato attesi |
| `SubmittedUnverified` | il backend dichiara applicazione ma non fornisce riscontro osservato |
| `PartialOrUnknown` | ricevuta incoerente/ignota o eccezione dopo l'ingresso nel backend, anche per cancellazione |

Nessun retry o rollback automatico. La ricevuta appartiene all'istante dell'operazione: non garantisce che il testo resti immutato in seguito. L'assenza di effetti di `RejectedNoChange` è una responsabilità del backend qualificato, non una deduzione dal suo codice di errore.

Il chiamante futuro dovrà serializzare osservazioni, cambi focus e pubblicazione della libreria, e fornire la revisione corrente. I record di evidenza non attestano da soli la loro provenienza: il laboratorio nativo li costruisce leggendo il proprio EDIT dopo commit sintetici; la demo offre un ponte limitato a quel campo, ancora da provare con input umano. Non esiste ancora una sorgente qualificata per app esterne. Il coordinatore non conferisce autorità a una snapshot arbitraria.

## Verifiche e limiti operativi

- Test: [TriggerReplacementTests](../tests/TextExpander.Tests/TriggerReplacementTests.cs), **43 casi aggiunti**, suite complessiva **180/180 passati**; incluse gare, origine obsoleta, Unicode, ricevute errate ed errori dopo l'effetto.
- Laboratorio simulato: [programma](../tools/Ristretto.ReplacementLab/Program.cs), [5/5 scenari eseguiti nella alpha.5](../reports/alpha5-replacement-lab.log).
- Ultimi build/test alpha.7: [log](../reports/windows-alpha7-validation.log), [riepilogo](../reports/WINDOWS_ALPHA7_VALIDATION.json), [TRX](../reports/windows-alpha7-tests.trx).
- [21 scenari nativi alpha.7](../reports/windows-alpha7-native.json), limitati al proprio controllo.

Testo della snapshot, trigger e payload restano soltanto in oggetti effimeri; `ToString` di snapshot, richiesta e piano non li espone. Non vengono persistiti o loggati dal nuovo percorso. Le stringhe .NET non garantiscono cancellazione crittografica dalla memoria.

La GUI corrente è Preview 9; SQLite e il matcher conservano il comportamento precedente. La prova nativa F2a resta non verificata dopo il rifiuto `target_changed` della alpha.3. Il passo successivo è qualificare un backend reale e la provenienza delle osservazioni, prima di abilitare sostituzioni in altre applicazioni.
