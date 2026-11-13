# Esito della decisione — 13 novembre 2026

**Scelta chiusa dall'utente: SQLite come database locale.** Il confronto sottostante è conservato quale motivazione storica; le sezioni "proposta non ratificata" sono superate per la scelta del backend. Le policy di schema, backup e operatività restano da qualificare. [ADR-0007 approvato](../../docs/adr/ADR-0007-SQLITE-LOCAL-STORAGE.md) · [Specifiche correnti](../../docs/SQLITE_STORAGE.md).

---

# Valutazione storage locale e backup — decisione NON ancora ratificata

## Confronto su requisiti concreti

| Approccio | Vantaggi | Costi / limiti | Quando sceglierlo |
|---|---|---|---|
| JSON unico come DB | leggibile, portabile, facile copia/Git | scrittura totale, concorrenza, locking, recovery, grandi librerie | poche migliaia di snippet, bassa concorrenza |
| SQLite locale + JSON export | transazioni, indici, schema migrabile, backup coerente | più parti da testare, WAL, migrazioni | libreria crescente, undo, ricerca e relazioni |
| YAML editabile come Espanso | Git, file modulari, flessibile per esperti | parsing, include/cicli, commenti e merge, confusione per principianti | prodotto CLI/config-as-code, **non** requisito della nostra GUI |

**Proposta corrente, non definitiva:** `SQLite` come archivio locale gestito dall'app, `JSON` come export portabile e backup snapshot coerente. UI deve poter funzionare in assenza di rete/account. La tabella `snippets` non deve memorizzare testo normalizzato al posto di quello originale. Indici di matching eventualmente derivati e ricostruibili.

## Schema logico minimo (non schema SQL definitivo)

- `Group(Id, Name, Description, Enabled, CreatedUtc, ModifiedUtc)`
- `Snippet(Id, GroupId?, Name, Body, Description, Enabled, MatchMode, CaseMode, CreatedUtc, ModifiedUtc)`
- `TriggerAlias(Id, SnippetId, TriggerText, Mode, Enabled)` solo se alias richiesti prima della v1
- `Setting(Key, Value)` con resource/string-free config
- `SchemaInfo(Version, AppliedAt)`
- `RecoveryRecord(Id, CreatedUtc, Kind, Source, Hash, Status)` eventualmente solo se utile alla GUI.

Evitare soluzioni premature: tabella separata per ogni piccola proprietà, repository generico, event sourcing, CQRS framework, soft-delete universale o microservizi. Alcune funzioni opzionali (history, tags, profiles) possono arrivare come migrazioni mirate di vertical slice.

## SQLite — note concrete

- La `Backup API` ufficiale crea una copia consistente della sorgente; un file `.db` copiato isolatamente con transazioni WAL attive potrebbe non contenere tutte le modifiche. Fonte: https://www.sqlite.org/backup.html
- Specificare ciclo di vita delle connessioni, `PRAGMA foreign_keys=ON`, transazioni esplicite, upgrade idempotenti e test di integrity check.
- Non considerare l'atomicità di una singola transazione come garanzia che l'intero ZIP di backup (DB + manifest + eventuali asset) sia a sua volta atomico: staging, verifica e rename dell'archivio completo.
- Zero dipendenze cloud; cifratura opzionale richiede un ADR distinto e threat model.
- Progettare la sequenza di migrazione con backup **prima**, schema bump **dopo** commit, lettura di ritorno e fallback a snapshot. La compatibilità con i formati nostri vecchi sarà una decisione di rilascio, distinta dall'import storico Beeftext.

## Decisioni aperte

1. SQLite interno oppure JSON unico per MVP? Valutare costo implementativo su spike filesystem reale e benchmark 100/1.000/10.000 snippet.
2. Inclusione delle preferenze nel backup completo: scope versionato e filtro dati personali.
3. Backup cifrato: per la v1 non assumere necessario, prevedere solo formato future-proof e avvisi sui dati sensibili.
4. Quante copie e retention? Non adottare automaticamente il limite 50 di Beeftext; policy configurabile con almeno un punto sano.
5. `portable mode`: path relativi, directory non scrivibile, volumi removibili e backup fuori dal dispositivo portatile.
