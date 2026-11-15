# Compilazione, verifiche e procedure — Ristretto Alpha 20.25

**Baseline 20.25:** CS0246 della 20.24 riprodotti e corretti; **820 xUnit** passati, inclusi 41 nuovi casi. Registrazioni e inserimento delle nuove hotkey verificati con API simulate; prova desktop aperta. [Esiti](../reports/ALPHA20_25_CHECKS.md).

## Hotkey per snippet: prova manuale separata

Dopo la build chiudi le istanze precedenti e avvia un profilo sintetico: `dotnet run --project src/TextExpander.Desktop -- --data-dir ".local-cache/hotkey-smoke"`. Crea uno snippet con testo Unicode di prova, trigger `;firma` e hotkey `Ctrl+Alt+F`. Salva e verifica il messaggio di hotkey attiva. In un documento esterno vuoto premi e rilascia la combinazione: attendi una sola copia del testo, senza picker. Disabilita il gruppo e controlla che non inserisca più nulla; riabilita e riprova. Prova una combinazione già assegnata e il cambio di focus durante il rilascio. Queste prove desktop non sono state eseguite automaticamente. [Uso e limiti](ALPHA20_25_SNIPPET_HOTKEYS.md).

## Prerequisiti

Installa **.NET SDK 10**. Apri PowerShell nella cartella del progetto contenente `Ristretto.sln`. **Python non è più necessario**. Node.js serve solo per i test dell'estensione VS Code.

## 1. Controllare le sorgenti senza avviare l'interfaccia

```powershell
.\scripts\validate-source.cmd
```

Questo script avvia il tool C# `Ristretto.DevTools` ed esegue audit dei namespace/riferimenti, test propri e controlli statici su XAML, traduzioni, documentazione e SQLite tramite `Microsoft.Data.Sqlite`. Anche questo passaggio richiede .NET 10 perché deve compilare il tool. Il risultato atteso è `GATE SOURCE: PASS`. La Alpha 20.20 ha superato compilazione e test sul PC dell'utente. Nella Alpha 20.22 correggiamo il test del cursore UTF-16 e aggiungiamo regressioni sul picker e sul dimensionamento: se compare un errore di compilazione, invia il **primo** errore completo; non considerare riuscita una validazione interrotta.

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

## 6. Osservazione esterna R5 (facoltativa, Windows)

Questa prova **legge temporaneamente in RAM fino a 4096 unità UTF-16 da un campo esterno**: utilizzala soltanto con un documento sintetico senza dati personali. Non registra caratteri o tasti, non legge password, non scrive, non usa la clipboard e non installa hook. Supporta esclusivamente alcuni controlli Win32 `Edit` classici; Notepad moderno può essere escluso.

1. Prepara un campo `Edit` classico di test in una seconda applicazione e lascialo vuoto.
2. In PowerShell, dalla cartella `Ristretto`, avvia il comando seguente:

   ```powershell
   .\scripts\observe-classic-edit-text.cmd
   ```

3. Durante i primi cinque secondi, attiva il campo e posiziona il cursore, **senza digitare**.
4. Durante i cinque secondi successivi, digita per esempio `;firma` oppure `caffè ☕` mantenendo lo stesso controllo attivo.
5. Leggi il risultato nel terminale. `CandidateInsertion` è una differenza plausibile, non prova la provenienza umana. `NOT_RUN` indica che la lettura non è riuscita o il campo non è qualificato. Nessuno dei risultati autorizza espansioni automatiche.

Per dettagli, consultare [Alpha 20.21](ALPHA20_21_EXTERNAL_EDIT_OBSERVATION.md).

## 7. Controllare le novità della Alpha 20.22

**Scopo:** verificare il bug xUnit corretto e le due modifiche dell'interfaccia, senza usare dati personali.

1. Apri PowerShell nella cartella `Ristretto` della nuova versione, con Ristretto chiuso.
2. Esegui ` .\scripts\validate-source.cmd ` per verificare gli invarianti del sorgente. **Atteso:** `GATE SOURCE: PASS`.
3. Esegui ` .\scripts\validate.cmd `: compila ed esegue anche i test modificati. **Atteso:** `GATE CORE: PASS` e nessuna segnalazione del test `InsertionInMiddleAtCaretIsOnlyCandidate`.
4. Avvia Ristretto, apri la libreria e seleziona uno snippet lungo. Trascina il bordo destro della finestra e poi quello inferiore: il campo **Testo da espandere** deve occupare lo spazio orizzontale e verticale disponibile, senza sovrapporsi ai pulsanti.
5. Apri un documento sintetico vuoto in Blocco note. Nel picker cerca due snippet, premi **Freccia Giù** una volta per scegliere il secondo risultato e poi **Invio**. **Atteso:** viene inserito il contenuto del secondo risultato, non quello del primo. Con freccia Su torna al precedente. Senza frecce Invio usa il risultato già selezionato automaticamente.
6. Ripeti il quinto passo cliccando sul pulsante **Inserisci**: il risultato selezionato deve essere identico. Non è necessario ripetere tutte le prove del profilo SQLite per questo intervento.

La compilazione e il rendering della 20.22 **non sono stati verificati qui su .NET/Windows**: la verifica finale spetta ai test sul tuo computer.

## 8. Osservare modifiche in tempo reale su un controllo Edit — Alpha 20.23 (facoltativo)

**Scopo:** verificare se WinEvent notifica l'inserimento in un controllo classico `Edit` esterno. **Attenzione:** questa prova legge temporaneamente in RAM un massimo di 4096 unità UTF-16. Non usare password o documenti personali.

1. Esegui prima ` .\scripts\validate.cmd `: il risultato atteso è `GATE CORE: PASS`. Questa validazione non apre l'osservatore.
2. Prepara un'applicazione con un **Edit Win32 classico** di prova; Notepad moderno può essere escluso.
3. Da PowerShell, nella cartella `Ristretto`, avvia:

   ```powershell
   .\scripts\observe-classic-edit-live.cmd 15
   ```

4. Entro cinque secondi porta il focus nel campo di prova senza digitare. Quando compare «Osservazione attiva», digita `;firma` o `caffè ☕`.
5. Controlla i contatori finali. `PASS` conferma almeno un inserimento **plausibile**, non una digitazione certa; `NOT_RUN` indica assenza di dati utilizzabili o editor non compatibile. In nessun caso vengono modificati documenti.

[Dettagli, limiti e obiettivo](ALPHA20_23_LIVE_EDIT_OBSERVER.md).

## 8. Anteprima dei trigger Win32, nuova in Alpha 20.24 (facoltativa)

Questa prova **non espande** gli snippet. Legge temporaneamente in RAM testo **sintetico** di un controllo Win32 `Edit` esterno, senza log del contenuto e senza accedere alla libreria SQLite personale. Non usarla con informazioni riservate.

1. Apri un campo Win32 `Edit` classico in un programma esterno di prova; lascialo vuoto.
2. Da PowerShell, nella cartella del progetto, esegui:

   ```powershell
   .\scripts\observe-classic-edit-preview.cmd 15
   ```

3. Nei primi cinque secondi attiva il campo e non digitare. Quando compare l'avviso di osservazione attiva, digita `;firma` seguito da uno spazio.
4. Leggi il numero di **potenziali trigger**. `PASS` vale soltanto per la simulazione; `NOT_RUN` indica che il controllo non ha fornito evidenze sufficienti.

La notifica può derivare anche da un incollamento: **nessun risultato autorizza una modifica**. Vedi [Alpha 20.24](ALPHA20_24_TRIGGER_PREVIEW.md).
