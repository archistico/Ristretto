# Alpha 20.3 — Inserisci dal picker (Windows)

**Aggiornamento Alpha 20.4:** [Invio ora inserisce il primo risultato filtrato](ALPHA20_4_ENTER_INSERT.md) e nasconde il picker dopo l'invio. Le indicazioni sulla precedente scorciatoia Copia sotto descrivono l'introduzione della funzione in Alpha 20.3.

## Perché l'espansione non partiva nell'editor

La digitazione di un trigger in un editor esterno **non avvia attualmente un'espansione automatica**. Il motore globale resta da sviluppare. L'estensione VS Code dell'Alpha 19/20 offre un comando manuale sperimentale; non è un agente di sistema.

## Funzione aggiunta

Il picker mantiene **Copia testo** (scorciatoia Invio) e aggiunge **Inserisci**. Su Windows il nuovo comando incolla lo snippet nella finestra che aveva il focus prima dell'apertura del picker, se riconoscibile. Funziona anche per testo multilinea e Unicode (emoji, accenti, alfabeti non latini), utilizzando gli appunti e un solo comando `Ctrl+V` tramite Win32 `SendInput`.

**Prova:** aprire Notepad o VS Code, mettere il cursore nel testo, richiamare il picker con `Ctrl+Shift+Space`, selezionare lo snippet e premere Inserisci. Non aprire prima il picker dalla libreria: in quel caso la finestra destinataria è Ristretto e Inserisci rimane disabilitato. Se aperto dal menu tray potrebbe non essere disponibile perché il focus è sul menu/tray.

## Contratto di sicurezza

- Cattura soltanto HWND della finestra foreground, PID e HWND del controllo focalizzato, prima che il picker diventi attivo; non legge il testo del campo né registra tasti.
- Rifiuta processi interni, finestre invisibili, controllo senza focus, focus non discendente, controlli Win32 Edit password/readonly e shell/tray noti.
- Quando l'utente preme Inserisci, nasconde il picker, tenta di riattivare la finestra precedente e scrive lo snippet negli appunti. Ricontrolla finestra, processo, focus, modificatori e sequenza clipboard prima di inviare in **un solo batch** `Ctrl+V`.
- Non esegue retry dopo un invio parziale/incerto, non cancella alcun trigger già presente e non dichiara verificata la modifica del documento.
- Ogni apertura prepara una richiesta monouso; dopo il clic Inserisci resta disabilitato. Esc, chiusura, libreria o riapertura annullano le richieste precedenti ancora in attesa. Copia è disponibile dopo il termine dell'operazione pendente; per una nuova richiesta riaprire il picker dall'editor. [Ciclo di vita e test](ALPHA20_3_PICKER_SESSION.md), [coordinamento appunti](ALPHA20_3_PICKER_CLIPBOARD.md).
- L'inserimento **sostituisce gli appunti correnti con lo snippet e non li ripristina**. Campi password HTML/editor custom non sono rilevabili con certezza tramite il controllo Win32: non usare la funzione in campi sensibili.
- Windows può bloccare il focus (`SetForegroundWindow`) o l'input (`SendInput`, UIPI) verso app elevate: il picker mostra un messaggio in caso di rifiuto; Copia rimane disponibile. Sistemi non Windows: solo Copia.

## Matrice di test Windows (NON eseguita qui)

| Caso | Aspettativa |
| --- | --- |
| Notepad semplice + hotkey → Inserisci | Testo visibile nel punto del cursore; clipboard contiene testo |
| VS Code + hotkey → Inserisci | L'editor riceve l'incolla dove aveva il focus; documentare l'esito |
| Snippet multilinea con `café`, emoji, ideogrammi | Testo inserito intatto, senza alterare Unicode |
| Picker aperto dalla libreria | Pulsante disabilitato |
| Cambio destinazione o scheda durante il picker | Nessun inserimento se cambia HWND focus verificabile; per tab con stesso HWND il controllo non dà garanzia completa |
| Applicazione amministratore/elevata | Possibile rifiuto di Windows; mai retry automatico |
| Campo `Edit` password o sola lettura | Pulsante disabilitato |
| Doppio clic su Inserisci | Una sola richiesta; pulsante disabilitato |
| Chiusura/riapertura durante attesa appunti/focus | Richiesta precedente annullata; nessun suo invio successivo all'attesa e nessuna riapertura da vecchie continuazioni |
| Incollamento non accettato dall'editor | Esito non verificato; utente verifica manualmente |
| Scrivere `;firma` in un editor senza aprire picker | Nessuna espansione: funzionalità ancora non sviluppata |

## File

- `src/TextExpander.Infrastructure/Platform/Windows/WindowsPickerPasteService.cs`
- `src/TextExpander.Infrastructure/Platform/Windows/WindowsPickerInsertRequest.cs`
- `src/TextExpander.Desktop/App.axaml.cs`, `PickerWindow.axaml(.cs)`, `ViewModels/PickerViewModel.cs`
- `tests/TextExpander.Tests/WindowsPickerPasteTests.cs`
- Risorse localizzate: `src/TextExpander.Desktop/Localization/Strings*.resx`

## Verifiche

Test xUnit a livello di contratto con finti adapter Win32 e controllo del layout nativo, senza invocazioni Win32 nei test del picker. Corretta l'omissione di `hwndCaret` in `GUITHREADINFO`: la struttura ora rispetta le dimensioni richieste da Windows (72 byte x64, 48 byte x86; test eseguito su x64). Build .NET, suite xUnit e laboratori Windows superati; la prova manuale del pulsante negli editor resta da eseguire. Il gate nativo verifica i laboratori e non sostituisce questa prova. [Evidenze e limiti](ALPHA20_3_VALIDATION.md).
