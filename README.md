# Ristretto — text expander multipiattaforma

**Versione di lavoro: 3.9.0-alpha.20.20.** Stack: C# / .NET 10, Avalonia, SQLite e test xUnit.

Ristretto gestisce gruppi e snippet Unicode, consente la ricerca tramite picker e offre su Windows una hotkey globale configurabile per aprirlo. **Invio** e **Inserisci** nel picker hanno avuto una prova positiva su Notepad con documento sintetico Unicode; questo non dimostra ancora l'espansione automatica durante la digitazione.

## Alpha 20.20 — raccordo Unicode verificato per il laboratorio

L'utente ha confermato **compilazione e test PASS** della Alpha 20.19.5, chiudendo la migrazione degli strumenti di validazione in C#.

La Alpha 20.20 introduce `OwnedCommittedInputBridge`: unifica la verifica delle modifiche Unicode nel controllo posseduto, la correlazione con la revisione e la provenienza ammessa e il riconoscimento dei trigger. Il laboratorio Win32 utilizza il nuovo componente; i test xUnit sono stati ampliati. **La nuova versione deve ancora essere compilata e provata su Windows.**

La GUI, il picker, la hotkey e SQLite sono invariati. Il bridge non intercetta la tastiera globale e non abilita sostituzioni in editor esterni. Vedi [rapporto tecnico](docs/ALPHA20_20_OWNED_INPUT_BRIDGE.md).

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
- [Bridge input Unicode 20.20](docs/ALPHA20_20_OWNED_INPUT_BRIDGE.md) · [Hotfix CS9174](docs/ALPHA20_19_5_CS9174_FIX.md)
- [Migrazione strumenti C#](docs/ALPHA20_19_4_CSHARP_TOOLS.md)

Gli archivi e i report delle Alpha precedenti restano storici: alcuni descrivono la vecchia pipeline Python, non più disponibile in questa release.
