# Alpha 20.7 — CI headless e qualificazione R2

**Data:** 14 novembre 2026 · **Versione:** `3.9.0-alpha.20.7`.

## Implementazione

- `.github/workflows/ci.yml`: workflow GitHub Actions **pronto da collegare a un repository** con job `source` Linux e `headless-windows`. Usa permessi limitati a `contents: read`, checkout senza credenziali persistenti, timeout per job e artifact dei soli report di qualità non sensibili.
- `scripts/quality_gate.py --mode ci`: richiede Python, .NET 10, Node 22 e npm 10; esegue audit, test Python, controlli statici, restore, build Release della solution e dei quattro tool, xUnit, prova CLI su profilo sintetico, test Node e **contratto JSON vero .NET→Node**. In assenza di un tool o di un risultato necessario, ritorna `FAIL`.
- CI **non** esegue `native_windows`, il vero picker desktop o l'host VS Code. Questi stati restano `NOT_RUN` anche se `ci` passa. Per i test con focus/clipboard servono sessioni Windows interattive esplicitamente qualificate.
- Il report include versione e fingerprint SHA-256 del codice. `git_commit` è valorizzato soltanto se la piattaforma CI fornisce un `GITHUB_SHA` nel formato previsto; l'archivio ZIP senza repository non inventa un commit. Il fingerprint comprende anche il workflow.
- Il report `restore.status` diventa `FAIL` se il restore fallisce; la provenienza `online`/`local` è dichiarativa e **non** corrisponde a un audit vulnerabilità NuGet.
- Aggiunti sei test Python che coprono il nuovo gate, il fail-closed di Node/contratto/restore, il commit e la digestione dei workflow.

## Comandi

Su Windows, senza inviare tasti o aprire applicazioni:

```bat
scripts\validate-gate.cmd ci
```

Su Linux/macOS (presuppone SDK .NET 10 e Node/npm installati):

```bash
./scripts/validate-gate.sh ci
```

Rapporto locale: `.local-cache/gates/ci.json`. Il gate `source` rimane utile senza .NET. Il gate `full` include invece scenari nativi per il solo laboratorio Win32 controllato e richiede un desktop Windows appropriato.

## Evidenze e limiti di questa consegna

- **Eseguiti qui:** 26/26 test Python, 32/32 test Node, audit e controlli strutturali; verifica di sintassi/struttura del workflow.
- **Non eseguiti qui:** build .NET 10 e xUnit Alpha 20.7, test incrociato reale .NET→Node e job GitHub Actions Windows; nessun avvio grafico e nessuna prova native.
- **Storico separato:** la Alpha 20.5 ha evidenze conservate di 588/588 xUnit su Windows. Non attestano che il codice Alpha 20.7 sia già compilato.
- Nessuna modifica a XAML, ViewModel, database, hotkey, logica Inserisci o motore di sostituzione. La CI non certifica espansione automatica in editor esterni, tuttora non implementata.

## R2 — Preparazione delle prove desktop

Il prossimo passo pratico è [R2.01, matrice di verifica del picker](R2_PICKER_WINDOWS_ACCEPTANCE.md). I test devono usare un profilo `--data-dir` temporaneo, snippet sintetici e un editor di prova non contenente documenti personali. Segnalare separatamente `PASS`, `FAIL` e `NOT_RUN`. **Non** cercare di automatizzare l'invio a una finestra non qualificata in CI.
