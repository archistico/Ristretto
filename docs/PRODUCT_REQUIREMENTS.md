# Requisiti — catalogo e criteri di accettazione

> **Lettura corrente:** gli ID sotto sono conservati per tracciabilità e includono funzionalità future. Libreria SQLite, backup, import/export, picker, temi e cinque lingue sono implementati; motore e sostituzione sono verificati nei laboratori. L'espansione in app esterne resta da realizzare. Le note versionate registrano lo stato storico, non l'avanzamento odierno: [stato corrente](CURRENT_STATE.md) e [roadmap](FUNCTIONAL_ROADMAP.md).

## Requisito consolidato v2.8 — persistenza approvata (storico)

**REQ-STORAGE-001 APPROVATO:** libreria persistente **SQLite locale** per snippet/gruppi/trigger/impostazioni; senza server e senza account. **REQ-STORAGE-002:** JSON UTF-8 versionato per scambio/portabilità, distinto dal DB. **REQ-STORAGE-003:** backup consistente verificato e restore sicuro. **REQ-STORAGE-004:** indici matcher/picker solo in memoria, nessuna query per ogni tasto. **REQ-STORAGE-005:** conservazione esatta Unicode e dati utente. Lo schema e i test sono specificati ma ancora non implementati. [Dettagli](SQLITE_STORAGE.md) · [ADR-0007](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md).

---

# Requisiti funzionali — baseline proposta v1.5

**Legenda:** P0 essenziale al rilascio di qualità su una piattaforma certificata; P1 estensione vicina; P2 dopo prodotto stabile; P3 ricerca. Non sono tutte funzionalità già sviluppate. Gli ID sono stabili da ora in avanti.

| ID | Priorità | Requisito | Criterio di accettazione sintetico |
|---|---|---|---|
| FR-001 | P0 | Libreria snippet | CRUD, ricerca, duplicazione, spostamento, enable/disable senza perdita dati |
| FR-002 | P0 | Gruppi | Ordinamento, disattivazione collettiva, eliminazione non distruttiva di default |
| FR-003 | P0 | Trigger esatti | Matching deterministico, case rules, confini, scope applicativo; nessun trigger casuale |
| FR-004 | P0 | Unicode completo in storage | Round-trip emoji ZWJ, ideogrammi, combinazioni accentate, multilinea e RTL senza alterazioni |
| FR-005 | P0 | Espansione in app esterna | Plan validato prima di modificare il target; stato e focus governati |
| FR-006 | P0 | Metodi output | Interfaccia per incolla/digitazione con diagnostica e fallback espliciti |
| FR-007 | P0 | Gestione contenuti sensibili | Nessuna memorizzazione dei tasti, campi protetti, esclusioni per app |
| FR-008 | P0 | Import/export libreria | Formato versionato, validazione/anteprima/rollback, import Beeftext compatibile quando validato |
| FR-009 | P0 | Backup e ripristino | Salvataggio atomico, backup verificabile, ripristino con anteprima e conferma |
| FR-010 | P0 | GUI essenziale | Finestra compatta gruppi/snippet, editor, ricerca, feedback/errori, scorciatoie |
| FR-011 | P0 | Temi | Chiaro/scuro/automatico con contrasto e focus visibile |
| FR-012 | P0 | Multilingua UI | Inglese fallback, italiano e risorse predisposte fr/de/es; cambio senza riavvio da verificare |
| FR-013 | P0 | Qualità e diagnostica | Log senza payload sensibili, errori comprensibili, autotest delle capacità OS |
| FR-014 | P0 | Pausa motore | Stato chiaro in tray/finestra e shortcut configurabile; nessun hook lasciato attivo dopo uscita |
| FR-015 | P1 | Picker globale | Search-as-you-type, tastiera completa, inserisci/copia, recupero focus sicuro |
| FR-016 | P1 | Modalità trigger | Immediato, delimitatore mantenuto/scartato, confini di parola configurabili |
| FR-017 | P1 | Variabili sicure | Data/ora, clipboard su consenso contestuale, cursore, riferimenti aciclici |
| FR-018 | P1 | Form compilabili | Testo/textarea/choice/date, anteprima e annullamento senza alterare target |
| FR-019 | P1 | Profili app | Esclusioni, scelta strategia di inserimento e ritardi solo su evidenza |
| FR-020 | P1 | Cestino e Undo | Ripristino di eliminazioni, undo espansione solo ove sicuro |
| FR-021 | P1 | Tags/favoriti | Filtri picker, più classificazioni senza duplicare snippet |
| FR-022 | P1 | Import da concorrenti | Candidati Espanso/Raycast/AutoHotkey/Alfred; non importare azioni eseguibili |
| FR-023 | P1 | Lingua contenuto | Locale di date e numeri indipendente dalla lingua della GUI |
| FR-024 | P2 | Emoji e autocorrezione | Dizionari opt-in, niente collisioni con il codice digitato |
| FR-025 | P2 | Rich text e immagini | Valutazione separata su formati clipboard multipli e fallback plain text |
| FR-026 | P2 | Sincronizzazione file | Opzionale, export + conflict handling, no cloud mandatory |
| FR-027 | P2 | Statistiche d'uso | Locali e non invasive, opt-in, senza contenuto tasti |
| FR-028 | P3 | Regex trigger | Budget prestazionale, prevenzione ReDoS, limiti chiari |
| FR-029 | P3 | Scripting/estensioni | Sandboxing/policy consenso da studiare; non eseguito da import senza consenso |
| FR-030 | P3 | Team collaboration | Conflitti, permessi, auditing, crittografia; non nell'MVP |

## Invarianti

- Gli snippet sono dati dell'utente: nessun aggiornamento, import o cambio lingua li modifica senza conferma esplicita.
- Una coppia di trigger ambigua nello stesso contesto **non** è risolta casualmente.
- Nessuna espansione durante composizione IME attiva; gestione precisa dipende da segnali OS e da test.
- Se il sistema non può identificare il campo di destinazione o è protetto, l'inserimento automatico va disabilitato; picker/copia manuale solo se esplicitamente autorizzati.
- Cancel/timeout form avviene **prima** di cancellare trigger, quando il backend lo consente.
- L'app documenta i limiti platform e non tratta la disponibilità di una tray o shortcut come prova di supporto all'hook.

## Da approfondire prima di congelare i requisiti

1. Priorità tra matching `Strict/Loose`, delimitatori e scope app; collisioni e trigger sovrapposti.
2. Semantica di normalizzazione Unicode: differenza fra trigger e payload (mai normalizzare automaticamente il payload salvato).
3. Undo espansione, clipboard già modificata, target che cambia focus mentre si opera.
4. Versione iniziale di macOS/Linux e loro criteri di dichiarazione del supporto.
5. Limiti massimi dimensione snippet e caricamento di grandi librerie.

**Tracciabilità:** `data/FEATURE_BACKLOG_v1.5.csv` aggiunge ispirazioni, costi e fonti.

## Estensione v1.6 — precisione del riconoscimento (requisiti candidati)

| ID | Priorità | Specifica | Accettazione verificabile |
|---|---|---|---|
| FR-031 | P0 | Modalità trigger comprensibili | Immediate, AfterDelimiterKeep/Drop, ManualOnly con test per spazio/punto/Enter/Tab; nessun output ambiguo |
| FR-032 | P0 | Boundary Unicode-aware | Matching non vincolato ad ASCII; separatori configurabili e distinzione start/whole-word |
| FR-033 | P0 | Gestione conflitti preventiva | Trigger equivalenti evidenziati nell'editor/import, nessuna scelta casuale in runtime |
| FR-034 | P0 | Input IME/composizione | Non espandere su composizione non finalizzata; contesto unsupported esplicito |
| FR-035 | P0 | Buffer volatile e contestuale | Reset su focus/caret/selection/nav/permessi non affidabili; nessuna persistenza del digitato |
| FR-036 | P0 | Cancellazione sicura | No backspace ciechi su password/non-text/surface ignote; errori parziali dichiarati |
| FR-037 | P0 | Confronto Unicode prevedibile | Originale preservato; eventuale NFC solo per match secondo policy e segnalazione collisioni |
| FR-038 | P1 | Scelta manuale su alias condivisi | Picker/chooser con conferma e controllo focus, mai commit implicito ambiguo |
| FR-039 | P1 | Diagnostica guidata del motore | Distinguere BoundaryRejected, Collision, PermissionDenied, ClipboardConflict, TargetLost senza payload |
| FR-040 | P2 | Autocorrect e regex | Moduli futuri separati da trigger snippet; non automatizzare fino a prove anti-false-positive |

**Relazioni:** FR-031–037 raffinano FR-003/004/005/007/013; FR-038 propone, senza ratifica, di portare parte del picker FR-015 a P0 per il fallback. Vedi [contratto motore](TRIGGER_ENGINE.md) e [CSV test](../data/TRIGGER_SCENARIOS.csv).


## Nuovi requisiti candidati v1.7 — capacità native (NON implementati)

| ID | Priorità | Requisito | Criterio osservabile |
|---|---|---|---|
| FR-041 | P0 | Capability discovery affidabile | `Unknown` / `Unsupported` / `PermissionRequired` visibili, nessun fallback occulto |
| FR-042 | P0 | Gestione dei permessi e revoca | Motore non opera oltre il consentito; pausa e ripresa senza riavvio quando supportato |
| FR-043 | P0 | Nessuna confusione tasto/testo committato | IME in preedit non produce trigger; percorso non osservabile = niente auto-match |
| FR-044 | P0 | Inserimento Unicode qualificato per target | Fixture emoji/RTL/CJK passano per backend ed editor dichiarati |
| FR-045 | P0 | Controllo focus prima di effetti esterni | Se target o generazione cambiano, non cancellare/inserire in altra finestra |
| FR-046 | P0 | Clipboard lease best-effort e non distruttiva | Copia concorrente dell'utente non viene sovrascritta con restore cieco |
| FR-047 | P0 | Campi protetti fail-closed | `UIA_IsPassword`/Secure Input/ignoto non consentono automatismo insicuro |
| FR-048 | P0 | Report capacità per sistema | Compositore, versione, metodo e limiti elencati, niente badge generici |
| FR-049 | P1 | Profili di compatibilità per target | Backend suggerito/override esplicito, diagnosticabile senza payload |
| FR-050 | P1 | Fallback manuale senza promessa di focus | Picker/copia disponibili quando backend automatico è impossibile |

Riferimenti: [NATIVE_INPUT_OUTPUT.md](NATIVE_INPUT_OUTPUT.md) e [NATIVE_SPIKE_MATRIX.csv](../data/NATIVE_SPIKE_MATRIX.csv).

## Requisiti candidati v1.8 — integrità e migrazione (NON implementati)

| ID | Priorità | Requisito | Accettazione da verificare |
|---|---|---|---|
| FR-051 | P0 | Staging import e restore | Cancel/invalid source non modifica dati persistenti |
| FR-052 | P0 | Compatibilità Beeftext backup JSON v10 | Gruppi, proprietà, testi, UUID e timestamp mappati con report fedele |
| FR-053 | P0 | Import CSV Beeftext | 3 campi keyword/snippet/name UTF-8, quote e multilinea; errori per header incompatibile |
| FR-054 | P0 | Backup consistente prima delle migrazioni | Riavvio dopo failure torna a stato precedente completo o commit nuovo completo |
| FR-055 | P0 | Nessuna esecuzione dei file importati | Macro shell, PowerShell e script rimangono inerti |
| FR-056 | P0 | Report conversione e perdite | Avvisi per campi mancanti, rich text legacy, gruppi e variabili non convertite |
| FR-057 | P0 | Unicode preservato in storage ed export | Fixture complex Unicode, NFD e emoji round-trip esatto |
| FR-058 | P1 | Espanso YAML import limitato | trigger/replace basilari; form/regex/shell non abilitati, include sicuri |
| FR-059 | P1 | Preview diff e collisioni | Preview per gruppo/trigger/ID, scelta esplicita del conflitto |
| FR-060 | P1 | Backup verificabile con restore di prova | integrità, schema, percorsi, retention e report |
| FR-061 | P1 | Export JSON portabile distinto da backup | Formato versionato stabile e pubblicamente documentato |
| FR-062 | P1 | Portable mode paths e file esterni | No corruzione su volume rimosso o directory read-only |

Vedi [DATA_IMPORT_BACKUP.md](DATA_IMPORT_BACKUP.md) e [matrice persistenza](../data/PERSISTENCE_AUDIT_SCENARIOS.csv).

## Nuove proposte tracciabili v1.9 — **non ancora ratificate**

| ID | Priorità | Requisito | Criterio |
|---|---|---|---|
| FR-063 | P0 | Avvio istanza singola e tray | Seconda apertura attiva GUI senza secondo motore; chiudi != esci |
| FR-064 | P0 | Stato/pausa persistente e sicuro | Reset buffer, icona e menu sincronizzati; rilascio risorse a Exit |
| FR-065 | P0 | Preferenze import/export transazionali | Nessuna modifica parziale o scambio chiavi; round-trip verificato |
| FR-066 | P0 | Profili portable e installed distinti | Percorsi, backup e autostart isolati; nessuna perdita dati |
| FR-067 | P0 | Traduzioni dinamiche GUI e tray | Cambio runtime, fallback inglese e traduzioni per regione |
| FR-068 | P0 | Privacy e compatibilità app indipendenti | Campi protetti ≠ applicazioni che non supportano Ctrl+V |
| FR-069 | P1 | Import drag-and-drop validato | Non dedurre formato dalla sola estensione; esito dei multipli esplicito |
| FR-070 | P1 | Gestione aggiornamenti distribuita correttamente | Offline-safe, verifica payload e rispetto opt-in |
| FR-071 | P0 | Conformità licenze dipendenze | Inventario NuGet/assets/emoji e notices in ogni release |

## Requisiti candidati aggiuntivi v2.2 — emoji e dipendenze native

| ID | Priorità | Requisito | Stato |
|---|---|---|---|
| FR-063 | P0 | Preservare output Unicode esatto, anche senza catalogo emoji e con input IME | proposto; test non eseguiti |
| FR-064 | P0 | Inserimento con adapter nativo che riporti errore/partial ed eviti cancellazioni non sicure | proposto; spike OS necessario |
| FR-065 | P1 | Ricerca emoji offline per parola chiave e integrazione picker, localizzabile | proposto |
| FR-066 | P1 | Catalogo versionato, validato, aggiornabile senza perdita dati, fonte/licenza tracciata | proposto |
| FR-067 | P1 | Alias shortcode univoci; keyword di ricerca non generano automaticamente trigger | proposto |
| FR-068 | P0 | Errori catalogo emoji non impediscono accesso alla libreria snippet | proposto |
| FR-069 | P1 | Registrazione/scadenza hotkey senza leakage di risorse; esito visibile | proposto |
| FR-070 | P0 | Clipboard occupata o cambiata dall'utente non deve essere distrutta | proposto |

Riferimenti [emojilib](../research/audit/EMOJILIB_DIRECT_SOURCE_AUDIT_v2.2.md) e [XMiLib](../research/audit/XMILIB_DIRECT_SOURCE_AUDIT_v2.2.md).

## Tracciabilità dell'architettura v2.3

Le funzioni FR-001–FR-0xx mantengono gli ID storici. La mappatura dei casi d'uso P0/P1/P2 e l'assegnazione delle responsabilità sono nel [catalogo delle vertical slice](VERTICAL_SLICES.md); i confini OS sono in [PLATFORM_ADAPTER_CONTRACTS.md](PLATFORM_ADAPTER_CONTRACTS.md). Nessuna priorità funzionale è automaticamente cambiata dall'audit: l'architettura è una proposta per la futura implementazione.

## Requisiti Unicode P0 emersi v2.5 (candidati da ratificare)

- **UR-01:** archiviazione e output preservano il testo originale Unicode ben formato, senza normalizzazioni implicite.
- **UR-02:** chiave NFC di confronto proposta per trigger; equivalenza canonica non significa assenza di accenti o di varianti emoji.
- **UR-03:** matching, grapheme segmentation, word boundary, delimitatori e rimozione nel target sono problemi separati, verificabili indipendentemente.
- **UR-04:** nessuna espansione su testo IME non committato; backend senza informazioni affidabili ⇒ opzione manuale/picker.
- **UR-05:** trigger duplicati o collidenti per normalization/case nello scope attivo ⇒ segnalazione/ambiguità, non scelta casuale.
- **UR-06:** nessuna eliminazione automatica quando il numero di Backspace o il contesto di edit non sono verificabili; sempre via fallback esplicito.
- **UR-07:** la GUI deve mostrare trigger visivamente identici ma Unicode differenti, non forzare translitterazioni RTL o confusables.

[Contratto matcher](TRIGGER_ENGINE.md) · [test](MATCHER_TEST_PLAN.md).

## Requisiti non funzionali v2.7 (candidati)

| ID | Priorità | Requisito / criterio di accettazione |
|---|---|---|
| NFR-001 | P0 | Digitazione non bloccata da UI, I/O, backup o compilazione degli indici; callback misurato |
| NFR-002 | P0 | Overflow della coda → reset sessione e nessuna espansione rischiosa |
| NFR-003 | P0 | Snapshot immutabili atomici; nessun mix fra revisioni |
| NFR-004 | P0 | Nessuna seconda istanza del motore; tray/Exit/Pause/Resume non lasciano risorse native attive involontarie |
| NFR-005 | P0 | Picker latest-only, virtualizzato, navigabile dalla tastiera |
| NFR-006 | P0 | Metriche diagnostiche prive di input/snippet/clipboard |
| NFR-007 | P1 | Budget PB-01..PB-12 verificati con baseline D10 prima della release certificata |
| NFR-008 | P1 | 24h soak senza crescita monotona non spiegata; D50 esplorativo |

Dettaglio e limiti: [specifica](PERFORMANCE_RESOURCE_LIFECYCLE.md) · [piano](PERFORMANCE_TEST_PLAN.md). I budget sono proposte documentali, non prestazioni provate.
