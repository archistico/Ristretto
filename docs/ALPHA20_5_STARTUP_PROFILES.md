# Alpha 20.5 — Profili dati separati

**14 novembre 2026 · 3.9.0-alpha.20.5 · R1.01 completata, R1.02 parziale.**

Il percorso dati viene risolto una sola volta e condiviso da avvio, database, impostazioni, backup e singola istanza. Senza opzioni resta la directory locale `TextExpanderStudio` già usata dalle versioni precedenti: su Windows `%LOCALAPPDATA%\TextExpanderStudio`. Nessuna migrazione o copia della libreria.

## Comandi

Per aprire un profilo separato, dalla directory dei sorgenti:

```powershell
dotnet run --project src/TextExpander.Desktop/TextExpander.Desktop.csproj -- --data-dir ".local-cache\profilo di prova"
```

La cartella relativa viene risolta rispetto alla directory di lavoro del processo. Sono ammessi percorsi assoluti, spazi e Unicode, anche nella forma `--data-dir=<cartella>`. Una directory esplicita non valida non causa il ritorno al profilo personale.

Dopo la build Release, per controllare il percorso **senza aprire finestre**:

```powershell
dotnet src/TextExpander.Desktop/bin/Release/net10.0/Ristretto.dll --check-profile --data-dir ".local-cache\profilo di prova"
dotnet src/TextExpander.Desktop/bin/Release/net10.0/Ristretto.dll --help
dotnet src/TextExpander.Desktop/bin/Release/net10.0/Ristretto.dll --version
```

`--check-profile` crea la cartella se manca, scrive un file temporaneo esclusivo e lo elimina alla chiusura. Restituisce JSON con stato `profile_writable`, versione e percorsi. Non apre il database, non prende il mutex/lock e non registra hotkey: verifica la scrittura della cartella, non l'integrità SQLite o la disponibilità dell'istanza. Help/versione non accedono ai dati. Con un eseguibile Windows WinExe usare `dotnet ...dll` per raccogliere stdout/stderr nel terminale.

Codici d'uscita: `0` comando completato/istanza attivata, `2` opzioni o profilo non disponibili, `3` istanza già presente che non risponde. Gli errori vanno su stderr. Opzioni sconosciute, directory mancanti/vuote/duplicate e azioni diagnostiche incompatibili sono rifiutate prima dell'avvio desktop.

## Isolamento e hotkey

- Ogni directory usa `library.db` per snippet e impostazioni e `Backups/` per i backup del ViewModel reale.
- Mutex e pipe derivano dallo stesso percorso normalizzato; su Windows maiuscole/minuscole e separatori equivalenti identificano la stessa istanza. Due directory distinte hanno proprietari e attivazioni separati. Il secondo avvio dello stesso profilo attiva l'istanza già aperta.
- L'istanza principale mantiene anche `.ristretto.profile.lock` aperto in esclusiva. Questo aggiunge una difesa per alias symlink/junction: se il mutex usa un nome diverso ma il lock fisico è occupato, l'avvio si ferma prima del database. Il file vuoto rimane dopo la chiusura; non eliminarlo durante l'esecuzione. La prova con un alias reale resta aperta.
- Corretto il difetto emerso nei test: `WriteSettingAsync` rifiutava `picker.hotkey`, impedendo il salvataggio. La chiave è ora ammessa insieme a lingua e tema; chiavi sconosciute restano rifiutate. Nessuna modifica allo schema SQLite.
- Le impostazioni delle hotkey sono separate, ma la combinazione globale appartiene al desktop Windows. Un profilo nuovo propone ancora `Ctrl+Shift+Space`; il conflitto effettivo resta da qualificare. Si può salvare `None` per disattivarla. Chiudere le versioni precedenti prima di aggiornare.

## Verifiche

**588/588 xUnit**, inclusi **30 nuovi casi profilo** e i 50 casi esistenti del picker ViewModel. I nuovi test usano una directory utente sintetica per il percorso predefinito, percorsi relativi/Unicode, opzioni invalide, collisione file/cartella, nessun fallback, probe senza modificare il database, lock occupato e rilascio dopo errore, proprietà per profilo, secondo avvio e pipe reali, due SQLite indipendenti, backup dal ViewModel e persistenza hotkey dopo riapertura.

**10/10 controlli CLI** sul vero entry point compilato: help, versione, directory relativa Unicode, sintassi con `=`, opzioni invalide e collisione file/cartella. Ogni processo usa solo comandi diagnostici e directory temporanee sintetiche; nessun database o backend Avalonia avviato da questi controlli.

Build Release di solution e quattro strumenti, audit sorgenti/statici e sette test Python passati. Restore dalla cache NuGet locale; audit vulnerabilità online non eseguito. Python/Node restano da rilevare correttamente negli script in R1.04: audit Python e controllo CLI eseguiti direttamente. Resta il warning preesistente `xUnit2000` nella suite capacità adapter.

Verifica ripetuta da una nuova estrazione dello ZIP, inizialmente senza `bin`/`obj`: manifest SHA-256 verificato, solution/quattro strumenti compilati, 588 xUnit e dieci controlli CLI passati, audit e sette Python passati. Gli hash dei sorgenti funzionali e delle versioni corrispondono alla consegna.

Le prove xUnit aprono database e pipe sintetici, senza finestre, appunti o input nativo. Nessun avvio di VS Code o altri programmi grafici. R1.02 resta aperta per conflitti hotkey reali e alias; R2 resta aperta per il comportamento desktop, compreso filtro → Invio → incolla → chiusura picker. Le precedenti prove Node/native non sono state ripetute.

[Riepilogo JSON](../reports/STARTUP_PROFILE_VALIDATION.json) · [log build/test](../reports/startup-profile-validation.log) · [esiti CLI](../reports/STARTUP_PROFILE_CLI.json).

Per ripetere il controllo CLI dopo la build:

```powershell
python scripts/check_startup_profile.py --assembly src/TextExpander.Desktop/bin/Release/net10.0/Ristretto.dll --output .local-cache/startup-cli.json
```
