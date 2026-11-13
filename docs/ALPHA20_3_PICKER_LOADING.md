# Alpha 20.3 — caricamento del picker annullabile

## Problemi riprodotti

Tre test sul ViewModel di produzione sono falliti prima della correzione:

1. Una vecchia lettura completata dopo una nuova sostituiva i risultati aggiornati.
2. Un errore tardivo della vecchia lettura sovrascriveva il messaggio della sessione nuova.
3. Durante un nuovo caricamento rimanevano selezionabili gli snippet della libreria precedente.

## Comportamento attuale

All'avvio del caricamento il picker rimuove indice, risultati e selezione precedenti e mostra **Caricamento snippet…**, localizzato nelle cinque lingue disponibili. Copia e Inserisci restano disabilitati finché non esistono risultati validi della lettura corrente. Anche un evento di selezione in ritardo, riferito alla lista precedente, non abilita le azioni.

Ogni nuovo caricamento annulla il precedente e incrementa una generazione. Risultati ed errori vengono pubblicati solo se appartengono ancora alla generazione corrente. La costruzione dell'indice avviene tramite `Task.Run`; le proprietà e la collezione dei risultati vengono aggiornate tornando al contesto UI del chiamante. Il ViewModel va usato sul dispatcher UI, non è una collezione per scritture concorrenti arbitrarie.

Esc, chiusura, libreria, riapertura e uscita tramite il comando dell'app annullano anche il caricamento, attraverso il percorso di annullamento già usato da Inserisci. `WaitAsync` interrompe l'attesa anche quando il provider ignora il token; ciò non garantisce che l'I/O sottostante termini immediatamente. La compilazione dell'indice controlla il token fra gli snippet e dopo l'ordinamento, senza pubblicare indici parziali.

La query scritta durante il caricamento viene applicata al nuovo indice. Un normale ricaricamento mantiene la selezione per ID se lo snippet è ancora disponibile, usando il contenuto aggiornato. Un caricamento fallito lascia le azioni disabilitate, senza ripristinare la vecchia libreria. Un successivo caricamento può essere ritentato.

`ReloadAsync` restituisce `true` solo quando ha pubblicato con successo la snapshot corrente. `App.ShowPicker` usa l'esito per non sostituire un errore o una cancellazione con il messaggio «nessun destinatario». Il completamento del caricamento non riabilita un permesso Inserisci già consumato o revocato.

## Verifiche eseguite

**528/528 xUnit passati**, con **22 nuovi casi**: 20 sul ViewModel reale e due sull'annullamento della costruzione dell'indice. Le prove controllano letture e costruzioni trattenute da barriere deterministiche, completamenti fuori ordine, errori tardivi, query durante l'attesa, selezioni vecchie, retry, snapshot vuota, notifiche ai binding e traduzioni.

Il progetto test ora referenzia anche Desktop per usare il vero `PickerViewModel` e le sue risorse localizzate. Non vengono creati `Avalonia.Application`, finestre o backend grafici; store e dati sono sintetici. Le dipendenze fra i quattro assembly di produzione restano le stesse.

Solution e quattro strumenti compilati in Release con restore locale NuGet. Audit sorgenti e statici, più sette test Python, superati. Resta il warning preesistente `xUnit2000` nella suite capacità adapter.

Ripetuti build di solution/quattro strumenti e 528 xUnit da una nuova estrazione dello ZIP priva di `bin`/`obj`, dopo la verifica del manifest SHA-256: esito positivo. Sorgenti, progetto test e risorse compilati corrispondono alla consegna.

Il tool `Ristretto.UiReview` è stato adattato per elaborare le continuazioni del dispatcher mentre attende il nuovo caricamento asincrono, evitando il blocco con `GetResult` su un task pendente. Il tool è stato **compilato, non eseguito**; nessun render rigenerato.

Non sono stati avviati Ristretto, VS Code o laboratori grafici. Test nativi e Node precedenti non ripetuti. Non sono state misurate latenza o memoria della GUI, né esercitati appunti reali o digitazione IME.

[Riepilogo JSON](../reports/PICKER_LOADING_VALIDATION.json) · [log build e test](../reports/picker-loading-validation.log) · [indice Unicode precedente](ALPHA20_3_PICKER_SEARCH.md).

## Prove desktop ancora aperte

Riaprire rapidamente il picker mentre la libreria viene letta; chiudere e riaprire durante la costruzione dell'indice; modificare la query durante l'attesa; verificare focus, selezione e stato dei pulsanti dopo successo, errore o cancellazione. Restano separate la [prova Inserisci](ALPHA20_3_PICKER_INSERT.md), le misure di reattività con librerie grandi e la revisione grafica reale.
