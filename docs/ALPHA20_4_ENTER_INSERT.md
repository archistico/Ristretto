# Alpha 20.4 — Invio inserisce il primo risultato

## Uso

1. Mettere il cursore nell'editor esterno e aprire il picker con la hotkey configurata, normalmente `Ctrl+Shift+Space`.
2. Scrivere il filtro.
3. Premere e rilasciare **Invio**: il picker usa il **primo risultato dell'elenco filtrato**, anche se era selezionata un'altra riga, e avvia lo stesso inserimento del pulsante Inserisci.

Il picker si nasconde per ripristinare il focus e inviare l'incolla; dopo l'invio accettato rimane nascosto. Il pulsante **Inserisci** continua invece a usare la riga selezionata. **Copia testo** rimane un comando esplicito tramite il relativo pulsante: Invio non è più una scorciatoia per Copia.

Senza risultati, durante caricamento/operazione pendente o senza un destinatario esterno valido, Invio non avvia l'inserimento e non accoda un comando futuro. Se l'inserimento viene rifiutato o ha esito incerto, resta il comportamento precedente: il picker torna visibile con il messaggio di errore e non ritenta.

## Tastiera e implementazione

- Invio senza modificatori viene intercettato prima del comportamento predefinito dei controlli, così non attiva accidentalmente Copia quando quel pulsante ha il focus.
- L'azione parte al rilascio di Invio; il focus rimane sul picker mentre il tasto è premuto. Le ripetizioni del keydown nella stessa pressione non accodano inserimenti.
- Il gesto pendente viene azzerato su perdita del focus, chiusura, Esc, apertura libreria e nuova apertura del picker.
- `PickerViewModel.InsertFirstAsync` sceglie il primo risultato corrente e riusa `InsertSelectedAsync`: permesso monouso, serializzazione degli appunti e controlli del destinatario restano quelli esistenti.
- Le istruzioni nel picker sono aggiornate nelle cinque lingue. Versioni dei cinque progetti espliciti, gate e pacchetto allineati a **3.9.0-alpha.20.4**.

## Verifiche

**558/558 xUnit** passati, inclusi **50 ViewModel** e otto nuovi casi su primo risultato dopo filtro, selezione diversa/assente, elenco vuoto, destinatario non disponibile, caricamento, clipboard pendente, invio ripetuto e comportamento del pulsante Inserisci.

Solution e quattro strumenti compilati in Release con restore NuGet locale; audit sorgenti/statici e sette test Python passati. Resta il warning preesistente `xUnit2000` nella suite capacità adapter.

Build di solution/quattro strumenti e 558 xUnit ripetuti con esito positivo da una nuova estrazione dello ZIP priva di `bin`/`obj`, dopo la verifica del manifest SHA-256. I sorgenti, le versioni e le istruzioni compilati corrispondono alla consegna.

I test invocano il ViewModel con callback sintetici; non avviano Avalonia, non modificano appunti reali e non inviano input. Routing dei tasti, ripristino del focus e chiusura effettiva sul desktop devono ancora essere provati. Nessun nuovo avvio di VS Code o altro programma grafico effettuato.

[Riepilogo JSON](../reports/ENTER_INSERT_VALIDATION.json) · [log build/test](../reports/enter-insert-validation.log) · [limiti dell'inserimento Windows](ALPHA20_3_PICKER_INSERT.md).
