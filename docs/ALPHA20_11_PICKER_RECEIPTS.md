# Alpha 20.11 — Attestazione locale del risultato nell'Edit sintetico

**Versione:** `3.9.0-alpha.20.11` · **Data:** 14 novembre 2026 · **Roadmap:** R2.04 (laboratorio; prove Windows aperte).

## Perché questo incremento

Il laboratorio Alpha 20.9 chiedeva al secondo processo di inviare al controller l'intero testo del controllo, codificato Base64, per verificarlo. Si trattava di una fixture sintetica, ma il protocollo trasferiva comunque contenuto di testo fra processi e non associava esplicitamente la risposta a una sessione. Il vero `SendInput` continua a essere un'operazione distinta dall'osservazione del risultato.

## Implementazione

1. `tools/Ristretto.WindowsOutputLab/PickerOwnedEditReceipt.cs`: protocollo isolato per la prova manuale, con token per esecuzione, formato `PICKER_READY|token|window|edit` e `PICKER_RESULT|token|MATCH/MISMATCH/UNAVAILABLE`. Token casuale di 32 cifre esadecimali; **correlazione, non autenticazione**.
2. Il processo figlio controlla che l'`Edit` posseduto esista, legge localmente il valore e ne confronta le unità UTF-16 con la fixture inclusa nel laboratorio, **senza normalizzare NFC/NFD o LF/CRLF**. Non trasmette il testo, anche in caso di differenza.
3. Il controller convalida token, PID e handle prima di richiedere la verifica. Se la risposta è assente, malformata, di un'altra sessione o `UNAVAILABLE`, produce `NOT_RUN`, non PASS; il testo diverso produce `FAIL` e **nessun retry**.
4. Il precedente laboratorio F2a (`--self-test`) conserva il proprio protocollo `READY`/`RESULT` separato: il nuovo protocollo è specifico di `--picker-insert-demo`.
5. `tests/TextExpander.Tests/PickerOwnedEditReceiptTests.cs` esercita la forma delle risposte, i token non corrispondenti, handle invalidi, Unicode con emoji/ZWJ/NFD, newline e disponibilità. Il protocollo puro è compilato come `Compile Include` esplicito nel progetto test, senza referenziare l'eseguibile Win32. L'audit sorgenti riconosce questa compilazione condivisa, ma continua a rilevare accessi `internal` illeciti da altri assembly.

## Come ripetere la prova Windows

Da una nuova estrazione nella directory `Ristretto`:

```bat
scripts\validate.cmd
scripts\validate-picker.cmd
scripts\validate-native.cmd
```

Per la prova **manuale** (non avviata dalla CI):

```bat
scripts\validate-picker-desktop.cmd
```

Creare lo snippet di fixture in un profilo SQLite **sintetico**; attivare il campo del processo figlio, aprire il picker con hotkey, scegliere **Inserisci**, poi tornare alla console e dare Invio. Il laboratorio riporta esclusivamente lo stato. Non registra il testo ricevuto. Consultare anche la [matrice R2](R2_PICKER_WINDOWS_ACCEPTANCE.md) per le condizioni di prova.

## Cosa non dimostra

- Il solo confronto positivo non dimostra l'origine della modifica: per essere una prova del pulsante Inserisci occorre seguire la procedura, senza incollare manualmente.
- Il processo figlio contiene volutamente la fixture; è un laboratorio posseduto, non una integrazione con Notepad, Word, browser o VS Code.
- La verifica avviene **dopo** il trasferimento di focus alla console e non rende atomico il controllo di focus con l'invio `Ctrl+V` effettuato da Ristretto. La race residua permane.
- Non è stato implementato alcun riconoscimento automatico globale della digitazione, né modificato il meccanismo di invio del picker.

## Stato delle verifiche

Eseguiti qui: **27/27 test Python**, **32/32 test Node** e controlli statici di progetti, XAML, cinque localizzazioni, DDL SQLite e link Markdown. La tabella di evidenza leggibile automaticamente è in [`reports/ALPHA20_11_VALIDATION_STATUS.json`](../reports/ALPHA20_11_VALIDATION_STATUS.json). **Build .NET, nuovi xUnit e prova manuale su desktop Windows non sono stati eseguiti qui**. I 588 xUnit storici dell'Alpha 20.5 e i 31 scenari nativi precedenti non sono risultati della 20.11.

## Prossimo lavoro

Qualificare realmente il picker nel proprio controllo su Windows e poi in Notepad con la [matrice R2](R2_PICKER_WINDOWS_ACCEPTANCE.md), distinguendo `SubmittedUnverified` da testo effettivamente osservato. Continuare la gestione negativa di focus e clipboard, senza retry dopo esito incerto.
