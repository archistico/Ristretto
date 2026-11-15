# Alpha 20.19.5 — Correzione CS9174 nello strumento C#

## Cosa stiamo facendo

Consolidiamo `Ristretto.DevTools` dopo la migrazione degli strumenti da Python a C#/.NET 10. L'utente ha segnalato due errori del compilatore in `tools/Ristretto.DevTools/NotepadQualification.cs` alla riga 173 e alla riga 198 della Alpha 20.19.4.

## Causa verificata nei sorgenti

Entrambe le righe usavano `span.StartsWith([0xEF, 0xBB, 0xBF])`. Con gli overload disponibili, il compilatore tenta di interpretare la *collection expression* come un singolo `byte`, generando **CS9174**. Il controllo in questione rimuove il BOM UTF-8 opzionale prima di decodificare il file.

## Correzione

- Introdotto il metodo `WithoutUtf8Bom(ReadOnlySpan<byte>)`, che riconosce `EF BB BF` confrontando esplicitamente i byte e restituisce uno span sul contenuto originale, senza allocare un buffer.
- Sostituite entrambe le occorrenze del metodo ambiguo: lettura del documento salvato e lettura della fixture.
- Aggiunti tre test eseguibili dal gate `source`: file con BOM, BOM incompleto e fixture UTF-8 con BOM caricata e riaperta.
- Aggiunta una verifica statica C# nel `SourceAudit` contro la sintassi problematica; il test del formato versione ora utilizza `VERSION.txt` corrente.

Il comportamento del picker, il servizio Inserisci, la hotkey, la GUI e la persistenza SQLite non sono stati modificati.

## Verifiche e limiti

| Controllo | Esito |
|---|---|
| Ispezione e correzione delle due occorrenze CS9174 | Applicata nei sorgenti |
| Controllo di coerenza testuale, XML dei progetti e manifest SHA-256 | Eseguito in preparazione |
| Compilazione C# del nuovo tool | **NOT_RUN**: SDK .NET non disponibile nell'ambiente di preparazione |
| Test C# del tool, xUnit, gate SOURCE/CORE su Windows | **NOT_RUN** |
| Prova desktop/picker Notepad della nuova versione | **NOT_RUN**: nessuna modifica al codice picker |

## Come verificare su Windows

1. Estrai lo ZIP in una cartella nuova e apri **PowerShell** nella cartella `Ristretto`, dove si trova `Ristretto.sln`.
2. Esegui il controllo rapido dello strumento:

   ```powershell
   .\scripts\validate-source.cmd
   ```

   **Risultato atteso:** compilazione di `Ristretto.DevTools` senza CS9174, tre nuove regressioni sul BOM superate e `GATE SOURCE: PASS`.
3. Poi esegui tutti i controlli ordinari:

   ```powershell
   .\scripts\validate.cmd
   ```

   **Risultato atteso:** `GATE CORE: PASS`, senza gli errori CS9174. Se emergono altri errori, invia il primo messaggio completo del compilatore.

## Obiettivo e prossimi lavori

**Obiettivo finale:** text expander multipiattaforma con espansione automatica Unicode negli editor compatibili.

**Priorità immediata:** stabilizzare la pipeline di validazione tutta C# e confermare build e test della Alpha 20.19.5. Solo successivamente riprendere R5–R6 (input e sostituzione automatica Windows), mantenendo il picker manuale come alternativa.
