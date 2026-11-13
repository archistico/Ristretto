# Ristretto 3.9 Alpha 20 — contratto dell'adapter Visual Studio Code

**Data:** 13 novembre 2026. **GUI/hotkey/SQLite invariati.** Estensione locale manuale, opt-in, senza hook, listener di digitazione o espansione globale.

## Problema risolto

In Alpha 19 il destinatario veniva rilevato **dopo** le letture asincrone dell'export. Un cambio di scheda durante l'I/O poteva indirizzare il comando esplicito all'editor sbagliato. In Alpha 20 il comando cattura all'inizio un riferimento al `TextEditor`, al `TextDocument`, alla versione e alla selezione. Dopo `fs.stat`, dopo `fs.readFile`, dopo il parsing e immediatamente prima dell'edit, ricontrolla la stessa identità, il focus, lo stato del workspace, la selezione e la revisione. Controlla anche che il contenuto del range sia ancora esattamente quello riconosciuto.

Se il controllo fallisce, **nessun `TextEditor.edit` viene invocato**. Questo non costituisce una transazione compare-and-swap rispetto a modifiche concorrenti all'interno di VS Code. Dopo l'invio, un risultato non verificabile continua a essere *incerto* senza retry automatico; l'Undo è gestito dall'editor.

## Contratto export reale .NET → Node

Il comando [`scripts/validate-export-contract.cmd`](../scripts/validate-export-contract.cmd) (oppure [`validate-export-contract.sh`](../scripts/validate-export-contract.sh)) richiede .NET 10 SDK e Node.js 22. Esegue il test xUnit [`VscodeExportContractTests`](../tests/TextExpander.Tests/VscodeExportContractTests.cs) che crea una libreria SQLite temporanea **senza dati personali** e usa il **servizio di produzione** `PortableLibraryService.ExportAsync`. Il JSON temporaneo viene successivamente controllato dal parser/engine dell'estensione tramite [`verify-dotnet-export.js`](../tools/ristretto-vscode-extension/cli/verify-dotnet-export.js).

Casi previsti: versione/formato, GUID e metadati, gruppi/snippet abilitati, contenuti NFC/NFD, emoji ZWJ, testi lunghi (esclusi dall'adapter e non troncati), confine di parola. Test separati Node controllano anche cambi di editor, focus, selezione e versione del documento durante le operazioni asincrone.

**Importante:** l'export viene rigenerato dal servizio .NET, non è un JSON manuale spacciato per output .NET. Non modifica il database reale. Gli script eliminano l'export sintetico al termine. Nessun testo degli snippet viene registrato nei messaggi di errore.

## Verifiche effettive

- `npm run check`: **32/32 test Node superati** nell'ambiente di sviluppo della consegna (compresi 12 nuovi test), sintassi di `planner.js`, `controller.js`, `extension.js` valida.
- Script statici, validità XML/XAML, localizzazioni e controlli del pacchetto: verificati separatamente.
- **Non eseguiti qui**: compilation/.NET xUnit, contratto cross-runtime completo, avvio dell'Extension Development Host e prove native Win32. L'ultima baseline Windows storica verificata è Alpha 7.

## Come provare l'host reale

1. Eseguire `scripts\validate.cmd`, `scripts\validate-vscode.cmd` e `scripts\validate-export-contract.cmd` su Windows con .NET 10/Node installati.
2. Avviare il VS Code Extension Development Host come in [Alpha 19](ALPHA19_VSCODE_ADAPTER.md).
3. Con la fixture sintetica, aprire un file locale, digitare `;firma ` e invocare il comando manuale. Risultato atteso: `Caffè ☕ 漢字 ` con spazio finale e un solo Undo.
4. Ripetere cambiando scheda o focus immediatamente dopo l'invocazione: nessuna modifica deve colpire un editor diverso. Controllare anche workspace non attendibile, selezione multipla, documento chiuso e file remoti.
5. Registrare versione di VS Code, OS e risultati; **nessun supporto dichiarato** per Notepad, Word o l'espansione automatica durante la digitazione.

## Successiva Alpha 21

Prima eseguire/risolvere il gate Alpha 20; poi aggiungere prove dentro il vero VS Code Extension Host (API reale, non soltanto mock) e solo dopo valutare un adapter automatico opt-in specifico dell'editor. Non assumere che `onDidChangeTextDocument` provi un'origine umana.
