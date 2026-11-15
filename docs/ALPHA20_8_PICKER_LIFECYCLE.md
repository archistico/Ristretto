# Alpha 20.8 — Ciclo di vita del picker e integrazione del percorso Inserisci

**Versione:** `3.9.0-alpha.20.8` · **Data:** 14/11/2026 · **Area roadmap:** R2.02 (parziale), R2.04 (test del contratto, non native E2E).

## Difetto riproducibile nel codice precedente

`App.ShowPicker()` in Alpha 20.7 cancellava sempre la richiesta e tentava una nuova cattura della finestra in primo piano. Premendo nuovamente la hotkey mentre il picker era aperto e attivo, la finestra in primo piano era **Ristretto**, non l'editor originario: la cattura veniva rifiutata e il pulsante **Inserisci** risultava disabilitato. Inoltre il secondo caricamento inutile poteva interferire con ricerca e stato della prima sessione.

## Modifica

- `PickerSessionController`: proprietario della singola richiesta Inserisci e del numero di generazione; fa `Cancel()` quando la sessione viene chiusa, sostituita o invalidata.
- Nuova invocazione **quando il picker è già visibile e attivo**: conserva richiesta, destinatario e generazione, porta la ricerca in primo piano, **non** ricarica la libreria né acquisisce Ristretto come target.
- Nuova invocazione dopo **cambio di applicazione**: invalida il vecchio target, tenta la cattura del nuovo; se non qualificato, lascia solo **Copia**. Il vecchio destinatario non viene riutilizzato.
- Una continuazione asincrona è autorizzata ad aggiornare lo stato della finestra solo se la generazione di apertura è ancora quella attiva, anche quando entrambe le richieste sono `null`.
- Eventuali errori di ricognizione vengono contenuti come **assenza di destinatario**, anziché far propagare un'eccezione dall'handler `async void`.
- Nessun cambiamento ad Avalonia XAML, alla GUI Preview 9, a SQLite, alla hotkey configurabile o alla sequenza nativa di invio. Nessun hook globale o espansione automatica.

## Regressioni aggiunte

`tests/TextExpander.Tests/PickerSessionControllerTests.cs` contiene **13 test C#**, che esercitano il vero coordinatore e la richiesta di incolla monouso con un adapter Win32 simulato:

- hotkey ripetuta su picker attivo, anche senza target;
- mantenimento del target originale e disponibilità del percorso Inserisci;
- nuovo editor o finestra principale come sorgente, vecchia richiesta annullata;
- cambi di focus/clipboard, cancellazione durante attesa, completamenti precedenti;
- assenza di target con generazione nuova e successiva riacquisizione;
- invio parziale senza retry, errori della cattura senza target residuo.

Il simulatore **non** apre finestre reali e non invia tasti. I casi con clipboard reale, focus Windows reale, Enter, DPI e testo realmente apparso nel destinatario restano nella [matrice R2 Windows](R2_PICKER_WINDOWS_ACCEPTANCE.md) come `NOT_RUN`.

## Gate disponibili

Windows, test mirati senza GUI (richiede .NET 10):

```bat
scripts\validate-picker.cmd
```

Controllo completo del codice:

```bat
scripts\validate.cmd
```

Prove native precedenti, solo su Windows desktop interattivo:

```bat
scripts\validate-native.cmd
```

## Evidenze della consegna

| Verifica | Stato |
| --- | --- |
| Audit/statici XML, XAML, SQLite, namespace e link | PASS in ambiente di preparazione |
| Python test audit/quality gate | 26/26 PASS |
| Node adapter VS Code | 32/32 PASS |
| Nuovi test C# e build Alpha 20.8 | **NOT_RUN**: SDK .NET 10 assente in questo ambiente |
| Desktop: hotkey, clipboard, Inserisci in Notepad | **NOT_RUN** |
| CI GitHub e host VS Code reali | **NOT_RUN** |

La baseline **588 xUnit** è quella registrata nella versione Alpha 20.5, non va attribuita all'Alpha 20.8. La prova di invio a `SendInput` non garantisce che un editor abbia incollato il testo: la qualificazione desktop necessita di verifica del contenuto nel destinatario.

## Prossimo lavoro

R2.03–R2.04: test del percorso clipboard reale e un editor `Edit` di laboratorio in processo figlio con confronto del contenuto. Solo dopo: accettazione manuale di Notepad/focus/hotkey ed eventuali fix. Non certificare il flusso Windows sulla base dei soli test simulati.
