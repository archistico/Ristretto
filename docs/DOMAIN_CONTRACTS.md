# Contratti di dominio — specifiche e implementazione

> **Lettura corrente:** i contratti storici sotto includono proposte non implementate. F1a/F1b e il [contratto F2b](F2B_CONDITIONAL_REPLACEMENT.md) sono implementati; nella alpha.7 la sostituzione è verificata anche in un [EDIT Win32 posseduto](F2B_WINDOWS_OWNED_EDIT.md). Non è ancora qualificato un adapter per app esterne. [Stato corrente](CURRENT_STATE.md).

## Raffinamento dei port v2.6 (storico)

**Aggiornamento alpha.5:** [F2b](F2B_CONDITIONAL_REPLACEMENT.md) implementa ora preparazione della sostituzione, evidenze di origine, esiti tipizzati e il port applicativo `IConditionalReplacementTarget`, con il solo backend simulato. I contratti storici sotto non rappresentano tutti API C# esistenti; osservazione nativa e clipboard restano aperte.

`ExecuteExpansion` ha come esito *tipizzato* `BlockedNoChange`, `FailedNoChangeConfirmed`, `SubmittedUnverified`, `VerifiedComplete`, `PartialOrUnknownAfterEffects`. Uno status di cleanup clipboard può essere annotato separatamente, non sovrascrive l'esito del target. L'esecutore non può usare `bool event handled` come prova di testo inserito.

`ITextOutput` è responsabile degli effetti e dell'attestazione disponibile; `IClipboardBridge` (solo quando necessario) espone snapshot/lease, publish e restore condizionato. Target token, epoch e capacità sono volatili. Non introdurre un event bus o un progetto aggiuntivo: [contratto](OUTPUT_COORDINATION.md).

---

# Contratti di dominio e port applicativi — v2.4

> Specifica tecnica concettuale, non definizioni C# compilate. Il **Core contiene algoritmi puri**; le interfacce sono introdotte soltanto per confini I/O reali. Vedere [dominio](DOMAIN_MODEL.md), [adapter](PLATFORM_ADAPTER_CONTRACTS.md) e [architettura](ARCHITECTURE.md).

## 1. Casi d'uso, input e output

| Caso d'uso | Input essenziale | Output tipizzato | Effetti esterni |
|---|---|---|---|
| `AddSnippet` | titolo, template, trigger, gruppo, enabled | `Created(id,revision)` / `Invalid` / `Collision` | commit storage + publish snapshot |
| `UpdateSnippet` | id, expectedRevision, modifiche | `Updated` / `StaleRevision` / `Collision` | commit + publish snapshot |
| `EnableSnippet` | id, desiredState, revision | `Updated` / `StaleRevision` | commit + publish |
| `MoveSnippets` | ids, destinationGroup, revisions | `Moved` / `Conflict` | commit atomico |
| `DeleteGroupSafely` | id, `MoveTo` o `DeleteAll` con conferma | `Deleted` / `Cancelled` / `Blocked` | commit atomico |
| `ObserveInput` | `InputObservation`, target epoch | `MatchResult` | solo stato volatile |
| `PrepareExpansion` | candidato e snapshot, target token | `ExpansionPlan` / `Cancelled` / `Invalid` | eventuale form/lettura autorizzata |
| `ExecuteExpansion` | piano e target originario | `InsertionOutcome` | target verification, remove, insert, cleanup |
| `SearchPicker` | query, filtri, lingua keyword | risultati ordinati | letture/index |
| `ImportPreview` | documento esterno | preview + warning/unknown semantics | nessuna esecuzione macro |
| `ImportCommit` | preview con revision valida + scelta collisioni | `Committed` / `Stale` / `Invalid` | storage + publish |

Il caso d'uso può essere una normale classe/metodo C#. Non richiedere pattern `CommandHandler`, `IRequest`, mediator o generic repository per ogni riga.

## 2. Contratti di valore (senza handle OS)

### `InputObservation`

Campi minimi: `EventKind`, `ObservedCommittedText?`, `TargetEpoch`, `ObservationConfidence`, `BackendCode?`. `EventKind` separa `TextCommitted`, `CompositionState`, `Navigation`, `TargetChanged`, `Unknown`. La distinzione fra commit verificato e tasto inferito è obbligatoria; l'eventuale codice fisico di un tasto non entra nel modello di match come carattere testuale garantito.

### `TargetContext`

Campi minimi: `OpaqueTargetToken`, `TargetEpoch`, `ProtectionState`, `Capabilities`, `ObservedAt`, eventuale identificatore applicativo già sanificato. È un **token di confronto**, non identità di testo, handle persistente, permesso permanente o autorizzazione a loggare nome finestra/PID.

### `InsertionOutcome`

`Status`, `LastReachedStage`, `MayHaveModifiedTarget`, `VerificationLevel`, `ErrorCategory`, `RetryAllowedAutomatically=false` quando qualunque effetto potrebbe essere avvenuto. Non esporre testo utente o stack trace OS nella notifica; la diagnostica opzionale può includere nomi di API/errore senza contenuti.

### `CapabilitySet`

Capacità separate: `ObserveCommittedText`, `ResolveTarget`, `DetectProtectedField`, `RemoveTypedTrigger`, `InsertUnicode`, `UseClipboard`, `PlaceCursor`, `RegisterGlobalShortcut`. Ogni capacità ha `Available`, `PermissionRequired`, `Unsupported`, `Unknown` e facoltativamente motivazione/contesto. `Available` non è sinonimo di “compatibile con tutti gli editor”.

## 3. Port realmente necessari e ordine di creazione

| Port candidato | Owner | Minimo scopo | Quando si introduce |
|---|---|---|---|
| `ILibraryStore` | `Application/Ports` | carica/salva set coerente di snippet+gruppi, optimistic revision, transazioni | prima slice library |
| `IInputObservationSource` | `Application/Ports` | osserva eventi tipizzati, propone epoch e capacità | primo spike input nativo |
| `ITargetInspector` | `Application/Ports` | acquisisce/verifica token focus/protection/capability al preflight | primo spike output |
| `ITextOutput` | `Application/Ports` | rimozione trigger e inserimento con report dei passi, no testo nei log | primo spike output |
| `IClipboardBridge` | `Application/Ports` | clipboard snapshot/lease/conditional restore se supportato | quando scelta output clipboard |
| `IGlobalShortcutRegistrar` | `Application/Ports` | registrazione/rimozione con cleanup | picker hotkey |
| `IImportReader` (solo se utile) | `Application/Ports` | parse inerte di formato esterno e warning | importers P1 |

**Semplificazione pragmatica:** durante lo spike i port possono essere implementati da un adapter/facade per piattaforma; non creare sette DLL, sette servizi singleton, interfacce `IRepository<T>` o factories generiche. Se due port sono sempre inscindibili nella prima prova, unire la loro implementazione senza indebolire il contratto.

## 4. Responsabilità dei progetti

```text
Core:          Snippet/Group/Trigger + matching + expansion planning (puri)
Application:   use case, InputSession volatile, orchestrazione, contratti di I/O
Infrastructure: storage, importers, Windows/macOS/Linux adapters e cleanup
Desktop:      View/ViewModel, tema, localizzazione, dialog, tray, composition root
```

Direzione: `Desktop → Application → Core`; `Infrastructure → Application (+Core)`. `Desktop` compone `Infrastructure` a runtime. Vietato `Application → Infrastructure`; vietato `Core → Avalonia, SQLite, OS interop`.

## 5. Errori e policy di retry

| Categoria | Fase | Risposta | Safe retry? |
|---|---|---|---|
| `Validation` | edit/import/prepare | mostra campo / linea e correzione | sì dopo modifica |
| `StaleRevision` | commit | refresh o merge guidato | commit no, dopo nuova decisione |
| `Collision` | update matcher/import | conflitto e opzioni | solo dopo scelta |
| `UnknownComposition` | observe | pausa/reset | non automatico sul medesimo testo |
| `TargetChanged`, `Protected`, `PermissionDenied` | preflight | `Blocked` senza cancellazione | dopo nuovo consenso/contesto |
| `OutputUnsupported` | preflight | fallback manuale esplicito | non nell'auto-expand |
| `RemoveFailedBeforeEffects` | output | `NoChange` o `UnknownAfterEffects` se indeterminato | solo se provato nessun effetto |
| `InsertPartiallySent`, `ClipboardChanged` | output | `PartialFailure`/`UnknownAfterEffects` | NO retry automatico |
| `UnknownBackendFailure` | output | classe conservativa | NO retry automatico |

Eccezioni sono per bug/contratti infranti; esiti operativi attesi usano risultati tipizzati. Non convertire ogni eccezione in `false` perdendo l'informazione di fase.

## 6. Vincoli tecnici del contratto

- Un `ExpansionPlan` è immutabile e appartenente a un solo tentativo; non riciclarlo in una seconda finestra.
- Il buffer matcher è volatile e bounded; libreria e query picker persistono solo informazioni deliberate dell'utente.
- Un import può memorizzare codice come *testo*, ma non può eseguirlo, avviarlo né risolvere file esterni automaticamente.
- Il catalogo emoji espone `Search(query,locale)` e `Get(codepointSequence)` senza costringere `Core.Matching` a caricare il dataset; una stringa emoji non è un singolo `char`.
- `AppLanguage` governa UI, non cambia trigger/template. `MatchCulture` è scelta specifica, non derivata implicitamente dalla lingua della GUI.
- Il motore segnala `SubmittedUnverified` se l'OS accetta l'iniezione ma non è disponibile la verifica effettiva nell'editor.

## 7. Interfacce non previste

Per il momento NON servono `IDomainEventBus`, `IUnitOfWork<T>`, `IRepository<T>`, `ICommandBus`, `IMiddleware`, `IPlatformProviderFactory`, `IUnicodeStringService`, `IEventSourcingStore`. Si introducono solo davanti a un caso concreto misurato, spiegato da un ADR.

## 8. Gate e contratti aperti

Ratificare: livello di mutabilità/revision per storage; scope id app multipiattaforma; scelta di `IClipboardBridge` rispetto a `ITextOutput`; regole di composizione IME per backend; quali output dispongono di riscontro verificabile; dati permessi in diagnostica. Implementazione **non iniziata**.

## Raffinamento dei contratti per v2.5 (solo studio)

`MatchResult` espone `RawMatchedText`, un riferimento a `CommittedEventSpan` e, ove utile, `NormalizedMatchKey` per confronto diagnostico. `InputSession` preserva la relazione tra commit text e target temporaneo; non è un registratore di tastiera. `TargetContext`/adapter espongono `CanSafelyRemoveTrigger` solo quando la capability è dimostrata; se `Unknown`, il coordinatore non invia Backspace. `ExpansionPlan` contiene il testo preparato originale, non una cifra di tasti da premere. **Questi campi sono proposte contrattuali, non API C# già implementate.**

[Matcher](TRIGGER_ENGINE.md) · [Piano test](MATCHER_TEST_PLAN.md).
