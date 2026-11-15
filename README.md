# Ristretto — text expander multipiattaforma

**Versione di lavoro: 3.9.0-alpha.20.25.** Stack: C# / .NET 10, Avalonia, SQLite e test xUnit.

Ristretto gestisce gruppi e snippet Unicode, consente la ricerca tramite picker e offre su Windows una hotkey globale configurabile per aprirlo. **Invio** e **Inserisci** nel picker hanno avuto una prova positiva su Notepad con documento sintetico Unicode; questo non dimostra ancora l'espansione automatica durante la digitazione.

## Alpha 20.25 — Hotkey per snippet e correzione della build

- Corretti i due `CS0246` nei test dell'anteprima R5: mancava il namespace Application del tipo già esistente.
- Nuovo campo **Hotkey dello snippet (facoltativa)**: assegna per esempio `Ctrl+Alt+F`, salva e usa la combinazione nell'applicazione Windows attiva per inserire lo snippet senza aprire il picker. Vuoto o `None` rimuove l'associazione.
- Combinazioni duplicate segnalate; snippet/gruppi disabilitati non attivano hotkey. SQLite, backup e JSON conservano le associazioni; migrazione dal database precedente verificata con dati sintetici.
- **GATE CI: PASS**, anche da ZIP estratto in cartella nuova: 820 xUnit, inclusi 41 nuovi casi, 32 Node e contratto export .NET→Node. Prova delle nuove hotkey sul desktop ancora aperta; l'inserimento usa gli appunti e richiede un destinatario compatibile, come il picker.

[Uso, compatibilità e verifiche](docs/ALPHA20_25_SNIPPET_HOTKEYS.md).

## Alpha 20.24 — anteprima dei trigger nell'Edit esterno (R5)

- Nuovo comando facoltativo: `.\scripts\observe-classic-edit-preview.cmd 15`. Dopo 5 secondi di preparazione osserva un campo Win32 `Edit` sintetico e conta i **potenziali** riconoscimenti di `;firma` seguito da spazio.
- La simulazione riutilizza le politiche Unicode e i delimitatori del motore, ma **non attribuisce l'input alla digitazione** e **non può autorizzare alcuna sostituzione**. Testo letto solo temporaneamente in RAM; nessun contenuto salvato.
- Nuovi test xUnit e controlli statici. Gli errori di compilazione sono corretti nella 20.25 e la suite passa; la prova WinEvent sul desktop resta da eseguire. In questo incremento 20.24 picker, hotkey, GUI e SQLite erano invariati.

[Guida in italiano e limiti](docs/ALPHA20_24_TRIGGER_PREVIEW.md).

## Alpha 20.23 — osservazione WinEvent temporanea su Edit classici (R5)

- **Nuovo osservatore esplicito di eventi:** il laboratorio può ascoltare per 5–30 secondi le notifiche di modifica e di focus di un `Edit` Win32 esterno, senza leggere tasti né scrivere nell'editor.
- La sessione di osservazione invalida il contesto dopo cambi di finestra, controllo o letture non affidabili. I risultati sono **solo candidati**: non possono autorizzare l'espansione automatica.
- Nuovi xUnit e contratti `Ristretto.DevTools`; compilazione e prove native Windows della **20.23 da eseguire**. GUI, picker, hotkey, servizio Inserisci e SQLite invariati.

Per una prova facoltativa **esclusivamente su testo sintetico**, dalla cartella Ristretto:

```powershell
.\scripts\observe-classic-edit-live.cmd 15
```

L'osservatore può leggere temporaneamente in RAM fino a 4096 unità UTF-16 del campo di prova. [Guida completa e limiti](docs/ALPHA20_23_LIVE_EDIT_OBSERVER.md).

## Alpha 20.22 — correzione test e miglioramenti all'interazione

- **Corretto il test Unicode in mezzo al testo:** nella fixture `a☕b`, il cursore dopo l'inserimento è all'indice UTF-16 2 (non 3). Il motore continua correttamente a rifiutare cursori spostati.
- **Campo «Testo da espandere» reattivo:** cresce verticalmente e orizzontalmente con la finestra, senza altezza fissa o limite artificiale di larghezza.
- **Picker da tastiera:** frecce Su/Giù selezionano il risultato anche dal campo di ricerca; **Invio inserisce quello selezionato**, usando le stesse protezioni monouso del pulsante Inserisci.
- Nuovi xUnit e contratti DevTools aggiornati; **compilazione e test della 20.22 da confermare su Windows**. Non cambia il database, la hotkey o l'espansione globale.

[Dettagli tecnici e prova manuale](docs/ALPHA20_22_PICKER_EDITOR_AND_CARET.md).

## Alpha 20.21 — osservazione Unicode esplicita su un `Edit` esterno

Hai confermato **compilazione e test PASS della Alpha 20.20**: questa è la nuova baseline Windows. La 20.21 aggiunge un esperimento su un controllo Win32 `Edit` classico esterno. Con un comando manuale si confrontano due stati di un piccolo documento sintetico, senza salvare il testo letto o modificarlo. **Non abilita l'espansione automatica.**

```powershell
.\scripts\observe-classic-edit-text.cmd
```

**Attenzione:** la prova legge temporaneamente in RAM fino a 4096 unità UTF-16 del campo attivo, quindi va usata soltanto con un documento di prova senza dati personali. Notepad moderno, browser, Word e RichEdit possono essere esclusi. Compilazione e xUnit della nuova versione attendono ancora verifica su Windows. GUI, picker, hotkey, SQLite e servizio Inserisci sono invariati. [Dettagli](docs/ALPHA20_21_EXTERNAL_EDIT_OBSERVATION.md).

## Cosa era cambiato nella Alpha 20.19.4

- **Nessuna dipendenza da Python:** rimossi gli script `.py` e sostituiti con `tools/Ristretto.DevTools`, un programma console C# .NET 10.
- Audit namespace e riferimenti, controlli XAML/SQLite/localizzazioni, test dei tool, verifiche CLI, qualificazione Notepad, packaging ZIP e quality gate ora utilizzano C#.
- `scripts/validate.cmd`, `scripts/validate-source.cmd`, `scripts/validate-picker-notepad.cmd` e gli script equivalenti Linux sono semplici avviatori di .NET.
- Eliminato il validatore storico `verify-release.ps1`; gli invarianti di versione e i controlli strutturali principali sono trasferiti agli strumenti C# e gli xUnit continuano a verificare il comportamento.
- Risolto il warning **xUnit2000** invertendo gli argomenti dell'assert nel test dell'adapter Windows.

Il codice applicativo del picker, la hotkey e il database non sono stati modificati. L'adapter VS Code resta un esperimento facoltativo JavaScript, necessario per l'API di quell'editor e non incluso nel percorso principale.

## Requisiti

- **SDK .NET 10** per compilazione, test, controlli sorgenti e strumenti diagnostici. Python non è richiesto.
- **Windows 10/11** per la versione desktop Windows e le prove Notepad. I controlli strutturali C# sono portabili.
- **Node.js 22** soltanto per i controlli dell'estensione VS Code (`gate ci`/`gate full`), non per `gate core`.

## Comandi fondamentali

Dalla cartella contenente `Ristretto.sln`, in PowerShell:

```powershell
.\scripts\validate.cmd
.\scripts\validate-source.cmd
.\scripts\validate-picker.cmd
```

Il primo comando verifica sorgenti, compila, esegue xUnit e le prove CLI. Il secondo esegue audit, controlli strutturali e regressioni degli strumenti C#; **richiede anch'esso .NET 10**. Il terzo esegue i test mirati al picker. `PASS` indica soltanto il perimetro eseguito: un test grafico non avviato resta `NOT_RUN`.

Per Notepad, gli script continuano a usare una directory di test isolata:

```powershell
.\scripts\validate-picker-notepad.cmd prepare --open
.\scripts\seed-picker-notepad.cmd TOKEN
.\scripts\validate-picker-notepad.cmd launch --session TOKEN
.\scripts\validate-picker-notepad.cmd verify --session TOKEN
```

Sostituire `TOKEN` con l'identificativo mostrato da `prepare`. Per i passaggi descritti dettagliatamente, consultare [BUILD_AND_TEST](docs/BUILD_AND_TEST.md).

## Documentazione

- [Stato corrente](docs/CURRENT_STATE.md)
- [Roadmap unica](docs/ROADMAP.md)
- [Compilazione e verifiche in italiano](docs/BUILD_AND_TEST.md)
- [Handoff](docs/HANDOFF.md)
- [Indice dei documenti](docs/INDEX.md)
- [Osservazione esterna 20.21](docs/ALPHA20_21_EXTERNAL_EDIT_OBSERVATION.md) · [Bridge input Unicode 20.20](docs/ALPHA20_20_OWNED_INPUT_BRIDGE.md) · [Hotfix CS9174](docs/ALPHA20_19_5_CS9174_FIX.md)
- [Migrazione strumenti C#](docs/ALPHA20_19_4_CSHARP_TOOLS.md)

Gli archivi e i report delle Alpha precedenti restano storici: alcuni descrivono la vecchia pipeline Python, non più disponibile in questa release.
