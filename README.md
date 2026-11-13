# Ristretto

**Versione sorgenti: 3.9.0-alpha.20.5** (14 novembre 2026). Progetto open source in sviluppo: text expander desktop multipiattaforma in **C# / .NET 10, Avalonia, SQLite**. GUI **Preview 9** congelata; scorciatoia globale del picker configurabile su Windows.

## Cosa funziona oggi

- Libreria locale di gruppi e snippet, ricerca, modifica, import/export JSON, import Beeftext/Espanso e backup.
- Profili separati con `--data-dir`; `--check-profile` controlla la directory senza GUI. Corretto il salvataggio della hotkey del picker. [Comandi e verifiche](docs/ALPHA20_5_STARTUP_PROFILES.md).
- Picker richiamabile dalla tray e, su Windows, tramite hotkey globale configurabile (default `Ctrl+Shift+Space`, disattivabile con `None`). Il picker offre **Copia testo** e **Inserisci**: quest'ultimo incolla manualmente lo snippet nella finestra esterna precedentemente attiva, se il destinatario è verificabile.
- **Filtra e premi Invio:** inserisce il primo risultato filtrato e nasconde il picker dopo l'invio; il pulsante Inserisci usa invece la selezione. [Uso e verifiche](docs/ALPHA20_4_ENTER_INSERT.md).
- Motore Unicode di matching/attivazione e laboratori di sostituzione Win32 posseduti, **separati dal prodotto**.
- Adapter **sperimentale** per Visual Studio Code con espansione **solo su comando esplicito**, caricando un export JSON; non è stato verificato nel vero Extension Development Host.

**Nota Inserisci:** l'inserimento Windows utilizza gli appunti (che rimangono contenenti lo snippet), recupera il focus sul controllo esterno e invia `Ctrl+V`. Non cancella il trigger eventualmente già presente; non verifica che l'editor abbia applicato l'incolla. Se il picker viene aperto dalla libreria o non è possibile identificare il controllo, Inserisci è disabilitato. [Dettagli e limiti](docs/ALPHA20_3_PICKER_INSERT.md).

**Non disponibile:** espansione automatica globale mentre si digita nelle altre applicazioni, adapter Windows generale, hotkey globale macOS/Linux. Le prove in editor esterni non sono equivalenti ai test del laboratorio cooperante.

## Requisiti e avvio

- **.NET SDK 10**, con disponibilità delle dipendenze NuGet; per l'estensione VS Code serve **Node.js 22** per i test.
- Desktop Windows/macOS/Linux compatibile con Avalonia; per hotkey globale e test Win32 è necessario Windows.

```sh
dotnet run --project src/TextExpander.Desktop/TextExpander.Desktop.csproj
```

## Compilazione, test e controllo sorgenti

Da una **nuova estrazione** dello ZIP, nella directory `Ristretto/`:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

`validate.cmd` verifica la versione, compila **solution e quattro strumenti separati**, esegue gli xUnit e, se disponibili Node/Python, i test aggiuntivi. `validate-native.cmd` deve essere eseguito **separatamente su Windows in una sessione desktop interattiva**. Linux/macOS: `./scripts/validate.sh`.

Per verificare namespace, `ProjectReference`, versioni, XML, traduzioni e link Markdown **anche senza .NET**:

```sh
python3 scripts/audit_source.py
python3 -m unittest discover -s scripts -p 'test_audit_source.py' -v
python3 scripts/verify_static.py
```

Gli audit Python **non sostituiscono** la compilazione C#.

## Stato delle verifiche

- **Alpha 20.5:** profilo unico e directory separate; **588 xUnit**, inclusi 30 nuovi casi, e dieci controlli del vero entry point passati; build Release di solution/quattro strumenti. Nessun avvio grafico. [Rapporto](docs/ALPHA20_5_STARTUP_PROFILES.md).
- **Alpha 20.4:** Invio per il primo risultato filtrato; **558 xUnit** passati, inclusi 50 ViewModel, build Release di solution e quattro strumenti. Tastiera e incolla reali ancora da provare sul desktop; nessun avvio grafico eseguito. [Rapporto](docs/ALPHA20_4_ENTER_INSERT.md).
- **Comandi picker:** Copia/Inserisci coordinati senza sovrapposizioni, testo fissato al clic ed esiti delle copie precedenti ignorati. Errori di Copia localizzati senza dettagli grezzi. **550 xUnit** passati, inclusi 42 del ViewModel; solution e quattro tool compilati, nessun avvio grafico. [Dettagli](docs/ALPHA20_3_PICKER_CLIPBOARD.md).
- **Caricamento picker:** indice costruito su worker e caricamenti precedenti annullati; Copia/Inserisci disabilitati durante l'attesa, risultati ed errori tardivi ignorati. **528 xUnit** passati, inclusi 20 test del ViewModel reale senza avviare Avalonia; solution e quattro strumenti compilati. [Dettagli](docs/ALPHA20_3_PICKER_LOADING.md).
- **Ricerca picker:** corretto il taglio di emoji/accenti al limite del prefisso; query Unicode incomplete gestite senza eccezioni e indice normalizzato riutilizzato. **506 test xUnit** passati, inclusi 30 casi di ricerca; solution e quattro strumenti compilati. [Dettagli](docs/ALPHA20_3_PICKER_SEARCH.md).
- **Incremento picker:** richieste Inserisci monouso e annullabili, gestione degli errori nativi e delle continuazioni di sessioni precedenti. Build Release di solution e quattro strumenti, **484 test xUnit** superati (36 del picker). Nessuna applicazione grafica avviata per questi test. [Dettagli](docs/ALPHA20_3_PICKER_SESSION.md).
- **Alpha 20.2:** compilazione e test superati su Windows, confermati dall'utente. Questo non dimostra l'espansione automatica in editor esterni (non implementata).
- **Alpha 20.3:** corretta la struttura Win32 del focus e il trasporto del laboratorio remoto. Build Release di solution e quattro strumenti, **466 test xUnit**, **31 scenari nativi**, due prove tra processi, 32 test Node e contratto .NET→Node superati su Windows con dipendenze locali. [Evidenze e limiti](docs/ALPHA20_3_VALIDATION.md).
- **Da provare sul desktop:** Inserisci del picker in Notepad/VS Code e vero Extension Development Host. L'accettazione di `SendInput` non verifica la modifica nell'editor.
- **Nuova suite host VS Code:** 14 scenari e runner Windows con profilo isolato. Avvio del renderer bloccato in questa sessione (codice 49), nessuno scenario host eseguito. [Comando e dettagli](docs/ALPHA20_3_VSCODE_HOST_TESTS.md).

## Documentazione

Partire da [stato corrente](docs/CURRENT_STATE.md), [roadmap dettagliata R1–R8](docs/ROADMAP.md), [build e test](docs/BUILD_AND_TEST.md) e [handoff](docs/HANDOFF.md). R1.01 completata; seguono qualificazione dell'isolamento, versione unica e gate affidabili. L'[indice](docs/INDEX.md) raccoglie specifiche tecniche, ADR e storicità; il [report di correzione Alpha 20.2](docs/ALPHA20_2_CS0117_FIX.md) documenta la regressione CS0117 e le verifiche ancora pendenti.
