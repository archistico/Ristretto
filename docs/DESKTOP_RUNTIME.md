# Runtime desktop — v3.0

## Istanza unica

Un solo processo proprietario del file SQLite per directory di dati. Il componente `SingleInstanceHost` crea un nome stabile basato su SHA-256 del percorso della directory. La seconda istanza tenta di inviare un singolo comando di attivazione (`0x53`) tramite named pipe locale; **non** trasferisce contenuti degli snippet, configurazioni o altri dati. Un fallimento dell'attivazione non apre un secondo processo proprietario del database.

La topologia funziona in via progettuale su Windows/macOS/Linux, ma richiede prove su processi reali, inclusi avvio contemporaneo, crash, accessi separati e chiusura. Non è un meccanismo di protezione da processi ostili dello stesso utente.

## Tray

La GUI usa `TrayIcon`/`NativeMenu` di Avalonia 11: Apri libreria, Picker, Esci. Su Windows/macOS il pulsante X **nasconde** la finestra; il processo termina solo con Esci. Su Linux il pulsante X chiude l'app per evitare processi invisibili su desktop senza icona tray. Le capacità reali vanno testate con GNOME, KDE, X11 e Wayland: la presenza dell'icona non è garantita.

## Picker locale

Il picker viene caricato all'apertura e legge uno snapshot della libreria SQLite: nessuna query durante la digitazione della ricerca. Il motore di ricerca dedicato classifica i risultati: trigger esatto, prefisso trigger, prefisso nome, nome completo, trigger parziale, descrizione e primi 4096 caratteri di contenuto. Limitazione a 50 risultati; gruppi/snippet disabilitati esclusi.

La clipboard viene modificata **solo** alla pressione di Invio o del pulsante Copia. Nessun tentativo di rimandare il focus a un'applicazione esterna o incollare automaticamente. Il picker non è richiamabile globalmente e non equivale al motore di espansione.

## Sicurezza e privacy

- Nessun keylogger o hook globale in v3.0; nessuna telemetria delle ricerche.
- Gestione del contenuto clipboard soltanto su comando esplicito.
- Tray e IPC non espongono testo, trigger o percorso dell'archivio.
- Ripristino SQLite già configurato con copia di recupero in caso di successo.
- Attività successive: controllare che la GUI protegga le modifiche non salvate, testare permessi IPC e NativeMenu su tutti i sistemi.

## Fonte API

- Tray: https://docs.avaloniaui.net/controls/navigation/trayicon/
- Window lifecycle: https://v11.docs.avaloniaui.net/docs/reference/controls/window/
- AssetLoader: https://docs.avaloniaui.net/docs/fundamentals/including-assets/
- Clipboard: https://docs.avaloniaui.net/docs/how-to/clipboard-how-to

## v3.1 — bozze e gruppi

La finestra principale chiede conferma prima di abbandonare modifiche a snippet e gruppo. In tray la bozza rimane disponibile fino all'uscita; il picker resta esclusivamente locale e manuale. Vedere [Editor e gruppi](EDITOR_AND_GROUPS.md) e [Interoperabilità JSON](JSON_PORTABILITY.md).

## Rebranding v3.5 e continuità dei dati

- Nome pubblico: **Ristretto**; eseguibile `Ristretto.exe`, solution `Ristretto.sln`.
- I namespace `.NET` interni restano `TextExpander.*` per evitare un refactoring non necessario mentre si corregge la prima build.
- La directory dati rimane `TextExpanderStudio` e i nomi mutex/pipe sono intenzionalmente preservati: serve a riaprire gli snippet v3.4 e a impedire l'uso simultaneo dello stesso DB. **Non** cambiare questi identificativi senza una migrazione testata e senza gestire l'istanza unica su entrambi i nomi.
- Il nuovo assembly `Ristretto` modifica il percorso della risorsa tray, aggiornato a `avares://Ristretto/Assets/app-icon.ico`. Smoke test necessario su Windows/macOS/Linux.
