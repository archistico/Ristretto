# Contratto di migrazione — Origini Beeftext, Espanso e import generico

**Stato:** proposta tecnica v1.8, da ratificare. Nessun importatore è implementato. **Obiettivo:** l'utente non deve perdere dati né far eseguire accidentalmente macro presenti in file esterni.

## Inventario dei formati e fedeltà

| Origine | Dati ottenibili | Dati assenti / rischio | Strategia proposta |
|---|---|---|---|
| Beeftext backup JSON v10 | gruppi, snippet, UUID, date, stato, matching | preferenze e last-use in altri file | import migliore, gruppi preservati |
| Beeftext export JSON | combo e proprietà principali | gruppi non inclusi | scegliere gruppo destinazione, report avviso |
| Beeftext CSV | keyword, snippet, nome | descrizione, ID, gruppi, date, matching, enabled | defaults non ambigui, report perdita |
| Beeftext legacy v1–9 | variabili e nomi storici | conversioni, rich text storico | parser per versioni + report trasformazioni |
| Espanso match YAML | trigger, replace, variabili, form, regexp, condizionali | include, macro e app-scope non convertibili 1:1 | conversione parziale e quarantena |
| Espanso config YAML | comportamenti e filtri per app | scoping dipende da piattaforma | import separato, non applicare in automatico |
| Generico JSON (futuro nostro export) | versione completa, ID e schema | nessuno se stessa versione | round trip garantito e testato |

## Pipeline transazionale, step by step

```text
SelectFile(s) / DragDrop
  -> ValidatePathAndSize
  -> DetectFormat (senza interpretare il contenuto come codice)
  -> ParseInIsolation
  -> SchemaAndSemanticValidation
  -> NormalizeForComparisonOnly (payload originale intatto)
  -> MapToDraftDomain (non persistito)
  -> AnalyzeReferencesAndCollisions
  -> Preview + warnings + choices
  -> StageChanges + validate entire future dataset
  -> DurableBackupOrSnapshot
  -> AtomicCommit (DB transaction / safe file replace)
  -> VerifyReadback
  -> Report + optional Undo/RecoveryPoint
```

L'importazione **non** deve consultare URL, eseguire shell/PowerShell, caricare DLL, leggere altri file non selezionati o seguire include esterni senza consenso. Nessuna conversione invisibile di Unicode: se per confronto decidiamo NFC, preservare nei dati salvati gli scalar originali.

## Decisioni di conflitto nella GUI

- Nuovo elemento: `Aggiungi`.
- Stesso ID e stessa origine verificata: `Aggiorna` solo dopo confronto e conferma.
- Stesso trigger ma ID differente: `Rinomina trigger` / `Importa disattivato` / `Salta` / `Scegli esplicitamente` quando il motore lo supporta. Vietata scelta casuale.
- Stesso trigger con maiuscole/NFC equivalenti: segnalare collisione *effettiva* secondo le regole di contesto configurate.
- Referenza gruppo mancante: scegliere `Gruppo importati` o mappare manualmente; non spostare in silenzio.
- Snippet con macro sconosciute: quarantena come **testo letterale non espandibile** finché non vengono esaminate.
- Overwrite globale: consentirlo solo in `Ripristina intera libreria` dopo backup valido, preview e conferma chiara.

## Anteprima GUI — requisiti di dettaglio

Tabella con colonne: [Origine], [Gruppo origine], [Trigger], [Nome], [Esito], [Conflitto], [Avvisi], [Azione]. Filtri: `Tutti`, `Da importare`, `Conflitti`, `Non convertibili`, `Bloccati`. Pannello destro: confronto prima/dopo, con preview testo e simboli di controllo resi visibili, mai esecuzione. In basso riepilogo numerico: nuovi, aggiornati, saltati, disattivati, warning, errori bloccanti. Pulsante `Importa` disabilitato se il piano è incoerente.

**Copia per diagnostica:** permettere il report dei soli ID e categorie; snippet e percorsi non vanno allegati automaticamente.

## Formato di export nostro (proposta)

JSON UTF-8 `schemaVersion`, `exportedAtUtc`, `appVersion`, `groups[]`, `snippets[]`, `aliases[]` se esistono, `policies[]` se supportate, `metadata`, `capabilitiesRequired`. Scrivere ID stabili, linguaggi/locale di variabili espliciti e timestamp UTC con offset. La versione 1 dev'essere un contratto pubblicato con test golden, non un JSON incollato direttamente dalla tabella SQLite. Per serializzare UTF-8 preservare payload senza normalizzazioni, anche emoji composte e caratteri RTL.

**Migliore alternativa:** separare export portabile (interoperabilità), backup interno coerente (ripristino), report CSV (consultazione): non chiamarli tutti "backup".

## Recovery / crash consistency

1. Acquisire snapshot coerente prima di migrazione/ripristino; non spostare il file canonico per crearne il backup.
2. In caso di SQLite, usare una transazione per la mutazione e la SQLite Online Backup API / `VACUUM INTO` per una copia consistente: evitare `File.Copy` del solo `.db` quando WAL è attivo.
3. Validare un backup prima di considerarlo punto di recupero: checksum, manifest, apertura/quick-check DB o reparse JSON. `sha256` verifica integrità dei byte ma non legittimità/origine.
4. Se un crash si verifica durante un'importazione, al riavvio lo storage deve essere o esattamente quello precedente o quello nuovo, mai un mix. Per file JSON, staging nella stessa partizione, flush e replace con semantics specifiche per OS da testare.
5. Non eliminare automaticamente l'ultima copia sana quando una retention cancella backup vecchi; fallimenti di backup non devono essere registrati come successi.
6. Per file cifrati futuri documentare chiave, recovery e comportamento senza chiave; non implementare una "cifratura fatta in casa".

## Compatibilità e privacy

- Importare da Beeftext senza modificare mai i file originali.
- Scartare macro pericolose? **No**: conservarne il testo nell'anteprima, segnando le funzioni incompatibili e chiedendo se importare disattivate, così si evita perdita silenziosa.
- Profili di privacy: nascondere snippet in log/report per impostazione iniziale; eventuale preview solo locale.
- Controllare CSV injection: un export destinato a foglio di calcolo è diverso da un export lossless interoperabile. Se si aggiunge una mitigazione per spreadsheet, renderla modalità *esplicita*, perché il prefix cambia i dati.

## Gate di accettazione

- Parse su copie valide e malformate; nessuna modifica in caso di annullamento/errore.
- Round-trip esatto di Unicode (incl. ZWJ, combining, line endings se previsto).
- Import multiplo deterministico con collisioni e locale diversi; ID stabili dove possibile.
- Fault injection filesystem e SQLite; backup restore verificabile dopo crash simulato.
- `source-to-destination` mapping per ogni campo; zero loss senza warning.
- Release importatore solo dopo test su file reali anonimi/esempi forniti dall'utente e versione sorgente identificata.

**Vedi:** [audit Beeftext](BEEFTEXT_PERSISTENCE_SOURCE_AUDIT.md), [studio Espanso](ESPANSO_DATA_MODEL_AUDIT.md), [scenari](../../data/PERSISTENCE_AUDIT_SCENARIOS.csv).
