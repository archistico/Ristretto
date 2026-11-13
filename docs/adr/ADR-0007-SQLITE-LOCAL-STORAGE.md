# ADR-0007 — SQLite come storage locale del prodotto

- **Stato: APPROVATO dall'utente il 13 novembre 2026.**
- Ambito: persistenza operativa della futura app open source C#/.NET + Avalonia.
- Questa ADR **ratifica la tecnologia SQLite**, non ogni dettaglio dello schema: le scelte operative sono proposte da validare.
- Conseguenza: non è più aperto il confronto «SQLite oppure JSON come database primario».

## Decisione

1. SQLite è l'unico database operativo locale per snippet, gruppi, trigger e dati persistenti necessari al prodotto. **Nessun server/database di rete** e nessun login obbligatorio.
2. Il motore e il picker usano indici derivati **in memoria**: non interrogano SQLite per ogni pressione di tasto.
3. JSON UTF-8 **versionato** resta il formato di interscambio/import-export, distinto dal file SQLite e dal backup completo.
4. I backup completi devono contenere uno **snapshot SQLite consistente** realizzato mediante SQLite Online Backup API o altra tecnica SQLite ufficialmente supportata e verificata. **Non** copiare isolatamente un `.db` live con WAL attivo.
5. Schemi e migrazioni SQLite sono di proprietà di `Infrastructure`, orchestrati dalle slice applicative; `Core` non conosce SQLite.
6. Progettazione e dati Unicode: preservare payload e trigger originali, senza normalizzare al salvataggio. Le chiavi di confronto derivate non sono fonte di verità.

## Motivazione

Rispetto al JSON monolitico, SQLite facilita transazioni di modifica, concorrenza controllata, vincoli referenziali, migrazioni locali e copie consistenti. Per 10.000+ snippet il matcher resta comunque in memoria; la scelta di SQLite **non** implica una query per evento. Per un programma a istanza singola è sufficiente una sola pipeline di scrittura, senza ORM o framework CQRS obbligatori.

## Proposte operative, non ancora misurate

- `journal_mode=WAL` su filesystem locale compatibile; `synchronous=FULL` come impostazione conservativa iniziale, da verificare misurando su dispositivi reali; `foreign_keys=ON` su **ogni** connessione; timeout limitato e gestione esplicita di `SQLITE_BUSY`.
- Driver candidato: `Microsoft.Data.Sqlite`, accesso SQL parametrizzato e migrazioni SQL semplici. **Non** imporre EF Core: rivalutare solo se esistono benefici concreti.
- Scritture serializzate in `Application`, letture brevi su connessioni separate e precompilazione/pubblicazione di snapshot del matcher fuori dal thread GUI.
- Migrazioni con `PRAGMA user_version`, backup di recupero prima di aggiornamenti a rischio e rifiuto esplicito di database con schema futuro non supportato.
- Storage locale fuori dalla cartella dell'app installata; modalità portable soltanto esplicitamente abilitata e su percorso scrivibile/compatibile.
- Al mutare dei trigger, usare una *mutation fence* **prima** del commit, per evitare che eventi fra commit e ricostruzione consumino uno snapshot obsoleto; sbloccare dopo pubblicazione della revisione confermata o rollback sicuro.

## Alternative scartate come **storage operativo primario**

- File JSON unico: resterebbe necessario implementare manualmente indici, locking, recovery, aggiornamenti concorrenti e scritture atomiche dell'intera libreria.
- YAML Espanso: buono per configurazioni editabili, non è il formato interno da privilegiare per una GUI semplice.
- Database remoto, server separato, ORM complesso o event sourcing: nessun requisito P0 li giustifica.

## Conseguenze, limiti, verifiche

- WAL supporta un writer per volta; un DB condiviso su filesystem di rete non è supportato dal nostro contratto operativo. Backup in directory sincronizzata è diverso dall'apertura condivisa di un database attivo.
- `synchronous=NORMAL` in WAL è consistente ma può perdere l'ultimo commit in caso di blackout; la scelta `FULL` è una **proposta di default**, non una metrica di prestazione certificata.
- `Microsoft.Data.Sqlite` esegue le API ADO.NET `*Async` in modo sincrono: non usarle per presumere I/O non bloccante; eseguire i comandi lontano dal thread UI.
- FTS5 `unicode61` elimina per default alcuni diacritici latini: **non usarlo come matcher degli snippet**; qualsiasi FTS per il picker richiede test di semantica e privacy.
- Privacy: SQLite ordinario e backup non sono cifrati automaticamente; l'eventuale cifratura richiede una specifica e un provider nativo adeguati.
- Nessuna implementazione C#, benchmark, test dell'app o migrazione eseguita in questa fase.

## Documenti correlati

[Specifica storage](../SQLITE_STORAGE.md) · [Piano test](../SQLITE_TEST_PLAN.md) · [Dati/import/backup](../DATA_IMPORT_BACKUP.md) · [Concorrenza](../PERFORMANCE_RESOURCE_LIFECYCLE.md) · [Roadmap](../ROADMAP.md) · [Evidenze](../../research/audit/SQLITE_TECHNICAL_EVIDENCE_v2.8.md).
