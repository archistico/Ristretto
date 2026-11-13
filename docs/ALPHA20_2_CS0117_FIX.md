# Alpha 20.2 — Hotfix CS0117 e audit di visibilità cross-assembly

**Data:** 13 novembre 2026. **Ambito:** correzione build Core/Application, audit Python, test e documentazione. Nessun nuovo supporto ad editor esterni e nessuna modifica alla GUI Preview 9, alla hotkey o al database.

## Errore riprodotto dalla segnalazione Windows

In `scripts\validate-native.cmd` la compilazione del laboratorio si fermava su `CommittedEditDeltaVerifier.cs:129`: **CS0117** — `CommittedInputSession` non espone `IsValidText`. Il metodo esisteva in `TextExpander.Core.Matching.CommittedInputSession`, ma era `internal static` e perciò non accessibile da `TextExpander.Application`. La precedente analisi strutturale verificava solo namespace e `using`, non l'accessibilità dei membri.

## Correzione

- `src/TextExpander.Core/Matching/CommittedInputSession.cs`: `IsValidText` diventa `public static`. Corpo e regole Unicode invariati; non viene permessa alcuna sostituzione aggiuntiva.
- `tests/TextExpander.Tests/CommittedEditDeltaVerifierTests.cs`: nuovo test del contratto pubblico per emoji, accenti combinati, arabo, surrogate non accoppiate e caratteri di controllo.
- `scripts/audit_source.py`: controllo conservativo per chiamate `Tipo.Metodo(...)` a membri dichiarati `internal static` in un assembly differente. Non sostituisce il compilatore; possono esistere forme di accesso non rilevate o accessi autorizzati da `InternalsVisibleTo` (non utilizzato qui).
- `scripts/test_audit_source.py`: due casi che dimostrano rispettivamente l'errore su `internal` e l'accettazione su `public`.
- `scripts/verify-release.ps1`: verifica esplicita dell'API condivisa prima dell'esecuzione nativa.

## Verifiche e limiti

- **Eseguiti qui:** 7/7 unit test Python dell'audit; audit di progetto senza segnalazioni; riproduzione statica della regressione sui vecchi sorgenti Alpha 20.1; 32/32 test Node; XAML/XML, cinque localizzazioni, schema SQLite e 607 collegamenti locali Markdown superati.
- Non eseguibili nell'ambiente di revisione: `dotnet build`, xUnit e laboratorio Windows in sessione grafica. **Non dichiarare la correzione compilata finché non viene eseguita su Windows.**
- L'Alpha 20.1 è **non compilante** nella prova Windows comunicata dall'utente; la Alpha 20.2 costituisce un hotfix candidato, non ancora una baseline certificata.

## Come verificare

Da un'estrazione pulita del nuovo ZIP, dentro `Ristretto/`:

```bat
scripts\validate.cmd
scripts\validate-native.cmd
```

Se un altro errore di compilazione emerge dopo CS0117, conservarne il percorso, la riga e il messaggio: prima del rilascio la compilazione effettiva di **tutti** i progetti rimane il gate autorevole.
