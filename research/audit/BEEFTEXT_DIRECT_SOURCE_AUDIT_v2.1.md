# Beeftext — audit diretto dell'intero snapshot applicativo

**Metodo:** lettura di tutti i principali moduli C++ e delle configurazioni di progetto presenti nel file ZIP utente, con campionamento mirato delle funzioni più sensibili. Nessuna esecuzione o compilazione. **Non è un audit di sicurezza esaustivo** e mancano i due submodule.

## Inventario osservato

- 269 file nello snapshot, tra cui 67 `.cpp`, 67 `.h`, 18 `.ui`.
- Moduli: `Combo`, `Snippet`, `Clipboard`, `Picker`, `Preferences`, `Group`, `Emoji`, `Backup`, `LastUse`, `Update`, `Dialogs`, oltre a `InputManager`, `KeyboardMapper`, `MainWindow`, `I18nManager` e `ProcessListManager`.
- `Beeftext/main.cpp:57–135`: single instance → QSettings/cartelle → conversioni legacy → manager/libreria/GUI → shortcut → event loop. I singleton agevolano il coordinamento ma limitano l'isolamento dei test.
- Gli `.ui` Qt sono la fonte più aderente per le schermate; conservare nell'analisi UX ciò che mostrano, senza copiare meccanicamente la struttura dei widget nella GUI Avalonia.

## Flusso di espansione verificato

1. `Beeftext/InputManager.cpp:119–156`: intercetta eventi con `WH_KEYBOARD_LL`, filtra alcune applicazioni e gestisce shortcut.
2. `Beeftext/InputManager.cpp:238–304`: traduce key event attraverso `ToUnicodeEx` usando il layout della finestra foreground, gestisce breaker, backspace e spazi. Non equivale a un feed universale di *committed text* degli IME.
3. `Beeftext/Combo/ComboManager.cpp:184–223`: cerca matching negli snippet e, se più di uno corrisponde, **seleziona casualmente** un risultato (`rng_`).
4. `Beeftext/Combo/Combo.cpp:377–384`: verifica modalità strict/loose e sensibilità maiuscole.
5. `Beeftext/Combo/Combo.cpp:406–453`: espande variabili, attende 300 ms dopo form, sospende temporaneamente hook, cancella il trigger, esegue i fragment e riposiziona il cursore.
6. `Beeftext/BeeftextUtils.cpp:179–218`: cancellazione mediante Backspace e incolla con clipboard; ripristino o pulizia programmata dopo 1000 ms.
7. `Beeftext/BeeftextUtils.cpp:229–246`: metodo alternativo `insertTextByTyping` che cicla sui `QChar` ed emette input Unicode.

## Rischi di interoperabilità verificabili nel codice

| ID | Evidenza statica | Rischio / decisione per il nuovo software | Stato |
|---|---|---|---|
| BF-01 | `ComboManager.cpp:206–220` | collisioni casuali: imporre regola deterministica o chooser | Confermato staticamente |
| BF-02 | `Combo.cpp:429–439` usa `keyword_.size()` per cancellazione | numero unità UTF-16 ≠ numero di Backspace necessari in tutti gli editor e IME | Rischio da testare |
| BF-03 | `BeeftextUtils.cpp:229–245` itera `QChar` | i surrogate pair e gli emoji composti richiedono test d'iniezione; non inferire correttezza da UTF-16 valido | Rischio da testare |
| BF-04 | `BeeftextUtils.cpp:193–218` usa timer 1s clipboard | nuova copia dell'utente può interferire con ripristino/pulizia | Rischio da testare |
| BF-05 | `Combo.cpp:413–417` pausa fissa 300ms | performance e focus variabili tra app | Confermato staticamente |
| BF-06 | `PreferencesManager.cpp:537` legge `kKeyUseCustomSound` per `UseCustomBackupLocation` | errore di mapping nell'export | Difetto del codice nello snapshot |
| BF-07 | `PreferencesManager.cpp:587` legge lo splitter ma scrive `kKeyGeometry` | possibile corruzione geometria durante l'import | Difetto del codice nello snapshot |
| BF-08 | `I18nManager.cpp:53–55` mappa risorse `pl_PL` verso directory `nl_NL` | localizzazione polacca non affidabile in migrazione | Difetto del codice nello snapshot |
| BF-09 | `Clipboard/ClipboardManagerDefault.cpp:63–170` enumera e ripristina formati | formati speciali, ownership e race clipboard da verificare | Rischio da testare |
| BF-10 | `.gitmodules` punta a due submodule che non sono popolati | build del progetto originale non riproducibile da ZIP solo | Vincolo di baseline |

Nessuno di questi rischi dimostra un malfunzionamento in tutte le applicazioni. I difetti BF-06–BF-08 sono **contraddizioni testuali direttamente visibili nel sorgente dello snapshot**; non sono bug runtime certificati di una release specifica.

## UI e lifecycle: cosa è interessante

- `Picker/PickerWindow*`, `PickerModel`, `PickerSortFilterProxyModel`: separazione fra ricerca, modello, finestra e shortcut; preservare la rapidità, ma non l'accoppiamento con i singleton globali.
- `Preferences/PreferencesDialog.ui`: opzioni di matching, clipboard e comportamento native; per la nuova GUI, separare impostazioni essenziali e avanzate.
- `MainWindow.cpp` e `main.cpp`: finestra che può chiudersi senza terminare il processo, tray e istanza singola; servono condizioni testate di avvio/uscita/riavvio.
- `I18nManager.cpp` e `Translations/*.ts`: supporto lingue regionali nel progetto originale; in Avalonia serve localizzazione osservabile e aggiornamento live di tutti i menu, incluso tray.

## Cosa non replicare

Singleton diffusi e dipendenze cicliche tra UI/motore; trigger collision casuale; dipendenza da timer fissi per correttezza; cancellazione sicura inferita dal solo numero di unità UTF-16; restore clipboard incondizionato; scrittura di preferenze progressiva senza convalida completa.

## Prossime prove richieste

Eseguire l'app originale in VM Windows (solo dopo aver ottenuto XMiLib ed emojilib), riprodurre BF-01, BF-02/03 con emoji non-BMP, BF-04 con copia durante l'espansione e BF-06/07 con un export/import isolato. Il nuovo prodotto avrà test automatizzati puri per matching, parser, storage e test reali mirati per nativi.
