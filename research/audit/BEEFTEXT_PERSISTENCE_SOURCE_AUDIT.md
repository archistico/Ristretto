# Audit del sorgente Beeftext — Persistenza, migrazioni, import, backup (v1.8)

**Data ricognizione:** 12 novembre 2026. **Metodo:** lettura diretta di singoli file `master` attraverso GitHub Raw, confronto con wiki del produttore e documentazione Espanso. **Perimetro:** audit statico mirato; non è un checkout integrale, non è stato compilato Beeftext e nessuna ipotesi è stata verificata eseguendo l'app. Le linee e URL sottostanti sono verificabili; il ramo `master` potrebbe cambiare. Prima dell'implementazione fissare un commit SHA e ri-esaminare i punti rilevanti. **Impatto:** criticità architetturali candidate, non dichiarazioni di exploit sfruttabili né bug riprodotti.

## Executive summary

1. La libreria Beeftext usa `comboList.json`, JSON con `fileFormatVersion` **10**, `combos` e `groups`. Un export mirato dei soli combo non trasporta gruppi; il backup della libreria invece li comprende.
2. `ComboList::load/readFromJsonDocument` cancella la collezione *prima* di completare la validazione. Dopo un errore lo stato in memoria può risultare vuoto o parziale; il sorgente dichiara esplicitamente il contenuto indeterminato su errore.
3. `ComboList::save` apre il percorso in `WriteOnly` e scrive direttamente i byte; non usa, **in questo metodo**, staged write + replace atomico.
4. `ComboManager::saveComboListToFile` richiama l'archiviazione se l'auto-backup è attivo. `BackupManager::archive` usa `QFile(filePath).rename(dstPath)`: sposta il file corrente nel backup **prima** di scriverne uno nuovo. La copia precedente esiste nell'archivio se il rename è riuscito, ma il file canonico manca nell'intervallo fra rename e save.
5. `ComboManager::restoreBackup` carica in memoria il backup e poi salva: non è presente una fase di staging con rollback garantito nella funzione.
6. L'import da file gestisce conflitti per keyword e tempi di modifica, ma opera sulla collezione corrente e salva dopo la modifica; una failure parziale può lasciare un import incompleto. Non è una transazione unica.
7. Le date di ultimo uso sono mantenute separatamente in `comboLastUse.json`, con UUID come chiave di collegamento; dunque un export della libreria non va equiparato a un'immagine completa di ogni metadato.

## Tabella delle evidenze dirette

| ID | Grado | Componente, intervallo sorgente | Osservazione fattuale | Conseguenza per il nostro progetto |
|---|---|---|---|---|
| BF-D01 | D | `Combo/ComboList.h` 32–35 | `defaultFileName=comboList.json`; versione schema 10 | importatore version-aware |
| BF-D02 | D | `Combo/ComboList.cpp` 21–25, 336–344 | Chiavi di root + scelta includeGroups | classificare import/export vs backup |
| BF-D03 | D | `Combo/ComboList.cpp` 346–403 | Lettura cancella il modello e può fallire durante il loop | parse in struttura temporanea |
| BF-D04 | D | `Combo/ComboList.cpp` 413–449 | Load svuota, save scrive direttamente | atomicità della nostra persistenza |
| BF-D05 | D | `Combo/Combo.cpp` 30–46, 96–129 | Proprietà, compatibilità legacy, HTML→plain text | migrazione versionata, report perdita rich text |
| BF-D06 | D | `Combo/Combo.cpp` 134–139, 439–453 | Requisiti validità e JSON per combo | validazioni e campi |
| BF-D07 | D | `Group/Group.cpp` 12–18, 36–51, 113–121 | Gruppo: UUID/nome/descrizione/date/abilitazione | importazione gruppi |
| BF-D08 | D | `Combo/MatchingMode.h` 16–20 | `Default=0,Strict=1,Loose=2` | non usare enum impliciti |
| BF-D09 | D | `Combo/CaseSensitivity.h` 13–17 | `Default=0,Sensitive=1,Insensitive=2` | mapping esplicito |
| BF-D10 | D | `BeeftextConstants.h` 22–25 | Timestamp `Qt::ISODateWithMs` | parser date con offset |
| BF-D11 | D | `Combo/ComboManager.cpp` 90–109 | Ricostruzione gruppi e autosave su vecchio formato | non cambiare dati all'apertura senza backup |
| BF-D12 | D | `Combo/ComboManager.cpp` 119–142 | Archivia prima del salvataggio; restore poi save | snapshots verificati, rollback |
| BF-D13 | D | `Backup/BackupManager.cpp` 13–16, 110–137 | Massimo 50 backup; nome data+ms, archive con rename | retention sensibile al tempo, file canonico temporaneamente assente |
| BF-D14 | D | `Combo/ComboImportDialog.cpp` funzioni `loadCombosFromCsvFile`, `onEditPathTextChanged`, `performFinalImport`, `onImport` | CSV 3 colonne, UUID rigenerati, overwrite/skip/newer | preview, mapping e commit transazionale |
| BF-D15 | D | `LastUse/ComboLastUseFile.cpp` 19–27, 33–45 | Ultimo uso separato per UUID | metadati d'uso opzionali, non parte dei combo JSON |
| BF-D16 | D | wiki Import and export | JSON senza gruppi, CSV `keyword,snippet,name`; backup con gruppi | UI deve etichettare export/backup distintamente |

**Fonti dirette (evitare copie di codice nel progetto):**

- `ComboList.cpp`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/ComboList.cpp
- `ComboList.h`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/ComboList.h
- `Combo.cpp`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/Combo.cpp
- `ComboManager.cpp`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/ComboManager.cpp
- `Group.cpp`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Group/Group.cpp
- `MatchingMode.h`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/MatchingMode.h
- `CaseSensitivity.h`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/CaseSensitivity.h
- `BackupManager.cpp`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Backup/BackupManager.cpp
- `ComboImportDialog.cpp`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/Combo/ComboImportDialog.cpp
- `ComboLastUseFile.cpp`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/LastUse/ComboLastUseFile.cpp
- `BeeftextConstants.h`: https://raw.githubusercontent.com/xmichelo/Beeftext/master/Beeftext/BeeftextConstants.h
- Wiki: https://github.com/xmichelo/Beeftext/wiki/Import-and-export

## Formato libreria, schema e versioning

**Root:** `{ "fileFormatVersion": 10, "combos": [...], "groups": [...] }`. In formato JSON di export snippet, `groups` può essere `[]`, e ogni combo non presenta la proprietà `group`; negli archivi completi può invece esserci il riferimento `group` per UUID.

**Combo:** `uuid`, `name`, `keyword`, `snippet`, `description`, `matchingMode` (numero), `caseSensitivity` (numero), `creationDateTime`, `modificationDateTime`, `enabled`; `group` facoltativo in backup. Il valore Unicode di `snippet` va preservato *esattamente* (normalizzazione automatica sconsigliata). In presenza di `useHtml: true` il codice legacy converte HTML in plain text: in una migrazione noi dovremo segnalare esplicitamente la trasformazione.

**Group:** `uuid`, `name`, `description`, `creationDateTime`, `modificationDateTime`, `enabled`. `Group::isValid` rifiuta un gruppo con UUID nullo, nome vuoto o date invalide.

**Versioni legacy:** prima di v2 `comboText/substitutionText`, prima di v3 `created/lastModified`, prima di v9 `useLooseMatch`, prima di v10 nessuna descrizione o case sensitivity esplicita; cambio schema può aggiornare la data di modifica. `ComboList` rifiuta file dichiarati più recenti della propria versione. Abbiamo quindi bisogno di import con policy esplicita per versioni non supportate, non di una procedura che riscriva immediatamente i sorgenti dell'utente.

**Validazioni:** distinguere struttura JSON, campi essenziali, date, UUID, confini Unicode, duplicati ID e collisioni keyword. Un file con struttura valida potrebbe comunque essere semanticamente ambiguo; questo va mostrato nell'anteprima, non risolto casualmente.

### Nota di sicurezza sui file

Non accettare il contenuto `snippet` come comando da eseguire. Stringhe come `#{powershell:...}` o macro di altri text expander sono **dati**, non istruzioni da avviare, fino a una futura conversione esplicitamente approvata. Non usare la validazione di sintassi del formato come autorizzazione a eseguire azioni. Non eseguire espressioni regex del file durante il parsing, non seguire import ricorsivi o symlink fuori dalla radice selezionata.

## Rischi statici prioritizzati (da verificare con riproduzione)

| Rischio | Evidenza | Severità candidata | Verifica suggerita |
|---|---|---|---|
| Perdita dello stato in memoria dopo JSON invalido | `clear()` prima di parse | Alta | file troncato, oggetto non array, gruppo corrotto, fallimento a metà |
| Assenza file canonico dopo rename backup + failure write | `archive` prima di `save` | Alta | filesystem read-only / disco pieno / crash dopo rename |
| Scrittura parziale o troncamento della libreria | QFile direct WriteOnly | Alta | fault injection durante write/close |
| Ripristino senza stage | load in memoria → save | Alta | backup valido ma disk write fallisce |
| Perdita gruppi esportando JSON combo | includeGroups false | Media | import export su gruppo non default |
| Import parziale e collisioni | mutazione prima della persistenza | Media | conflitti e errori in append/write |
| Timestamp toccati in migrazione legacy | Combo::touch() su vecchi formati | Media | verificare conservazione storia originaria |
| `BackupManager::moveBackupFolder` ramo apparentemente invertito | condizione `(!file.exists()) || rename(...)` conduce al flag `error` | Media, **candidato** | riproduzione unit; non etichettare come bug confermato |
| Riferimenti gruppo mancanti | `ensureCorrectGrouping` assegna primo gruppo | Media | report invece di riconciliazione silenziosa |
| CSV interpretato come formule da spreadsheet | RFC CSV/OWASP | Media | `=...`, `+...`, unicode full-width, escaping |

### Limiti dell'audit

- Non abbiamo controllato un clone completo con commit fissato, la cronologia git, tutti gli entry point di UI e le librerie Qt/XMiLib necessarie: **A4 non è completato**.
- Non abbiamo compilato Beeftext, creato backup sul programma, né iniettato fault reali. Le osservazioni statiche sono **evidenze sul codice letto**, non esiti runtime.
- La rinomina di file fra filesystem e i dettagli di atomicità possono variare. Non dedurre proprietà assolute su Windows/macOS/Linux dal solo codice Qt.
- Questo documento specifica proposte per la nuova codebase, non il comportamento certificato di una release binaria di Beeftext.

**Documenti correlati:** [contratto persistenza](../../docs/DATA_IMPORT_BACKUP.md), [migrazione](MIGRATION_CONTRACT.md), [matrice test](../../data/PERSISTENCE_AUDIT_SCENARIOS.csv), [fonti](SOURCE_EVIDENCE.md).
