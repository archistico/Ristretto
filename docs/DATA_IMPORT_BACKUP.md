# v2.9 — backup SQLite nativo

È implementato codice per backup consistente tramite SQLite Online Backup API, validazione `integrity_check`/`foreign_key_check` e ripristino con staging e recovery copy. Il backup è un `.db` e non include un secondo formato JSON. Import/export JSON, Beeftext ed Espanso **restano non implementati**. Il restore richiede ancora validazione su Windows/macOS/Linux.

---

# Aggiornamento approvato v2.8 — SQLite

**SQLite è stato scelto dall'utente come database locale operativo il 13 novembre 2026.** Non mantenere aperta la scelta SQLite/JSON. Le sezioni storiche sottostanti descrivono il ragionamento precedente e devono essere lette come **storico**, non come stato corrente. La specifica aggiornata e vincolante del confine storage è [SQLITE_STORAGE.md](SQLITE_STORAGE.md) con [ADR-0007 approvato](adr/ADR-0007-SQLITE-LOCAL-STORAGE.md). JSON resta formato portabile, non database interno; backup = snapshot SQLite consistente e pacchetto verificato. Schema, WAL/FULL e retention sono proposte tecniche da testare.

---

# Dati, persistenza, import/export e ripristino — contratto canonico v1.8

**Stato:** proposta di design, non decisione ratificata e nessun codice implementato. Il documento distingue evidenze Beeftext/Espanso da scelte per la futura applicazione.

## Obiettivi non negoziabili

- Gli snippet sono dati dell'utente: **nessun salvataggio, migrazione, import o restore può distruggere dati esistenti senza una via di recupero verificabile**.
- Unicode end-to-end: conservare testo, emoji ZWJ, accenti composti/decomposti, RTL, CJK, controlli, multilinea e caratteri matematici senza normalizzazione dei payload.
- Zero esecuzione di contenuti da file importati: macro/script/shell/riferimenti remoti rimangono inerti. La presenza di variabili testuali non autorizza esecuzione.
- L'utente può distinguere `Importa snippet`, `Esporta snippet`, `Backup completo`, `Ripristina libreria` e `Report CSV`.
- Operazioni interrompibili con esito chiaro e log privi di contenuto sensibile; errori non devono modificare la libreria in uso.

## Scelta storage: proposta, non approvata

**Candidato principale:** SQLite locale (transazioni, indici, migrazioni) + JSON UTF-8 versionato per export interoperabile + snapshot coerenti di backup. **Alternativa:** archivio JSON unico con staging e write atomico. Il trade-off completo è in [Storage decision record](../research/audit/STORAGE_DECISION_RECORD.md); ADR-014 e ADR-021 non sono ratificati.

## Modello minimo

`Group` (ID, nome, descrizione, enabled), `Snippet` (ID, gruppo, nome, trigger, contenuto, descrizione, matching mode, case sensitivity, enabled, UTC timestamps). ID indipendenti da ordine e lingua UI. Aliases, tags, profili, stats e form sono estensioni per slice future; lo storage non deve impedirle, ma non implementarle prima del bisogno.

**Semantica della compatibilità:** il formato di persistenza nostro e il formato di import Beeftext sono contratti distinti; un'importazione non deve sovrascrivere il file originale di Beeftext. Le date locali vanno lette correttamente e mantenute in UTC nel nuovo modello, senza mutare arbitrariamente la data storica di modifica durante import.

## Formati del vecchio Beeftext accertati nel sorgente

- File libreria predefinito: `comboList.json`, root `fileFormatVersion` (**v10**), `combos`, `groups`.
- Campi combo: UUID/nome/keyword/snippet/descrizione/modalità riconoscimento/case/date/enabled, group UUID se incluso.
- Gruppi: UUID/nome/descrizione/date/enabled. Export solo combo JSON senza gruppi; backup JSON con gruppi. CSV in 3 campi `keyword,snippet,name`, senza header, UTF-8; non confondere con il report "cheat sheet" CSV che ha header/4 colonne.
- Legacy: v1 `comboText`/`substitutionText`, v3 date rinominate, v9 matching, v10 descrizione/case; HTML legacy viene convertito in plain text nel loader originale. I dati ultimo utilizzo sono separati in `comboLastUse.json`.
- Debolezze di robustezza: load che svuota prima di validare, write diretto, backup che rinomina il file sorgente prima del nuovo save, restore non staged. Tutti sono riscontri statici, **non bug riprodotti**.

Dettaglio con evidenze: [Audit sorgente Beeftext](../research/audit/BEEFTEXT_PERSISTENCE_SOURCE_AUDIT.md).

## Espanso: import limitato e sicuro

Leggere file `match/*.yml`/`.yaml`, riconoscere `matches`, `trigger(s)`, `replace`, eventuali `form`, `regex`, `vars`. Non aprire `imports` esterni senza consenso e controllo della radice; non eseguire `shell`/`script`; non abilitare regex importate automaticamente. Preferenze `config/*.yml` e per-app sono un import futuro separato; su Wayland lo scope app potrebbe non essere disponibile. Dettaglio: [Studio Espanso](../research/audit/ESPANSO_DATA_MODEL_AUDIT.md).

## Pipeline import/ripristino (da implementare)

`Selezione -> Parse isolato -> Validazione -> Mapping -> Report campi persi -> Collisioni -> Preview -> Conferma -> Snapshot -> Commit transazionale -> Verifica -> Report`.

La preview mostra cosa cambierà, con tabella, filtri e diff. Regole dei conflitti: **non** scegliere casualmente, né risolvere automaticamente il conflitto fra trigger equivalenti nel matching effettivo. Permettere scelta esplicita di group mapping e `Importa disattivato` per macro sconosciute.

### Salvataggi e backup

1. Ogni save usa staging e commit con proprietà coerenti col backend; per SQLite, transazione e snapshot via Backup API/VACUUM INTO verificati; per file, temp stesso filesystem + flush/replace con fallback OS dichiarato.
2. Mai usare il backup come `rename` distruttivo dell'unica copia valida; un save fallito lascia un recovery point leggibile.
3. Versioni dati e migrazioni idempotenti, checksum e lettura di prova prima di dichiarare un backup valido; retention mai senza almeno una copia sana.
4. Restore: aprire e validare un *candidato* indipendente, visualizzare statistiche e differenze, creare snapshot della libreria corrente, quindi eseguire commit atomico e verificare.
5. Non includere automaticamente preferenze sensibili, history, log, dump di clipboard o file esterni; scope del backup definito e leggibile nell'interfaccia.
6. Import/export PDF/CSV a scopo umano non equivalgono a un backup lossless.

### GUI

Schermata `Importa` con sorgente, gruppo, avvisi, filtri per conflitti, diff, opzioni di commit e riepilogo prima/dopo; `Ripristina` con dimensione, data, versione, stato integrità e differenze. Le operazioni sono traducibili e accessibili da tastiera. Nessun backup o restore silenzioso.

## Test richiesti

- JSON vuoto, troncato, dati con duplicati UUID/keyword, versioni più recenti e vecchie, riferimenti gruppo mancanti.
- CSV UTF-8 senza header, virgole/virgolette/newline, BOM, input formula-like, file enormi e byte invalidi; preservare payload se la modalità è interoperabile.
- YAML alias/bomb/ricorsione, import esterno, path traversal, symlink, shell/regex/form/variabili non convertibili.
- Unicode NFC/NFD, emoji family, skin tone, bandiere, Arabic RTL, CJK, multilinea, NUL/control chars e stringhe lunghe.
- Collisioni post-normalizzazione e per scope; import idempotente; snapshot prima del commit; fault injection su ogni fase.
- 1, 100, 1.000 e 10.000 snippet; memoria e tempi da misurare nei futuri spike.

**Matrice operativa:** [PERSISTENCE_AUDIT_SCENARIOS.csv](../data/PERSISTENCE_AUDIT_SCENARIOS.csv) — tutti scenari `NOT_RUN`.

## Rischi e scelte aperte

- JSON vs SQLite come storage MVP; backup cifrato opzionale; retention e portable mode; ID originali da preservare in import o mapping sorgente→destinazione; contenuto di preferenze e statistiche nel backup.
- Il formato export JSON della nostra applicazione deve essere documentato e stabilizzato **prima** del rilascio pubblico. Nessuna promessa di compatibilità completa con macro Espanso/Beeftext.
- Un file CSV aperto da un foglio di calcolo può essere interpretato come formula (`=`, `+`, `-`, `@`): una modalità sicura per spreadsheet può introdurre quoting/prefix e dunque **non** è lossless. Presentarla come export distinto.

**Registro ADR:** [DECISIONS.md](DECISIONS.md). **Fonti:** [SOURCE_EVIDENCE.md](../research/audit/SOURCE_EVIDENCE.md).

## Stato implementato v3.1

L’applicazione ora include **import/export del proprio JSON v1** per gruppi e snippet: import additivo e transazionale, senza preferenze né script. Il backup e ripristino completi rimangono tramite SQLite Online Backup API. Per schema, limiti e test vedere [JSON_PORTABILITY](JSON_PORTABILITY.md). Gli importatori diretti Beeftext/Espanso e il preflight dei conflitti sono ancora pianificati.
