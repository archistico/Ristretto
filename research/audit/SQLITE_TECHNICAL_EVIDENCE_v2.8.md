# Evidenze tecniche SQLite — studio v2.8

**Fonti ufficiali consultate il 13 novembre 2026.** Le evidenze qui elencate supportano le scelte proposte; non equivalgono a benchmark sul prodotto.

| ID | Fonte | Evidenza | Implicazione progettuale |
|---|---|---|---|
| S28-01 | [SQLite — WAL](https://www.sqlite.org/wal.html) | Lettori e writer possono coesistere, un solo writer per volta; WAL non va usato come DB condiviso via network FS | writer seriale, file DB locale, backup chiuso per sincronizzazione |
| S28-02 | [SQLite — pragma](https://www.sqlite.org/pragma.html) | `synchronous=NORMAL` in WAL è consistente ma non garantisce durabilità dell'ultimo commit a blackout; `FULL` aggiunge sync | proposta `FULL` iniziale con benchmark reale |
| S28-03 | [SQLite — Online Backup API](https://www.sqlite.org/backup.html) | Snapshot consistente del database attivo; non copiare solo `.db` con WAL | backup snapshot con verifiche |
| S28-04 | [SQLite — VACUUM INTO](https://www.sqlite.org/lang_vacuum.html) | Snapshot alternativo coerente e più compatto, con costi/limiti differenti | opzione da valutare solo se vantaggiosa |
| S28-05 | [SQLite — FK](https://www.sqlite.org/foreignkeys.html) | Enforce foreign keys solo se attivato, a livello di connessione | `foreign_keys=ON` per ogni nuova connessione |
| S28-06 | [Microsoft.Data.Sqlite async](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async) | chiamate `Async` si eseguono in modo sincrono | non usare API async come falsa garanzia di non blocco UI |
| S28-07 | [Microsoft.Data.Sqlite connection strings](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings) | `Foreign Keys`, `Default Timeout`, `Pooling`, e cautela sul `Cache=Shared` con WAL | connessioni configurate esplicitamente; niente shared cache per default |
| S28-08 | [SQLite — FTS5](https://www.sqlite.org/fts5.html) | `unicode61` rimuove di default alcuni diacritici; `trigram` ricerca sottostringhe | FTS non deve determinare la semantica del matcher |

## Sintesi confrontata con i sorgenti originali

- Beeftext salva snapshot/libreria JSON v10; i difetti di carico, write, restore già tracciati nell'[audit originale](BEEFTEXT_PERSISTENCE_SOURCE_AUDIT.md) motivano la scelta di transazioni e recovery. **Non** implica che SQLite da solo renda sicuro il cambio di file o il backup multi-componente.
- Espanso legge configurazioni YAML; ciò rimane compatibile con un importatore non esecutivo, separato dal formato interno dell'applicazione ([studio Espanso](ESPANSO_DATA_MODEL_AUDIT.md)).
- Catalogo emojilib è distribuzione di asset; la scelta SQLite non obbliga a duplicare emoji/keyword nella tabella snippets.
- Il contratto Unicode del Core resta indipendente dal database; **nessuna modifica alle decisioni NFC non ancora ratificate**.

## Verifiche ancora necessarie

Provider SQLite/NuGet/versione nativa sui tre OS, gestione WAL su filesystem portabili, modalità read-only quando il DB è di schema futuro, migrazioni, file lock e crash, backup+restore in presenza di operazioni concorrenti, ricerca 10k/50k, lingua/emoji/IME, profilo di memoria. Non dichiarare conformità o test PASS prima di misure vere.
