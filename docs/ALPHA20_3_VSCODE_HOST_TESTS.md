# Alpha 20.3 — suite per il vero host VS Code

## Implementato

Suite JavaScript senza dipendenze npm, caricata tramite `--extensionTestsPath`, e runner Windows che usa una copia **già installata** di VS Code. Il percorso segue la [documentazione ufficiale dei test di estensione](https://code.visualstudio.com/api/working-with-extensions/testing-extension).

Il runner crea una cartella nuova con workspace sintetico, profilo e directory estensioni separati. Imposta anche `VSCODE_PORTABLE`: la [modalità portable](https://code.visualstudio.com/docs/setup/portable) prevale su `--user-data-dir` e `--extensions-dir`. Non installa l'adapter nel profilo personale, non legge il database di Ristretto e non richiede download.

Sono previsti **14 scenari host**, ancora **non eseguiti**:

1. Attivazione reale dell'estensione, comando registrato, workspace attendibile e finestra focalizzata.
2. Comando registrato su documento non salvato, con delimitatore conservato.
3. Un Undo ripristina il trigger; Redo ripristina l'espansione.
4. Comando su file locale sintetico.
5. Trigger NFD e range UTF-16 reale.
6. Trigger emoji ZWJ, contenuto multilinea e Undo.
7. Documento di oltre 100.000 caratteri con prefisso conservato.
8. Trigger all'interno di una parola non espanso.
9. Adapter disabilitato: documento invariato.
10. Selezione non vuota e cursori multipli: nessuna modifica.
11. Cambio documento durante la lettura dell'export: nessun reindirizzamento.
12. Cambio selezione durante la lettura: nessuna modifica.
13. Modifica concorrente reale: contenuto concorrente conservato.
14. Export malformato: documento invariato.

Gli scenari 11–13 sospendono deterministicamente solo la lettura dell'export usando `createExpandHandler`; documenti, versioni, selezioni e API di modifica sono quelli del vero host. Gli altri scenari invocano il comando registrato dell'estensione. Non si tratta di prove di digitazione fisica, IME, perdita del focus della finestra di sistema, sola lettura o provider remoti.

## Esecuzione Windows

Da una sessione desktop, nella radice `Ristretto/`:

```bat
scripts\validate-vscode-host.cmd -CodeExecutable "C:\percorso\VSCode\Code.exe"
```

Specificare l'eseguibile `Code.exe`, non `code.cmd`. È possibile indicare `-OutputDirectory` (una **cartella nuova**) e `-TimeoutSeconds` (default 120). Il wrapper usa una execution policy limitata al processo PowerShell, come gli altri gate del progetto.

Per preparare profilo e argomenti **senza avviare VS Code**, aggiungere `-PrepareOnly`. Il risultato è `PREPARED_NOT_RUN`; gli argomenti sono salvati in `launch-arguments.json`. Dopo il crash segnalato dall'utente, nessun ulteriore avvio desktop è stato effettuato e sono state rimosse le opzioni grafiche sperimentali dai nuovi avvii del runner.

Il test può aprire una finestra temporanea di VS Code. Non interagire con quella finestra durante la prova. Il runner non sceglie o chiude altri processi per nome; in caso di timeout interrompe soltanto il processo che ha avviato.

- `host-results.json`: risultati prodotti dalla suite, senza contenuti dei documenti.
- `launch-result.json`: esito del runner, anche quando l'host non parte.
- `stdout.log`, `stderr.log`: diagnostica locale di VS Code.
- `PASS` richiede uscita zero, report valido e tutti i 14 scenari superati. Report assente, timeout o test fallito fanno fallire il comando. Una cartella esistente viene rifiutata per conservare le prove precedenti.

Questo gate è esplicito e separato da `validate.cmd`; non avviare applicazioni desktop automaticamente durante i normali test senza GUI.

## Risultati di questa sessione — 13 novembre 2026

**Eseguiti:** controllo sintattico della suite, parsing PowerShell, **32/32 test Node** esistenti, controlli statici del progetto. Il runner ha creato il profilo portable isolato e ha registrato correttamente il mancato avvio dell'host.

**Host NOT_RUN:** VS Code installato **1.127.0**, Windows x64. L'avvio raggiunge il processo principale ma il renderer si arresta con `launch-failed`, codice **49**. Nessun `host-results.json` è stato prodotto; **zero scenari host verificati**. Le prime prove con il processo GPU separato si arrestavano con `0xC0000135` e una terminazione fatale `GPU process isn't usable`; il successivo tentativo con `--in-process-gpu` falliva nel renderer. Le opzioni grafiche sperimentali sono state successivamente rimosse, senza un nuovo avvio dell'applicazione.

L'utente ha segnalato una finestra di errore `Code.exe`, eccezione `0x80000003`, durante queste prove. Il codice corrisponde a `STATUS_BREAKPOINT` secondo la [tabella Microsoft](https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-erref/596a1078-e883-4972-9bbc-49e60bebca55). È compatibile con una terminazione fatale del processo in quel periodo; senza dump o stack del processo non si può attribuirlo con certezza a uno specifico tentativo o modulo. L'indirizzo riportato da Windows non identifica da solo la causa. La rimozione delle opzioni non è una correzione del crash verificata sul desktop.

[Riepilogo ed esito del runner](../reports/VSCODE_HOST_VALIDATION.json). Gli esiti .NET e Win32 della [baseline precedente](ALPHA20_3_VALIDATION.md) restano validi per quei sorgenti, che non sono stati modificati da questo incremento. Non sono stati rieseguiti i test .NET/nativi per l'aggiunta di questa suite JavaScript.

**Prossimo passo:** ripetere il gate da una sessione desktop in cui VS Code avvia normalmente il renderer; completare separatamente la [prova Inserisci del picker](ALPHA20_3_PICKER_INSERT.md).
