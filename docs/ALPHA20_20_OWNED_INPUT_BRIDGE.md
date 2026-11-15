# Alpha 20.20 — Bridge di input Unicode verificato (R5)

## Obiettivo

Concentrare in un unico componente applicativo i tre passaggi già sperimentati nel controllo Win32 posseduto: **(1) delta di testo Unicode verificato; (2) provenienza associata alla stessa revisione; (3) attivazione del trigger**. Questo riduce la duplicazione nel laboratorio e rende possibile provare la logica senza richiamare Win32 in ogni test.

## Modifiche ai sorgenti

- `OwnedCommittedInputBridge` riceve uno snapshot iniziale e osserva transizioni consecutive. Nessuno dei suoi metodi legge tastiera, clipboard o documenti esterni.
- Se la modifica non è un inserimento contiguo verificato, la sessione di attivazione viene azzerata. Lo stesso vale per origine sconosciuta, incollamento, modifica programmatica, composizione IME, revisione saltata, focus o campo non qualificato.
- Una transizione senza modifica (`NoChange`) non conta come nuovo carattere.
- `Invalidate()` azzera riconoscimento e baseline: il primo snapshot successivo serve **solo** a stabilire la nuova baseline, senza interpretare retroattivamente il testo.
- `Rebase()` permette al controllo proprietario di ricominciare dopo una sostituzione; il testo prodotto dall'espansione non viene riciclato come input.
- L'esito `OwnedCommittedInputResult` non stampa contenuto del documento, trigger o snippet nella diagnostica.
- `OwnedEditRecognitionSession` del laboratorio usa il bridge per l'input; le verifiche di sostituzione e il contratto dell'editor posseduto restano invariati.

## Confini di sicurezza

Il bridge **non autorizza l'espansione su Notepad, Word, browser o altri editor esterni**. L'osservazione `Ready` indica soltanto che il matcher ha riconosciuto il trigger su un flusso verificato. Una sostituzione richiede ancora un adapter qualificato e un piano atomico validato al momento dell'esecuzione.

La correlazione di `WM_CHAR` con una revisione nell'editor posseduto non prova che il tasto sia stato premuto fisicamente da una persona. Questa limitazione resta esplicita.

## Test previsti

Sono aggiunti test xUnit su inserimento con delimitatore, Unicode combinato, `NoChange`, provenienza non affidabile, revisioni e destinatari cambiati, IME, focus, baseline dopo interruzione e fixture sintetiche solo nei test. È previsto anche un controllo per la privacy del `ToString`.

## Verifiche disponibili e aperte

- Base **Alpha 20.19.5**: compilazione e test **PASS** confermati dall'utente.
- Nuovo codice **Alpha 20.20**: integrato nei sorgenti e sottoposto a controllo strutturale, **build e xUnit non eseguiti qui** perché l'SDK .NET 10 non è installato.
- Laboratorio Win32: compilazione e test nativi **da eseguire su Windows**.

## Istruzioni dettagliate (Windows)

1. Chiudi Ristretto e gli editor di prova, quindi estrai l'archivio completo in una nuova cartella.
2. Apri PowerShell nella cartella `Ristretto` che contiene `Ristretto.sln`.
3. Esegui `scripts\validate-source.cmd` dalla cartella del progetto per verificare i sorgenti e le regressioni degli strumenti C#.
4. Esegui `scripts\validate.cmd` per compilare la solution ed eseguire xUnit. Il risultato previsto è `GATE CORE: PASS`. Verifica che il gruppo `OwnedCommittedInputBridgeTests` non presenti errori.
5. Quando hai una sessione desktop Windows interattiva, esegui `scripts\validate-native.cmd`. Il laboratorio usa soltanto i propri controlli di prova e non scrive in documenti personali.

```powershell
.\scripts\validate-source.cmd
.\scripts\validate.cmd
.\scripts\validate-native.cmd
```

Il gate nativo non costituisce prova di espansione automatica in editor esterni.
