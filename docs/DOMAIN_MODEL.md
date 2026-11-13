# Modello di dominio — specifica di riferimento v2.4

> **Lettura corrente:** questo documento conserva concetti, ID e proposte originari; non è una dichiarazione che tutto il dominio sia ancora da sviluppare. Libreria, matcher F1a/F1b e piano F2b hanno implementazioni e test. Per il comportamento effettivo vedere [stato corrente](CURRENT_STATE.md), [F1a](F1_RECOGNITION_ENGINE.md), [F1b](F1B_ACTIVATION_POLICY.md) e [F2b](F2B_CONDITIONAL_REPLACEMENT.md). Le sezioni sotto mantengono lo stato delle proposte alla loro data.

> **Stato:** specifica per lo studio, **non codice implementato**. Le invarianti classificate **P0** sono la proposta da testare e ratificare; valori numerici, policy linguistiche e funzionalità P1/P2 sono esplicitamente aperti. [Architettura](ARCHITECTURE.md) · [Contratti](DOMAIN_CONTRACTS.md) · [Flussi](EXPANSION_LIFECYCLE.md) · [Test](DOMAIN_TEST_PLAN.md) · [Fonti](../research/audit/FOUR_SOURCE_DOMAIN_TRACE_v2.4.md).

## 1. Scopo e anti-obiettivi

Il progetto open source C#/.NET 10 LTS proposto e Avalonia conserva la semplicità d'uso di Beeftext e i confini di Espanso senza replicare i manager globali né introdurre un framework di dominio. La GUI, i sistemi operativi, i formati d'import e il database NON sono l'identità degli oggetti del Core. Una **Vertical Slice** possiede il caso d'uso; un modello condiviso esiste solo quando un'invariante è realmente riutilizzata.

Non adottare per ora: event sourcing, CQRS bus, mediator generalizzato, ORM obbligatorio, script arbitrary nel Core, AST universale di macro, repository generico, un assembly per entità. Non confondere la parola “immutabile” con la necessità di una gerarchia astratta.

## 2. Mappa dei confini

| Concetto | Progetto/ownership | Persistenza? | Interagisce con l'OS? |
|---|---|---|---|
| `Snippet`, `Group`, `Trigger`, policy/ID | `Core/Snippets`, `Core/Matching` | tramite storage separato | No |
| `MatchResult`, `CompiledTriggerSnapshot`, `ExpansionPlan` | `Core/Matching` e `Core/Expansion` | Solo i dati originari, NON i piani | No |
| `InputSession`, `ExpansionAttempt` | `Application/Features/TriggerRecognition` e `ExpandText` | No | Solo attraverso porte |
| `TargetContext`, `InputObservation`, `InsertionOutcome` | DTO contrattuali minimi `Application/Ports` e risultati; semantica in Core dove pura | No | Valori prodotti da adapter |
| `LibraryRevision` e optimistic concurrency | `Application/Library` + infrastruttura storage | Sì | No |
| `ClipboardLease`, handle/focus native, SendInput | `Infrastructure/Platform/*` | No | Sì |
| ViewModel, finestra editor/picker, testi tradotti | `Desktop/Features/*` | UI settings solo via app | Sì, tramite host |
| Import record Beeftext/Espanso | `Infrastructure/Importers` | Solo dopo validazione | Mai eseguire variabili importate |

I nomi descrivono **concetti**, non classi obbligatorie. `InputObservation`, `TargetContext` e `InsertionOutcome` sono contratti applicativi, **non entità persistenti**: non forzarli in `Core` se introducono dipendenze o dettagli OS.

## 3. Identità, revisioni e data ownership

- Identità di `SnippetId` e `GroupId`: GUID/UUID stabili generati dall'applicazione. Duplicare crea un nuovo ID; rinominare o spostare mantiene l'ID. Importare UUID esterni richiede mapping/strategia collisioni, mai riuso cieco.
- `Revision` monotona per ogni aggiornamento logico; salvataggi concorrenti falliscono con `StaleRevision` e offrono merge/ricarica nella GUI. Non derivarla da `ModifiedAt` o da timestamp wall-clock.
- `LibraryRevision` cambia quando il matcher deve ricevere un nuovo snapshot: creazione, eliminazione, abilitazione, modifiche trigger/scope, gruppo abilitato. La nuova snapshot è pubblicata in modo atomico dopo commit storage.
- `CreatedAt` e `UpdatedAt` sono metadata UTC assegnati da `Application`/storage tramite clock controllabile, non da algoritmi del matcher. Date/ora visualizzate secondo l'impostazione locale dell'utente.
- Stato runtime, buffer tastiera, contenuto clipboard e testo digitato nelle app esterne non entrano mai nei record della libreria né nei log.

## 4. `Snippet` — entità principale

| Campo concettuale | Tipo/semantica | Invariante/proposta |
|---|---|---|
| `Id` | `SnippetId` | non vuoto, stabile, univoco |
| `Revision` | versione per concorrenza | positivo dopo persistenza; evoluzione monotona |
| `GroupId` | riferimento a `Group` | gruppo esistente nel commit; nessun riferimento pendente |
| `Title` | stringa Unicode | nome leggibile; per P0 non vuoto dopo validazione UI; limiti numerici TBD |
| `Description` | stringa Unicode facoltativa | non influenza il matching |
| `Template` | `SnippetTemplate` | contenuto preservato, non normalizzato; modalità `Literal` o `Structured` |
| `Trigger` | `TriggerDefinition` | per P0 un trigger principale; alias multipli P1 |
| `Enabled` | bool | flag locale; non include lo stato del gruppo |
| `Scope` | `ActivationScope` | globale P0; per-app futura solo con identità stabile OS |
| `CreatedAt`, `UpdatedAt` | UTC | mai usare per scegliere trigger in conflitto |

**Invarianti:** nessun template viene eseguito al semplice caricamento; uno snippet disattivato non è candidato al matcher; disattivare il gruppo non modifica permanentemente `Enabled`; spostamento/duplicazione rispettano le revisioni; modifica titolo/contenuto non deve perdere emoji/line ending; template vuoto è ammesso solo se il caso d'uso lo giustifica esplicitamente (per P0 preferire rifiuto o disattivazione).

**Tag e categorie:** evitare di duplicare il concetto di gruppo con gerarchie/tag prima che la UX ne dimostri la necessità. Descrizioni, tooltip, statistica d'uso e ricerca testuale sono separate dal contratto di matching.

## 5. `Group` — entità di organizzazione

Campi candidati: `Id`, `Revision`, `Name`, `Description?`, `IsEnabled`, `SortOrder`, `IsSystemDefault?` e metadata. P0: un gruppo predefinito creato all'inizializzazione, eliminazione del gruppo default non consentita, nomi duplicati nella stessa libreria da gestire esplicitamente (policy P0 proposta: no duplicati per confronto definito). Non permettere cancellazione silenziosa di snippet del gruppo.

La slice `Groups/DeleteSafely` offre **Annulla**, **Sposta snippet in gruppo scelto** oppure **Elimina definitivamente gruppo e snippet** solo con conferma esplicita; la scelta di cestino/soft delete è P1. Aggiornamenti a gruppo e riferimenti dei suoi snippet sono una singola operazione logica di storage, con rollback in caso di fallimento.

Lo stato effettivo è `Snippet.Enabled && Group.IsEnabled && ScopeAllowed && AppEngineEnabled` **calcolato** dalla snapshot. Non salvare `IsUsable` come dato duplicato.

## 6. `TriggerDefinition` — value object, non sequenza di tasti

| Proprietà | Proposta P0 | Nota |
|---|---|---|
| `Pattern` | sequenza Unicode non vuota, scalar validi | no scorciatoie hardware implicite |
| `Activation` | `Immediate`, `OnDelimiter`, `ManualPicker` | `ManualPicker` non rileva digitazione automatica |
| `Boundary` | `Anywhere`, `WordBoundary` | regola Unicode documentata e fixture per script |
| `CasePolicy` | `Ordinal` (default), `InvariantIgnoreCase` candidata | nessuna conversione dipendente dalla lingua UI |
| `Normalization` | `None` (default), `NFCCompare` candidata P1 | solo confronto, mai modifica al payload |
| `DelimiterPolicy` | set di delimitatori di attivazione, `Keep`/`Consume` | default e whitespace da ratificare |
| `PrefixOverlapPolicy` | ambiguità rilevata in configurazione | non espandere `:sig` prima di `:signature` senza policy esplicita |

**Regole:** trigger vuoto rifiutato; sequenze UTF-16 non valide rifiutate senza sostituzione automatica U+FFFD; nessun `Regex` per P0; un'eventuale regex P1 sarà separata dalla definizione di trigger letterale per limiti di tempo, sicurezza e determinismo. Non definire come equivalenti `char`, Unicode scalar, grapheme, numero di tasti fisici e numero di cancellazioni.

Trigger duplicati nello stesso scope effettivo = **conflitto esplicito** in editor/import; nessuna scelta random. Preferire blocco di attivazione o scelta tramite picker. Tra scope specifico e globale, il criterio di specificità è candidato, da ratificare insieme alla UX. I trigger sovrapposti immediati vanno segnalati al salvataggio.

## 7. `SnippetTemplate` e contenuto

- `RawText` memorizza la sequenza Unicode esatta di testo originale: includere emoji ZWJ, variation selectors, accenti combinati, bidi controls e scelta di terminatori di riga. Conservare gli stessi scalari e terminatori, senza normalizzazione o sostituzione silenziosa. L'esportazione JSON può cambiare rappresentazione di escape/byte, **non** il valore logico dei caratteri: non promettere identità byte-for-byte di file riserializzati.
- `Mode = Literal`: token come `#{cursor}`, `{Enter}`, `\\n`, SQL/JSON devono restare testo letterale (salvo la rappresentazione Unicode ricevuta). È la modalità raccomandata P0 per appunti e snippet semplici.
- `Mode = Structured` (subset P0/P1 da ratificare): token espliciti per cursor marker, data/ora e variabili sicure; parser totale con errori localizzabili. Un token sconosciuto produce `UnsupportedInstruction`, mai esecuzione implicita.
- `InvalidUnicode`: rifiuto/report durante input o import, con possibilità di ispezione del sorgente corrotto. Mai correggere silenziosamente codice surrogato invalido.
- Limiti massimi per dimensione template, variabili, profondità di riferimenti, buffer e durata: **policy configurable da fissare con misure**; niente valori arbitrari proclamati come requisiti già ratificati.

## 8. `MatchResult` — esito puro del riconoscimento

Risultato discriminato, non semplice bool:

| Variante | Significato | Cosa può fare l'Application |
|---|---|---|
| `NoMatch` | sequenza valida, nessuna corrispondenza | conserva/limita il buffer volatile |
| `PendingDelimiterOrLongerMatch` | attesa necessaria | nessuna cancellazione |
| `Candidate` | uno snippet applicabile in snapshot e contesto coerenti | avvia `Prepare` |
| `Ambiguous` | collisione o scope non risolvibile | mostra scelta, oppure non fa nulla |
| `Suppressed` | policy disabilitata/contesto escluso/non affidabile | non agire, eventualmente reset |
| `InvalidObservation` | evento non confermato, composizione IME, target incerto | reset/pause, nessun effetto |

Per `Candidate`: ID snippet, snapshot revision, pattern riconosciuto, eventuale delimitatore e stato di mantenimento, **span logico del testo committato osservato** (non `BackspaceCount`). Si può includere una prova di corrispondenza opaca all'Application, ma mai un handle OS dentro il Core. In `Ambiguous` la UI non deve fingere che la scelta sia determinata.

La snapshot immutabile dei trigger è costruita fuori dall'hook, sostituita atomicamente dopo commit di modifiche libreria, con versionamento. Il matcher opera su dati congelati e non legge direttamente SQLite mentre si digita.

## 9. `InputSession` e `InputObservation` — stato effimero

`InputObservation` arriva dall'adapter con: `CommittedText` (solo quando il backend ha evidenza adeguata del testo composto), `CompositionStarted/Updated/Ended`, `Navigation`, `FocusChanged`, `Unknown`, `BackendError`. Include token opaco di contesto/epoch e, se noto, livello di affidabilità dell'osservazione. Un keydown/scan code **non diventa** automaticamente testo committato. Uno `CompositionEnded` non equivale necessariamente a un commit osservato.

`InputSession` in `Application` conserva un **buffer volatile delimitato** e un identificatore opaco del target. Reset su cambio target, navigazione, nuova selezione, cambio di metodo di input, protezione/contesto sconosciuti o cambio snapshot invalidante. Mai serializzare input osservato, né includerlo nei log o nella telemetria. Se il backend non può conoscere un testo IME affidabile, disabilitare l'auto-match in quel contesto, mantenendo picker/copia manuale se disponibili.

## 10. `ExpansionPlan` — value object immutabile

È il risultato della **preparazione completa** di uno snippet prima degli effetti esterni. Comprende: `PlanId` effimero, `SnippetId`/revision, `SnapshotRevision`, contenuto risolto, `Steps` ordinati, `Requirements` (capacità di inserimento e focus), `CursorAnchor?`, `OriginalMatchProof` opaca e limiti. Non conserva privilegi, handle, `ClipboardLease` o `ITextOutput`.

`Step` P0: `InsertText` (anche multilinea e Unicode), eventuale `CursorAnchor` posizionato in un confine grapheme valido; `Delay`, invio di tasti particolari e macro avanzate **non** P0 senza test. La strategia materiale d'inserimento (`Paste`, `UnicodeInjection`) è decisa dall'Application col port native secondo capability/target, non codificata nel template.

**CursorAnchor:** è un'ancora logica nel contenuto finale, non numero di Backspace. Una posizione come indice UTF-16 può essere usata internamente solo insieme a un controllo che sia confine valido di grapheme; l'esecuzione sul target richiede una capability verificata. Se non disponibile: non fingere di averla applicata; informare l'utente o rinunciare al movimento. Più anchor e selezioni complesse sono P1.

Regole: il piano non può contenere variabili da risolvere dopo la rimozione del trigger; errore/cancel di form termina `Prepare` in `NoChange`; il piano è invalido se cambia snapshot/target nei modi descritti dal contratto. L'espansione non è una transazione ACID con l'editor esterno.

## 11. `TargetContext` — contratto opaco verso la piattaforma

Dati minimi: `TargetEpoch` (cambia quando focus/contesto non più verificabile), `TargetToken` opaco, `Protection` (`Normal`, `Protected`, `Unknown`), `ObservationConfidence` (`VerifiedCommitted`, `Inferred`, `Unavailable`), `Capabilities` (`Available`, `PermissionRequired`, `Unsupported`, `Unknown` per funzione), eventuale ID applicazione normalizzato **solo per scope**. Il Core non contiene PID, `HWND`, oggetti AX o handle X11.

Un `TargetContext` rappresenta una **fotografia e una prova temporanea**, non un diritto durevole a scrivere. `Preflight` immediatamente prima degli effetti deve confrontare il destinatario al momento del match con quello attuale; se la stabilità non si può dimostrare, fail closed. La verifica non elimina completamente la race dopo il preflight: dichiararla nei rischi residui. Campi protetti/non classificabili mai espansi automaticamente per default.

## 12. `InsertionOutcome` — risultato veritiero dell'effetto

| Stato | Cosa significa | Possibili effetti | Policy retry |
|---|---|---|---|
| `NoChange` | non tentata alcuna modifica | nessuno | nuovo trigger possibile |
| `Cancelled` | form/scelta interrotta prima di modifiche | nessuno | nuovo trigger possibile |
| `Blocked` | permesso/target/capability non adeguati prima del primo effetto | nessuno | dopo soluzione del blocco |
| `UncertainNoChange` | preflight inconcludente, operazione non avviata | nessuno per nostra decisione | niente retry cieco |
| `SubmittedUnverified` | comando di output inviato/accettato dal backend, contenuto target non verificato | possibilmente sì | **no retry automatico** |
| `VerifiedComplete` | osservazione affidabile dimostra risultato atteso | sì | inutile |
| `PartialFailure` | modifica iniziata e parte dell'output fallita/notoriamente incompleta | sì | **no retry automatico** |
| `UnknownAfterEffects` | effetti possibili ma stato non accertabile | forse sì | **no retry automatico** |

La diagnostica conserva **solo** fase, codice d'errore, backend e codice correlazione temporaneo, senza testo di trigger/snippet/clipboard. `InjectionDispatched` e `VerifiedComplete` sono diversi: `SendInput` che riporta N eventi non prova che l'editor abbia ricevuto il payload. Nessuna promessa di Undo universale o rollback esterno.

## 13. Componenti da NON trasformare in entità di dominio

`ClipboardLease` è un dettaglio dell'adapter con cleanup/restore condizionale; `PlatformCapabilityReport` è un contratto di diagnostica/app; `EmojiDatasetVersion` è metadata di import/catalogo; `ImportedBeeftextCombo` e `EspansoMatch` sono DTO di origine; `Theme`, `CultureInfo` e `LocalizationResource` sono preferenze/UI; `ShortcutRegistration` è un handle runtime OS; `StatisticsOfUse` non guida il matcher e non va aggiunto prima della necessità. Il modello resta piccolo.

## 14. Invarianti trasversali (gate di progettazione)

| ID | Invariante | Gate |
|---|---|---|
| DM-01 | identità snippet/gruppo stabile, revision monotona | unit + integration |
| DM-02 | riferimento a gruppo valido dopo ogni commit | integration |
| DM-03 | enabled di gruppo e snippet applicati senza scrivere flag duplicato | unit |
| DM-04 | conflitti di trigger espliciti, niente random | unit + GUI |
| DM-05 | evento non committato e target ignoto non provocano cancellazione | unit + native |
| DM-06 | nessun `string.Length` per stabilire cancellazione nel target | architecture + native |
| DM-07 | preparazione completa prima degli effetti; cancel lascia testo invariato | application |
| DM-08 | focus/target preflight immediatamente prima di rimozione | application + native |
| DM-09 | payload Unicode e line ending preservati nel valore logico | unit + persistence |
| DM-10 | outcome dopo invio senza verifica non viene chiamato successo verificato | application |
| DM-11 | nessun retry automatico dopo possibili modifiche esterne | application + failure tests |
| DM-12 | no plaintext user input nei log/telemetria | security tests |
| DM-13 | snapshot matcher aggiornata dopo commit, mai prima | integration |
| DM-14 | import non esegue script/variabili e non sovrascrive dati con validazione parziale | importer tests |
| DM-15 | Core indipendente da Avalonia/OS/SQLite, Application da native libs | architecture tests |

## 15. Decisioni da ratificare prima dello sviluppo

1. Identificatori/strategie import e forma della `Revision`, in base allo storage finale (SQLite vs JSON).
2. Limiti numerici di trigger/template, tempi massimi, dimensione del buffer e retention della sessione.
3. P0 attivazione `Immediate` vs `OnDelimiter`, insieme al default di `Boundary`, `CasePolicy`, gestione prefissi.
4. Uso di `InvariantIgnoreCase` e `NFCCompare` sui trigger internazionali; criterio di collisione.
5. Template P0 solo letterale o subset minimo di variabili sicure e anchor.
6. Quali adapter sanno distinguere **davvero** committed text/IME e verificare target/output.
7. Cursor placement: comportamento visibile per target che non lo supportano.
8. Stato `VerifiedComplete`: quali piattaforme/editor offrono una prova sufficientemente forte, altrimenti `SubmittedUnverified`.

**Prossima lettura:** [flussi/stati](EXPANSION_LIFECYCLE.md), [contratti applicativi](DOMAIN_CONTRACTS.md), [scenari test](DOMAIN_TEST_PLAN.md) e [ADR 0003](adr/ADR-0003-DOMAIN-MODEL-AND-OUTCOME.md).

## Addendum v2.5 — Mapping degli intervalli Unicode

`MatchResult` mantiene `RawMatchedText` e l'**intervallo di eventi committati** sorgente; se disponibile, conserva una descrizione separata di `NormalizedMatchKey`. Non usare un offset o una lunghezza della `MatchKey` per decidere che cosa eliminare nell'editor. `ExpansionPlan` non codifica un conteggio Backspace universale; `TargetContext` indica se la rimozione sicura è supportata o `Unknown` e `InsertionOutcome` riporta `Unsupported` senza modifiche quando necessario. Sono raffinamenti dei contratti, non nuove entità/progetti.

[Specifica delle regole](TRIGGER_ENGINE.md) · [ADR proposta](adr/ADR-0004-UNICODE-MATCHING-AND-WORD-BOUNDARIES.md) · [test](MATCHER_TEST_PLAN.md).
