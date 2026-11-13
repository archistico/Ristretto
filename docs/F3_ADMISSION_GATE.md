# Alpha 8 — Gate di ammissione dell'espansione (F3, primo incremento)

**Stato del codice:** implementato nei sorgenti, test .NET e runtime Windows della alpha.8 **non ancora rieseguiti**. La GUI Preview 9 resta invariata.

## Perché serve

La presenza di un trigger `Ready` non dà il permesso di cancellare o inserire testo. Prima dell'esecuzione occorre la provenienza verificata di testo Unicode **già committato**, un destinatario sicuro, consenso esplicito e l'assenza di stati di sospensione. L'invio di keycode o `SendInput` **non** dimostra che il carattere sia stato inserito o che il caret sia invariato.

## Codice effettivo

- `src/TextExpander.Application/Features/Output/ExpansionAdmission.cs`: `ExpansionAdmissionPolicy` ed esito con ragione precisa; `QualifiedReplacementDispatcher` blocca il backend prima di ogni effetto quando l'ammissione fallisce.
- `tools/Ristretto.WindowsOutputLab/OwnedEditRecognitionSession.cs`: integra il gate nel percorso osservazione → riconoscimento → preparazione → esecuzione per il **solo EDIT posseduto**. Non altera il prodotto desktop.
- `tools/Ristretto.WindowsOutputLab/OwnedEditTarget.cs`: nella demo interattiva è richiesto che la finestra del laboratorio sia foreground e il proprio EDIT sia davvero in focus. Prima della modifica nativa la condizione è ricontrollata. Nella demo i modificatori Shift/Ctrl/Alt/Win vengono rilevati con `GetAsyncKeyState` e bloccano il tentativo; non si traducono tasti in caratteri. Le fixture automatiche restano isolate nel proprio processo e thread UI, anche se non hanno focus globale.
- `tests/TextExpander.Tests/ExpansionAdmissionTests.cs`: prove di rifiuto, conservazione del testo, Unicode, stato protetto, revisioni, scope e single-use.

## Matrice delle capacità (implementate, non promesse)

| Contesto | Lettura committata | Sostituzione condizionale | Gate alpha.8 |
|---|---|---|---|
| EDIT Win32 posseduto dal laboratorio | Delta reali del proprio controllo, IME disassociato | Sì, nello stesso thread e con readback | **Possibile con consenso** |
| Applicazione esterna Win32 (`EDIT` non posseduto) | Non certificata | Non certificata | **Negata** |
| Word, browser, editor web, terminali | Non certificata | Non certificata | **Negata** |
| Campi password, read-only, composizione o stato ignoto | Negata | Negata | **Negata** |
| macOS, Linux X11, Wayland | Nessun adapter nativo | Nessun adapter nativo | **Negata** |

## Rifiuti e semantica

Il controllo richiede **tutte** le seguenti condizioni: piano presente; evidenza presente; opt-in; non in pausa e non escluso; laboratorio proprio; testo committato verificato; focus sicuro; stato composizione verificato e inattivo; nessun modificatore; snapshot presente e normale; capacità `VerifiedConditionalReplace`; identità, epoch, revisione, testo e caret coincidenti con il piano.

Il risultato del gate è soltanto un'autorizzazione preliminare. L'adapter nativo deve **sempre** confrontare nuovamente l'intera snapshot prima di un edit indivisibile. Il `ReplacementExecutionOutcome` conserva le quattro condizioni di esito: `BlockedNoChange`, `VerifiedComplete`, `SubmittedUnverified`, `PartialOrUnknown`. Dopo l'ingresso nel backend **non** si ritenta automaticamente.

L'evidenza è un DTO in memoria **non** un certificato crittografico né un controllo dei privilegi OS; la sua attendibilità deriva dall'implementazione dell'adapter cooperante. `ExpansionTargetScope` non può essere impostato a `OwnedCooperatingLaboratory` da un futuro adapter esterno senza una reale dimostrazione dell'identità. Nessun adapter esterno è collegato in questa alpha, e tutti gli scope esterni vengono negati comunque.

## Privacy

Le snapshot sono temporanee; né `ToString()` dell'evidenza né i log della demo contengono testo digitato, contenuto dello snippet o clipboard. I report opzionali registrano soltanto contatori e classificazione dell'esito. Non sono state introdotte persistenze SQLite o tabelle nuove.

## Criteri di completamento da verificare su Windows

1. `scripts\validate.cmd`: compilazione, xUnit e quattro tool senza errori.
2. `scripts\validate-native.cmd`: suite nativa in una sessione desktop Windows; il numero storico **21/21** è riferito ad alpha.7, non un risultato nuovo.
3. Demo con digitazione realmente umana, `--replacement-demo --report <file-nuovo.json>`: almeno un risultato `VerifiedComplete`; provare anche perdita focus e riattivazione. Un report con stato PASS attesta il risultato osservato **nel solo controllo proprio**, non la provenienza fisica dei tasti.
4. Solo in milestone successiva: adapter per una specifica applicazione esterna + classificazione del campo, provenienza committata, revisione/caret e ricevuta verificabile. Fino ad allora picker/copia manuale.

[Comandi](BUILD_AND_TEST.md) · [Roadmap](FUNCTIONAL_ROADMAP.md) · [Verifiche manuali](ALPHA8_MANUAL_TEST_MATRIX.md).
