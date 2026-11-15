# Alpha 20.27 — Prove della form Registra hotkey

La nuova suite usa **i controlli Avalonia del prodotto**: apre la libreria e il dialogo, invia eventi di tastiera e puntatore al backend headless e verifica il risultato sul ViewModel e su un database SQLite sintetico.

## Correzioni

- **Shift+Tab:** dopo una combinazione completa, Shift avviava un nuovo gesto e cancellava la combinazione prima che Tab spostasse il focus. Ora la navigazione conserva l'ultima combinazione completata. Un nuovo tasto principale o la perdita di attivazione invalidano questa memoria; non viene recuperata una combinazione precedente dopo un tentativo non valido.
- **Pulsanti fuori dal bordo:** in francese e tedesco la riga delle azioni superava la larghezza del dialogo. Ora i pulsanti possono andare a capo e l'altezza della finestra segue il contenuto.

Nessuna modifica allo schema SQLite, al JSON, al picker o al servizio di inserimento.

## Cosa verificano i nuovi test

**24 nuovi casi:** 22 di integrazione UI e due sullo stato di acquisizione. Il filtro completo della cattura esegue 50 casi.

| Flusso | Verifica automatica |
| --- | --- |
| Pressione/rilascio | Display `ALT + CTRL + SHIFT + B`, conferma consentita dopo il rilascio, valore canonico `Ctrl+Alt+Shift+B` |
| Navigazione | Tab, Shift+Tab e conferma con Invio sul pulsante; combinazione conservata |
| Annullamento | Annulla, Esc e chiusura restituiscono null; Rimuovi restituisce una stringa vuota |
| Tasto non supportato | F12 non rende disponibile la conferma né recupera una vecchia combinazione |
| Libreria e persistenza | Il vero pulsante apre il dialogo; la conferma cambia la bozza, soltanto Salva cambia SQLite |
| Ripristino simulato | Libreria disabilitata fino alla fine del coordinatore; annullamento/errore conservano la hotkey precedente |
| Disposizione | Tre azioni contenute nell'area client, cinque lingue, temi chiaro/scuro, larghezze 440 e 540 DIP |

Le prove caricano stili e risorse di produzione in un'applicazione di test separata. Non avviano il lifetime desktop, la tray, worker nativi, appunti o il profilo personale. La pausa e il ripristino sono funzioni simulate attorno al vero coordinatore; la registrazione Windows resta da provare separatamente.

## Esecuzione

```powershell
dotnet test tests/TextExpander.Tests/TextExpander.Tests.csproj -c Release --filter 'FullyQualifiedName~HotkeyCaptureTests|FullyQualifiedName~HotkeyCaptureWindowTests'
```

Le prove fanno parte anche di CORE e CI. `Avalonia.Headless.XUnit` è una dipendenza solo dei test, versione **11.3.22**, uguale alla patch Avalonia del prodotto. Cambiando Avalonia occorre allineare anche questo pacchetto.

## Limiti e prossimi passi

Il backend headless prova dispatch, focus fra controlli, disposizione e collegamento al database. Non certifica la resa dei font su Windows, DPI reali, tastiere fisiche, AltGr/IME, attivazione del sistema operativo o sospensione/ripristino di `RegisterHotKey`. Non sono stati acquisiti nuovi screenshot.

R2.08 avanza ma rimane aperta con R2.01/R2.09. Seguire la [matrice Windows](WINDOWS_QUALIFICATION_PLAN.md); lasciare i suoi casi desktop `NOT_RUN` finché non vengono eseguiti davvero. [Risultati della release](../reports/ALPHA20_27_CHECKS.md).
