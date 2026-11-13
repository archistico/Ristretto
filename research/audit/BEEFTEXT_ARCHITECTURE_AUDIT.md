# Audit Beeftext — struttura, bootstrap, GUI e integrazioni (v1.9)

**Data dell'analisi:** 12 novembre 2026. **Tipo:** ispezione statica di file pubblici; **nessuna compilazione o prova runtime**. I sorgenti consultati sono nella branch mobile `master` di GitHub; **non** è stato acquisito un checkout completo né uno SHA di `master`. La release v16.0 è mostrata sulla pagina Releases, con commit breve `1f24a1c`, ma i file letti da `master` **non sono attestati** come identici a quel tag. Per riproducibilità occorre un checkout `git clone --recurse-submodules` con hash verificato. Non sono stati copiati sorgenti di terzi nel dossier.

## A. Inventario ragionato delle cartelle

Il file `Beeftext/CMakeLists.txt` enumera i sorgenti, mentre `.gitmodules` elenca i submodule. Non si tratta del risultato di uno scanner dell'intero repository.

| Cartella / area | File rappresentativi | Responsabilità osservata | Equivalente candidato |
|---|---|---|---|
| `Beeftext/main.cpp` | `main.cpp`, `BeeftextGlobals.*`, `BeeftextConstants.*` | Bootstrap, single instance, cartelle, vita applicazione | Composition root desktop, `AppLifecycle` |
| `Beeftext/InputManager.*` | `InputManager`, `KeyboardMapper`, `Shortcut` | Hook globali Win32, rilevazione e shortcut | `InputAdapter.Windows`, eventi normalizzati |
| `Beeftext/Combo/` | `Combo`, `ComboList`, `ComboManager`, `ComboVariable`, `MatchingMode`, `CaseSensitivity` | Biblioteca e motore, editor, modelli Qt e import | Slice Library / Match / Expand / Import |
| `Beeftext/Snippet/` | `TextSnippetFragment`, `DelaySnippetFragment`, `KeySnippetFragment`, `ShortcutSnippetFragment` | Piano di inserimento per frammenti | Parser/piano puro e executor |
| `Beeftext/Clipboard/` | `ClipboardManager`, `Default`, `Legacy` | Backend clipboard alternativo | `ClipboardLease` + adapter nativo |
| `Beeftext/Picker/` | `PickerWindow`, `PickerModel`, proxy, delegate, mover/resizer | Ricerca rapida, selezione, posizionamento | Picker Avalonia |
| `Beeftext/Group/` | `GroupList`, widget, dialog | Organizzazione e abilitazione collettiva | Slice Groups |
| `Beeftext/Preferences/` | `PreferencesManager`, `PreferencesDialog`, `PrefPane*` | QSettings, cinque sezioni UI, temi/locale/shortcut | Slice Settings con validazione e change notification |
| `Beeftext/I18nManager.*`, `Theme.*` | `I18nManager`, `Theme` | Traduttori Qt a runtime, look & feel | risorse localizzate osservabili / design tokens |
| `Beeftext/Emoji/` | `EmojiManager`, `EmojiList` | Emoji shortcodes, dati | indicizzazione Unicode e ricerca emoji futura |
| `Beeftext/Backup/`, `LastUse/` | `BackupManager`, `ComboLastUseFile` | Backup e uso storico | Slice Backup/Restore / Usage |
| `Beeftext/Update/` | `UpdateManager`, `UpdateCheckWorker`, dialog | Controllo aggiornamenti asincrono su sito ufficiale | updater opt-in e verificato per canale distribuzione |
| `Beeftext/ProcessListManager.*` | `ProcessListManager`, `BeeftextGlobals` | Elenchi di eseguibili sensibili/esclusi, wildcard | Regole per-app con policy di privacy distinte |
| `Beeftext/MainWindow.*` | `MainWindow`, `.ui` | Tray, menu, drag-drop import/restore, finestra e coordinamento | Avalonia window + thin ViewModel |
| Root, installer, script | `.gitmodules`, `CMakeLists`, `.sln`, `.pro`, `Installer/`, `Scripts/PowerShell`, `Vendor/` | Build traduzioni/installazione/risorse | pipeline .NET e packaging per OS |

**Confine di audit:** per `Vendor/`, `Installer/` e `.github/` sono verificati soltanto l'esistenza nel repository o riferimenti nel build/README. La lista interna completa, checksum e licenze di ogni asset non sono stati enumerati.

## B. Sequenza d'avvio, osservata in `main.cpp`

1. Registra metatypes Qt; crea `QApplication`.
2. Verifica single instance (`SingleInstanceApplication` della XMiLib); seconda istanza esce, prima istanza può portare la finestra davanti.
3. Disattiva `quitOnLastWindowClosed`; imposta nome app/organizzazione e crea directory.
4. Costruisce il singleton preferenze, imposta log e avvio automatico; rimuove un precedente file di aggiornamento marcato per eliminazione.
5. Se necessario gestisce la transizione da vecchi snippet rich text.
6. Crea `ComboManager`, `UpdateManager`, `EmojiManager`; carica gli emoji se richiesti.
7. Crea `MainWindow`, forza un handle Win32 anche con finestra inizialmente nascosta.
8. Primo avvio: mostra la finestra e offre il tutorial. Altrimenti l'app resta prevalentemente in tray.
9. Registra collegamento fra nuova istanza e `showWindow()`, imposta shortcut picker, avvia event loop.
10. All'uscita salva i dati d'ultimo utilizzo, registra log e scarica traduzioni prima della distruzione dell'app.

**Proposta per Avalonia:** conservare il *comportamento* e non la sequenza di singleton. Un `AppStartupCoordinator` piccolo e testabile deve ordinare `OpenProfile → ValidateLibrary → ApplyLocale/Theme → OpenUI/Tray → ProbeCapabilities → StartInput` con stati degradati espliciti; nessuna acquisizione di tasti prima della valutazione di permessi e policy. La seconda istanza passa un evento sicuro alla prima senza avviare un secondo motore.

## C. Connessioni GUI ↔ motore

- `MainWindow` ricava e inserisce dinamicamente i menu Gruppi/Snippet dai widget relativi, costruisce il tray in codice, osserva shortcut da `InputManager` e le modifiche a `PreferencesManager`.
- Il tray ha i comandi Apri, Nuovo combo, Nuovo gruppo, Pausa/Riprendi, Preferenze, Esci. L'icona cambia quando l'app è in pausa e viene ricreata su cambio lingua.
- `closeEvent()` persiste geometria e posizione del divisore; l'uscita applicativa è un'azione esplicita, distinta dalla chiusura finestra.
- Il drag-and-drop accetta file/URL, usa **solo il primo elemento** e decide import o restore in base all'estensione; rinvia il dialog con `QTimer::singleShot(0)` per non bloccare il producer del drag.
- `ProcessListManager` usa wildcard convertiti in regex, confronto case-insensitive per il nome dell'eseguibile. L'elenco delle applicazioni **sensibili** nel codice è presentato come elenco di software che non supportano correttamente Ctrl+V: non è una classificazione universale dei campi password. Le applicazioni **escluse** disattivano la sostituzione. Questi concetti non vanno confusi.

**Miglioria:** comandi UI su un servizio applicativo, non chiamate `InputManager::instance()` dalla vista; supporto multi-file drag/drop con anteprima; niente ripristino automatico solo perché il nome ha una certa estensione.

## D. Dati e preferenze: effetti collaterali importanti

- Preferenze installate: `QSettings` con chiavi organizzazione/app (backend Windows secondo Qt); portable: `QSettings` con file INI. La libreria snippet è distinta dalle preferenze.
- `PreferencesDialog` contiene sezioni `Behavior`, `Combos`, `Emojis`, `Appearance`, `Advanced`; pulsanti Import/Export/Defaults/Close.
- `PreferencesManager::save()` esporta JSON direttamente su `QFile`, senza `QSaveFile` in quel metodo.
- `PreferencesManager::load()` valida la sintassi JSON, quindi chiama `fromJsonDocument()`; quest'ultima **scrive progressivamente** in QSettings e attiva anche side effect (percorso libreria, modalità clipboard, backup, ecc.). Non risulta una validazione in memoria di tutto il documento prima dell'applicazione.
- Difetto statico A: `toJsonDocument()` popola `UseCustomBackupLocation` leggendo `UseCustomSound` (linea ~508). L'export può quindi contenere un valore errato.
- Difetto statico B: `fromJsonDocument()` scrive lo stato splitter nella proprietà `Geometry` (linea ~557), invece che in `MainWindowSplitterState`; persistenza geometria potenzialmente alterata.
- Difetto statico C: il codice di migrazione i18n polacco usa in tre mapping una destinazione `nl_NL/...` (linee ~48–50). Va trattato come possibile errore di migrazione, non un problema riprodotto.
- Questi difetti sono **riscontri del testo sorgente mobile**; verificare sui tag e su test reali prima di registrarli come bug certificati della release v16.

## E. Build e dipendenze: coerenza da verificare

`CMakeLists.txt` root richiede CMake >=3.10 e include XMiLib e Beeftext. Il CMake dell'app (letto da `master`) dichiara `project VERSION 14.0`, C++20, Qt6 Core/Gui/Widgets/Network, dipendenza `Winmm`, build di traduzioni PowerShell. `Beeftext.pro` dichiara invece `CONFIG += c++14` e moduli Qt anche Multimedia. La pagina GitHub Releases presenta v16.0. **Non** concludere che un percorso di build sia rotto: è un disallineamento di metadati e opzioni che richiede un checkout e una build di prova.

## F. Cosa non copiare nell'architettura nuova

| Soluzione originale | Motivazione | Scelta candidata |
|---|---|---|
| Singleton di quasi tutti i manager | Difficili da isolare e testare | DI solo al composition root, interfacce strette per dipendenze esterne |
| `QSettings` con setter ad effetti immediati durante import | Stato parziale possibile | `ValidateAll → Preview → Commit → Notify` |
| Miscela widget/modello Qt/motore | Meno riuso headless | funzioni pure in Core, use case per slice, ViewModel sottili |
| Build Windows con PowerShell/Win32 e Qt | Non portabile tale quale | .NET SDK e packaging differenziato per OS |
| Liste 'sensitive' basate su app Ctrl+V | Non sono automaticamente sicurezza password | policy di privacy separata da compatibilità output |
| Aggiornamenti tramite server legacy | Ownership/affidabilità diversa | distributore del nuovo progetto, checksum/firme secondo canale |

## G. Cosa resta aperto

`A4` rimane **parzialmente completato**: senza tarball/clone non possiamo contare tutti i file, verificare la provenienza precisa di `Vendor/`, auditare i commit e testare i diversi target della build. Eseguire `git clone --recursive ...`, annotare `git rev-parse HEAD`, `git submodule status --recursive`, verificare BOM/licenze binarie, produrre SBOM e compilare una tag/release in ambiente Windows isolato. Nessun PASS runtime.

Fonti puntuali: [registro v1.9](SOURCE_TRACE_V19.md). La lettura dei file raw non autorizza riprodurre interi sorgenti C++ nel progetto.
