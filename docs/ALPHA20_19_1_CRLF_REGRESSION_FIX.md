# Alpha 20.19.1 — Correzione test Unicode multilinea su Windows

## Sintomo confermato

Dopo l'avvio di `scripts\validate.cmd` / gate `CORE`, il test Python `test_exact_multiline_unicode_matches_when_saved` falliva con `AssertionError: 'PASS' != 'FAIL'`. Il gate si interrompeva con `python_regressions=FAIL`: tutti gli step seguenti, inclusi restore, build e xUnit, risultavano `NOT_RUN`, non `FAIL`.

## Causa riprodotta

Nel setup del test `Path.write_text()` poteva tradurre il fine riga `\n` in `\r\n` sotto Windows. Il test aggiungeva poi un'ulteriore conversione `replace('\n', '\r\n')` al testo della fixture: dove era già presente CRLF si otteneva CRCRLF (`\r\r\n`). Il confronto rigoroso del file salvato rifiutava questa sequenza; il difetto era **nel test**, non nella logica del qualificatore o del picker.

La condizione è stata riprodotta su Linux creando una fixture con byte CRLF: lo stesso codice produceva `FAIL` e, dopo la correzione, `PASS`.

## Correzione implementata

- `scripts/test_qualify_notepad.py`: creazione della fixture con `newline='\n'`, indipendentemente dal sistema operativo.
- Test multilinea: `normalize_newlines(BASE_TEXT + fixture(root))` prima della singola conversione di LF in CRLF.
- Tre regressioni: fixture di origine Windows CRLF con salvataggio CRLF, fixture Windows CRLF con salvataggio LF, rifiuto esplicito di CRCRLF alterato.
- `scripts/qualify_notepad.py` **non modificato**. Rimangono rigorosi i controlli sulle differenze di normalizzazione Unicode NFC/NFD, posizione e contenuto.

## Risultati, limiti ed evidenze

Risultati della consegna nel file [report JSON](../reports/ALPHA20_19_1_VALIDATION_STATUS.json). I test Python/Node e gli audit disponibili sono stati eseguiti in ambiente Linux; compilazione .NET e test Windows interattivi sono segnati `NOT_RUN`. La prova utente precedente di inserimento Unicode in Notepad tramite picker rimane valida come evidenza separata, ma non certifica la build di questa hotfix.

## Come verificare su Windows

1. Estrarre lo ZIP completo in **una cartella nuova** e aprire PowerShell nella cartella `Ristretto` con `Ristretto.sln`.
2. Per verificare prima i test Python, eseguire:

   ```powershell
   python -m unittest discover -s scripts -p "test_*.py" -v
   ```

   **Atteso:** tutti i test completati con `OK`, incluso `test_exact_multiline_unicode_matches_when_saved`.
3. Per verificare il gate completo di compilazione e xUnit:

   ```powershell
   .\scripts\validate.cmd
   ```

   **Atteso:** `python_regressions=PASS`, quindi restore, build e xUnit senza errori. Se compare un errore, condividere il primo traceback o messaggio del compilatore.
4. La prova nativa Windows del focus resta facoltativa e separata; non serve ripetere la prova del picker Notepad per controllare questo hotfix, perché i sorgenti applicativi sono invariati.

## Dove siamo, obiettivo, prossimi lavori

**Ora:** ripristinata l'affidabilità multipiattaforma della suite Python. **Obiettivo:** poter digitare un trigger Unicode in un editor Windows compatibile ottenendo automaticamente lo snippet, senza richiamare il picker. **Prossimo lavoro:** validare la barriera di focus R5 della 20.19 su Windows, qualificare una sorgente di testo Unicode effettivamente committato e solo successivamente implementare l'espansione R6. VS Code resta un'integrazione facoltativa.
