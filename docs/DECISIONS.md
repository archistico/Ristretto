# Decisioni correnti — 13 novembre 2026

- **Interfaccia:** GUI Preview 9 implementata, temi chiaro/scuro, cinque lingue e contenuti Unicode. [Guida](GUI.md) e [prove manuali aperte](GUI_VISUAL_ACCEPTANCE.md).
- **Persistenza:** SQLite per archivio e backup; JSON per portabilità. Import esterni additivi, non esecutivi e disattivati.
- **Motore:** riconoscimento e sostituzione verificati nei laboratori; nessuna autoespansione globale nel prodotto. [Stato](CURRENT_STATE.md).
- **Consegna:** ZIP completo con radice `Ristretto/`, manifest SHA-256 e materiali correnti; cache, build e duplicati esclusi.
- **Scelte aperte:** adapter esterni e IME, certificazione multipiattaforma, licenza/distribuzione. [Roadmap](FUNCTIONAL_ROADMAP.md).

Le sezioni seguenti conservano le decisioni e le proposte storiche con i loro ID. Diciture come “futuro” o “non implementato” si riferiscono alla versione della sezione; per l'avanzamento odierno prevale lo stato corrente. Non si ratificano automaticamente le proposte originarie.

---

# Decisioni implementative v3.2

- Importazioni esterne da Beeftext/Espanso: **non esecutive** e **disattivate**, con anteprima e conferma, finché il modello non supporterà fedelmente opzioni e variabili originarie.
- Dipendenza esterna introdotta: `YamlDotNet 18.1.0` (MIT) nel solo progetto Application, per evitare un parser YAML artigianale. Il controllo licenze va ripetuto alla prima release.
- Gli importatori non sostituiscono mai il database completo; l'unico backup resta SQLite.

---

# Decisioni confermate v2.9

- SQLite come unico archivio e backup completo in formato DB SQLite, senza JSON di backup parallelo.
- JSON soltanto per import/export interoperabile (ora implementato).
- Prima slice sviluppata: gestione libreria, anteprima locale, GUI Avalonia, lingue e temi; nessun hook globale.
- Licenza open source: scelta ancora aperta.

---

# Decisione approvata v2.8 — SQLite

| ID canonico | Stato | Decisione | Conseguenze |
|---|---|---|---|
| [ADR-0007](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md) | **APPROVATO DALL'UTENTE (13/11/2026)** | **SQLite** database locale operativo; JSON versione portabile e import/export | chiuso il dibattito SQLite vs JSON; schema, migration, WAL/FULL, retention ancora proposti |

**Interpretazione di riferimenti precedenti:** tutte le righe storiche che indicano “SQLite vs JSON: aperto” (`ADR-038`, `ADR-065`, ecc.) sono **superate da ADR-0007**. Non cancellare la motivazione storica e non confonderla con decisioni ancora aperte (licenza, Unicode, adapter). Niente quinta assembly né codice applicativo.

---

# Decisione proposta v2.7 — non ratificata

[ADR-0006](adr/ADR-0006-RESOURCE-LIFECYCLE-AND-CONCURRENCY.md): unico processo, coda di input bounded e reset se sovraccarica, snapshot immutabili aggiornati fuori dal callback, picker disaccoppiato, lifecycle con stati espliciti. I budget PB-01..PB-12 sono proposte da misurare, **non** accordi di prestazione. Non modificare gli stati dei precedenti ADR.

---

# Decisione proposta v2.6 (non ratificata)

| ADR-0005 | Proposto | Preflight, serializzazione effetti, output `SubmittedUnverified` distinto da `VerifiedComplete`, clipboard lease condizionale, no retry/undo automatico dopo effetti | Richiede esperimenti nativi per attestare capability e policy default | 

Dettagli in [ADR-0005](adr/ADR-0005-OUTPUT-COMMIT-AND-CLIPBOARD.md). Non interpretare come ratifica.

---

# Registro decisioni (ADR sintetici)

## Deciso dall'utente / vincoli del progetto

| ID | Stato | Decisione e motivazione |
|---|---|---|
| ADR-001 | Confermato | Prodotto **open source** distinto da Beeftext, nuova codebase C#/.NET + Avalonia |
| ADR-002 | Confermato | Windows/macOS/Linux come piattaforme-obiettivo, dichiarazioni limitate a prove reali |
| ADR-003 | Confermato | Vertical Slice Architecture semplice, test, buona ingegneria senza overengineering |
| ADR-004 | Confermato | GUI moderna, veloce, compatta, gradienti discreti, temi chiaro/scuro |
| ADR-005 | Confermato | GUI multilingua; emoji/Unicode/alfabeti stranieri supportati in storage e nel motore, con prove OS |
| ADR-006 | Storico | **Fase iniziale solo studio** di GUI, codice Beeftext e concorrenti (conclusa con avvio sviluppo da v2.9) |

## Proposte tecniche non ancora ratificate / da verificare

| ID | Stato | Decisione candidata | Rischio da chiudere |
|---|---|---|---|
| ADR-010 | Proposto | Local-first senza account, sync e cloud obbligatori | backup/export |
| ADR-011 | Proposto | Core plain text e inserimento adattivo clipboard/keys | backend reali |
| ADR-012 | Proposto | Matching deterministico, niente scelta casuale | ambiguità/scope |
| ADR-013 | Proposto | ResX con servizio localizzazioni osservabile | cambio runtime XAML |
| ADR-014 | Proposto | SQLite locale + JSON export versionato | costo/migrazioni |
| ADR-015 | Proposto | Nessun scripting automatico da input/import | UX power users |
| ADR-016 | Proposto | Picker anche come fallback manuale in contesti non supportati | focus/app restrictions |
| ADR-017 | Aperto | Licenza MIT vs Apache-2.0 vs GPL, governance, CLA/no CLA | derivazioni e dipendenze |
| ADR-018 | Aperto | Policy Unicode normalization, case folding, grapheme deletion | prove IME/editor |
| ADR-019 | Aperto | Supporto native API specifiche e Wayland per compositor | permessi/app target |
| ADR-020 | Aperto | Trigger immediato/terminale/delimiter, conflitti e preferenze | compatibilità Beeftext |
| ADR-021 | Aperto | Portabilità dell'archivio e import concorrenti | format version fidelity |
| ADR-022 | Aperto | UI language initial set e qualità traduzioni | community workflow |

**Regola:** una proposta non è una decisione adottata finché non è ratificata con motivo, data e conseguenze. Alla prima implementazione creare un ADR completo solo per scelte davvero irreversibili.

## Questioni specifiche emerse in v1.6 (non ratificate)

| ID | Stato | Questione / proposta | Condizione |
|---|---|---|---|
| ADR-023 | Proposto | Trigger di default conservativi: prefisso o word-boundary; immediate solo con bassa possibilità di false positive | scegliere UX con esempi reali |
| ADR-024 | Proposto | Conflitti interni equivalenti bloccano l'auto-espansione fino a risoluzione; chooser opzionale | ratifica comportamento su duplicati importati |
| ADR-025 | Proposto | Buffer di testo volatile e minimo, reset su cambi di contesto; no raw-key persistence | verifica OS adapter e test privacy |
| ADR-026 | Proposto | Cancellazione solo con strategia nativa/capability validata; altrimenti abort o copia esplicita | spike sulle app target |
| ADR-027 | Aperto | Picker P0 come fallback, invece dell'attuale P1 | valutazione UX e target support |

Nessuna proposta qui equivale a nuova decisione imposta all'utente. Vedere [TRIGGER_ENGINE.md](TRIGGER_ENGINE.md) per le alternative.


## Nuove proposte ADR v1.7 — da ratificare

| ID | Stato | Tema | Decisione candidata / caveat |
|---|---|---|---|
| ADR-028 | Proposto | Capability-first native adapters | Adapter OS dichiarano `Available/RequiresPermission/Unsupported/Unknown`; niente fallback silenzioso |
| ADR-029 | Proposto | Wayland tier | Separare T0 GUI/copia, T1 picker globale, T2 inserimento autorizzato, T3 auto-espansione verificata per compositor |
| ADR-030 | Proposto | Clipboard non distruttiva | Ripristino solo condizionale e per formati supportati, nessuna promessa di transazione multi-MIME |
| ADR-031 | Proposto | Permission UX | Controlli e messaggi localizzati; non imporre privilegi input/driver in fase iniziale |
| ADR-032 | Aperto | Backend predefinito per piattaforma | Stabilire solo dopo spike per app/IME e misure affidabilità |
| ADR-033 | Aperto | Sicurezza campi non classificabili | Fail-closed come impostazione iniziale; quantificare costo UX |
| ADR-034 | Aperto | Wayland privileged path | Valutare se `evdev/uinput` debba rimanere plugin/feature opzionale o fuori scope |
| ADR-035 | Aperto | Picker manuale P0 | V1.6 lo proponeva; ora motivazione più forte per Wayland, non ancora ratificata |

Le decisioni v1.7 non sovrascrivono ADR precedenti. Fonte: [NATIVE_INPUT_OUTPUT.md](NATIVE_INPUT_OUTPUT.md).

## Proposte ADR v1.8 — Persistenza e migrazione (NON ratificate)

| ID | Stato | Proposta / problema da chiudere |
|---|---|---|
| ADR-036 | Proposto | Importazione non esecutiva di Beeftext/Espanso, anteprima e avvisi per macro non compatibili |
| ADR-037 | Proposto | Snapshot coerente prima di restore, commit atomicamente verificabile e recovery point |
| ADR-038 | Aperto | SQLite interno vs JSON unico, confronto di semplicità/affidabilità |
| ADR-039 | Proposto | Export JSON versionato lossless; backup completo distinto da export e CSV |
| ADR-040 | Aperto | UUID originali, mapping, gruppo di destinazione e collisioni fra formati |
| ADR-041 | Aperto | Retention/cifratura/portable backup e scopo delle preferenze nei backup |
| ADR-042 | Proposto | Unicode payload originale byte/char-preserved; normalizzazione solo nella chiave di confronto documentata |

**Evidenze:** [audit C++](../research/audit/BEEFTEXT_PERSISTENCE_SOURCE_AUDIT.md), [migrazione](../research/audit/MIGRATION_CONTRACT.md), [storage](../research/audit/STORAGE_DECISION_RECORD.md). Non trasformare questi ADR in scelte adottate senza una decisione esplicita.

## Nuove proposte ADR v1.9 — lifecycle, preferenze, dipendenze

| ID | Stato | Proposta / decisione in sospeso |
|---|---|---|
| ADR-043 | Proposto | `AppLifecycle` minimale con single-instance e pausa esplicita, niente service locator globale |
| ADR-044 | Proposto | Import delle preferenze validato interamente e applicato come batch, con fallback/recovery |
| ADR-045 | Proposto | Distinguere i profili `Excluded/Security` dai profili `OutputCompatibility` |
| ADR-046 | Proposto | Temi/lingua/shortcut aggiornabili runtime via notifiche osservabili senza riavvio |
| ADR-047 | Aperto | Regola first-run/tray/quit per Win/macOS/Linux e comportamento senza tray disponibile |
| ADR-048 | Aperto | Modalità portable e destinazione storage separate per OS; autostart e permessi |
| ADR-049 | Aperto | Politica license e attribution della nuova codebase e pacchetti NuGet/assets |
| ADR-050 | Aperto | Piattaforme minime e build pipeline; CMake/qmake originari non vincolano .NET |

Vedi [audit architetturale](../research/audit/BEEFTEXT_ARCHITECTURE_AUDIT.md), [settings/i18n](../research/audit/SETTINGS_LIFECYCLE_I18N.md) e [licenze](../research/audit/DEPENDENCIES_BUILD_LICENSES.md). Lo stato degli ADR rimane proposta/aperto, non decisione già presa.


## Proposte aggiuntive v2.0 (non ancora ratificate)

| ID | Stato | Proposta | Gate di verifica |
|---|---|---|---|
| ADR-050 | Proposto | Installazione e portable con **directory dati separate**; uninstaller non elimina la libreria | VM upgrade, uninstall, readonly |
| ADR-051 | Proposto | Updater automatico **non** obbligatorio in v1; release manuale verificabile | threat model + UX |
| ADR-052 | Proposto | SHA-256 **e** verifica autenticità del pacchetto dove applicabile; manifesto fidato | signing, release pipeline |
| ADR-053 | Aperto | MSI/MSIX/Inno/altro per Windows; firme e gestione UAC | test Win + installer policy |
| ADR-054 | Aperto | DMG/PKG notarizzato per macOS, tar.gz/AppImage o DEB/RPM per Linux | CI OS + disponibilità native backend |
| ADR-055 | Proposto | SBOM e third-party notices per ogni release; license review | checkout pinned, deps list |
| ADR-056 | Aperto | Self-contained vs framework-dependent .NET per piattaforma | misure dimensioni, bootstrap e patching |

Vedi [politica di distribuzione](RELEASE_DISTRIBUTION_POLICY.md) e [audit v2.0](../research/audit/BEEFTEXT_RELEASE_INSTALLER_UPDATER_AUDIT.md).

## Decisioni/questioni aggiornate v2.1 (non ratificate salvo dove esplicitato)

- **Confermato dall'utente:** utilizzare gli ZIP sorgenti forniti come baseline di audit; escludere dal dossier `archive` e `beeftext-screenshots`; conservare documento canonico unico per argomento.
- **Proposto:** non importare struttura di 15 crate Espanso, né manager globali Beeftext; preferire poche assembly e Vertical Slice.
- **Proposto:** errori output vanno restituiti come risultato esplicito; mai considerare `event consumed` un'operazione riuscita (Espanso executor `text_inject.rs`).
- **Aperto:** licenza finale nuova app (Beeftext MIT, Espanso GPLv3), policy riuso e contribuzione; decisione prima di copiare materiale di terzi.
- **Aperto:** storage SQLite vs JSON; necessarie ADR e benchmark/robustezza.
- **Aperto:** fonte e versione attestata dei submodule; snapshot ZIP senza `.git` non fornisce SHA commit.
- **Aperto:** strategia input per ogni OS da ratificare solo dopo spike reali, soprattutto Wayland.

Riferimenti: [baseline](../research/audit/LOCAL_SOURCE_BASELINE_v2.1.md) e [confronto](../research/audit/CROSS_PROJECT_ENGINE_DESIGN_v2.1.md).

## Proposte v2.2 — submodule e catalogo emoji (non ratificate)

| ID | Stato | Proposta o questione | Condizione |
|---|---|---|---|
| ADR-057 | Proposto | Catalogo emoji offline, versione e fonte pin-nata; non dipendenza runtime npm | verifica licenza e qualità dati |
| ADR-058 | Proposto | Keyword per lingua distinte da alias/trigger di espansione; niente attivazione automatica per alias ambigui | UX picker e test collisioni |
| ADR-059 | Proposto | Adapter di output restituiscono risultato esplicito e controllano `SendInput`; nessuna fiducia implicita nell'iniezione | spike Win + altri OS |
| ADR-060 | Proposto | Eventi Unicode, coppie surrogate, VS16 e ZWJ validati in E2E; il dato stringa resta invariato | test OS e IME |
| ADR-061 | Proposto | Hotkey con ownership deterministica e cleanup su failure | prove collisione Win/macOS/Linux |
| ADR-062 | Aperto | Quale catalogo Unicode/emoji e versioni/lingue usare nella v1 | validazione copertura e notices |
| ADR-063 | Aperto | Commit submodule corretti per compilazione fedele Beeftext v16 | gitlink verificabile |

Nessuna proposta modifica le ADR confermate dall'utente. Evidenze in [XMiLib](../research/audit/XMILIB_DIRECT_SOURCE_AUDIT_v2.2.md) ed [emojilib](../research/audit/EMOJILIB_DIRECT_SOURCE_AUDIT_v2.2.md).

## v2.3 — architettura consolidata per studio, non ancora implementata

| ID | Stato | Proposta | Note |
|---|---|---|---|
| ADR-060 | **Consolidato per studio / da approvare** | 4 progetti logici `Core`, `Application`, `Infrastructure`, `Desktop` con slice per feature | [ADR completo](adr/ADR-0001-VERTICAL-SLICE-BOUNDARIES.md) |
| ADR-061 | **Consolidato per studio / da approvare** | Core puro, no OS/UI in Core e Application; port nativi in Infrastructure | La validazione concreta avviene dopo spike |
| ADR-062 | **Consolidato per studio / da approvare** | `Prepare` + `Preflight` prima di trigger deletion, outcome parziali espliciti | Nessun rollback universale |
| ADR-063 | **Proposto** | .NET 10 LTS come baseline invece di .NET 11 STS | Supporto fino a novembre 2028; rivedere prima di avviare sviluppo |
| ADR-064 | **Proposto** | No `TreeDataGrid` Pro senza decisione su licenza, no processi separati per default | Valutare `DataGrid` community/controllo alternativo |
| ADR-065 | **Aperto** | Licenza finale, SQLite vs JSON, OS certificati, policy key→committed text | Non ratificati |

La presenza di un ADR in forma completa *non* implica l'approvazione: quelli nuovi rimangono **proposti**. Riferimenti: [ADR confini](adr/ADR-0001-VERTICAL-SLICE-BOUNDARIES.md), [licenze](adr/ADR-0002-LICENSE-AND-DEPENDENCIES.md).

## Proposta v2.4 — ADR stabile senza riutilizzare ID della vecchia tabella

- [ADR-0003 — Modello di dominio minimale e outcome di inserimento](adr/ADR-0003-DOMAIN-MODEL-AND-OUTCOME.md): **PROPOSTO, non ancora ratificato**. Include `Snippet`, `Group`, `TriggerDefinition`, `MatchResult`, `ExpansionPlan` in Core, `InputSession`/`TargetContext`/`InsertionOutcome` al confine applicativo, identificatori/revisioni, piano completo prima degli effetti, nessun retry cieco e `SubmittedUnverified` distinto da successo attestato.
- **Aperti**: UUID importati, storage SQLite vs JSON, limiti numerici, politica case folding/NFC e overlap, subset macro P0, output verificabile per target/OS, caret placement e default delimiter.
- Gli ID ADR-0xx nelle tabelle storiche contengono numerazioni riutilizzate; **per le nuove decisioni usare esclusivamente il nome file `ADR-0003`, `ADR-0004`...** con ID univoco. Le vecchie righe sono testimonianza storica e non vanno riscritte silenziosamente.

## v2.5 — decisione proposta, non ratificata

[ADR-0004 — Matching Unicode, NFC e word boundary](adr/ADR-0004-UNICODE-MATCHING-AND-WORD-BOUNDARIES.md) rimane **PROPOSTO**. Il default di v2.4 `Exact` è stato reinterpretato come opzione conservativa; la nuova proposta è `NFC` *solo per la MatchKey*. Nessun passaggio automatico di licenza, storage, scope priority o OS supportato. La fase di studio non ratifica l'implementazione.
