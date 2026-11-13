# Audit comparativo diretto dei quattro sorgenti — v2.3

**Metodo:** lettura statica di quattro snapshot ZIP forniti dall'utente. I percorsi indicati sono relativi alla root di ciascun archivio; i numeri sono righe del sorgente estratto. Non sono commit Git identificati, non è stata compilata alcuna applicazione originale e non sono stati verificati comportamenti runtime. L'audit è finalizzato a **mappare responsabilità**, non a copiarne le implementazioni.

## 1. Provenienza e limiti

| Input | Cartella ZIP | Limite |
|---|---|---|
| Beeftext | `Beeftext-master/` | snapshot master, senza revisione commit e con directory submodule vuote |
| Espanso | `espanso-dev/` | snapshot dev, senza revisione commit; codice Rust GPLv3 |
| XMiLib | `XMiLib-master/` | snapshot master non dimostrato compatibile con Gitlink Beeftext |
| emojilib | `emojilib-main/` | dataset release 4.0.3 incompatibile per formato con file richiesto da Beeftext (v2.2) |

Inventario e SHA-256 delle copie sono disponibili in [baseline v2.1](LOCAL_SOURCE_BASELINE_v2.1.md) e [submodule v2.2](SUBMODULE_AUDIT_SUMMARY_v2.2.md). Non sono presenti né reinserite copie dei quattro sorgenti nel dossier.

## 2. Beeftext: responsabilità effettive

| Evidenza direttamente letta | File e linee | Impatto architetturale |
|---|---|---|
| Hook e interpretazione eventi + shortkeys nello stesso componente | `Beeftext/InputManager.cpp:83-155`, `:238-319` | dividere adapter input da matching semantico |
| Segnali input connessi a manager dei combo | `Beeftext/Combo/ComboManager.cpp:53-59` | raccordo unico application invece di singleton Qt |
| Matcher su lista e scelta random in caso di più risultati | `Beeftext/Combo/ComboManager.cpp:194-224` | matcher puro con conflitti espliciti |
| Emoji shortcode verificati dopo i combo | `Beeftext/Combo/ComboManager.cpp:184-187`, `:225-267` | catalogo emoji policy separata dal matcher snippet |
| Valutazione, frammenti e sostituzione fusi nella classe `Combo` | `Beeftext/Combo/Combo.cpp:406-449` | preparazione e executor distinti |
| Libreria con conversione JSON e salvataggio | `Beeftext/Combo/ComboList.cpp:395-503` | storage fuori dal Core; importatore inerte |
| Picker richiama espansione su timer dopo chiusura | `Beeftext/Picker/PickerWindow.cpp:274-302` | token target e preflight, non delay fisso |
| Clipboard copia format-by-format e ripristina | `Beeftext/Clipboard/ClipboardManagerDefault.cpp:63-151` | broker clipboard platform, lease condizionale |
| Emoji JSON con `char` per shortcode | `Beeftext/Emoji/EmojiManager.cpp:133-165` | conversione dataset versionata |
| Tray e app lifecycle concentrati nella MainWindow | `Beeftext/MainWindow.cpp:118-170`, `:270-300` | shell separata da app lifecycle |
| `PreferencesManager` carica/salva config | `Beeftext/Preferences/PreferencesManager.cpp` | validazione atomica delle preferenze |
| Traduce UI con `QTranslator` | `Beeftext/I18nManager.cpp:250-303` | risorse I18n dinamiche Avalonia |

**Interpretazione:** Beeftext è una buona guida alla GUI e ai workflow Windows, ma le classi uniscono logica, effetto esterno e rappresentazione; non si propone un porting 1:1.

## 3. Espanso: separazione interessante ma non da copiare integralmente

| Evidenza direttamente letta | File e linee | Impatto architetturale |
|---|---|---|
| `Matcher` restituisce stato e risultati; non inietta testo | `espanso-match/src/lib.rs:23-57` | algoritmo matching puro riutilizzabile concettualmente |
| Rolling matcher mantiene più percorsi di match | `espanso-match/src/rolling/matcher.rs:31-112` | gestire trigger sovrapposti senza stato UI |
| `Renderer` produce `Success/Aborted/Error` e supporta extension | `espanso-render/src/lib.rs:26-97`, `:120-...` | separare preparazione di contenuto, variabili e output |
| `DefaultProcessor` orchestra eventi/dispatcher | `espanso-engine/src/process/default.rs:56-175` | sequenza esplicita degli effetti, senza bus obbligatorio |
| Injector è un contratto separato dall'engine | `espanso-inject/src/lib.rs:41-...` | porte platform narrow |
| Source distingue acquisizione input | `espanso-detect/src/lib.rs:43-...` | non far chiamare OS API al matcher |
| Clipboard multi-OS in codice dedicato | `espanso-clipboard/src/lib.rs` e `win32/`, `cocoa/`, `x11/`, `wayland/` | adapter clipboard diversi per sessione |
| Config loader e schema match separati | `espanso-config/src/lib.rs:23-56`; `espanso-config/src/matches/mod.rs:28-95` | modello interno e import esterno non coincidono |
| UI remote e loop sono distinti | `espanso-ui/src/lib.rs:29-70` | GUI/tray non deve conoscere algoritmi del motore |

**Interpretazione:** riprendere confini logici `observe → match → prepare → execute` e test puri, **non** copiare Rust GPLv3 né imitare il numero di crate, middleware e dispatcher.

## 4. XMiLib: infrastruttura, non un Core da migrare

| Evidenza | File e linee | Esito |
|---|---|---|
| `SendInput` più volte senza verifica quantità eventi | `XMiLib/SystemUtils.cpp:59`, `:97`, `:123`, `:143`, `:181` | adapter deve riportare errore/limiti |
| Helper Unicode con keydown flag + keyup non conforme | `XMiLib/SystemUtils.cpp:163-183` | riscrivere e provare su app reali, niente copia |
| Shortcut Win32 con `GlobalAddAtom`, `RegisterHotKey` | `XMiLib/GlobalShortcut/GlobalShortcut.cpp:30-35` | hotkey adapter e cleanup dedicato |
| Clipboard Win32 RAII | `XMiLib/Scoped/ScopedClipboardAccess.cpp:24-51` | gestione lifetime nell'adapter, `using`/SafeHandle |
| Istanza singola in helper dedicato | `XMiLib/SingleInstanceApp.cpp:27-...` | lifecycle in host con OS helper se necessario |
| CSV, file, debug log e Qt widgets | `XMiLib/File/`, `DebugLog/`, `String/` | usare BCL e componenti app propri, niente clone `Utils` universale |

**Interpretazione:** non referenziare direttamente XMiLib/Qt dal nuovo software; se si riusa codice MIT, conservare copyright e notices (scelta da approvare).

## 5. emojilib: dati e schema, non architettura

- `emojilib-main/package.json` identifica un progetto JS con dati esterni; **non** serve il runtime Node per leggere le keyword nella nuova app.
- `emojilib-main/dist/emoji-en-US.json` mappa caratteri emoji alle keyword; è differente dallo schema che Beeftext richiede in `Beeftext/Emoji/EmojiManager.cpp:133-165` e dal percorso `Submodules/emojilib/emojis.json` in `EmojiManager.cpp:21-36`.
- La slice `EmojiCatalog` carica/valida/specifica versione e indice, separa lingua dell'interfaccia da lingua parole chiave. Shortcodes riconosciuti globalmente e autocorrezione sono **funzioni diverse**: devono essere attivate con policy esplicita.

## 6. Mappatura conclusiva per l'applicazione C#

| Origine | Responsabilità | Destinazione | Riuso |
|---|---|---|---|
| Beeftext `InputManager` + Espanso detect | Eventi/IME/layout | `Infrastructure/Platform/*` + sessione `TriggerRecognition` | design, non codice |
| Beeftext `ComboManager` + Espanso match | Matching e conflitti | `Core/Matching` | design, con regola deterministica nuova |
| Beeftext `ComboVariable` + Espanso render | Variabili + template | `Core/Expansion` + `Application/ExpandText/Prepare` | design, policy esecuzione nuova |
| Beeftext clipboard + Espanso clipboard | Inserimento e clipboard | `Infrastructure/Platform/*` | concept, contratto outcome |
| Beeftext `ComboList` + Espanso config | Dati/serializzazione | `Infrastructure/Persistence`, `ImportExport` | import/fixtures, non modello runtime |
| Beeftext UI + Espanso UI | Main/editor/tray | `Desktop/Features`, `Desktop/Shell` | UX, non Qt/Rust |
| XMiLib shortcut/instance/input | Interop Windows | `Infrastructure/Platform/Windows` | concetti, riscrittura |
| emojilib JSON | Dataset emoji | `Application/EmojiCatalog` + data file verificato | eventuale dataset MIT con notices |

## 7. Risultati consolidati e limiti

**Confermato dal codice:** separazione logica fattibile, boundary OS necessari, matcher non deve dipendere da UI, caricamento di dataset emoji distinto, utility XMiLib OS-specific, Espanso render/match separati.

**Proposta architetturale, non fatto originale:** quattro progetti produttivi, planner puro, snapshot atomica dei trigger, `ClipboardLease`, fallimenti tipizzati, capability reporting, SQLite candidato, runtime in singolo processo.

**Non verificato:** compilabilità degli snapshot originali, compatibilità tra commit submodule, correttezza dei comportamenti dei sistemi operativi, latenza, protezione universale dei campi password, input IME committato in tutte le app, Wayland automatico, prestazioni GUI.

Da leggere insieme a [architettura canonica](../../docs/ARCHITECTURE.md), [catalogo slice](../../docs/VERTICAL_SLICES.md) e [test](../../docs/ARCHITECTURE_TEST_PLAN.md).
