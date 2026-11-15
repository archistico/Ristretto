# Alpha 20.19.4 — migrazione degli strumenti a C#

## Perché

Gli strumenti di validazione erano stati realizzati in Python senza una decisione esplicita sullo stack. Su richiesta dell'utente, ora usano C# e .NET 10 come il resto di Ristretto. La migrazione non introduce nuove funzioni desktop.

## Migrazione delle funzioni

| Prima | Ora |
|---|---|
| `quality_gate.py` | `Ristretto.DevTools gate` |
| `audit_source.py` e `verify-release.ps1` | `Ristretto.DevTools audit` e `static` |
| `verify_static.py`, `verify_gui_redesign.py`, `validate_static.py` | `Ristretto.DevTools static` |
| `test_*.py` | `Ristretto.DevTools selftest` e `DevToolsMigrationTests` xUnit |
| `check_startup_profile.py` | `Ristretto.DevTools startup` |
| `qualify_notepad.py` | `Ristretto.DevTools notepad` |
| `package.py` | `Ristretto.DevTools package` |

I report storici Python sono conservati come documentazione precedente e non vengono eseguiti. I report nuovi distinguono `PASS`, `FAIL` e `NOT_RUN`.

## Compatibilità e rischi

La conversione non è un mero cambio di linguaggio: i vecchi test Python non possono essere considerati automaticamente trasferiti. I nuovi controlli C# coprono i principali invarianti, ma **la parità completa di comportamento e il funzionamento nativo sono da confermare tramite compilazione/xUnit e prove Windows**. Il tool C# usa `Microsoft.Data.Sqlite` per controllare SQLite in memoria anziché il provider Python.

La qualificazione Notepad conserva cartelle di sessione isolate, confronto Unicode rigido e ricevute prive del testo. Nessun hook globale è attivato. Il progetto VS Code usa JavaScript solo perché richiesto dall'API dell'editor ed è fuori dal percorso prioritario.

## Verifiche e stato

**Implementato nei sorgenti.** Il compilatore .NET e gli xUnit non sono disponibili in questo ambiente; non viene dichiarato un test Windows passato per questa migrazione. La conferma più recente dell'utente è il `GATE CORE: PASS` della 20.19.3.

Per verificare: [Guida in italiano](BUILD_AND_TEST.md).
