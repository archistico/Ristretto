# Gestione gruppi ed editor — interazioni v3.1

## Gruppi

Nel pannello sinistro è possibile creare un gruppo, modificare il nome del gruppo selezionato, attivarlo/disattivarlo e cancellarlo **soltanto se non contiene snippet**. La cancellazione è preceduta da una conferma. Il database usa transazioni: l'eliminazione non rimuove a cascata gli snippet. Se il gruppo viene modificato altrove, l'operazione rifiuta uno stato precedente incoerente. La disattivazione si riflette nel picker locale, che esclude i gruppi disabilitati.

## Protezione modifiche non salvate

Le modifiche ai campi dello snippet (`titolo`, `trigger`, `contenuto`, `descrizione`, `attivo`) o al nome del gruppo provocano un indicatore nel pannello editor. Quando una bozza è presente, una conferma è richiesta prima di:

- cambiare snippet o gruppo selezionato;
- creare un nuovo snippet o un nuovo gruppo;
- importare un JSON o ripristinare un backup;
- uscire dall'applicazione (menu tray o chiusura diretta su piattaforme dove non rimane in tray).

Su Windows/macOS chiudere la finestra normalmente la nasconde nella tray: **la bozza rimane in memoria** e la conferma avviene prima di uscire. In Linux, dove la policy di v3.0 è terminare l'app alla chiusura della finestra, appare prima la conferma. La protezione è limitata alla sessione: **non esiste ancora autosalvataggio delle bozze né ripristino dopo crash**. Se una conferma viene annullata, la scelta precedente deve rimanere attiva.

## Limitazioni dichiarate

Il cambio di gruppo di uno snippet già salvato non ha ancora una scelta esplicita nell'editor; spostamento e importazione selettiva saranno implementati successivamente. Non si può cancellare un gruppo pieno. I trigger duplicati non vengono risolti automaticamente. Le scorciatoie da tastiera sono ancora locali e il picker continua a limitarsi alla copia manuale.

## Matrice manuale essenziale

- Bozza nuova → clic su altro snippet → Annulla: stessi campi e stessa selezione; Conferma: carica l'altro snippet.
- Bozza → altro gruppo / Nuovo / Importa / Ripristina: nessuna perdita senza conferma.
- Bozza → nascondi nella tray → riapri: campi invariati; Esci dalla tray → conferma.
- Crea gruppo → rinomina → disattiva → picker non presenta gli snippet → riattiva → picker li presenta.
- Elimina gruppo vuoto → conferma → gruppo scomparso. Elimina gruppo pieno → rifiuto senza cancellazione.
- Importa JSON di una libreria che contiene omonimi → crea nuovi gruppi con suffisso e GUID indipendenti, preservando emoji e accenti.

I test UI devono essere eseguiti manualmente o tramite automazione Avalonia dopo aver completato la prima build, perché in questo ambiente manca .NET SDK.

Il pulsante **Salva** salva soltanto lo snippet; un eventuale nome di gruppo modificato ma non ancora confermato rimane nella bozza. L’export JSON segnala esplicitamente che include **solo dati salvati**, senza scartare la bozza.
