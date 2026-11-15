# Alpha 20.10 — Inserisci: integrità clipboard e identità del controllo

**Versione:** `3.9.0-alpha.20.10` · **Data:** 14 novembre 2026 · **Roadmap:** R2.03 e R2.04 (implementazione, qualificazione desktop aperta).

## Problemi affrontati

1. **Il numero di sequenza degli appunti non prova il contenuto.** Nell'Alpha 20.9 la sequenza poteva aumentare per una modifica prodotta da un altro programma: Ristretto avrebbe potuto inviare Ctrl+V con un testo diverso da quello selezionato.
2. **La validità della finestra principale non basta.** Un HWND figlio può chiudersi o diventare non più disponibile; il controllo focalizzato deve risultare ancora esistente e appartenere al PID catturato.

## Intervento nel percorso reale

- `App.InsertFromPickerAsync`: dopo `SetTextAsync(content)` legge `GetTextAsync()` e ricontrolla la sequenza degli appunti. La funzione `PickerClipboardReadbackVerifier.VerifiedSequence()` accetta solo la sequenza cambiata e rimasta stabile **e** il testo corrispondente allo snippet richiesto. È tollerata esclusivamente la trasformazione di fine riga `CRLF`/`LF` del clipboard di Windows, non la normalizzazione NFC/NFD né differenze di spazi, parole o emoji.
- L'eventuale mismatch, testo nullo o cambio di sequenza durante la rilettura produce sequenza verificata `0`: la richiesta monouso viene rifiutata **prima** del comando incolla. Non si registra il testo letto.
- `WindowsPickerPasteService` verifica che sia la finestra sia il controllo figlio siano disponibili e appartengano al PID catturato, durante l'acquisizione e immediatamente prima della chiamata nativa. Un controllo distrutto o passato a un altro PID blocca l'operazione.
- Il comportamento esistente rimane: focus e appunti vengono ricontrollati immediatamente prima dell'invio, non si ripetono richieste con esito incerto; la GUI, la hotkey e SQLite sono invariati.

**Limite tecnico:** una verifica positiva di appunti, PID e focus non è una transazione atomica con `SendInput`. Se un'altra applicazione cambia il focus o gli appunti nel piccolissimo intervallo fra preflight e inserimento, l'esito può essere incerto. `SubmittedUnverified` non significa inserimento confermato nell'editor. Gli appunti vengono modificati volontariamente e non ripristinati automaticamente, anche se l'operazione viene rifiutata dopo la scrittura. I controlli custom/browser e i campi protetti non sono universalmente identificabili tramite classi Win32; evitare dati sensibili nei campi non qualificati.

## Verifiche e regressioni

- Nuovi `PickerClipboardReadbackTests.cs`: sequenza invariata o modificata, lettura concorrente, testo diverso, `null`, conversione CRLF, emoji/ZWJ, NFD vs NFC, alfabeti non latini, prefissi e Unicode completo.
- Nuovi casi `WindowsPickerPasteTests.cs`: controllo figlio non disponibile, PID del figlio differente, controllo chiuso durante la preparazione e appunti sostituiti nell'attesa. Test nativi simulati tramite un'interfaccia fake: non inviano tasti reali.
- `scripts\validate-picker.cmd` include la nuova suite di regressione.
- Eseguiti in questa preparazione: **26/26 Python** e **32/32 Node**, controllo statico progetti/XAML/SQLite/localizzazioni/link. **Compilazione .NET 10, nuovi xUnit e laboratorio interattivo Windows NON ESEGUITI** in questo ambiente.
- I **588 xUnit storici** sono della Alpha 20.5: non sono un risultato della Alpha 20.10.

## Qualificazione desktop ancora richiesta

Estrarre lo ZIP completo in una cartella **nuova**, mai sopra dati reali. In sessione Windows eseguire `scripts\validate.cmd`, `scripts\validate-picker.cmd` e, per la prova interattiva col profilo sintetico, `scripts\validate-picker-desktop.cmd`.

Aggiunti alla [matrice R2](R2_PICKER_WINDOWS_ACCEPTANCE.md) i casi di rifiuto con clipboard diversa da quella attesa, child-window terminata o con PID diverso e cambio del destinatario mentre la scrittura è sospesa. **Questi casi non risultano passati sul desktop**.

## Passaggio successivo

R2.04/R2.05: osservare inserimento effettivo nel controllo figlio e qualificare separatamente Notepad. Studiare come limitare i rischi della finestra di race residua; non trasformare una conferma `SendInput` in successo verificato né attivare l'espansione automatica globale.
