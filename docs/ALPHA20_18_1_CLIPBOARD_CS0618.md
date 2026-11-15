# Alpha 20.18.1 — Correzione warning CS0618 e avanzamento R2.05

**Data:** 15 novembre 2026. **Base:** Alpha 20.18. **Ambito:** sostituzione API obsoleta Avalonia, audit anti-regressione e documentazione. **Nessuna modifica funzionale intenzionale a picker, hotkey, SQLite o GUI.**

## Cosa stiamo facendo

Rimuoviamo il warning di compilazione `CS0618` segnalato dall'utente in `src/TextExpander.Desktop/App.axaml.cs`, riga 274 della Alpha 20.18. La vecchia API `IClipboard.GetTextAsync` è obsoleta in Avalonia 11.3.22. Il metodo consigliato è `ClipboardExtensions.TryGetTextAsync`, nel namespace `Avalonia.Input.Platform`, e restituisce sempre `Task<string?>`.

La rilettura della clipboard resta inserita tra la verifica del cambio di sequenza e la verifica finale del contenuto; nessun cambiamento al confronto Unicode o all'invio singolo di `Ctrl+V`.

## Correzioni

1. `App.axaml.cs`: `using Avalonia.Input.Platform;` e `await clipboard.TryGetTextAsync()` al posto della vecchia chiamata.
2. `scripts/audit_source.py`: rilevazione delle chiamate obsolete `.GetTextAsync(` nei sorgenti C#.
3. `scripts/test_audit_source.py`: due nuovi test per chiamata obsoleta rifiutata e API moderna accettata.
4. Versione e documentazione allineate; nessun cambio allo schema SQLite e nessuna nuova dipendenza NuGet.

## Nuova evidenza positiva per R2.05

L'utente ha eseguito la procedura su **Windows/Notepad** nella Alpha 20.18: `prepare --open`, `seed`, `launch`, `status`, `verify`. Lo stato è `MATCHES_EXPECTED`; la ricevuta è `PASS | saved_synthetic_document_matches_unicode_fixture`. L'utente ha confermato che **il picker ha funzionato**.

Questa evidenza conferma il contenuto del documento sintetico Unicode salvato e l'utilizzo del picker riferito dall'utente. **Non** certifica la provenienza tramite cattura della tastiera, la compatibilità generale di tutti gli editor né l'espansione automatica globale. I casi negativi focus/clipboard/destinatario restano da qualificare.

## Verifiche Alpha 20.18.1

| Verifica | Esito |
| --- | --- |
| Nuovi test Python dell'audit e suite completa | **PASS** (61 test, eseguiti qui) |
| Test Node per adapter sperimentale VS Code | **PASS** (32 test, eseguiti qui) |
| Audit C# using/namespace, riferimenti e API deprecata | **PASS** statico |
| Verifiche statiche XAML, SQLite, localizzazioni e GUI | **PASS** statico |
| Build .NET e xUnit della 20.18.1 | **NOT_RUN** — SDK assente nell'ambiente di preparazione |
| Prova reale Windows di questa hotfix | **NOT_RUN** |
| Inserimento Unicode Notepad sulla 20.18 precedente | **PASS riferito dall'utente** |

## Come verificare la versione corretta

1. Estrarre lo ZIP della Alpha 20.18.1 in una **cartella nuova**; non sovrapporre file delle versioni precedenti.
2. Chiudere Ristretto, quindi aprire PowerShell nella cartella `Ristretto` che contiene `Ristretto.sln`.
3. Eseguire dalla radice del progetto il comando seguente. Controllare che compili senza il warning `CS0618` in `App.axaml.cs` e che i test terminino con esito positivo.

   ```powershell
   .\scripts\validate.cmd
   ```
4. Aprire il Blocco note con un **documento di prova**, richiamare il picker, selezionare uno snippet di test e premere **Invio**. Verificare che il testo venga inserito come in Alpha 20.18.
5. Per una prova Unicode riproducibile usare la procedura completa di [Alpha 20.18](ALPHA20_18_NOTEPAD_DIAGNOSTICS.md), usando una nuova sessione sintetica.

Il codice di produzione non tenta alcuna espansione automatica in editor esterni. Non utilizzare documenti personali per i test.

## Obiettivo e prossimi lavori

**Ora:** rimuovere il warning, conservare l'inserimento manuale validato e aggiornare la matrice R2 con il primo PASS Notepad.

**Obiettivo finale:** digitare un trigger Unicode e ottenere lo snippet automaticamente in editor compatibili, mantenendo il picker e la hotkey come alternativa.

**Da fare:** build/test Windows 20.18.1, prove di regressione e negative R2, progettazione del primo riconoscimento globale sicuro su Windows (R5–R6). Adapter VS Code in pausa come integrazione facoltativa, non prerequisito dell'espansione generalista.
