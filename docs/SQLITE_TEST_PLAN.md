# Piano di verifica persistenza SQLite — v2.8

**Solo studio; i test della nuova applicazione non sono stati eseguiti.** [Matrice CSV](../data/SQLITE_SCENARIOS_v2.8.csv) e [contratto](SQLITE_STORAGE.md).

## Classi di test

- **Schema e invarianti:** FK, `CHECK`, UUID, timestamp, aggiornamenti/revisioni, trigger multipli, conflitti business gestiti dal Core. Usare DB temporanei per test indipendenti.
- **Concorrenza:** snapshot matcher revisione N/N+1, writer seriale, letture GUI, transient `SQLITE_BUSY`, conflitti di edit concorrente; callback tastiera mai bloccante.
- **Migrazioni:** schema precedente, futuro e interrotto; backup prima del cambiamento, `user_version` nella transazione, recovery verificabile.
- **Backup/restore:** snapshot sotto WAL e writer attivo, hash/manifest, alterazioni, disco pieno, crash durante `.partial`, restore atomico con rollback.
- **Unicode:** verificare round-trip **esatto** per accenti NFC/NFD, emoji ZWJ, variazioni di pelle, CJK, RTL, tab, `\0` dove il modello lo consente; SQLite `TEXT` è dati, non segmentazione in grafemi.
- **Ricerca:** distinguere matcher in memoria, ricerca picker e FTS eventualmente futura; controllare falsi positivi con accenti e parole non latine.
- **Percorsi e sicurezza:** installata/portable, directory non scrivibile, condivisione di rete non supportata, permessi, copie cloud chiuse e sensibili.
- **Performance:** ripetere per 100/1.000/10.000/50.000 snippet su Win/macOS/Linux, CPU/RSS/latency percentili e concorrenza; soglie v2.7 candidate, non verificate.

## Protocollo minimo di accettazione

1. Identificare OS, filesystem, versione app/.NET, versione `Microsoft.Data.Sqlite`, SQLite nativo, schema, dimensione libreria.
2. Creare fixture riproducibili e anonimizzate; includere righe Unicode identiche byte-for-byte all'origine.
3. Eseguire test unitari Core, integrazione DB reale su file temporaneo e test E2E delle slice; separare fallimenti di schema/commit/pubblicazione.
4. Misurare crash/restart, blocchi DB, file `-wal`/`-shm`, recovery e compatibilità; non cancellare file SQLite nativi a mano.
5. Contrassegnare `PASS`/`FAIL` solo con strumenti/log effettivi. Stato documentale attuale della matrice: **NOT_RUN**.

## Gate P0 prima dell'implementazione e dei rilasci

- Approvata **solo** la scelta SQLite. DDL, default WAL/FULL, backup retention e provider devono essere ratificati o validati durante la prima slice.
- DB di schema futuro non deve essere sovrascritto automaticamente; restore deve fallire senza danni ai dati correnti.
- Nessuna regressione su Unicode e contenuti non eseguibili importati.
- Nessun accesso a SQLite da callback nativo; stato matcher invalido dopo commit fino a nuovo snapshot coerente.

**Riferimenti:** [ADR](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md) · [Specifica](SQLITE_STORAGE.md) · [Roadmap](ROADMAP.md) · [Evidenze](../research/audit/SQLITE_TECHNICAL_EVIDENCE_v2.8.md).
