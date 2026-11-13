# SQLite — implementazione e specifica

> **Lettura corrente:** SQLite è l'archivio operativo e il formato di backup; JSON è implementato per import/export. L'ultima suite Windows comprende 180 test passati. Le sezioni sotto conservano differenze tra schema proposto e implementazione e gli stati alla loro data; non attestano l'esecuzione di ogni scenario documentale. [Stato corrente](CURRENT_STATE.md), [build e test](BUILD_AND_TEST.md), [rapporti](../reports/README.md).

## Decisione applicata nella v2.9 (storico)

Schema SQLite v1 realmente scritto in `src/TextExpander.Infrastructure/Storage/SqliteLibraryStore.cs`: `groups`, `snippets` (trigger singolo per MVP), `settings`, `app_meta`. WAL/FULL, revisioni e backup DB nativo. **Differenza rispetto allo schema proposto nello studio:** non è presente una tabella `triggers` separata; la normalizzeremo solo se la funzione degli alias multipli lo richiederà. Le capacità di restore richiedono test runtime, ancora NOT_RUN. Non è previsto un formato parallelo di backup JSON: JSON resta solo interoperabilità futura.

---

# SQLite — contratto canonico di persistenza (studio v2.8)

**Decisione approvata:** [ADR-0007](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md). **Stato del presente documento:** schema e policy operative sono proposte verificabili; **nessun codice applicativo è stato scritto**.

## 1. Obiettivi e confini

SQLite conserva la libreria operativa. L'app è single-user/local-first, funziona offline e non impone servizi esterni. Tutte le operazioni sugli snippet sono transazionali e riproducibili, con rollback del database in caso di eccezione. Il contenuto dell'utente rimane Unicode esatto: NFC solo nelle **MatchKey derivate in memoria** quando sarà ratificata la relativa ADR. Le ricerche nel picker non sono il matching automatico.

**Dentro SQLite in prima versione:** gruppi, snippet, una o più definizioni di trigger per snippet (modello flessibile, senza esporre alias multipli subito), preferenze stabili selezionate, metadati di schema/revisione.

**Fuori SQLite:** password e credenziali, testo effettivamente digitato in altre applicazioni, buffer del matcher, sessioni e target correnti, clipboard, eventi diagnostici che contengano snippet/trigger, indice di ricerca/emoji ricostruibile. Il catalogo emoji distribuito con il prodotto è una risorsa separata; non richiede duplicazione nel DB all'MVP.

**Non implementare anzitempo:** sync multiutente, CRDT, server, change-event log completo, FTS obbligatorio, schema di cronologia complesso, un repository generico per ogni tabella o un ORM pesante.

## 2. Collocazione per piattaforma

| Ambiente | Directory dati predefinita proposta | Note |
|---|---|---|
| Windows | `%LOCALAPPDATA%/TextExpanderStudio/` | non in `Program Files`; ACL utente |
| macOS | `~/Library/Application Support/TextExpanderStudio/` | permessi utente |
| Linux | `${XDG_DATA_HOME:-~/.local/share}/textexpander-studio/` | rispettare XDG |
| Portatile | cartella `data/` relativa alla distribuzione, **solo opt-in** | testare scrivibilità, disco removibile, permessi, lock |

Nome logico: `library.db` (non esportazione). In WAL possono coesistere file `library.db-wal` e `library.db-shm`; non cancellarli manualmente e non spostare il DB mentre le connessioni sono attive. I percorsi devono essere assoluti e canonici; bloccare il traversal dei nomi da import/export. Per backup scelti dall'utente su cartelle cloud: trasferire un pacchetto **chiuso e verificato**, mai il database vivo in WAL. Il programma deve rilevare file system non compatibili e non promettere WAL su share SMB/NFS.

## 3. Schema logico MVP — proposta minima

| Entità | Campi essenziali | Vincoli e considerazioni |
|---|---|---|
| `groups` | `id` TEXT PK, `name`, `description`, `enabled`, `sort_order`, `revision`, `created_utc`, `updated_utc` | `CHECK(enabled IN (0,1))`; eliminazione solo se policy sugli snippet risolta |
| `snippets` | `id` TEXT PK, `group_id` FK, `title`, `body`, `description`, `enabled`, `revision`, `created_utc`, `updated_utc` | FK con `ON DELETE RESTRICT` per gruppo; `body` esatto, mai convertito in NFC |
| `triggers` | `id` TEXT PK, `snippet_id` FK, `pattern_text`, `activation_mode`, `boundary_mode`, `case_mode`, `delimiter_mode`, `enabled` | `ON DELETE CASCADE` quando si elimina uno snippet; trigger non vuoto; conflitti **nel motore**, non appiattiti da un indice UNIQUE |
| `settings` | `key` TEXT PK, `value_json`, `updated_utc` | validazione e whitelist per chiave nella slice settings; niente credenziali |
| `app_meta` | riga unica `id=1`, `library_revision` | monotona: ogni mutazione che cambia la libreria aumenta la revisione |
| Schema version | `PRAGMA user_version` | incrementato **nella stessa transazione** della migrazione |

Timestamps in UTC ISO 8601 con formato fissato oppure epoch integer; non affidarsi all'ordine lessicografico di date con offset misti. `id` UUID testuale canonico, stabile e non derivato da nome/lingua. `revision` per optimistic concurrency; `library_revision` per coordinare DB e snapshot matcher. La preservazione di UUID esterni importati è regolata dall'importatore, non dalla PK del DB da sola.

### Esempio DDL di analisi, non migration pronta per produzione

```sql
CREATE TABLE groups (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL CHECK(length(name)>0),
  description TEXT NOT NULL DEFAULT '',
  enabled INTEGER NOT NULL DEFAULT 1 CHECK(enabled IN (0,1)),
  sort_order INTEGER NOT NULL DEFAULT 0,
  revision INTEGER NOT NULL DEFAULT 1 CHECK(revision>=1),
  created_utc TEXT NOT NULL,
  updated_utc TEXT NOT NULL
);
CREATE TABLE snippets (
  id TEXT PRIMARY KEY,
  group_id TEXT NOT NULL REFERENCES groups(id) ON DELETE RESTRICT,
  title TEXT NOT NULL CHECK(length(title)>0),
  body TEXT NOT NULL,
  description TEXT NOT NULL DEFAULT '',
  enabled INTEGER NOT NULL DEFAULT 1 CHECK(enabled IN (0,1)),
  revision INTEGER NOT NULL DEFAULT 1 CHECK(revision>=1),
  created_utc TEXT NOT NULL,
  updated_utc TEXT NOT NULL
);
CREATE TABLE triggers (
  id TEXT PRIMARY KEY,
  snippet_id TEXT NOT NULL REFERENCES snippets(id) ON DELETE CASCADE,
  pattern_text TEXT NOT NULL CHECK(length(pattern_text)>0),
  activation_mode TEXT NOT NULL CHECK(activation_mode IN ('Immediate','Delimiter','Manual')),
  boundary_mode TEXT NOT NULL CHECK(boundary_mode IN ('Anywhere','WordBoundary')),
  case_mode TEXT NOT NULL CHECK(case_mode IN ('Sensitive','Insensitive')),
  delimiter_mode TEXT NOT NULL CHECK(delimiter_mode IN ('Keep','Drop')),
  enabled INTEGER NOT NULL DEFAULT 1 CHECK(enabled IN (0,1))
);
CREATE TABLE settings (
  key TEXT PRIMARY KEY,
  value_json TEXT NOT NULL CHECK(json_valid(value_json)),
  updated_utc TEXT NOT NULL
);
CREATE TABLE app_meta (
  id INTEGER PRIMARY KEY CHECK(id=1),
  library_revision INTEGER NOT NULL DEFAULT 0 CHECK(library_revision>=0)
);
INSERT INTO app_meta(id,library_revision) VALUES(1,0);
CREATE INDEX idx_snippets_group ON snippets(group_id);
CREATE INDEX idx_triggers_snippet ON triggers(snippet_id);
```

**Nota importante:** `CHECK(length(...))` controlla una stringa SQLite, non la segmentazione Unicode in grafemi e non sostituisce le invarianti Core. La tabella `triggers` è una proposta volutamente minima, senza assumere che tutti gli alias siano esposti nella UI MVP. `json_valid()` richiede supporto JSON nelle build native scelte: verificarlo alla definizione del provider prima di confermare il DDL. Il primo schema migrabile deve usare la versione SQLite distribuita, non la versione del tool di sviluppo.

## 4. Configurazione e concorrenza

**Inizializzazione connessioni:** attivare `PRAGMA foreign_keys=ON` per ciascuna connessione; aprire DB locale; negoziare `PRAGMA journal_mode=WAL` e verificarne il risultato; impostare `PRAGMA synchronous=FULL` inizialmente; timeout occupato finito (per esempio 2–5 s, da misurare). Non impostare `Cache=Shared` con WAL: Microsoft ne scoraggia la combinazione. Preferire `Microsoft.Data.Sqlite` con SQL parametrizzato, senza EF Core obbligatorio.

**Modello writer:** una sola pipeline seriale di comandi mutanti. Ogni comando apre/esegue una breve transazione, valida revisioni e vincoli, aggiorna `app_meta.library_revision`, quindi `COMMIT`. Le letture della GUI usano connessioni proprie per periodi brevi. Non bloccare né thread UI né hook tastiera; le operazioni `Microsoft.Data.Sqlite` sono sincrone anche quando si richiamano gli overload `Async`.

**Optimistic concurrency:** `UPDATE snippets SET ..., revision=revision+1 WHERE id=@id AND revision=@expectedRevision`. Se sono interessate 0 righe, segnalare `Conflict` e chiedere all'utente la scelta; non sovrascrivere silenziosamente. Gli update di una group che ne cambiano l'enabled e le modifiche ai trigger aumentano anch'essi `library_revision`.

**Pubblicazione matcher:** non è atomica assieme al COMMIT SQLite. Protocollo proposto:

1. **Prima del commit** di una modifica che influenza il matcher, l'orchestratore alza un *mutation fence* in memoria, impedendo l'avvio di nuove espansioni con la vecchia snapshot. Questo chiude la finestra di race fra commit DB e invalidazione del matcher; eventuali espansioni già in corso sono gestite dal coordinatore single-flight.
2. Eseguire la modifica in transazione e incrementare `library_revision` nello stesso commit. Se avviene `ROLLBACK`, la fence può essere rimossa e torna utilizzabile la vecchia snapshot **solo se la revisione non è cambiata**.
3. Dopo `COMMIT`, un worker prepara lo snapshot del matcher sulla libreria confermata e lo pubblica con revisione corrispondente, se ancora attuale. Solo allora si rimuove la fence.
4. Se indicizzazione fallisce, mantenere GUI e dati salvati ma lasciare l'espansione automatica sospesa, con un avviso e un comando di riprova; non tornare arbitrariamente allo snapshot precedente.
5. L'input callback legge solo il flag di fence e lo snapshot in memoria: **mai** interroga DB o attende la ricostruzione. Il picker ricontrolla ID e revisioni prima di applicare comandi.

Questo assicura comportamento **fail-closed** fra commit e pubblicazione, senza promettere atomicità inter-processo inesistente. I benchmark dovranno dimostrare che le ricostruzioni sono sufficientemente rapide; in seguito si potrà passare all'aggiornamento incrementale.

## 5. Migrazioni e compatibilità

- Evoluzione tramite migrazioni con ID/versione ordinata e revisionata in Git; `PRAGMA user_version` è la sorgente della versione schema, aggiornata solo nella stessa transazione del DDL compatibile con transazione.
- Aprendo un DB con `user_version` più alto di quello supportato, **non** aprire in scrittura e non eseguire downgrade automatici. Offrire export/ripristino compatibile dove possibile.
- Prima di una migrazione che può cambiare dati, creare backup recuperabile e verificarlo. In caso di errore rollback e non dichiarare aggiornamento eseguito.
- **Nessuna promessa generica** di compatibilità con ogni vecchia versione interna durante lo sviluppo. Le release distribuite richiederanno politica di migrazione e backup documentata.
- Nei test includere migrazioni interrotte, riaperture, `user_version` errato, constraint vecchi e ripristino a database precedente. Le migrazioni non devono eseguire macro o codice importato.

## 6. Backup, ripristino e JSON portabile

**Tre operazioni distinte in UI:** `Esporta snippet`, `Backup completo`, `Ripristina backup`; import Beeftext/Espanso è una quarta funzione con anteprima.

- **Backup database consistente:** connessione sorgente + Online Backup API SQLite verso file temporaneo separato e nuovo (oppure `VACUUM INTO` quando motivato). Verificare che la copia si apra, `PRAGMA integrity_check`, `PRAGMA foreign_key_check`, revisione e schema. Un checksum dell'archivio segnala alterazioni del backup, non sostituisce i controlli semantici.
- **Archivio di backup:** manifest JSON con `formatVersion`, `appVersion`, `dbSchemaVersion`, data UTC, dimensioni, digest SHA-256, componenti inclusi; includere preferenze **non sensibili** soltanto con scope dichiarato; nessuna esecuzione di script. Scrivere in `.partial`, verificare e infine pubblicare con rename/sostituzione sicura quando supportata; conservare il backup precedente sano in caso di errore.
- **Ripristino:** anteprima di schema/revisione, verifica hash + integrità, confronto con libreria corrente, backup di sicurezza dello stato corrente, chiusura/quiet dei servizi che usano il DB, sostituzione verificata dello store e riapertura con nuovo matcher. Fallimento: ripristinare lo stato precedente in modo controllato. Non sostituire la libreria attiva semplicemente perché il file è sintatticamente leggibile.
- **JSON portabile:** modello esplicito di schema e mapping e report di campi non supportati; JSON importato non diventa automaticamente trusted. Non serializzare direttamente tabelle, colonne, percorsi o istruzioni eseguibili come formato pubblico stabile.
- **Retention:** nessun numero arbitrario ratificato; usare politica configurabile e proteggere almeno un punto di ripristino verificato. Test disco pieno, file in uso, antivirus, errori di permesso, sleep e crash.

SQLite normale, JSON esportato e backup non sono cifrati di default. Proteggere directory e file e spiegare all'utente la possibilità di contenuti confidenziali. Cifratura lato DB e password di backup sono **funzionalità separate non ancora approvate**; non spacciarle per automatiche o ottenibili impostando semplicemente la password nella stringa di connessione.

## 7. Ricerca, Unicode e prestazioni

- Matcher: snapshot immutabili in memoria, **nessuna FTS5 né query SQL per ogni tasto**.
- Picker MVP: indice di ricerca in memoria indipendente dal matcher, aggiornato dopo commit. Non duplicare il contenuto degli snippet in FTS prima di misurare una necessità reale.
- FTS5 futura: `unicode61` di default è case-insensitive e rimuove alcuni diacritici latini, per cui non conserva la semantica del matcher. `trigram` facilita substring ma introduce costi e vincoli per termini corti. Usarlo solo per la ricerca **voluta dall'utente**, con test CJK/emoji/case, e valutazione privacy dei token duplicati.
- In SQLite `NOCASE` e molte funzioni di confronto non realizzano tutte le equivalenze Unicode culturali. Le collisioni e l'ordine dei trigger sono definiti nel Core, non tramite un indice UNIQUE su `pattern_text COLLATE NOCASE`.
- Performance: misurare CRUD 100/1.000/10.000/50.000 snippet, backup con writer attivo, startup e pubblicazione indice. Non promettere target v2.7 come già raggiunti.

## 8. Mappa Vertical Slice

| Slice | Operazione | Confine transaction / stato |
|---|---|---|
| `Library/CreateSnippet` | inserimento snippet + trigger | transazione unica, revision increment, publish indice |
| `Library/EditSnippet` | update con expected revision | transazione unica o `Conflict`, publish indice |
| `Library/DeleteGroup` | scelta spostamento/elim. snippet | `RESTRICT` nel DB, workflow esplicito nella GUI |
| `Settings/ChangePreference` | validazione chiave e valore | batch atomico per preferenze correlate, evento i18n/tema dopo commit |
| `Import/ApplyPreview` | staging + collision report + conferma | commit tutto-o-niente, nuova revisione, publish |
| `Backup/Create` | consistent snapshot + verifica + pubblicazione | nessuna modifica al DB live |
| `Backup/Restore` | check + checkpoint/quiescence + recovery point | cambio archivio, riapertura, rebuild indice |
| `Runtime/Matching` | matching dal buffer volatile | **zero SQL**, zero I/O/GUI nel callback |

L'infrastruttura offre pochi repository specifici per slice e un confine transazionale; il Core non conosce `Microsoft.Data.Sqlite`. Nessun layer generico repository/UnitOfWork se non porta valore concreto.

## 9. Attività ancora aperte

1. Validare DDL/schemi con la versione SQLite nativa distribuita e testare la portabilità dei timestamp.
2. Qualificare WAL/FULL, busy timeout, backup/restore e percorsi su Windows/macOS/Linux + portable/removible.
3. Definire formato export JSON pubblico v1, scope preferenze, cifratura eventuale, policy retention.
4. Determinare se attivare alias multipli nella prima versione dell'editor; la tabella è già predisposta.
5. Ratificare il dettaglio dello schema e le policy di backup. La sola scelta **SQLite** è stata già approvata dall'utente.

**Riferimenti:** [ADR-0007](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md) · [Piano test](SQLITE_TEST_PLAN.md) · [Audit dati](../research/audit/BEEFTEXT_PERSISTENCE_SOURCE_AUDIT.md) · [Import Espanso](../research/audit/ESPANSO_DATA_MODEL_AUDIT.md) · [Evidenze SQLite](../research/audit/SQLITE_TECHNICAL_EVIDENCE_v2.8.md).

## Compatibilità del profilo dopo la rinomina (v3.5)

Il nome dell'app è **Ristretto**, ma per non far sparire gli snippet già creati dalla v3.4 il database rimane **temporaneamente** in `TextExpanderStudio/library.db`. Il percorso è una scelta di compatibilità, non il nuovo nome del prodotto. Una futura migrazione al profilo `Ristretto` richiederà un backup SQLite verificato, l'arresto del motore, il trasferimento controllato e una verifica di ripartenza; non è implementata in questa release.
