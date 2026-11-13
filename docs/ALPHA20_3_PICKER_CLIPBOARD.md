# Alpha 20.3 — Copia e Inserisci senza sovrapposizioni

## Comportamento

Il picker esegue **una sola operazione Copia/Inserisci alla volta**. Durante l'operazione entrambi i pulsanti e Invio per Copia sono disabilitati e lo stato mostra «Operazione in corso…». Un comando ricevuto mentre l'altro è pendente viene ignorato, non accodato o ripetuto automaticamente.

La selezione viene fissata al clic: cambiando riga durante Copia, il testo già passato al provider resta quello scelto inizialmente. L'esito di quella copia non viene però mostrato come successo o errore della nuova selezione. Chiusura, riapertura e ricaricamento invalidano anche la pubblicazione dei messaggi delle copie precedenti.

Una scrittura degli appunti già iniziata **non viene revocata né ripristinata** alla chiusura. Se il provider è ancora in attesa, anche il picker riaperto aspetta il termine effettivo prima di consentire un'altra operazione sugli appunti. Il rilascio di questa attesa riabilita solo i comandi ancora autorizzati nella nuova sessione; non consuma il nuovo permesso Inserisci e non riabilita quello vecchio.

Inserisci resta monouso per apertura. Il coordinamento copre anche attivazione, preparazione appunti e invio nativo del comando. Un'eccezione inattesa del callback Inserisci viene trattata come **esito incerto**, senza retry. Terminata l'operazione, Copia può essere usata come fallback. Il solo ritorno del callback non prova che l'editor abbia incollato il testo.

Gli errori di Copia mostrano un messaggio localizzato; dettagli grezzi dell'eccezione, che potrebbero contenere percorsi o testi, non vengono visualizzati o registrati da questo percorso. Messaggi di attesa e fallimento sono disponibili in cinque lingue.

## Implementazione e limiti

`PickerViewModel` gestisce `IsClipboardBusy`, `CanCopy`, `CanInsert` e i callback `CopySelectedAsync`/`InsertSelectedAsync`. `PickerWindow` passa soltanto i callback di piattaforma e collega il pulsante Copia a `CanCopy`. La guardia separata del solo pulsante Inserisci è stata sostituita dalla coordinazione condivisa.

Il codice precedente in `PickerWindow` aggiornava il messaggio dopo l'attesa clipboard senza verificare la sessione e senza impedire una seconda copia o un inserimento concorrente. Il nuovo percorso ricontrolla generazione e selezione, anche prima di chiamare il provider, e conserva gli esiti della sessione più recente.

Il coordinamento riguarda **i due comandi del picker**, non l'accesso esclusivo agli appunti di sistema. Altre applicazioni possono modificarli; i controlli nativi e i [limiti di Inserisci](ALPHA20_3_PICKER_INSERT.md) restano validi. Una scrittura o un invio già effettuati non diventano transazionali o verificati grazie alla serializzazione.

## Verifiche eseguite

**550/550 xUnit** superati, inclusi **42 test del ViewModel**: 22 casi nuovi su contenuto Unicode/multilinea, doppio comando, Copia durante Inserisci e viceversa, esiti dopo chiusura o ricaricamento, cambio selezione, errore di caricamento più recente, permesso Inserisci della sessione nuova, eccezioni senza dettagli sensibili, notifiche ai binding e cambio lingua durante l'attesa.

I test usano il ViewModel di produzione con callback controllati da `TaskCompletionSource`, dati sintetici e risorse localizzate vere. Non inizializzano Avalonia, non leggono o scrivono gli appunti reali e non inviano input. Non viene dichiarata una riproduzione desktop del difetto precedente.

Solution e quattro strumenti compilati in Release con restore locale NuGet. Audit sorgenti/statici e sette test Python passati. Resta il warning preesistente `xUnit2000` nella suite capacità adapter. Nessun nuovo avvio di Ristretto, VS Code o laboratori grafici; test nativi e Node precedenti non ripetuti.

Build di solution/quattro strumenti e 550 xUnit ripetuti con esito positivo da una nuova estrazione dello ZIP priva di `bin`/`obj`, dopo la verifica del manifest SHA-256. Sorgenti, XAML, risorse e guard compilati/verificati corrispondono alla consegna.

[Riepilogo JSON](../reports/PICKER_CLIPBOARD_VALIDATION.json) · [log build/test](../reports/picker-clipboard-validation.log) · [caricamento picker precedente](ALPHA20_3_PICKER_LOADING.md).

## Prove desktop ancora aperte

Verificare pulsanti e Invio durante scritture clipboard lente, selezione diversa durante Copia, chiusura/riapertura mentre un comando è pendente, Copia come fallback dopo Inserisci rifiutato/incerto e cambio lingua. Restano aperte la ricezione effettiva del testo negli editor e le prove di concorrenza con altre applicazioni che usano gli appunti.
