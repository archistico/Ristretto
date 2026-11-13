# Coordinamento dell'espansione e inserimento sicuro — Specifica v2.6

**Stato: PROPOSTA DOCUMENTALE.** Nessun algoritmo C# implementato; nessuna prova nativa eseguita. Integra [Ciclo di vita](EXPANSION_LIFECYCLE.md), [modello](DOMAIN_MODEL.md), [contratti](DOMAIN_CONTRACTS.md), [input/output nativo](NATIVE_INPUT_OUTPUT.md), [Unicode](UNICODE_IME.md) e [ADR-0005](adr/ADR-0005-OUTPUT-COMMIT-AND-CLIPBOARD.md).

**Aggiornamento alpha.5:** la proposta storica seguente resta il riferimento per l'output OS non transazionale. È ora implementato un sottoinsieme managed separato: [F2b — planner/esecutore condizionale e target simulato](F2B_CONDITIONAL_REPLACEMENT.md). Verificato soltanto in memoria; nessuna clipboard o modifica a editor reali. L'ADR non viene considerata ratificata per backend nativi.

## 1. Scopo e confine della garanzia

Una sostituzione **non è una transazione atomica** tra processi. Il programma può preparare perfettamente lo snippet, ma non dispone normalmente di un commit/rollback universale sull'editor esterno. Il successo di una chiamata Win32, Quartz o XTest **non garantisce** che il contenuto esatto sia entrato nel campo corretto. L'architettura deve quindi ridurre i rischi prima della prima modifica e comunicare rigorosamente ciò che è stato osservato dopo.

Obiettivi non negoziabili: niente modifica senza verifiche disponibili; niente effetto prima che il piano sia valido; niente secondo tentativo automatico quando una modifica può essere già avvenuta; clipboard dell'utente non sovrascritta dopo una sua nuova copia; fallback manuale chiaro; compatibilità Unicode attestata per applicazione/OS e non per sola persistenza.

## 2. Terminologia

- **RawMatchedText:** sequenza originale committata al campo, mai ricostruita da NFC o `string.Length`.
- **Target token/epoch:** identificazione effimera di finestra/sessione/accessibility target; non un'asserzione di transazione o identità del caret.
- **Prepared plan:** testo finale, eventuali azioni ammesse, caret intent e metodo candidato; immutabile e legato a un tentativo.
- **Preflight:** valutazione delle capacità e del contesto **prima** dell'effetto. Non elimina la race che può avvenire fra verifica e invio.
- **First effect boundary:** prima azione che può modificare il destinatario (Backspace, paste, testo simulato, selezione/sostituzione).
- **Attempt outcome:** cosa sappiamo *effettivamente*, non il risultato sperato.
- **Clipboard lease:** possesso temporaneo e condizionato del contenuto pubblicato, non un lock esclusivo del sistema.

## 3. Stati del coordinatore (uno alla volta per sessione attiva)

| Stato | Entrata | Uscite ammesse | Modifica del target? |
|---|---|---|---|
| `Observed` | match con origine e snapshot | `Preparing`, `Cancelled` | No |
| `Preparing` | espansione variabili, form, scelta, piano | `Ready`, `Cancelled`, `Invalid` | No |
| `Ready` | plan immutabile | `Preflighting` | No |
| `Preflighting` | target, epoch, protezione, capacità, policy | `Blocked`, `Executing` | No |
| `Executing` | unica sezione serializzata per target/sessione | `CleaningUp` con outcome | Sì, forse |
| `CleaningUp` | ripristino hook/risorse, clipboard condizionale | `Completed`, `Unknown`, `Partial`, `Failed` | Potenzialmente sì |
| `Completed` | verifica del risultato disponibile | terminale | Sì |
| `Unknown` | output inviato, verifica target assente o insufficiente | terminale | Forse |
| `Partial` | errore durante effetti o esito ambiguo | terminale | Sì/forse |
| `Blocked`/`Cancelled`/`Invalid` | gate non superato | terminale | No |

**Principio operativo:** outcome terminale dopo una possibile modifica, nessun retry implicito, anche se l'utente ha premuto due volte una scorciatoia; nuova azione richiede nuovo contesto e conferma ove necessario.

## 4. Protocollo di coordinamento (specifica, NON pseudo-codice da compilare)

1. Osservare solo un trigger **committato** e verificare che la fonte non stia emettendo testo pre-edit IME. Memorizzare `RawMatchedText`, token di origine, epoch, snapshot della regola.
2. Serializzare i tentativi per sessione di destinazione; se ne arriva un secondo mentre uno è in corso, ignorarlo o segnalarlo senza coda di sostituzioni vecchie.
3. Preparare **interamente** il piano prima del primo effetto: sostituzioni, variabili, form, riferimento ad altri snippet, validazione, limite dimensione, contenuto originale e posizionamento cursore. Un dialogo può cambiare il focus: dopo la sua chiusura è obbligatorio revalidare.
4. Se il target è `Protected`, `Composing` o `Unknown` con dati insufficienti, bloccare l'automatismo. Non usare una regola universale basata sul nome del processo per *supporre* che non sia un campo password.
5. Scegliere un metodo di inserimento tra quelli **disponibili e qualificati per quel target**; distinguere metodo di rimozione da metodo di inserimento. `Clipboard`, `SyntheticUnicode`, `Paste`, `ReplaceSelection`, `ManualCopy` sono capacità, non promesse.
6. Confrontare token/epoch **immediatamente** prima di qualsiasi modifica; risolvere l'eventuale stato modificatori premuti, lock IME, selezione e caret nei limiti visibili dell'API. Se non si può rimuovere in sicurezza il trigger già digitato, **non tentare** la sostituzione automatica; offrire il picker/copia.
7. Entrare in sezione di output protetta da cleanup garantito (equivalente concettuale di `try/finally`), disabilitando la ricorsione degli eventi sintetici senza disattivare la gestione degli errori.
8. Applicare il piano solo con le azioni previste dal backend qualificato; dopo ogni fase potenzialmente irreversibile registrare il *LastReachedStage* in memoria volatile. Se la verifica di target cambia, interrompere **senza** completare il resto in una seconda finestra.
9. Se si usa clipboard, acquisire snapshot nei formati effettivamente leggibili; pubblicare payload; attendere una condizione di consumo quando disponibile, altrimenti classificare il risultato come non verificato. Non assumere che un ritardo fisso sia un acknowledgement di paste.
10. Eseguire cleanup dei modificatori, dei permessi, dei callback e dei dati temporanei. Ripristinare gli appunti **solo se** il contenuto temporaneo è ancora quello del programma e la piattaforma permette una verifica adeguata; in caso di dubbio preferire preservare gli appunti correnti.
11. Emit `InsertionOutcome` dettagliato senza registrare snippet, testo digitato, contenuto clipboard, nome completo del documento o finestra sensibile. Aggiornare l'eventuale statistica di uso soltanto secondo una semantica esplicitamente definita (p.es. `Submitted` distinta da `Verified`).

## 5. Rimozione del trigger: quattro condizioni minime

- **Origine identica:** focus/target coerenti con la sessione di digitazione, niente navigazione o selezione che invalidi il buffer dopo il match.
- **Semantica accertata:** il backend sa che cosa rappresentano i caratteri committati e come il target tratta la rimozione, almeno per la classe testata. NFC è solo una chiave di confronto.
- **Effetto verificabile per quanto possibile:** se un metodo di rimozione selettiva/accessibility replace è praticabile e testato, preferirlo al Backspace sintetico. Non assumere che selezionare esattamente il testo sia universalmente disponibile.
- **Niente calcolo ingenuo:** `string.Length` conta unità UTF-16, `Rune` scalari Unicode, grapheme cluster elementi estesi; nessuno dei tre equivale al numero di Backspace in ogni editor.

Per `👍🏽`, `👨‍👩‍👧‍👦`, `❤️`, testo Devanagari e IME giapponese: se la cancellazione non è certificata per target/versione, bloccare automatico e offrire **inserimento manuale**. L'utente può comunque salvare/cercare/copiare qualunque snippet Unicode valido.

## 6. Selezione del metodo di inserimento

| Modalità | Vantaggio | Rischio | Sblocco |
|---|---|---|---|
| Text APIs dell'editor (se disponibili) | possono sostituire un range in modo più preciso | copertura non uniforme, permessi, focus/selection | spike per app/OS, ownership verificabile |
| Simulazione tasti Unicode | evita di sostituire clipboard, può essere rapida per testi brevi | UIPI, layout, input non consegnato, app speciali, emoji composte | test per classe app, mai associare invio all'output osservato |
| Clipboard + paste | Unicode e testi lunghi più semplici | gestione asincrona paste, proprietario clipboard, formati multipli, race | lease condizionale e supporto target |
| Selezione/picker + copia manuale | non modifica automaticamente il target | richiede azione utente | sempre disponibile salvo blocco accesso ai dati stessi |

**Policy P0 proposta:** plain text come default; ricompense/trasformazioni `rich text`, macro e invio di tasti speciali fuori dal nucleo P0 salvo esigenza verificata. Threshold di scelta tra metodi empirico per piattaforma/editor, **nessuna soglia hardcoded definitiva** prima dei test.

## 7. Clipboard: lease e limiti

1. Se non è necessario l'inserimento tramite clipboard, non leggerne il contenuto.
2. Se si usa, trattare il backup come **best-effort**: alcuni formati possono essere non esportabili, differiti, molto grandi o sensibili; soglia e consenso secondo privacy policy.
3. Scrivere un payload temporaneo e memorizzare un indicatore del possesso solo se la piattaforma lo espone. Windows `GetClipboardSequenceNumber`, macOS `NSPasteboard.changeCount` e X11 ownership sono segnali utili **ma non costituiscono un lock atomico**. Vedere [fonti](../research/OUTPUT_SOURCE_REGISTER_v2.6.md).
4. Dopo che è stato richiesto paste, **non interpretare un timer come conferma** che l'editor abbia letto la clipboard.
5. Se il clipboard cambia dopo l'inserimento temporaneo (per es. l'utente copia altro), **non** ripristinare la copia precedente né svuotare la nuova clipboard.
6. Se la provenienza non può essere verificata, classificare come `ClipboardRestoreSkippedUnknown` e preservare lo stato attuale. Evitare di perdere il testo precedente e di lasciare informazioni sensibili nei log.
7. Una cancellazione della clipboard automatica dopo 1s è vietata come default: può distruggere il contenuto dell'utente. Il comportamento di cleanup deve essere qualificato e configurabile.
8. Prevedere diagnostica non invasiva per `ClipboardSnapshotUnsupported`, `ClipboardBusy`, `RestoreSkippedConcurrentChange`, `PasteSubmittedUnverified`, `ClipboardOwnershipLost`.

Su X11 distinguere `CLIPBOARD` da `PRIMARY` e i cambi di ownership. Su Wayland, disponibilità di clipboard e inserimento non implica accesso generalizzato alla digitazione. **Nessuna piattaforma garantisce un restore esatto e universale di tutti i formati.**

## 8. Identità del target, focus e casi di race

`TargetContext` riporta un token opaco, epoch, process/application metadata strettamente necessari e *capability*, acquisiti al momento. La validazione prima dell'effetto è una condizione **necessaria ma non sufficiente**: il focus può cambiare dopo il controllo. Quindi l'outcome deve poter essere `UnknownAfterEffects`, non `VerifiedComplete`. Il motore non deve fare `bring-to-front` arbitrario o riprovare nella finestra che risulta ora attiva.

Il picker salva un token di origine prima di prendersi il focus; dopo conferma l'invio è condizionato al ritorno allo **stesso** target verificabile. Se il token non è recuperabile, pulsante `Copia testo` / incolla manualmente, senza attivazione forzata.

## 9. Esiti tipizzati e loro significato

| `Status` | Effetti possibili | UI / retry |
|---|---|---|
| `CancelledBeforeEffect` | No | silenzioso, nuovo tentativo consentito |
| `BlockedNoChange` | No | spiegazione e alternativa, retry dopo azione esplicita |
| `FailedNoChangeConfirmed` | No dimostrato | possibile nuovo tentativo in contesto ricontrollato |
| `SubmittedUnverified` | Sì, ma risultato non osservato | nessun toast di successo certo, nessun retry automatico |
| `VerifiedComplete` | Sì, testo esatto osservato con metodo documentato | successo, ma solo sulle API/target che lo consentono |
| `PartialOrUnknownAfterEffects` | Sì/forse | avviso prudente, nessun retry e nessun rollback automatico |
| `ClipboardRestoreSkipped` | Target indipendente | avviso discreto se interferisce con workflow; non svuotare clipboard |

`InsertionOutcome` deve includere `AttemptId` effimero, `Stage`, `MayHaveModifiedTarget`, `VerificationLevel`, `ErrorCategory`, `BackendKind`, `ClipboardStatus`. Vietato dedurre `VerifiedComplete` dal solo valore di ritorno di `SendInput`/paste o da un timeout.

## 10. Undo/rollback (esplicitamente POST-MVP)

L'annullamento rapido stile Espanso è una funzionalità di ergonomia, non una prova di transazionalità. Abilitarlo soltanto con una capacità esplicita e dopo test su target, grapheme, posizionamento cursore e azioni intervenute. Un'interruzione di focus, click, digitazione o evento IME invalida la possibilità di undo implicito. Nel P0, fornire `Copia testo` e guida per la correzione manuale; NON cancellare automaticamente il risultato parziale.

## 11. Interfaccia utente per gli esiti

- `BlockedNoChange`: «Espansione non eseguita: questa applicazione non consente l'inserimento automatico in sicurezza. Puoi copiare lo snippet.»
- `SubmittedUnverified`: «Testo inviato, ma l'inserimento non può essere verificato in questa applicazione.» (notifica non invasiva, configurabile)
- `PartialOrUnknownAfterEffects`: «L'inserimento potrebbe essere incompleto. Controlla il testo prima di riprovare.»
- `ClipboardRestoreSkipped`: «Gli appunti sono cambiati durante l'espansione; il nuovo contenuto è stato mantenuto.»

Linguaggio dei messaggi mediante `.resx` e `FlowDirection` appropriato. Il normale utente non vede identificativi OS, token o codici errore grezzi. Sezione diagnostica avanzata: fase, backend, classe errore, capacità, senza contenuto utente.

## 12. Criteri di accettazione per iniziare l'implementazione nativa

1. Test fake di tutti i passaggi preflight→execution, incluse cancellazioni e focus cambiante.
2. Differenza provata tra API `call accepted` e contenuto verificato; output type non collassa tutto su `bool`.
3. Clipboard nuova dell'utente mai sovrascritta nei casi documentati di clipboard ownership cambiata.
4. Assenza di doppia esecuzione automatica dopo errore parziale e dopo picker che perde il contesto.
5. Unicode test con NFC/NFD, emoji ZWJ, skin tone, RTL, CJK, IME *committed* vs *composing* per ciascun backend qualificato.
6. Policy di funzionalità disponibili per Windows, macOS, X11 e Wayland per distro/compositor/app. Nessun PASS di compatibilità basato su mock.
7. Tempi/timeout, consumo clipboard e impatto su app specifiche **misurati** e documentati, non presunti.
8. Nessun segreto/snippet/clipboard nei log standard. Crash durante output deve risultare diagnosticabile senza replay automatico.

**Decisione storica v2.6:** la proposta richiedeva ratifica [ADR-0005](adr/ADR-0005-OUTPUT-COMMIT-AND-CLIPBOARD.md) prima dell'implementazione OS. La alpha.5 implementa soltanto la tranche managed/simulata descritta sopra nell'ambito dello sviluppo corrente; backend nativi e policy clipboard restano da qualificare e definire.
