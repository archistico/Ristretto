# Architettura — guida alla lettura corrente

La solution mantiene quattro assembly di produzione e il progetto xUnit. I quattro tool di laboratorio sono separati dall'app desktop. Il Core implementa F1a/F1b; Application include il contratto di sostituzione; la prova nativa alpha.7 resta confinata al proprio EDIT, senza observer globale nel prodotto.

Le sezioni versionate sotto conservano l'evoluzione dei confini architetturali; le diciture di avanzamento si riferiscono alla versione indicata. Per funzioni e verifiche odierne consultare [stato corrente](CURRENT_STATE.md) e [rapporti](../reports/README.md).

## Implementazione v2.9 — prima Vertical Slice (storico)

La solution implementa 4 progetti (`Core`, `Application`, `Infrastructure`, `Desktop`) e uno xUnit, senza quinta assembly di produzione. `Core` contiene modelli e matcher puro; `Application` contiene i casi d’uso; `Infrastructure` implementa `ILibraryStore` su SQLite e Online Backup API; `Desktop` offre GUI Avalonia. Infrastruttura nativa di input/output **non implementata**. Build runtime **non verificata**: assente SDK nell’ambiente.

---

# v2.8 — confine SQLite confermato

**SQLite approvato** come persistence backend locale: `Infrastructure/Storage` contiene accesso parametrizzato, migrazioni e backup; `Application` coordina use case, transazioni logiche, serializzazione scritture e pubblicazione matcher; `Core` resta puro; `Desktop` usa ViewModel e servizi Application. `Runtime/Matching` e callback nativi non interrogano mai DB. Indici in memoria ricostruibili e versionati dopo `COMMIT` e `library_revision` coerente. [Specifica canonica](SQLITE_STORAGE.md) · [ADR approvato](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md).

---

# Aggiornamento v2.7 — Motore leggero, snapshot e lifecycle

[Specifica canonica delle prestazioni](PERFORMANCE_RESOURCE_LIFECYCLE.md) e [ADR-0006 proposta](adr/ADR-0006-RESOURCE-LIFECYCLE-AND-CONCURRENCY.md). **Nessuna quinta assembly**: l'input callback nativo non attende I/O o GUI; eventi in queue bounded, sessione invalidata a overflow; matcher seriale con snapshot immutabili; index del picker separato, query cancellabili; output single-flight e stop esplicito. I budget PB-01..PB-12 non sono misurati. La qualità del risultato e la tutela del testo prevalgono sulla velocità.

---

# Output v2.6: confine applicazione/OS

Il coordinatore `ExecuteExpansion` vive nell'`Application` (slice), usa `ExpansionPlan` immutabile da `Core` e port nativi implementati in `Infrastructure`. `Desktop` renderizza errori e fallback. Non creare fifth assembly, middleware chain o universal unit-of-work per gestire un workflow che attraversa applicazioni esterne: **non esiste commit atomico cross-process**. [Contratto dettagliato](OUTPUT_COORDINATION.md).

---

# Architettura canonica — proposta v2.3 (fase di studio)

> **Stato:** architettura candidata **consolidata per lo studio**, non codice implementato. I confini logici sono definiti; API e modalità di inserimento restano soggetti agli spike nativi. Questo è il documento canonico: vecchie indicazioni divergenti vanno considerate supersedute da questa versione.
>
> **Fonti dirette:** `Beeftext-master.zip`, `espanso-dev.zip`, `XMiLib-master.zip`, `emojilib-main.zip`. Dettagli e linee: [audit comparativo](../research/audit/FOUR_SOURCE_ARCHITECTURE_AUDIT_v2.3.md).

## 1. Obiettivo e principi

Prodotto desktop **open source**, C# / **.NET 10 LTS** come baseline proposta, **Avalonia** per GUI Windows/macOS/Linux; rapido, compatto, immediato, offline-first, tema chiaro/scuro/automatico, localizzazione e Unicode completo. .NET 11 STS esiste già alla data dello studio, ma .NET 10 LTS riduce la frequenza degli upgrade maggiori (fonte: https://dotnet.microsoft.com/en-us/platform/support/policy). La versione Avalonia va bloccata solo dopo verifica API, licenze e build.

Il programma deve essere un **text expander**, non una piattaforma general-purpose di macro. La priorità è l'affidabilità del riconoscimento, del rendering e dell'inserimento; GUI e persistenza non possono accoppiarsi agli hook globali.

Principi vincolanti:

1. **Vertical Slice Architecture pragmatica:** organizzare per casi d'uso; non trasformare ogni slice in un assembly, layer, mediator o repository generico.
2. **Core algoritmico puro:** matching, Unicode policy, conflitti, parsing di strutture, pianificazione senza I/O, API native, Avalonia o database.
3. **Effects boundary:** distinguere pianificazione e interazioni esterne. Nessun rollback universale dopo modifica di una app di terzi.
4. **Capabilities esplicite:** `Available`, `PermissionRequired`, `Unsupported`, `Unknown` per singola capacità e contesto; non equiparare “GUI avviabile” a “text expansion automatica supportata”.
5. **Privacy fail-closed:** non raccogliere testo da password/campi protetti; non persistere i tasti; log privi di payload; limite sulla memoria temporanea.
6. **Unicode end-to-end:** contenuto originale preservato; nessun uso di `string.Length` come sinonimo del numero di Backspace per eliminare il trigger; IME solo su testo realmente committato, se osservabile.
7. **Dipendenze verificabili:** preferire SDK .NET e librerie open source mantenute, con SBOM e controllo licenze; nessun riuso del codice GPLv3 Espanso in un progetto dalla licenza incompatibile.
8. **Semplicità misurabile:** un'astrazione deve separare un effetto esterno reale, un'incertezza di piattaforma o un confine testabile; non esiste “perché Clean Architecture lo richiede”.

## 2. Quattro progetti di produzione, non quindici

Proposta iniziale (nomi indicativi, non riservati):

```text
Ristretto.sln
src/
  TextExpander.Core/                # Modelli e algoritmi puri
    Snippets/                        # entità e value objects essenziali
    Matching/                        # trigger, delimitatori, collisioni, Unicode
    Expansion/                       # parser, segmenti, validazione e piani puri
    Search/                          # ranking del picker, se realmente condiviso
  TextExpander.Application/         # Vertical slice; orchestrazione dei casi d'uso
    Features/
      Library/                       # Add/Edit/Delete/Move/Enable/Find
      Groups/                        # Add/Rename/Enable/DeleteSafely
      TriggerRecognition/            # sessione di matching su buffer volatile
      ExpandText/                    # preparazione + coordinatore effetti
      Picker/                        # Search/Open/Select/Copy/Insert
      EmojiCatalog/                  # Load/Search/Keywords
      Settings/                      # Read/Validate/Apply
      ImportExport/                  # Inspect/Preview/Commit/Export
      BackupRestore/                 # Snapshot/Validate/Restore
      Diagnostics/                   # CapabilityReport/RedactedEvents
      Lifecycle/                     # Start/Pause/Resume/Exit/SecondInstance
    Ports/                           # SOLO interfacce per veri confini I/O
  TextExpander.Infrastructure/      # Implementazioni di persistenza e OS
    Persistence/                     # backend locale + migrazioni
    Files/                           # export/backup atomicità FS
    Importers/                       # parser formato Beeftext/Espanso
    Platform/
      Windows/                       # hook, iniezione, focus, clipboard, hotkey
      MacOS/                         # Quartz/AX/pasteboard, per quanto consentito
      Linux/X11/                     # X11/XKB/XTest/selection, test reali
      Linux/Wayland/                 # capability-driven, backend per compositor
    Diagnostics/                     # sink locali con redazione
  TextExpander.Desktop/             # Avalonia, presentation e composition root
    Features/
      Library/ Groups/ Picker/ Settings/ Diagnostics/
    Shell/                          # menu, navigation, tray, single window
    Themes/ Localization/ Assets/
    Bootstrap/                      # DI e wiring del runtime

tests/
  TextExpander.Core.Tests/
  TextExpander.Application.Tests/
  TextExpander.Infrastructure.Tests/
  TextExpander.Desktop.Tests/        # GUI/viewmodel in seguito
  TextExpander.Native.Tests/         # SOLTANTO dopo primo adapter reale
```

### Dipendenze permesse

```text
Desktop ────────> Application ────────> Core
  |                    ^
  └───> Infrastructure ┘
                    |
                    └───────────────> Core
```

- **Core** non referenzia nessun altro progetto produttivo; nessuna dipendenza ad Avalonia/SQLite/Win32.
- **Application** referenzia Core, usa modelli e porte per effetti esterni; non conosce tipi di database o classi Avalonia.
- **Infrastructure** referenzia Application (+ Core, se serve) e implementa porte; nessun `ViewModel`.
- **Desktop** referenzia Application e Infrastructure per costruire il composition root; l'uso di tipi Core per binding DTO semplici è da limitare, non vietato dogmaticamente.
- Nessuna dipendenza ciclica. Test e tool non costituiscono dipendenze produttive.

Non creare `TextExpander.Windows`, `MacOS`, `X11`, `Wayland` come progetti vuoti. Estrarli **solo dopo** che un adapter concreto lo richiede per P/Invoke, build condizionale, firma, packaging, sicurezza o isolamento della dipendenza. Non creare un processo background separato solo per analogia con Espanso: deciderlo dopo la misurazione.

## 3. I componenti condivisi e cosa NON vi entra

### `Core.Snippets`

ID stabili, titolo, trigger, testo originale, gruppo, enabled, scope, modalità di match, case policy, metadata essenziali. Validazioni pure, preservazione puntuale del payload Unicode. L'oggetto non chiama salvataggi, notifica GUI o scanner di processi. Versione/formato di import e record database **non** sono il modello di dominio.

### `Core.Matching`

- Compilazione di una **snapshot immutabile** di trigger attivi quando cambia la libreria, sostituita atomicamente nel motore senza bloccare l'hook.
- Matching esatto e a delimitatore per priorità P0, confini espliciti, collisioni determinate senza random.
- Buffer di input **volatile, per contesto**, con limite misurabile; invalidazione su cambio target, cursor/navigation, selezione o contesto incerto. Nessun salvataggio del digitato.
- Distinguere eventi tastiera fisica da testo committato. Un evento `Unknown`/IME non osservabile **non** può essere spacciato per carattere inserito.
- NFC soltanto per confronti, secondo policy esplicita; **mai riscrivere il contenuto snippet dell'utente**. Case folding e confini con Unicode richiedono fixture e attenzione a culture/lingue.
- Caratteri UTF-16, Unicode scalar e grapheme cluster sono grandezze differenti; nessuna loro equivalenza con “tasti Backspace necessari nel campo di destinazione”.

### `Core.Expansion`

Parser delle istruzioni supportate, esecuzione logica di interpolazioni **puramente deterministiche** su input già risolti e produzione di `ExpansionPlan` immutabile: segmenti testo, azioni consentite, posizionamento cursore (se compatibile), requisiti. Non invia tasti. Variabili con input utente o accesso al sistema sono risolte nella **preparazione applicativa** prima del primo effetto esterno.

### `Core.Search` / `EmojiCatalog`

Ranking e ricerca in memoria possono essere condivisi quando necessari a GUI e picker; dataset emoji versionato e validato, con parole chiave locali. Il dataset non viene incorporato nel matcher globale per default; autocorrezione e shortcode emoji sono policy **opt-in** distinte dagli snippet.

## 4. Vertical slice applicative e ownership

La fonte completa è [catalogo delle slice](VERTICAL_SLICES.md). Per ogni caso d'uso:

- una richiesta/risposta esplicita;
- un handler o use-case concreto (non per forza un'interfaccia);
- validazione al confine;
- dipendenze solo verso algoritmi puri o porte necessarie;
- test affiancati sul comportamento;
- nessun binding Avalonia dentro la slice applicativa.

Le slice possono condividere **modelli e regole invarianti** dal Core ma non devono chiamarsi a vicenda indiscriminatamente. Coordinamenti trasversali (`ExpandText` legge snapshot della libreria, usa capacità OS; `ImportExport` aggiorna libreria) sono espliciti. Solo estrarre servizi condivisi quando due slice usano lo stesso comportamento con identica semantica.

## 5. Percorso di una espansione

```text
[OS Input Observation]
       │ stato evento: Committed / Composing / Unknown
       v
[Input Session / Target Context]───invalid?───> Reset / NoChange
       │
       v
[Core Trigger Matching]───────────no match/conflict───> NoChange / Chooser
       │ un MatchCandidate deterministico
       v
[Application PrepareExpansion]
       │ risoluzione safe variables, eventuali form (CANCEL prima degli effetti)
       v
[ExpansionPlan immutabile]───────invalid─────────────> NoChange
       │
       v
[Native Preflight: focus, target, permessi, modalità, clipboard]
       │ unsupported / target lost ──> Cancelled (senza cancellazione)
       v
[Optional Trigger Removal]──> [Insert Text/Actions]
       │                         │
       └───────────────> [Outcome: Success/Partial/Failed]
                                 │
                                 v
             [Conditional clipboard cleanup + redacted diagnostics]
```

### Requisiti dell'orchestrazione

- È un **coordinatore singolo per l'effetto esterno**; nessun secondo inserimento concorrente nella stessa sessione.
- Hook nativo a **latenza minima**, non esegue query SQLite, UI, clipboard o chiamate bloccanti; forwarding asincrono e budget/buffer limitati. La scelta di `Channel` o altra coda avviene nello spike per misurare perdite/overflow e ordering.
- Il piano si costruisce prima di modificare il target; i popup di compilazione non partono dopo l'eliminazione del trigger.
- Prima di ogni effetto verificare identità target/focus secondo ciò che la piattaforma permette; se è impossibile garantire la destinazione, **non procedere automaticamente**.
- Gli input sintetici devono essere identificabili e non riattivare il matcher; ripristino/pausa hook con cleanup in ogni percorso (`finally`/disposable).
- Rimozione trigger e iniezione testo sono **effetti non atomici**: un insuccesso può essere `PartialInsert`; nessuna promessa generica di rollback, annullamento o riconciliazione perfetta.
- Preferire metodo appropriato per piattaforma e target (eventi testo Unicode, incolla con lease clipboard, eventualmente API di accessibilità ove consentito) e mostrare esplicitamente eventuali override/limitazioni.

## 6. Contratti per dati, preferenze e importazione

- Una sola fonte di verità per la libreria locale. **SQLite è candidato**, non ADR ratificato; confronto finale con JSON prima di codificare migrazioni o ORM.
- Export pubblico versionato JSON UTF-8 ≠ backup completo ≠ import esterno. Tutti con anteprima, validazione, report di perdita e commit consistente.
- Importatori Beeftext/Espanso **non eseguono** script/shell, né risolvono automaticamente file referenziati senza permesso; moduli di infrastruttura isolati dal motore live.
- Scritture a prova di crash dove supportate, snapshot e ripristino in staging, backup con retention; mai scrivere dati utente nella directory uninstallabile dell'app normale.
- Preferenze: parsing/validazione/applicazione in batch; cambi sensibili (hotkey, backend, OS permissions) possono fallire e vanno riportati senza lasciare la UI in stato falsamente attivo.

## 7. GUI, i18n e tema

`Desktop` ospita View e ViewModel per feature, finestra principale compatta a due pannelli, editor, picker, preferenze e diagnostica. Menu tray visualizza stato del motore ma non contiene algoritmo. Interfaccia responsive per densità e tastiera; visual assets leggeri, gradienti discreti e niente animazioni necessarie.

`ResX` come candidato per le traduzioni e servizio osservabile per cambio a runtime; localizzazione di date/ora in snippet distinta dalla lingua dell'interfaccia. `RequestedThemeVariant` per chiaro/scuro/automatico (https://docs.avaloniaui.net/docs/styling/theme-variants). Le stringhe `x:Static` in Avalonia non aggiornano automaticamente l'UI: serve binding osservabile o refresh esplicito (https://docs.avaloniaui.net/docs/app-development/localizing). Le traduzioni dell'utente e la libreria snippet non cambiano al cambio lingua UI.

Valutazione della versione Avalonia prima dello sviluppo: in **Avalonia 12** compiled bindings sono predefiniti con `x:DataType` (https://docs.avaloniaui.net/docs/data-binding/compiled-bindings). `TreeDataGrid` nella documentazione v12 è classificato **Avalonia Pro**; non adottarlo senza licenza appropriata. Per la lista compatta valutare `DataGrid` o layout a righe con binding e virtualizzazione adeguata (https://docs.avaloniaui.net/controls/data-display/structured-data/treedatagrid).

## 8. Adapter di piattaforma e sicurezza

Vedere [contratti nativi e matrice capability](PLATFORM_ADAPTER_CONTRACTS.md). Windows: hook, focus e input Win32 con P/Invoke piccolo (`LibraryImport` quando adatto); macOS: event taps, Accessibility e pasteboard; Linux X11: XTest/XKB/selections; Linux Wayland: disponibilità subordinata a compositor/protocolli/permessi. Il portale `GlobalShortcuts` supporta hotkey, **non** osservazione del testo; il portale `RemoteDesktop` autorizza input di sessione, non una soluzione automatica generale per ogni campo.

Privilegi minimi, niente keylogging persistente, nessun telemetria obbligatoria, diagnostica con eventi redatti, no esecuzione di macro importate. Le capability devono poter peggiorare dinamicamente dopo revoca permessi, cambio display server o Secure Input.

## 9. Lifecycle e concorrenza

1. Avvio singola istanza per profilo; seconda istanza porta avanti la finestra o invia comando sicuro.
2. Acquisizione percorso profilo/config; validazione e inizializzazione storage; recovery prima dell'hook.
3. Caricamento libreria e costruzione snapshot matcher.
4. Rilevamento capacità/permessi e registrazione hotkey.
5. Abilitazione osservazione soltanto quando pronto; UI può funzionare anche se hook `Unsupported`.
6. `Pause` disattiva trigger, azzera buffer, mantiene gestione libreria e picker dove consentito; `Resume` verifica nuovi permessi; `Exit` libera hotkey/hook/risorse e chiude I/O.
7. Cambio libreria pubblica nuova snapshot; eventi pendenti con versione vecchia sono scartati o gestiti per policy documentata, **mai** applicati ad oggetti eliminati.

Un solo processo è il default iniziale. Un worker separato o architettura multi-processo si aggiunge solo per prova di isolamento permessi/sicurezza/affidabilità; non per moda.

## 10. Qualità, prestazioni e licenze

Test puri di matching/rendering; integrazione di persistence/clipboard fake; GUI smoke; test runtime di hook/focus/IME su sistemi reali. Benchmark con mediana e p95 per rilevamento/espansione/picker e grandi librerie, **soglie stabilite dopo baseline**, non promesse arbitrarie.

.NET 10 LTS preferito a .NET 11 STS per stabilità del prodotto. Dipendenze minime. `CommunityToolkit.Mvvm` è candidato, non obbligo. Mediator, event bus, plugin scripting, sync cloud, CQRS generico, AutoMapper, ORM e DI tramite reflection non entrano di default. SQLite o JSON resta una decisione da ratificare.

Licenza del **nuovo prodotto** da decidere prima del primo commit. Beeftext, XMiLib ed emojilib MIT richiedono rispetto dei notices se il codice/dataset viene riusato; Espanso GPLv3 è fonte di studio funzionale/architetturale, non codice da copiare in un progetto destinato a licenza permissiva.

## 11. Decisioni e gate

- **Consolidato per lo studio:** quattro progetti produttivi logici; vertical slice per feature; core puro; integrazioni native isolate; preparazione prima dell'effetto; UI Avalonia senza P/Invoke; Unicode e privacy invarianti; no overengineering.
- **Aperto:** nome e licenza; SQLite vs JSON; scelta controlli Avalonia e versione esatta; contratti esatti del bridge nativo; OS/versioni certificate; politica collisioni UX finale; trigger globale in editor con IME parziale; permessi Wayland; opt-in emoji; metrica prestazioni.
- **Gate prima di sviluppo:** approvazione della mappa feature, del contratto P0 del motore e del piano degli spike; licenza/dipendenze verificate.
- **Gate prima di rilascio:** prove su OS/app target certificati, permessi/revoca, clipboard concorrente, import/restore, Unicode/IME, accessibilità GUI e pacchetti firmati ove applicabile.

Appendici operative: [slice](VERTICAL_SLICES.md) · [contratti](PLATFORM_ADAPTER_CONTRACTS.md) · [test](ARCHITECTURE_TEST_PLAN.md) · [ADR](adr/ADR-0001-VERTICAL-SLICE-BOUNDARIES.md) · [audit diretto](../research/audit/FOUR_SOURCE_ARCHITECTURE_AUDIT_v2.3.md).

## Addendum canonico v2.4 — oggetti e confini dei dati

La proposta delle quattro assembly resta invariata. Per il prossimo gate il riferimento unico al modello è [DOMAIN_MODEL.md](DOMAIN_MODEL.md); i passaggi e le uscite sono in [EXPANSION_LIFECYCLE.md](EXPANSION_LIFECYCLE.md), i soli port necessari in [DOMAIN_CONTRACTS.md](DOMAIN_CONTRACTS.md). `Snippet/Group/Trigger` e gli algoritmi sono Core; `InputSession` e `ExpansionAttempt` sono Application; `TargetContext`/`InsertionOutcome` sono contratti applicativi senza handle OS, implementati dagli adapter nativi. **Il modello dei record esterni Beeftext/Espanso non diventa il nostro modello dati**.

Regole rinforzate: optimistic revision sugli aggiornamenti; snapshot matcher pubblicata solo dopo commit; il template literal preserva la sequenza Unicode; preparazione completa prima di qualsiasi cancellazione; stato `SubmittedUnverified` quando la correttezza del testo nel target non è attestabile. `string.Length` non autorizza un certo numero di Backspace. Confronto `InvariantIgnoreCase` e `NFCCompare`, valori limite, storage finale, metodi OS e verifiable output sono proposte/decisioni aperte. Vedi [ADR-0003](adr/ADR-0003-DOMAIN-MODEL-AND-OUTCOME.md) (**proposto**).

## Evoluzione v3.4 — backend Windows isolato

`UnicodeOutputPlan` si trova nella vertical slice `Application/Features/Output` e non dipende da API native o Avalonia. `WindowsUnicodeTextOutput` e la sola classe di P/Invoke `Win32TextApi` si trovano in `Infrastructure/Platform/Windows`. La GUI non li invoca in questa versione. [Specifiche e limiti](WINDOWS_OUTPUT_SPIKE.md).
