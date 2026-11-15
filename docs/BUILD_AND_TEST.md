# Compilazione e verifiche — Ristretto Alpha 20.19.2

Istruzioni operative **in italiano**. Lavorare nella radice `Ristretto` estratta dallo ZIP completo. Utilizzare una nuova cartella per ogni versione e chiudere l'applicazione prima delle prove che usano la hotkey globale.

## Prerequisiti

- **Python 3.10 o successivo** per audit, gate e qualificazione del documento sintetico.
- **.NET SDK 10** per compilazione, xUnit, seeding SQLite sintetico e avvio della GUI.
- **Windows desktop interattivo** per Notepad e laboratori Win32; questi test non possono essere dichiarati superati su un sistema headless.
- **Node.js 22 e npm** solo per il gate CI/completo e l'adapter VS Code. Le dipendenze NuGet devono essere disponibili per il restore.

## 0. Correzione del controllo CLI su Windows (Alpha 20.19.3)

Nella Alpha 20.19.2 il gate `CORE` superava tutti i passaggi fino a `ui_review_build`, ma `startup_cli` terminava con `UnicodeDecodeError` e `AttributeError` nel processo Python. Il messaggio conteneva il byte `0x95`, interpretabile nella codepage Windows ma non in UTF-8. Il difetto riguarda **solo lo script di verifica**, non la libreria SQLite né il picker.

Nella nuova versione `check_startup_profile.py` acquisisce `stdout`/`stderr` in modalità binaria. I comandi con errore atteso (codice 2) devono avere `stderr` non vuoto e `stdout` vuoto, senza decodificare il messaggio localizzato. I comandi riusciti devono continuare a produrre **stdout UTF-8 valido**; percorsi Unicode e JSON sono verificati senza sostituzioni di caratteri.

Da **PowerShell** nella cartella `Ristretto`, per verificare prima soltanto lo script Python:

```powershell
python -m unittest discover -s scripts -p "test_check_startup_profile.py" -v
```

**Risultato atteso:** 9 test superati. Poi eseguire la verifica integrale:

```powershell
.\scripts\validate.cmd
```

**Risultato atteso:** `startup_cli=PASS` e `GATE CORE: PASS`; i passi `node_tests` ed `export_contract` continueranno a essere `NOT_RUN` in modalità `CORE` per scelta del gate, non sono errori. Non serve rifare il test Unicode manuale Notepad: il picker non è cambiato. Se fallisce, inviare il primo traceback di `startup_cli` e il riepilogo del gate. [Report](ALPHA20_19_3_STARTUP_CLI_ENCODING.md).

## 0. Correzione del controllo versione PowerShell (Alpha 20.19.2)

Nella versione precedente `validate.cmd` si interrompeva con «ERRORE VALIDAZIONE RISTRETTO: Versione MSBuild non valida», anche su un'estrazione nuova. Il problema era nella **regex di `scripts/verify-release.ps1`**, non nella versione reale del progetto e nemmeno in MSBuild: la regex ammetteva solo `alpha.20.19` e rifiutava `alpha.20.19.1`.

La Alpha 20.19.2 corregge la regola e mantiene la verifica di coerenza con `VERSION.txt`. Per controllare **soltanto** il gate preliminare, da PowerShell nella cartella che contiene `Ristretto.sln`, eseguire:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\verify-release.ps1
```

**Risultato atteso:** `SORGENTI RISTRETTO 3.9.0-alpha.20.19.3 VERIFICATI` e codice di uscita `0`. Poi eseguire normalmente ` .\scripts\validate.cmd `, che avvia la compilazione e i test. Non serve pulire `bin`/`obj`: il vecchio errore era emesso **prima** di MSBuild. Questo script PowerShell non è stato eseguito nell'ambiente di preparazione Linux; il suo esito resta da confermare sul PC Windows.

[Rapporto dettagliato](ALPHA20_19_2_VERSION_GATE_FIX.md).

## 0. Verificare la correzione della suite Python su Windows (Alpha 20.19.1)

Nella Alpha 20.19 il gate `CORE` si fermava su `test_exact_multiline_unicode_matches_when_saved` con `PASS != FAIL`. Il motivo era il raddoppio di `\r` durante la preparazione dei fine riga del test, non un problema dimostrato nel picker.

Dalla radice `Ristretto`, aprire **PowerShell** e lanciare:

```powershell
python -m unittest discover -s scripts -p "test_*.py" -v
```

**Risultato atteso:** tutti i test Python superati, compresi `test_exact_multiline_unicode_matches_when_saved` e i tre nuovi test `test_windows_crlf_source_fixture_matches_saved_crlf_document`, `test_windows_crlf_source_fixture_matches_saved_lf_document` e `test_stray_carriage_return_must_remain_failure`. Non deve esserci una sequenza `CRCRLF` prodotta dal test. Se questo comando termina con errore, interrompere il gate e condividere il primo traceback, non modificare la fixture reale.

Poi eseguire ` .\scripts\validate.cmd ` per compilare e verificare .NET. Non confondere il PASS della suite Python con la validazione della GUI o della sonda focus Win32. [Dettagli](ALPHA20_19_1_CRLF_REGRESSION_FIX.md).

## 0.b Verificare la correzione del warning CS0618

Nella Alpha 20.18.1 il metodo `IClipboard.GetTextAsync()` (obsoleto in Avalonia 11.3.22) è stato sostituito con `TryGetTextAsync()` in `App.axaml.cs`. Per verificare che il warning non compaia più eseguire `validate.cmd` dalla radice del nuovo archivio. Se la compilazione è riuscita, avviare una prova **manuale** del picker in Blocco note: richiama la hotkey, scegli uno snippet di prova, premi **Invio** o **Inserisci** e conferma che il testo venga ricevuto. La prova deve usare una libreria di test e un file sintetico.

Per evitare che l'API obsoleta venga reinserita, `audit_source.py` segnala ora le chiamate `.GetTextAsync(` nei sorgenti C#. Il controllo statico non sostituisce la compilazione con l'SDK .NET 10.

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

## 4. Esperimento Windows sui cambi di focus (facoltativo)

Questo comando serve a capire se Windows notifica correttamente i cambi di finestra e di campo attivo. **Non registra tasti o testo, non modifica appunti e non espande snippet.** Non è necessario eseguirlo per continuare lo sviluppo.

1. Chiudere eventuali applicazioni che contengono dati riservati. Aprire Blocco note con un file di prova vuoto.
2. Aprire PowerShell nella directory principale `Ristretto`, quella contenente `Ristretto.sln`.
3. Eseguire:

   ```powershell
   .\scripts\observe-focus.cmd 15
   ```

4. Per i 15 secondi indicati, passare da Blocco note a un'altra finestra e tornare al campo di testo; **non è necessario digitare**.
5. Alla fine leggere i soli conteggi: cambi finestra, cambi focus, barriere di riconoscimento. `PASS` indica che la sottoscrizione era attiva ed è stata arrestata, **non** che Ristretto possa riconoscere testo esterno. `NOT_RUN` indica che Windows o il desktop non hanno consentito la prova.
6. Per una durata differente, usare un numero da 3 a 30 al posto di 15. L'esperimento non è abilitato nell'applicazione Ristretto normale.

Per i limiti e le garanzie vedere il [rapporto Alpha 20.19](ALPHA20_19_FOCUS_BOUNDARY.md).

## 5. Pulizia degli artefatti di compilazione

Eseguire **prima l'anteprima**:

```powershell
.\scripts\clean-build.cmd -Preview
```

Se l'elenco contiene soltanto `bin` e `obj` dei progetti desiderati, eseguire:

```powershell
.\scripts\clean-build.cmd
```

Confermare con `ELIMINA` quando richiesto. Gli script non devono toccare SQLite, sorgenti, documentazione o `node_modules`. Dopo la pulizia la prima build sarà più lenta, perché rigenera `bin` e `obj`.

## 6. Interpretare le evidenze

- **PASS**: soltanto la verifica indicata è passata, su quel sistema/target/versione.
- **FAIL**: la prova è stata eseguita ma non ha soddisfatto l'atteso; leggere il motivo e conservare la ricevuta.
- **NOT_RUN**: non c'è evidenza di esecuzione o la verifica non è arrivata alla fase richiesta; **non equivale a FAIL**.

La baseline con compilazione e xUnit confermati dall'utente è **Alpha 20.15**. La prova di inserimento Unicode in Notepad dell'**Alpha 20.18 è PASS** sul documento salvato e l'utente conferma che il picker ha eseguito l'inserimento. La Alpha 20.18.1 corregge il warning CS0618 senza test Windows confermati; la Alpha 20.19 aggiunge un esperimento di focus, ma **compilazione .NET, xUnit e prova nativa di questa nuova versione sono ancora NOT_RUN**. Per i report storici vedere [indice](INDEX.md) e [changelog](CHANGELOG.md).
