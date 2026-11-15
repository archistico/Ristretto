# Alpha 20.9 — Inserisci: preflight clipboard e prova EDIT posseduto

**Versione:** `3.9.0-alpha.20.9` · **Data:** 14 novembre 2026 · **Roadmap:** R2.03 (parziale), R2.04 (strumento preparato, prova aperta).

## Correzione nel percorso reale

In `App.InsertFromPickerAsync`, prima della scrittura viene letta la sequenza di sistema degli appunti. `SetTextAsync` deve terminare e una nuova sequenza non nulla deve essere osservata. Se non è cambiata, la preparazione fallisce e **non** viene chiamato `SendInput`: si evita di incollare inconsapevolmente dati precedenti dagli appunti.

Il controllo `WindowsPickerPasteService.Paste` conserva le verifiche già presenti su destinatario, finestra, controllo focalizzato, modificatori e sequenza immediatamente prima di `Ctrl+V`. La sequenza diversa **non dimostra** che gli appunti contengano il testo corretto; un'applicazione concorrente può cambiarli. Un invio accettato resta `SubmittedUnverified` fino a una prova nel destinatario.

**Limiti espliciti:** il picker modifica gli appunti dell'utente e lascia lo snippet copiato. Non ripristina appunti precedenti e non ripete automaticamente tentativi con risultato incerto. Non è espansione automatica alla digitazione.

## Nuovo comando: laboratorio interattivo con campo realmente separato

```bat
scripts\validate-picker-desktop.cmd
```

L'esperimento non parte dalla CI, `validate.cmd` o `validate-native.cmd`. Richiede un desktop Windows interattivo, Ristretto aperto con un **profilo sintetico**, e un'azione manuale dell'utente. Il laboratorio avvia un secondo processo che possiede un controllo Win32 `Edit` **multilinea**. Il controller verifica che il PID della finestra sia quello del figlio. Non imposta mai gli appunti e non invia tasti al posto dell'utente.

1. In un profilo sintetico di Ristretto creare uno snippet con corpo ESATTAMENTE uguale alla fixture che il laboratorio stampa all'avvio, disponibile anche nel file `data/PICKER_INSERT_SYNTHETIC_FIXTURE.txt` (due righe, accenti, emoji, ZWJ, NFD e alfabeti non latini). È importante non includere spazi finali o righe extra.
2. Avviare `scripts\validate-picker-desktop.cmd`; cliccare nel campo di prova e richiamare il picker con la hotkey configurata.
3. Selezionare lo snippet e usare **Inserisci** (non Copia, né un incolla manuale). Non digitare direttamente nel controllo.
4. Tornare alla console e premere Invio: solo a questo punto il laboratorio chiede al proprio processo figlio il testo ricevuto. Il confronto avviene sui **byte UTF-16** (incluse le interruzioni di riga), non sul numero di eventi `SendInput`. Il contenuto letto non compare nei log.

Il confronto prova solo **il contenuto nel campo sintetico**: non può distinguere da solo il pulsante Inserisci da un incolla manuale effettuato dall'utente. La procedura richiede di seguire esplicitamente il passaggio 3.

Esiti distinti: `PASS` (contenuto identico nel controllo), `FAIL` (diverso), `NOT_RUN` (ambiente non interattivo, timeout, destinatario non verificato o risposta assente). Il laboratorio termina il proprio processo figlio e non tocca i documenti di terze parti. Non attribuire un `PASS` della prova isolata a Notepad/Word/browser: questi richiedono la matrice R2.

### Regressioni unit test

`WindowsPickerPasteTests` include casi per clipboard invariata, zero/non verificabile, cambio valido e wrap-around del contatore, più un controllo di assenza totale di invio quando la sequenza non avanza. Sono test C# aggiunti ma **non eseguiti** nell'ambiente di preparazione senza .NET 10.

## Evidenze disponibili e prossimi controlli

- Test Python e Node dell'adapter: eseguibili nel sistema di preparazione, registrati nel riepilogo di consegna.
- Test .NET della 20.9 e prova interattiva `validate-picker-desktop.cmd`: **NOT_RUN** fino alla prima esecuzione Windows.
- Baseline 588 xUnit: report storico Alpha 20.5, non un risultato della 20.9.

**Successivamente:** test R2-H/R2-I su Notepad, cambi focus e privilegi, clipboard modificata da un altro processo, poi integrazione nel gate di accettazione manuale. L'espansione automatica in altre applicazioni resta un progetto separato.
