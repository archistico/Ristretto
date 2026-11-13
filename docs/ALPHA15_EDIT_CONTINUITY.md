# Alpha 15 — continuità degli eventi nell’EDIT Win32 posseduto

**Versione:** `3.9.0-alpha.15` · **Interfaccia:** Preview 9 invariata · **Build/xUnit/native:** `NOT_RUN` nel contesto di consegna.

## Problema affrontato

Alpha 13/14 richiedono snapshot consecutivi e provenienza della modifica. Tuttavia, uno spostamento del caret e il ritorno alla posizione precedente possono avvenire **senza** cambiare testo o revisione: il buffer del trigger non deve sopravvivere a tale navigazione. Lo stesso vale per shortcut di editing, paste senza effetto, mouse e input IME.

## Implementazione

- `OwnedEditDispatchPolicy` (Application): classificatore *puro*, indipendente da Win32 P/Invoke. Riceve ID del messaggio, parametro scalare e indizio della presenza di Ctrl/Alt; restituisce solo provenienza candidata e `BreaksContinuity`, senza memorizzare testo, HWND o caratteri.
- `OwnedEditTarget.PumpPendingMessages`: classifica **esclusivamente** i messaggi ricevuti dal proprio controllo `Edit` (e gli eventi invalidanti della propria finestra padre); aumenta `TargetEpoch` quando la continuità è interrotta. La sessione Alpha 13 rileva il cambio epoch e azzera il riconoscimento.
- Eventi invalidanti: frecce, Home/End, PageUp/PageDown, Backspace/Delete, Tab/Enter/Esc, tasti di sistema, shortcut Ctrl/Alt, `WM_CHAR` di controllo, messaggi di paste/cut/undo, IME, spostamenti del mouse, focus, comandi di modifica/posizionamento visibili nel *message pump*.
- `WM_CHAR` stampabile può restare candidato; non costituisce prova fisica/umana. Le **unità surrogate di `WM_CHAR`** sono ammesse soltanto come candidati transitori, perché Windows può emettere una coppia UTF-16 in due messaggi; il `CommittedEditDeltaVerifier` rifiuta comunque documenti con surrogate non accoppiate. `WM_UNICHAR` richiede invece un punto di codice valido. Ctrl/Alt invalidano; Shift da solo non interrompe la continuità.
- Se `_epoch` raggiunge il limite massimo, il target viene invalidato invece di riutilizzare il numero.
- Fixture native ristrette (`PostOwnedContinuityBreakFixture`): accettano soltanto messaggi di controllo/navigazione invalidanti inviati all’EDIT del laboratorio. Nessun accesso a finestre esterne.

## Matrice di verifica

| Caso | Verifica prevista |
|---|---|
| Caratteri stampabili ASCII/Unicode | classificazione candidata, senza promozione a evento umano |
| Navigazione e riposizionamento | epoch invalidata anche senza modifica del documento |
| Buffer `;fir` + navigazione + `ma ` | nessuna espansione retroattiva |
| Backspace su documento vuoto | epoch invalidata, testo invariato |
| Ctrl/Alt + carattere / acceleratori / `WM_SYS*` | invalidazione prudente |
| Incolla, taglia, copia, undo, comandi edit, mouse | invalidazione o provenienza non attendibile |
| IME / composizione | continuità interrotta |
| Surrogate UTF-16 singole in `WM_CHAR` | solo candidati, **mai** input committato senza verifica del testo completo |
| WM_PAINT e WM_KEYUP | nessuna invalidazione arbitraria |
| GUI / hotkey / SQLite | sorgenti identici alla alpha.14 |

`NativeReplacementLab` aggiunge **tre** casi. Il totale atteso della suite nativa diventa **28** (25 precedenti + 3), ma rimane da eseguire. I nuovi xUnit sono in `OwnedEditDispatchPolicyTests.cs`; il totale generale dei test deve essere misurato dall’esecuzione e non dedotto da una stima.

## Limiti deliberati

Questa policy vede i messaggi *estratti dalla coda del nostro thread*. Un `SendMessage` diretto, controlli specializzati, input di editor esterni o gestori di clipboard non passano necessariamente per questo percorso. I cambiamenti non attribuibili sono comunque rifiutati dal gate di provenienza e dalle revisioni, ma un comando **senza modifica** non osservato nel pump resta fuori dalla garanzia. Il laboratorio inoltre usa polling: può scartare transizioni rapide e più revisioni, anziché ricostruirle. Nessuna lettura o modifica di testi di terze parti. Nessun hook globale. Nessuna qualifica del supporto IME generale. Non utilizzare la classificazione per autorizzare l’espansione su Word/browser.

## Consegna e verifica Windows

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

Facoltativa, in sessione interattiva:

```powershell
dotnet run -c Release --project tools/Ristretto.WindowsOutputLab -- --replacement-demo
```

**Criterio di accettazione:** build senza errori, tutti gli xUnit, 28 scenari nativi e prova interattiva positiva, senza alterare UI/hotkey. In assenza di tale evidenza il codice resta `IMPLEMENTATO_NON_VALIDATO`.

**Prossimo passo:** una strategia di eventi transazionali *push* nel solo editor cooperante per non dipendere dal polling, poi un contratto di capacità per il primo editor esterno. Non estendere questo `message pump` locale come se fosse un watcher globale.
