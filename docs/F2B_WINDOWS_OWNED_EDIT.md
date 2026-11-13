# F2b Windows — sostituzione in un vero EDIT controllato

**Alpha.7, 13 novembre 2026.** Il percorso riconoscimento → preparazione → sostituzione → verifica è ora implementato e testato in un controllo Win32 `EDIT` reale, creato dal laboratorio. È un target cooperante nello stesso processo/thread UI, non un adapter per editor esterni arbitrari.

## Risultato verificato

**21/21 scenari nativi passati**: ASCII, raw NFD con NBSP preservato, CJK, emoji ZWJ e arabo; payload Unicode/multiriga esatto; caret a indice 65.536; revisioni obsolete e ABA; selezione intervenuta e destinatario diverso; readonly/disabilitato/password; cancellazione; thread errato; piano consumato; ricevuta persa; errore dopo effetto; navigazione durante un prefisso e target distrutto.

Le fixture vengono inserite mediante messaggi indirizzati al solo controllo posseduto. Il riconoscitore riceve la differenza letta dal campo reale, non una trascrizione presunta dei tasti. Il risultato è confrontato come sequenza UTF-16 esatta insieme a identità, epoch, revisione e selezione.

Questo dimostra il percorso nativo nel target cooperante. **Non dimostra `SendInput`, digitazione umana, IME generale, compatibilità con applicazioni terze o osservazione globale.**

## Ripetere il test automatico

Da una nuova estrazione compilata con `scripts\validate.cmd`:

```powershell
dotnet run -c Release --no-build --no-restore --project tools/Ristretto.WindowsOutputLab -- --replacement-test
```

Per un rapporto strutturato, indicare un file nuovo in una cartella esistente:

```powershell
dotnet run -c Release --no-build --no-restore --project tools/Ristretto.WindowsOutputLab -- --replacement-test --report reports/nuova-prova-nativa.json
```

La finestra di prova viene mostrata senza attivarla e chiusa al termine. Nessun `SendInput`, clipboard o hook. I rapporti esistenti non vengono sovrascritti. Il report contiene solo nomi scenari, esiti, metadati ambiente e hash dell'eseguibile, mai testo del campo o payload.

## Integrazione alpha.8 (da verificare)

La demo passa ora per il [gate F3](F3_ADMISSION_GATE.md) e prima di modificare il proprio EDIT controlla il foreground/focus. Può emettere un rapporto JSON di esito privo di testo. Invariato il perimetro: nessun editor esterno.

## Demo interattiva disponibile

```powershell
dotnet run -c Release --no-build --no-restore --project tools/Ristretto.WindowsOutputLab -- --replacement-demo
```

Cliccare nel campo del laboratorio e digitare **`;firma` seguito da spazio**. La demo prova a sostituirlo con una firma sintetica multilingue. Il delimitatore resta presente. Chiudere la finestra per terminare. Questa modalità è implementata, ma la verifica con digitazione umana resta **NOT_RUN** in questa sessione.

Alpha.8: la demo può emettere `--report reports/file-nuovo.json` e termina con errore se nessuna espansione è stata verificata. Queste modifiche richiedono ancora verifica su Windows. La demo osserva solo il proprio campo. Richiede una singola modifica contigua e verificabile al caret precedente; cancellazioni, selezioni, navigazione, stato ignoto o revisioni saltate resettano il riconoscimento. Il polling prudente può rinunciare all'espansione se riceve più modifiche fra due letture. Non è ancora un observer generale né una funzione attiva in `Ristretto.exe`.

## Implementazione e confine della garanzia

- [OwnedEditTarget](../tools/Ristretto.WindowsOutputLab/OwnedEditTarget.cs) crea e possiede finestra e controllo, senza accettare HWND esterni. Ricontrolla processo/thread/parentela e invalida gli handle dopo distruzione.
- [OwnedEditRecognitionSession](../tools/Ristretto.WindowsOutputLab/OwnedEditRecognitionSession.cs) confronta snapshot native prima/dopo; inoltra solo inserimenti contigui effettivamente osservati; risolve il raw originale a confini grafemici, con NFC solo come chiave; riusa il Core e il planner esistenti.
- Il backend confronta la snapshot completa e modifica il campo sul suo thread UI senza pompare eventi fra i passaggi. Le procedure native sono quelle del controllo posseduto e della finestra cooperante del laboratorio; non sono ammessi callback applicativi arbitrari in quella sezione.
- Usa [EM_SETSEL](https://learn.microsoft.com/en-us/windows/win32/controls/em-setsel) e [EM_REPLACESEL](https://learn.microsoft.com/en-us/windows/win32/controls/em-replacesel). Quest'ultimo non fornisce un valore di successo: il laboratorio rilegge testo e caret. Gli indici sono offset nella snapshot di questo EDIT, non un numero di Backspace.
- Legge [EM_GETSEL](https://learn.microsoft.com/en-us/windows/win32/controls/em-getsel) tramite i due puntatori DWORD, evitando il limite dei 16 bit del valore di ritorno combinato.
- Le revisioni sono mantenute dalla finestra cooperante: un batch posseduto incrementa una volta; altre modifiche sono rilevate con [EN_CHANGE](https://learn.microsoft.com/en-us/windows/win32/controls/en-change). Il caso `WM_SETTEXT` multilinea, che non emette tale notifica, è coperto esplicitamente dal batch di fixture. Non si attribuisce a Windows una revisione documentale universale.
- [ImmDisableIME](https://learn.microsoft.com/en-us/windows/win32/api/imm/nf-imm-immdisableime) viene chiamato sul solo thread CLI prima della prima finestra; il contesto del controllo è disassociato e verificato assente. Non viene modificato l'IME della GUI o di altre applicazioni. Questo esclude l'IME dalla prova, non ne dimostra il supporto.
- I campi password, readonly, disabilitati o con contesto ignoto vengono rifiutati prima di leggere testo; il contatore delle letture è verificato nei test nativi. Un errore dopo la selezione è già trattato come possibile effetto, senza retry o rollback.

L'esecuzione sullo stesso thread si basa sul comportamento sincrono documentato di [SendMessageW](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendmessagew). Questa sezione cooperante non è una transazione universale tra processi: non implementare lo stesso contratto mediante preflight e invio separati verso un editor terzo.

## Evidenze e prossimi passi

- [Rapporto nativo finale](../reports/windows-alpha7-native.json), [log](../reports/windows-alpha7-native.log).
- [Validazione completa](../reports/windows-alpha7-validation.log), [xUnit](../reports/windows-alpha7-tests.trx), [riepilogo](../reports/WINDOWS_ALPHA7_VALIDATION.json).

Prossimo passo: provare la demo con input umano, quindi scegliere/qualificare un adapter per un'applicazione esterna e una sorgente affidabile di testo committato. L'IME, i campi protetti esterni, i cambi focus e i risultati parziali richiedono prove proprie.

La modalità F2a `--self-test` con `SendInput` resta separata e senza prova di consegna superata. La GUI Preview 9, SQLite e la libreria personale restano invariati.
