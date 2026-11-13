# Alpha 19 — primo adapter per un editor reale: Visual Studio Code

**13 novembre 2026 · 3.9.0-alpha.19 · adapter sperimentale / opt-in · test VS Code reali da eseguire.**

## Risultato e perimetro

Nel progetto è inclusa una **estensione locale per Visual Studio Code** in [`tools/ristretto-vscode-extension`](../tools/ristretto-vscode-extension/README.md), creata usando l'API ufficiale `vscode.commands.registerCommand` e `TextEditor.edit`. Non richiede npm install, database, rete o toolchain TypeScript per essere avviata. I sorgenti e i test usano JavaScript + Node (test runner integrato).

L'estensione esegue un'**espansione manuale ed esplicitamente autorizzata dentro un vero editor**: cerca il trigger davanti a un cursore singolo, lo sostituisce con il testo dello snippet tramite un unico `TextEditor.edit`, preservando un eventuale delimitatore già inserito. Il documento e il range sono controllati prima della richiesta; l'esito è controllato dopo, con esito *incerto* se la revisione cambia in modo inatteso. Il comando può fallire su editor readonly e non tenta retry/rollback automatici. È un vero percorso tramite API di terze parti, ma **non** è un'espansione automatica quando si digita. Non interpreta eventi `onDidChangeTextDocument` come evidenza certa di digitazione umana.

## Collegamento ai dati di Ristretto

Si usa **soltanto** l'export JSON versionato `text-expander-studio/snippets` v1 già prodotto da Ristretto (`Esporta JSON`). L'utente sceglie il file nel comando VS Code `Ristretto: Scegli file JSON esportato`. L'estensione non legge direttamente il database SQLite, non usa la hotkey globale di Ristretto e non stabilisce IPC con l'app.

- L'utente deve abilitare esplicitamente `ristrettoVscode.enabled = true`, predefinito **false**. Senza file o workspace attendibile: nessuna modifica.
- Solo documenti `file` e `untitled` con una singola selezione vuota; unicamente quando VS Code ha il focus.
- Trigger con confine iniziale di parola, NFC/NFD, emoji e alfabeti non latini; duplicati: nessuna sostituzione; gruppo/snippet disabilitato: escluso.
- Si considera solo la riga del cursore: non si attraversano newline, e i delimitatori vengono mantenuti. `;firma` + spazio funziona su comando come `Caffè ☕ 漢字 `.
- Il file di export può contenere fino a 8 MiB; contenuti degli snippet sopra **65.536** unità UTF-16 non sono selezionati da questo adapter (Ristretto Core permette fino a un milione). Non vengono troncati o sostituiti parzialmente.
- L'export va aggiornato manualmente dopo le modifiche alla libreria. Gli snippet non vengono memorizzati in un'altra banca dati.

## Sicurezza e limiti **da non confondere**

`TextEditor.edit` esegue una modifica editoriale con undo; il contratto dell'API non garantisce una compare-and-swap contro tutte le modifiche concorrenti. Il controllo di `TextDocument.version` immediatamente prima e dopo l'operazione è una mitigazione e non una prova di transazione atomica. Qualora sia rilevata una revisione inattesa **non si ripete la modifica**, perché potrebbe essere stata applicata.

L'estensione non osserva la tastiera, non registra il testo digitato, non legge la clipboard, non invia dati in rete e non scrive nel contenuto dell'export. Non viene eseguita in workspace non attendibili. Non ci sono API per dimostrare se una modifica in VS Code è digitazione, incollamento, macro o altra estensione: per questo in Alpha 19 l'unico evento di ammissione è **l'invocazione manuale del comando**. Non implica compatibilità con Notepad, browser, Word, editor remoti o campi protetti.

**L'API di VS Code non offre nel presente adapter una prova generale di password field, provenienza committata o cambi atomici in altre applicazioni.** Non collegare questa implementazione alla registrazione globale della tastiera.

## Verifiche e gate

Qui sono stati eseguiti i test Node della logica (`npm test`) con fixture sintetica e il controllo sintattico (`npm run check`). Le prove nel vero host VS Code e le suite .NET/Windows rimangono **NOT_RUN** finché non vengono eseguite sul PC.

Da eseguire manualmente con VS Code:

1. Estrarre tutto lo ZIP, aprire VS Code con l'estensione locale (`code --extensionDevelopmentPath=<percorso-assoluto>/Ristretto/tools/ristretto-vscode-extension --new-window`).
2. Nella finestra VS Code: eseguire `Ristretto: Scegli file JSON esportato`, scegliendo la fixture sintetica oppure un export della propria libreria; abilitare `ristrettoVscode.enabled` nelle Impostazioni.
3. Aprire un file locale di testo o un documento non salvato, digitare `;firma ` e poi eseguire `Ristretto: Espandi trigger al cursore` dal Command Palette.
4. Atteso con fixture: `Caffè ☕ 漢字 ` (spazio conservato), un'unica operazione annullabile con Undo. Provare anche testo `foobar;firma` (nessuna sostituzione), due cursori (rifiuto), file readonly (non applicato), documento remoto (rifiuto).
5. Ripetere con un export reale e documentare OS, versione VS Code, lingua editor, esiti e casi di concorrenza.

### Roadmap di uscita dall'esperimento

- **Gate 1:** validazione Windows delle Alpha 8–19 e prova reale con Extension Development Host.
- **Gate 2:** contract test fra JSON generato da .NET e parser JS; verifica benchmark con librerie grandi e Unicode al limite.
- **Gate 3:** progettazione dell'eventuale modalità automatica *specifica per VS Code*, con valutazione del rischio per gli eventi non attribuibili; non basta ascoltare onDidChangeTextDocument.
- **Gate 4:** adapter per ulteriori editor/API supportate oppure fallback al picker; in seguito F4–F6.

Fonti ufficiali delle API, da verificare periodicamente: [VS Code Commands](https://code.visualstudio.com/api/extension-guides/command), [VS Code API](https://code.visualstudio.com/api/references/vscode-api), [Testing Extensions](https://code.visualstudio.com/api/working-with-extensions/testing-extension).
