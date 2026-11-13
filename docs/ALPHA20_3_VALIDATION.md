# Alpha 20.3 — consolidamento e validazione Windows

**Data:** 13 novembre 2026. Versione sorgenti: **3.9.0-alpha.20.3**.

## Difetti riprodotti e corretti

1. Il picker dichiarava `GUITHREADINFO` con cinque HWND invece di sei: mancava `hwndCaret`. Il test di regressione misurava 64 byte anziché 72 su x64. `GetGUIThreadInfo` richiede la dimensione della struttura completa; reinserito il campo prima di `RECT`. Riferimenti: [struttura Microsoft](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-guithreadinfo) e [funzione](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguithreadinfo).
2. Il fake del picker legava l'ascendenza al controllo attualmente focalizzato: passando a un altro figlio dichiarava distrutto il precedente. Separato l'insieme dei controlli dal focus; verificati sia cambio focus sia distruzione del controllo, senza inviare input.
3. Il laboratorio remoto configurava stdin con `Encoding.UTF8`, emettendo un BOM prima della prima richiesta. Riprodotto con due figli sintetici: senza BOM uscita 0 e ricevuta `Verified`; con BOM uscita 4 e `DuplicateOrInvalid`. Il trasporto ora usa `new UTF8Encoding(false)`. Il protocollo mantiene il rifiuto rigoroso e il gate monouso.

## Verifiche eseguite

| Verifica | Risultato |
| --- | --- |
| .NET SDK / runtime dei test | 10.0.201 / 10.0.5, Windows x64 |
| Solution Release | Compilata |
| Quattro strumenti separati | WindowsOutputLab, CoreBenchmark, ReplacementLab, UiReview compilati |
| Suite xUnit finale | **466 passati, 0 falliti, 0 saltati** |
| Picker | 18 casi, inclusi layout nativo e rifiuti su focus/clipboard/controllo distrutto |
| EDIT Win32 posseduto | **31/31** scenari passati |
| Processo figlio cooperante | Ready verificato, 1 sostituzione verificata, 0 rifiuti/incerti, uscita 0 |
| Editor remoto posseduto | Scenario valido verificato e snapshot obsoleto rifiutato; replay negato |
| Adapter VS Code Node | **32/32** test e sintassi dei tre moduli superati |
| Contratto .NET→Node | Export del servizio reale: 2 gruppi, 6 snippet, 3 idonei, 7 verifiche superate |
| Audit sorgenti e statico | Superati; **7/7** test Python passati |

Il restore online iniziale falliva con `NU1301` durante il collegamento TLS al servizio firme NuGet. Le compilazioni usano i pacchetti già presenti nella cache locale, tramite `RestoreSources`, con `NuGetAudit=false` limitato al processo. Nessuna disattivazione TLS o delle verifiche delle firme.

Verificata anche una nuova estrazione dello ZIP, inizialmente priva di `bin`/`obj`: manifest SHA-256 corretto, build di solution e quattro strumenti, 466 xUnit, 31 scenari nativi, smoke cooperante/remoto, Node, Python e contratto .NET→Node superati. I log build/nativo conservano anche questa seconda esecuzione; i sorgenti compilati corrispondono a quelli consegnati.

`validate.cmd` completa build e xUnit ma in questa sessione non rileva Python/Node con `where`; i relativi controlli e il contratto incrociato sono stati eseguiti separatamente con gli interpreti disponibili. Rimane un warning preesistente `xUnit2000` in `WindowsExternalAdapterCapabilityTests` (ordine expected/actual), senza errori di compilazione.

Evidenze: [riepilogo JSON](../reports/WINDOWS_ALPHA20_3_VALIDATION.json), [risultati xUnit TRX](../reports/windows-alpha20-3-tests.trx), [log build](../reports/windows-alpha20-3-build.log), [log nativo](../reports/windows-alpha20-3-native.log).

## Prove ancora da eseguire

- **Picker Inserisci:** hotkey e incolla effettivo in Notepad/VS Code, Unicode, multilinea, focus e selezioni; seguire la [matrice manuale](ALPHA20_3_PICKER_INSERT.md).
- **Vero Extension Development Host VS Code:** Undo, focus, concorrenza e file grandi.
- Audit vulnerabilità NuGet online, runtime macOS/Linux, IME e compatibilità editor esterni.

Gli scenari nativi qualificano esclusivamente gli EDIT creati dal laboratorio. Non esercitano l'incollamento del picker, non inviano testo a editor di terze parti e non attestano espansione automatica globale.
