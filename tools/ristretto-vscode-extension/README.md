# Ristretto — adapter sperimentale VS Code (Alpha 20)

Questa **non** è un'estensione pubblicata nel Marketplace e non è un plugin installato automaticamente. È il primo adapter opt-in per un editor di terze parti, usando l'API ufficiale di VS Code. Non installa hook, non legge SQLite e non registra i tasti digitati.

## Avvio senza dipendenze npm

Serve VS Code (desktop) e un export JSON v1 di Ristretto. Per i soli test locali è necessario Node.js (consigliato v22). Dal percorso `Ristretto/tools/ristretto-vscode-extension`:

```shell
npm test
npm run check
```

Per aprire l'**Extension Development Host** senza installazione permanente:

```shell
code --extensionDevelopmentPath="PERCORSO_ASSOLUTO/Ristretto/tools/ristretto-vscode-extension" --new-window
```

Al suo interno:

1. `Ctrl+Shift+P` → `Ristretto: Scegli file JSON esportato`. Scegli un export creato da Ristretto. Per una prova priva di dati personali usa `fixtures/synthetic-export.json`.
2. Apri Impostazioni e abilita `ristrettoVscode.enabled`. **È falso per impostazione predefinita.**
3. In un file di testo locale digita `;firma ` (spazio finale), lascia un solo cursore e usa `Ctrl+Shift+P` → `Ristretto: Espandi trigger al cursore`.
4. Con la fixture il risultato atteso è `Caffè ☕ 漢字 `, con uno spazio finale conservato. `Ctrl+Z` annulla la sostituzione.

La scorciatoia globale di Ristretto che apre il picker resta indipendente. Qui il comando ha **nessuna scorciatoia predefinita**; eventualmente assegnane una nelle Impostazioni tasti di VS Code, non è globale.

## Perimetro di sicurezza

- Ammessi solo workspace attendibili, editor `file` o `untitled`, un cursore senza selezione e finestra attiva.
- All'interno della riga, trigger Unicode NFC/NFD, delimitatore già inserito conservato, confine iniziale verificato; se più snippet hanno lo stesso trigger attivo non espande.
- Ogni invocazione legge nuovamente l'export (solo locale, massimo 8 MiB); nessun aggiornamento automatico dalla libreria. Snippet troppo lunghi (più di 65.536 unità UTF-16) non sono espansi.
- L'API `TextEditor.edit` fornisce un'operazione con Undo, ma non garantisce una compare-and-swap transazionale generale. Un esito incerto non causa retry o rollback. Non usare l'adapter per file/documenti critici senza test.
- L'adapter non ascolta `onDidChangeTextDocument`, non modifica testi durante la normale digitazione, non analizza la clipboard e non ha telemetria.

[Specifiche e matrice test](../../docs/ALPHA19_VSCODE_ADAPTER.md).

## Suite nel vero Extension Development Host

Dalla radice del progetto, su Windows: `scripts\validate-vscode-host.cmd -CodeExecutable "C:\percorso\Code.exe"`.

Il runner usa VS Code già installato, con profilo portable isolato e documenti sintetici, e carica `test/host/index.js` tramite `--extensionTestsPath`. Sono implementati 14 scenari, inclusi comando registrato, Undo/Redo, Unicode, multilinea, documenti grandi e cambi concorrenti. Report host e log di lancio sono distinti. Nessuna dipendenza npm o installazione nel profilo personale.

**Stato:** controllo sintattico e 32 test Node superati; vero host **NOT_RUN** in questa sessione perché il renderer di VS Code termina con codice 49. [Comandi, evidenze e limiti](../../docs/ALPHA20_3_VSCODE_HOST_TESTS.md).

## Test contrattuale produttore .NET → consumer Node (Alpha 20)

Con .NET 10 SDK e Node.js 22 disponibili, dalla radice `Ristretto`:

- Windows: `scripts\validate-export-contract.cmd`
- Linux/macOS: `./scripts/validate-export-contract.sh`

Il test xUnit crea una **libreria SQLite sintetica**, esporta attraverso `PortableLibraryService.ExportAsync` e passa il JSON all'adapter. Verifica NFC/NFD, emoji, gruppi/spunti disabilitati e snippet troppo lunghi; il file temporaneo è cancellato dallo script. Non legge il database personale. Vedere [Alpha 20](../../docs/ALPHA20_VSCODE_CONTRACT.md).
