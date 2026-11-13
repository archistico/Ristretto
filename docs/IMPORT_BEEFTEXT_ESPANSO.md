# Importatori esterni — Beeftext ed Espanso (aggiornato v3.3)

**Stato:** implementazione sorgente predisposta, **non compilata né verificata con xUnit** nell'ambiente di creazione. Importazione disabilitata per ogni snippet proveniente da software esterni; database SQLite come archivio operativo e unico formato di backup integrale.

## Flusso GUI aggiornato v3.3

1. `Importa` nella finestra principale: scegli `.json`, `.yml` o `.yaml`.
2. Lettura rigorosa UTF-8 (BOM UTF-8 facoltativo) con limite 8 MiB. Parsing **inerte e senza scritture SQLite**.
3. La preview costruisce una riga per ogni snippet convertibile: titolo, trigger, gruppo e rischi (`Collisione`, `Dinamico`, `Opzioni non supportate`). Le collisioni sono calcolate sia con il DB esistente sia **tra tutte le voci del file**, con trigger normalizzati NFC.
4. Selezione iniziale dei soli snippet **privi di rischi**; con una scelta consapevole si possono includere manualmente anche gli elementi segnalati. Campo filtro per titolo/trigger/gruppo, bottoni per selezione sicura e per pagina, paginazione da 60 elementi. La selezione resta conservata quando si cambia pagina o filtro.
5. Nessuno script o macro è eseguito. Gli snippet esterni rimangono **disattivati anche se vengono scelti manualmente**. I file importati mantengono il contenuto originale come testo letterale.
6. Conferma possibile solo con almeno uno snippet selezionato. Il commit include esclusivamente i gruppi referenziati dagli snippet selezionati e crea nuovi GUID. Non tocca i dati già presenti.
7. **Controllo di concorrenza**: nella stessa transazione SQLite la revisione della libreria viene confrontata con quella letta nella preview. Se altri salvataggi hanno modificato la libreria, l'importazione è rifiutata e va ricreata la preview.
8. Ricaricamento della GUI solo dopo il commit riuscito. Annullamento, parser non valido, revisione obsoleta o selezione invalida non modificano il database.

Le anteprime non sono backup. Il backup completo è sempre un file SQLite ottenuto tramite SQLite Online Backup API.

Consultare [la specifica dell'importazione selettiva](IMPORT_REVIEW.md) per limiti e matrice delle prove.

## Beeftext JSON v10

Riconoscimento tramite `fileFormatVersion: 10`, `groups[]` e `combos[]`. Sono letti `uuid`, `name`, `keyword`, `snippet`, `description`, `group`, `enabled`, `matchingMode`, `caseSensitivity` e `useHtml` (solo segnalazione). Il collegamento UUID gruppo→snippet è conservato tramite nuovi ID; i combo senza gruppi utilizzano un gruppo importato dedicato.

**Limiti espliciti:** vengono conservati trigger, nome, descrizione, contenuto Unicode e stato del gruppo; **tutti gli snippet sono importati disattivati**, indipendentemente dal loro stato precedente, perché matching mode e case sensitivity non sono ancora nel modello corrente. Le opzioni non rappresentabili sono segnalate. Il contenuto che contiene `#{...}` è conservato *letteralmente* e non interpretato; le variabili non vengono convertite. Non vengono importati le date d'uso, la descrizione del gruppo, i timestamp e i metadati non presenti nel nostro schema. Il rich text storico non viene convertito: eventuale HTML viene conservato come testo e segnalato.

**Formato non supportato:** JSON versioni 1–9 o superiori a 10; la lettura si interrompe esplicitamente. Import CSV Beeftext demandato a una slice successiva.

Fonti locali fornite: `Beeftext-master.zip`, percorsi `Beeftext/Combo/ComboList.cpp`, `Combo.cpp`, `Group/Group.cpp`.

## Espanso YAML

Riconoscimento di un documento YAML con `matches:`. Sono supportati per l'importazione inerte gli elementi con `trigger`, `triggers` e testo `replace`, oppure `form` conservato come stringa **letterale**. Una regola con più trigger produce più snippet. Gli snippet sono importati **disattivati**.

Le definizioni `vars`, shell, script, form, funzioni dinamiche e placeholder `{{...}}` / `[[...]]` sono **solo testo**: nessuna shell, download, callback o accesso ad altri file. Non vengono seguiti `imports`, include, glob o riferimenti a risorse esterne. Regex-only, immagini, regole prive di trigger/contenuto e strutture non rappresentabili sono saltate **con avviso**; non vengono convertite in trigger fittizi. Sono segnalati i vincoli per applicazione e le opzioni di matching non rappresentate. Il contesto di origine va consultato nel file originale.

Il parser è `YamlDotNet 18.1.0` (MIT, fonte NuGet: https://www.nuget.org/packages/YamlDotNet/18.1.0). Il formato YAML è complesso: per questa slice si applicano un limite del file (8 MiB), un limite sui match (10.000), controlli di profondità, numero nodi e cicli. Restano da validare i comportamenti su YAML ostile, tag/alias, file malformati e versioni future.

Fonti locali fornite: `espanso-dev.zip`, documentazione e configurazioni nella directory `espanso-dev/espanso/src/config` e moduli engine.

## Invarianti e test

- Preview senza scritture; commit transazionale e additivo; nessuna sovrascrittura di dati esistenti.
- Nessun contenuto dinamico viene interpretato, neppure durante l'anteprima.
- Titoli, trigger e contenuto originali preservano Unicode/emoji/combining marks; per il confronto il trigger viene portato a NFC al momento del salvataggio secondo la policy attuale.
- Gruppi, opzioni e snippet non importabili sono dichiarati nei warning. Importatore con zero snippet utilizzabili non può essere confermato.
- L'attivazione di uno snippet migrato è una **decisione successiva dell'utente** dopo la revisione.
- `ExternalImportTests.cs` contiene test xUnit predisposti. Sono necessari anche file Beeftext ed Espanso reali anonimizzati, prove Windows/macOS/Linux e una verifica di sicurezza del parser.

## Limiti e lavoro futuro

La GUI v3.3 consente la selezione per singolo snippet, ma non ancora un diff per campo o un editor di mappatura. Non esiste ancora una tabella persistente con i metadati originali `matchingMode`, variabili Espanso, la provenienza, i timestamp o i filtri per applicazione. Per il momento tali informazioni non vanno fatte sparire senza avviso. L'importazione CSV, il supporto versioni Beeftext più vecchie e una mappatura completa delle variabili sono fuori dallo scope v3.3.

**Backup e ripristino:** restano esclusivamente SQLite tramite Online Backup API. Il JSON portabile e gli YAML di origine non sono backup completi della nostra applicazione.
