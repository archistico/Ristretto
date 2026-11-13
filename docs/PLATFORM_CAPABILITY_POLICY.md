# Policy di compatibilità e comunicazione delle capacità

**Proposta v1.7 · Non è un elenco di piattaforme già certificate.**

## Principio

Dire che `l'app parte su macOS` non equivale a dire `l'espansione automatica funziona in ogni app macOS`. Idem Wayland/clipboard/IME. Le note di rilascio dichiarano per ogni platform una combinazione verificata di capacità.

| Capacità | Significato | Prova minima |
|---|---|---|
| GUI | Libreria, editor, impostazioni | installazione pulita + test GUI |
| Tray | Menu presenza/pausa | ambiente desktop noto |
| Picker | Ricerca e selezione manuale | tastiera, focus, shortcut oppure apertura locale |
| Copy | Copia esplicita del testo in clipboard | Unicode+formati+copy race |
| Insert | Inserimento in app esterna | permessi+focus+editor target+clipboard |
| AutoExpand | Osservazione, matching, cancellazione e inserimento | test riproducibili end-to-end per OS+app |

Supporto potrebbe essere **GUI/Picker Only**, **Insert Supported** o **AutoExpand Tested**. Nessuno status derivato solo da compile success.

## Esperienza quando una capacità manca

`Preferenze > Compatibilità` spiega il limite in lingua selezionata; eventuale link alla guida ufficiale dei permessi. La UI offre azione `Copia` quando consentita. Nessuna richiesta di installare driver o concedere privilegi come requisito occulto. Nessuna frase “supportato ovunque”.

## Open source

Documentare librerie P/Invoke e dipendenze native, licenze, firma/notarizzazione e aggiornamento, senza imporre servizi cloud. Decidere packaging dopo spike, inclusi AppImage/deb/rpm/macOS bundle/Windows installer.

Documenti collegati: [NATIVE_INPUT_OUTPUT.md](NATIVE_INPUT_OUTPUT.md), [PLATFORMS.md](PLATFORMS.md), [SECURITY_PRIVACY.md](SECURITY_PRIVACY.md).
