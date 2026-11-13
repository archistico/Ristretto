# Alpha 20.3 — ciclo di vita di Inserisci nel picker

## Comportamento

Ogni apertura del picker prepara una richiesta per il destinatario esterno appena catturato. Al clic su **Inserisci**, il pulsante viene disabilitato e la richiesta può essere eseguita **una sola volta**, anche se l'invio fallisce o ha un esito incerto. **Copia testo** è disponibile quando l'operazione pendente è terminata; i due comandi ora condividono il [coordinamento degli appunti](ALPHA20_3_PICKER_CLIPBOARD.md). Per una nuova richiesta occorre riaprire il picker dall'editor esterno.

Esc, chiusura del picker, apertura della libreria, riapertura del picker e uscita tramite il comando dell'app annullano la richiesta precedente. Se l'operazione è ancora in attesa degli appunti o del ripristino del focus, l'annullamento impedisce di avviare successivamente l'input. Una continuazione della vecchia richiesta non riapre il picker né modifica il messaggio di una nuova sessione.

Gli appunti già scritti non vengono ripristinati dall'annullamento. Non è possibile revocare un comando nativo già inviato: l'esito resta inviato/non verificato oppure incerto. Nessun retry automatico, nessuna rimozione del trigger.

## Implementazione

- `WindowsPickerInsertRequest`: richiesta monouso con controllo di annullamento prima dell'attivazione e dopo ciascuna attesa asincrona; non contiene testo dello snippet né log di contenuti.
- `WindowsPickerPasteService`: gestione delle eccezioni anche durante la verifica iniziale del destinatario. Prima di entrare nell'invio nativo, un errore restituisce `Rejected`; da quel punto un errore restituisce `PartialOrUnknown`.
- `App` e `PickerWindow`: annullamento delle richieste precedenti e verifica della sessione prima di applicare un risultato asincrono alla UI.
- `verify_static.py`: guard aggiornato per il nuovo collegamento tra picker e richiesta.

Il difetto delle eccezioni è stato riprodotto prima della correzione con due casi: `Win32Exception` e `InvalidOperationException` uscivano da `TryActivate` durante `ValidTarget`. Entrambi ora terminano senza input e senza eccezione propagata.

## Verifiche

**484/484 test xUnit** superati, inclusi **36 casi del picker** (18 nuovi). Coprono ordine attivazione/appunti/focus, doppio clic concorrente, riutilizzo dopo invio completo/parziale/bloccato, rifiuto dell'attivazione, annullamento prima del clic e durante entrambe le attese, modifica degli appunti, eccezioni prima/durante l'invio e richiesta nuova indipendente da quella annullata.

Solution e quattro strumenti compilati in Release, restore dalla cache NuGet locale. Audit sorgenti, controlli statici e sette test Python superati. Il solo warning preesistente `xUnit2000` riguarda l'ordine degli argomenti di un'asserzione nella suite capacità adapter.

Ripetuti build di solution/quattro tool e 484 xUnit da una nuova estrazione dello ZIP senza `bin`/`obj`, con verifica del manifest SHA-256: esito positivo. I sorgenti compilati corrispondono a quelli consegnati.

Gli adapter dei test del picker sono simulati: nessun `SendInput`, nessun appunto reale e nessuna finestra esterna modificata. **Non sono stati avviati VS Code, Ristretto o laboratori grafici per queste prove.** I 31 scenari nativi e i 32 test Node del consolidamento precedente restano evidenze precedenti; non sono stati rieseguiti per questo incremento.

[Riepilogo JSON](../reports/PICKER_INSERT_SESSION_VALIDATION.json) · [log build e test](../reports/picker-insert-session-validation.log) · [baseline precedente](ALPHA20_3_VALIDATION.md).

## Prove manuali ancora aperte

La [matrice del picker](ALPHA20_3_PICKER_INSERT.md) resta da eseguire sul desktop. Aggiungere: doppio clic su Inserisci, Esc/chiusura durante una richiesta, apertura della libreria o di una nuova sessione durante l'attesa, pulsante disabilitato dopo rifiuto/esito incerto. I test gestiti non dimostrano che Notepad o VS Code abbiano ricevuto il testo, né qualificano gli eventi reali del desktop.
