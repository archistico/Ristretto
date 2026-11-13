# Esportazione e importazione JSON — contratto v3.1

**Decisione:** SQLite rimane l'unico formato di backup completo. Il JSON è esclusivamente un formato di scambio.

## Schema v1

```json
{
  "format": "text-expander-studio/snippets",
  "version": 1,
  "groups": [
    {"id": "df82e3a1-d629-4a67-9c79-65aac638c9af", "name": "Messaggi", "isEnabled": true}
  ],
  "snippets": [
    {"id": "2bdb7fb0-631f-47ba-88b6-d367bd004a36", "groupId": "df82e3a1-d629-4a67-9c79-65aac638c9af", "title": "Saluto", "trigger": ";ciao", "content": "Ciao 🌍", "description": "", "isEnabled": true}
  ]
}
```

Se la GUI contiene modifiche non salvate, un avviso conferma che i dati in memoria non saranno esportati. Il file è codificato in UTF-8, con proprietà `camelCase`. I caratteri accentati, le sequenze combinanti e le emoji sono mantenuti nel contenuto; i **trigger** sono normalizzati in NFC in fase di salvataggio SQLite, secondo la policy attuale. L'ordinamento dei dati esportati segue l'ordinamento dello snapshot SQLite. I campi `id` del JSON sono utili a ricostruire le relazioni **nel file**, ma **non sono riutilizzati nel database di destinazione**.

## Importazione additiva e transazionale

1. Controlla la dimensione massima del file (32 MiB), la versione e l'identificatore del formato.
2. Deserializza il documento con profondità limitata.
3. Verifica unicità degli ID nel file, validità di nomi e snippet, riferimenti ai gruppi esistenti nel file e limiti sul numero di oggetti (100.000 ciascuno).
4. In una transazione SQLite crea nuovi gruppi e snippet con **nuovi GUID**. I nomi dei gruppi già esistenti vengono differenziati con ` (import 2)`, ` (import 3)` ecc. La transazione deve essere interamente confermata oppure annullata.
5. Incrementa una sola volta la revisione della libreria. Ricarica la visualizzazione soltanto dopo un risultato riuscito.

Non modifica gruppi, snippet o preferenze preesistenti. Non importa impostazioni personali, media esterni o script. Non esegue variabili, comandi, template o shell. Non converte formati non riconosciuti (Beeftext JSON / Espanso YAML richiedono successivi importatori separati).

## Esportazione sicura

Prepara uno snapshot coerente, lo serializza in un file `.partial` nella cartella di destinazione e lo sposta sul percorso finale **solo dopo** la scrittura completa. Il file finale preesistente **non viene sovrascritto**. Il formato JSON **non è un backup SQLite** e non contiene preferenze (`theme`, `language`) o metadati runtime.

## Rischi e successivi miglioramenti

- **Collisioni di trigger:** l'importazione può creare trigger omonimi; il matcher segnalerà ambiguità senza scegliere a caso. Una futura anteprima pre-import mostrerà conflitti e opzioni per risolverli.
- **Revisioni future:** qualunque `version` diversa da 1 viene rifiutata esplicitamente; non ignorare silenziosamente informazioni incompatibili.
- **Sicurezza file:** percorsi scelti dall'utente, limiti sulla grandezza e nessuna esecuzione di contenuti. Validare anche errori di disco pieno, file bloccati, JSON troncato e sincronizzazione esterna.
- **Test:** consultare `tests/TextExpander.Tests/PortableLibraryTests.cs`. Gli xUnit non sono stati eseguiti in questo ambiente.
