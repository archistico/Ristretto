# Compilazione e verifiche — Ristretto Alpha 20.18

Istruzioni operative **in italiano**. Lavorare nella radice `Ristretto` estratta dallo ZIP completo. Utilizzare una nuova cartella per ogni versione e chiudere l'applicazione prima delle prove che usano la hotkey globale.

## Prerequisiti

- **Python 3.10 o successivo** per audit, gate e qualificazione del documento sintetico.
- **.NET SDK 10** per compilazione, xUnit, seeding SQLite sintetico e avvio della GUI.
- **Windows desktop interattivo** per Notepad e laboratori Win32; questi test non possono essere dichiarati superati su un sistema headless.
- **Node.js 22 e npm** solo per il gate CI/completo e l'adapter VS Code. Le dipendenze NuGet devono essere disponibili per il restore.

## 1. Compilare e verificare il codice

Aprire **PowerShell** nella cartella `Ristretto` (quella con `Ristretto.sln`). Eseguire:

```powershell
.\scripts\validate.cmd
```

Lo script esegue il gate `core`: audit dei sorgenti, regressioni Python, controlli statici, restore/compilazione .NET e xUnit, build dei quattro strumenti ausiliari e test sintetici CLI. **Esito atteso:** comando terminato senza errori. I test native/editor grafici non sono inclusi e non si possono dedurre da un risultato verde.

Per i soli test del picker:

```powershell
.\scripts\validate-picker.cmd
```

Per i controlli Python/strutturali **senza compilare**:

```powershell
.\scripts\validate-source.cmd
```

Per il gate CI con Node ed export JSON .NET→Node (non interattivo):

```powershell
.\scripts\validate-gate.cmd ci
```

## 2. Qualificare il picker in Notepad

Questa verifica usa **soltanto un documento e un profilo SQLite sintetici**; non legge appunti o documenti personali e non esegue automaticamente input. È importante **non saltare il salvataggio**. Procedura dettagliata, codici di errore e significato di `PASS/FAIL/NOT_RUN`: [Alpha 20.18](ALPHA20_18_NOTEPAD_DIAGNOSTICS.md).

```powershell
.\scripts\validate-picker-notepad.cmd prepare --open
.\scripts\seed-picker-notepad.cmd TOKEN
.\scripts\validate-picker-notepad.cmd status --session TOKEN
.\scripts\validate-picker-notepad.cmd launch --session TOKEN
```

Dopo aver premuto **Invio/Inserisci** nel picker e **Ctrl+S** nel file sintetico di Notepad, aprire un altro PowerShell se il comando `launch` tiene occupato il primo:

```powershell
.\scripts\validate-picker-notepad.cmd verify --session TOKEN
```

Sostituire sempre `TOKEN` con l'identificativo visualizzato da `prepare`. `NOT_RUN` significa che il documento non è ancora stato modificato e salvato. `FAIL` significa che il file salvato non corrisponde, con un motivo diagnostico più preciso; per ripetere la prova creare una **nuova sessione**.

## 3. Prove native e altri adapter

Su Windows interattivo:

```powershell
.\scripts\validate-native.cmd
.\scripts\validate-picker-desktop.cmd
```

Il secondo comando apre un laboratorio `Edit` controllato e richiede operazioni manuali. Non inserisce testo automaticamente in Notepad. Il percorso VS Code è separato: `scripts\validate-vscode-host.cmd` richiede un `Code.exe` funzionante; [dettagli e limiti](ALPHA20_3_VSCODE_HOST_TESTS.md). Un PASS dei test Node non prova l'host reale.

## 4. Pulizia degli artefatti di compilazione

Eseguire **prima l'anteprima**:

```powershell
.\scripts\clean-build.cmd -Preview
```

Se l'elenco contiene soltanto `bin` e `obj` dei progetti desiderati, eseguire:

```powershell
.\scripts\clean-build.cmd
```

Confermare con `ELIMINA` quando richiesto. Gli script non devono toccare SQLite, sorgenti, documentazione o `node_modules`. Dopo la pulizia la prima build sarà più lenta, perché rigenera `bin` e `obj`.

## 5. Interpretare le evidenze

- **PASS**: soltanto la verifica indicata è passata, su quel sistema/target/versione.
- **FAIL**: la prova è stata eseguita ma non ha soddisfatto l'atteso; leggere il motivo e conservare la ricevuta.
- **NOT_RUN**: non c'è evidenza di esecuzione o la verifica non è arrivata alla fase richiesta; **non equivale a FAIL**.

La baseline Windows confermata dall'utente è **Alpha 20.15 (build, test e gesto Invio in Notepad)**. Le verifiche .NET e desktop della presente Alpha 20.18 restano aperte. Per i report storici vedere [indice](INDEX.md) e [changelog](CHANGELOG.md).
