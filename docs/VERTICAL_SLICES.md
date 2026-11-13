# v2.8 — Slice e SQLite

Storage ratificato: ogni slice modifica SQLite tramite **pochi repository specifici e transazioni esplicite**; nessun generic repository, ORM o DB query in callback. Le slice `Library`, `Groups`, `Settings`, `Import`, `Backup/Restore` usano l'infrastruttura; `Runtime` usa solo snapshot matcher. Una *mutation fence* blocca nuove espansioni prima di una transazione che modifica le regole; il commit DB precede la pubblicazione dell'indice; in caso di errore di indicizzazione la sostituzione automatica rimane fail-closed. [Mappa operazioni](SQLITE_STORAGE.md#8-mappa-vertical-slice) · [ADR-0007](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md).

---

# Catalogo canonico delle Vertical Slice — v2.3 (proposta)

**Scopo:** assegnare ownership dei casi d'uso senza introdurre un assembly per slice o un handler per ogni trivialità. Tutte le slice applicative vivono inizialmente in `TextExpander.Application/Features/<Nome>`; le View/ViewModel in `TextExpander.Desktop/Features/<Nome>`. La logica trasversale riutilizzabile sta in `Core` solo se pura.

## A. Slice P0 e interazioni

| Slice / use case | Responsabilità e input | Output / errore | Confini I/O | UI |
|---|---|---|---|---|
| `Library/AddSnippet` | Valida titolo, trigger, testo, gruppo, scope; evita conflitti | ID oppure Validation/Collision | Storage | Editor |
| `Library/UpdateSnippet` | Modifica contenuti e metadati con versione | Success/Conflict/Stale | Storage | Editor |
| `Library/DeleteSnippet` | Cestino/confirmation prima della perdita dati (policy P0/P1) | Deleted/Cancelled | Storage | Dialog |
| `Library/EnableSnippet` | Enable individuale + rigenerazione snapshot | Success/Conflict | Storage/MatchSnapshot | Lista |
| `Library/SearchSnippets` | Filtri per gruppo, label, trigger, testo | Pagine/ranking | Storage o index | Finestra principale |
| `Groups/CreateRenameMove` | Organizza snippet, disabled collettivo | Success/Collision | Storage | Sidebar |
| `Groups/DeleteSafely` | Riassegna o elimina con preview esplicita | Preview/Committed | Storage | Dialog |
| `TriggerRecognition/Observe` | Normalizza solo testo osservabile e contesto | MatchCandidate/Reset/Unknown | Native input observation | Stato motore |
| `TriggerRecognition/Resolve` | Applica scope/boundary/case/precedenze | Unico trigger/NoMatch/Collision | Core puro + snapshot | Avviso editor |
| `ExpandText/Prepare` | Interpreta snippet, variabili sicure, richiesta form | ImmutablePlan/Cancelled/Unsupported | Time, optional form | Eventuale form |
| `ExpandText/Execute` | Preflight, rimozione, inserimento, cleanup | Outcome + reason | Target, injection, clipboard | Notifica/diagnostica |
| `Settings/ReadApply` | Batch valide, tema/lingua/autostart/hotkey | Applied/Rejected/PartialPermissions | Settings, platform permissions | Preferenze |
| `ImportExport/Inspect` | Parser inerte Beeftext/JSON/CSV | Preview + warnings | File read | Wizard |
| `ImportExport/Commit` | Applica solo conversioni confermate | Imported/Cancelled/Conflict | Storage transaction | Wizard |
| `BackupRestore/Create` | Snapshot consistente e verifica | BackupArtifact/Failure | Storage/files | Preferenze |
| `BackupRestore/Restore` | Anteprima, staging, commit, recovery | Restored/FailedUntouched | Storage/files | Wizard |
| `Diagnostics/GetCapabilities` | Permessi/backend/problemi leggibili | CapabilityReport | Platform probe | Preferenze |
| `Lifecycle/StartPauseResumeExit` | Single instance, readiness, hook/hotkey | State/Reason | Native/session | Tray/shell |

**MVP per prima verticale end-to-end:** `Library/CreateSnippet` → snapshot → `TriggerRecognition` → `ExpandText/Prepare` → `ExpandText/Execute` **su una sola piattaforma certificata**. Successivamente GUI editor, picker e backup. Non attendere di implementare tutto il catalogo prima del primo flusso testabile, ma definire già le invarianti.

## B. Slice P1/P2: presenti nel disegno, assenti dall'MVP

| Slice | Quando | Motivo e dipendenze |
|---|---|---|
| `Picker/OpenSearchSelect` | P1 o P0 solo per fallback manuale | Keyboard-first, ricerca locale e focus originario; richiede hotkey + target identity |
| `EmojiCatalog/LoadSearch` | P1/P2 | Indice dataset testabile, gestione alias/lingua, non keylogger/auto-correct |
| `ExpandText/VariablesDateTime` | P1 | Clock deterministico in test; locale contenuto separata da UI |
| `ExpandText/Forms` | P1 | Preparare e annullare senza modificare target |
| `Library/TagsFavorites` | P1 | Classificazione senza duplicare gruppi |
| `Settings/AppProfiles` | P1 | Strategie per target, exclude/sensitive, errori chiari |
| `ImportExport/EspansoYaml` | P1 | Parser inerte; macro e shell rimangono disabilitate |
| `Library/UndoDeletion` | P1 | Recupero locale; non promettere undo dell'espansione su app esterne |
| `EmojiCatalog/Autocorrect` | P2 | Opt-in + collisioni, non fondere con matcher snippet |
| `ExpandText/RichText` | P2 | Matrice di compatibilità clipboard e formati |
| `Sync/LocalFile` | P2 | Solo dopo policy conflitti e integrità |
| `Scripting` / `Team` | P3 | Progetti separati solo se approvati; minacce/permessi/licenze |

## C. Regole per la collaborazione tra slice

- **Libreria → motore:** pubblica **snapshot immutabile**, non modelli mutabili di GUI. Non richiedere all'hook di cercare nel DB.
- **Impostazioni → motore:** pubblica cambio configurazione validato (trigger mode, enable/disable); buffer volatile viene invalidato quando il nuovo schema non è compatibile.
- **Picker → espansione:** richiede `Prepare` e `Execute` con il contesto del target originario. Se il contesto cambia, non incolla automaticamente; abilita copia esplicita.
- **ImportExport → libreria:** importa in staging e usa un unico commit applicativo; nessun parser può invocare l'esecuzione nativa.
- **BackupRestore → libreria:** isolamento di snapshot, transazione e recovery; vietato passare dati parziali al matcher.
- **Emoji → picker:** solo catalogo e indice; il ranking può essere condiviso se esiste un caso d'uso comune provato.
- **Diagnostics:** osserva categorie ed esiti, mai il contenuto digitato o i valori sensibili degli snippet.

## D. Contratti interni: esempi concettuali (NON codice da implementare)

```text
SaveSnippet(input, expectedVersion?) -> SaveResult(id, version, warnings)
UpdateGroup(id, enabled)            -> UpdatedGroup + NewLibraryVersion
BuildMatcherSnapshot(library)        -> ImmutableMatcherSnapshot(version)
ObserveInput(event, context)         -> MatchCandidate | Reset | Unsupported
PrepareExpansion(id, context)        -> ExpansionPlan | Cancelled | ValidationError
ExecuteExpansion(plan, targetToken)  -> Outcome(status, category, safeDetails)
InspectImport(stream, format)        -> ImportPreview(records, warnings, risks)
CommitImport(previewToken, options)   -> ImportOutcome
InspectCapabilities()                -> Capabilities(with evidence and limitation)
```

`PrepareExpansion` può aver bisogno di dati (data attuale, input utente), ma l'ultimo passaggio che prepara l'`ExpansionPlan` non deve mai cancellare testo esterno. Nessuna pretesa di “pure function” per l'intero caso d'uso con dialog o clipboard; **purezza del planner**, non finzione che l'interazione non abbia effetti.

## E. Errori e recovery espliciti

- `NotMatched`, `Collision`, `UnsupportedInput`, `CompositionActive`, `PermissionRequired`, `ProtectedTarget` sono esiti **senza effetti**.
- `CancelledBeforeEffect`, `TargetChangedBeforeEffect`, `ClipboardLeaseUnavailable` devono lasciare intatto il testo destinatario.
- `DeletionFailed`, `InsertionFailed`, `PartialInsert`, `ClipboardChangedByUser` sono esiti **dopo o durante effetti** da dichiarare; il ripristino non è sempre possibile.
- `LibraryCorrupt`, `MigrationRejected`, `RestoreFailed` non sostituiscono mai automaticamente libreria valida con dati incompleti.

## F. Regole di progettazione per singola slice

- Una cartella e uno scenario end-to-end, non 12 interfacce.
- Se un handler ha solo una dipendenza e 5 righe, mantenerlo semplice.
- DTO applicativi espliciti solo ai confini; `record` C# quando migliora chiarezza, non obbligo.
- Le transazioni sono proprietà dei casi d'uso che le richiedono; non attribuirle a `Repository<T>` generico.
- Niente `MediatR`, `AutoMapper`, service locator o event bus obbligatori.
- ViewModel leggere, compiled bindings, costruttori espliciti; navigazione semplice.

Vedere [architettura](ARCHITECTURE.md), [adapter nativi](PLATFORM_ADAPTER_CONTRACTS.md), [piano test](ARCHITECTURE_TEST_PLAN.md).

## Addendum v2.4 — input/output canonici delle slice

I nuovi [contratti concettuali](DOMAIN_CONTRACTS.md) e il [modello di dominio](DOMAIN_MODEL.md) chiariscono ownership e confini senza introdurre nuovi assembly. `AddSnippet/UpdateSnippet` impongono Revision e collision check, `Groups/DeleteSafely` effettua spostamento/cancellazione con commit coerente, `TriggerRecognition` possiede solo sessione volatile, `ExpandText` coordina `Prepare/Preflight/Execute`, `Picker` può inserire senza rimuovere trigger, `EmojiCatalog` non alimenta automaticamente il matcher globale. Tutti gli esiti operativi sono tipizzati; dopo side effects non è consentito retry silenzioso. Per i flussi completi: [EXPANSION_LIFECYCLE](EXPANSION_LIFECYCLE.md).

## Addendum v2.7 — Ownership di prestazioni e lifecycle

- `TriggerRecognition/Observe`: supervisione callback (Infrastructure), ingresso bounded (Application), niente storage/UI, `NeedsResync` su overflow; test in [piano performance](PERFORMANCE_TEST_PLAN.md).
- `Library/Update`, `Settings/Apply`, `ImportExport/Commit`: validano, compilano candidate snapshot fuori dal path input e pubblicano in modo atomico una revisione coerente dopo commit valido; sessioni interessate invalidate.
- `Picker/Search`: indice/paginazione distinti dal matcher, cancellazione delle query obsolete e risultati latest-only; `Desktop` materializza soltanto le righe visibili.
- `Lifecycle/StartPauseResumeExit`: stati e risorse OS formalizzati in [specifica](PERFORMANCE_RESOURCE_LIFECYCLE.md); nessun secondo processo/daemon per l'MVP.
- `Diagnostics/ReadPerformance`: metriche aggregate locali, mai corpo snippet, clipboard, testo digitato o query picker; disattivabile senza perdere correttezza.

## Stato delle slice implementate nel sorgente — v3.1

La slice `Library` ora espone rinomina/attiva/disattiva/elimina gruppi vuoti; `Portability` gestisce il JSON v1 additivo, con contratti in `Application` e scrittura transazionale in `Infrastructure`; `Desktop` possiede le conferme per le bozze e il dialog file. Le operazioni rimangono all’interno dei quattro progetti produttivi concordati, senza nuove assembly. [Dettagli JSON](JSON_PORTABILITY.md) · [Editor](EDITOR_AND_GROUPS.md). **Non è stata effettuata una build .NET in questa consegna.**
