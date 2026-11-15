# Compilazione, verifiche e procedure — Ristretto Alpha 20.20

## Prerequisiti

Installa **.NET SDK 10**. Apri PowerShell nella cartella del progetto contenente `Ristretto.sln`. **Python non è più necessario**. Node.js serve solo per i test dell'estensione VS Code.

## 1. Controllare le sorgenti senza avviare l'interfaccia

```powershell
.\scripts\validate-source.cmd
```

Questo script avvia il tool C# `Ristretto.DevTools` ed esegue audit dei namespace/riferimenti, test propri e controlli statici su XAML, traduzioni, documentazione e SQLite tramite `Microsoft.Data.Sqlite`. Anche questo passaggio richiede .NET 10 perché deve compilare il tool. Il risultato atteso è `GATE SOURCE: PASS`. La Alpha 20.19.5 ha superato compilazione e test sul PC dell'utente. Nella Alpha 20.20 aggiungiamo i test del nuovo bridge Unicode: se compare un errore di compilazione, invia il **primo** errore completo; non considerare riuscita una validazione interrotta.

## 2. Compilare ed eseguire tutti i test ordinari

```powershell
.\scripts\validate.cmd
```

Il gate `CORE` comprende sorgenti, controlli statici, `dotnet restore`, compilazione della solution, xUnit, build dei quattro laboratori, build degli strumenti C# e dieci prove CLI del punto d'ingresso senza GUI. Il risultato atteso è `GATE CORE: PASS`. Le prove desktop native e VS Code non eseguite rimangono `NOT_RUN`, non sono considerate superate.

Controlla inoltre che **non compaia il warning xUnit2000** in `WindowsExternalAdapterCapabilityTests.cs`, corretto con gli argomenti nell'ordine `expected, actual`.

Per il solo gruppo di test del picker:

```powershell
.\scripts\validate-picker.cmd
```

## 3. Eseguire la prova Notepad sintetica (facoltativa)

Questa procedura non apre documenti personali. I file vengono creati in `.local-cache/notepad-qualification` e non nella tua libreria di snippet.

1. **Prepara il documento di prova**:

   ```powershell
   .\scripts\validate-picker-notepad.cmd prepare --open
   ```

   Annota il token di sessione generato e verifica che Notepad mostri il file sintetico.

2. **Crea il profilo SQLite isolato**:

   ```powershell
   .\scripts\seed-picker-notepad.cmd TOKEN
   ```

   Sostituisci `TOKEN` con il valore della sessione: lo script non usa il database personale.

3. **Chiudi le altre istanze di Ristretto e apri quella di test**:

   ```powershell
   .\scripts\validate-picker-notepad.cmd launch --session TOKEN
   ```

   Ristretto viene avviato con `--data-dir` riferito soltanto al profilo sintetico.

4. **Inserisci manualmente lo snippet**. In Notepad porta il cursore alla fine con `Ctrl+End`, richiama il picker con la hotkey, seleziona `;ristretto-test`, premi **Invio** o **Inserisci**, quindi salva con `Ctrl+S`.

5. **Controlla l'esito salvato**:

   ```powershell
   .\scripts\validate-picker-notepad.cmd status --session TOKEN
   .\scripts\validate-picker-notepad.cmd verify --session TOKEN
   ```

   `PASS` indica la corrispondenza dei byte UTF-8 salvati alla fixture Unicode. `NOT_RUN` significa che il file non è stato salvato con l'inserimento. `FAIL` indica un contenuto differente. Non implica una prova di provenienza dei tasti né di espansione automatica.

## 4. Eseguire il gate CI e produrre un archivio

```powershell
.\scripts\validate-gate.cmd ci
```

Il gate CI include anche Node per la verifica facoltativa del contratto dell'estensione VS Code, ma non esegue prove native interattive. Per creare un archivio completo con manifest SHA-256:

```powershell
dotnet run -c Release --project tools\Ristretto.DevTools\Ristretto.DevTools.csproj -- package --output .local-cache\Ristretto_COMPLETO.zip
```

Gli script `clean-build.cmd` e `clean-build.ps1` restano disponibili per pulire `bin` e `obj`; richiedono un'azione esplicita.

## 5. In caso di errore

Condividi il primo `FAIL` e la relativa eccezione. I report vengono salvati in `.local-cache/gates/` senza testo degli snippet. Non usare `NOT_RUN` come prova di funzionamento. Un errore di compilazione dello strumento C# non dimostra un errore dell'applicazione: i due perimetri vanno distinti.
