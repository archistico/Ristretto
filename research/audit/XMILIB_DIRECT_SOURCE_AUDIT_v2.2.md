# Audit diretto XMiLib — v2.2

**Stato:** lettura statica dello ZIP `XMiLib-master.zip` fornito dall'utente. Evidenza verificata sui *byte dello snapshot*, non sulla release di Beeftext. **132 file regolari**, licenza MIT (`LICENSE.md`, Xavier Michelon). Il branch `master` senza cartella `.git` non attesta il commit del submodule effettivamente pin-nato da Beeftext.

## Architettura e responsabilità reali

| Area XMiLib | Sorgenti principali | Utilizzo riscontrato in Beeftext | Porting C# proposto |
|---|---|---|---|
| Eventi globali/hotkey | `XMiLib/GlobalShortcut/GlobalShortcut*.cpp` | `Beeftext/Picker/PickerWindow.cpp:27-41` registra la scorciatoia del picker | `IGlobalShortcutService` nel solo adapter OS; niente singleton pubblico |
| Sintesi input | `XMiLib/SystemUtils.cpp` | `Beeftext/BeeftextUtils.cpp:229-246`; `Snippet/KeySnippetFragment.cpp` | `ITextInjector`/`IKeyStrokeSender`; capability e risultato verificabile |
| Clipboard Win32 | `Scoped/ScopedClipboardAccess.cpp`, `ScopedGlobalMemoryLock.cpp` | `Beeftext/Clipboard/ClipboardManagerDefault.cpp:63-100` | wrapper Win32 con `SafeHandle`/`IDisposable`, esiti espliciti |
| Single instance | `SingleInstanceApp.cpp` | `Beeftext/main.cpp` | lifecycle per OS con IPC/attivazione finestra, separato dalla GUI |
| CSV e file | `File/CsvIO.cpp`, `File/FileUtils.cpp` | import/export e updater | serializzazione .NET con parser testato, no porting C++ |
| Utilità base | `VersionNumber`, `RandomNumberGenerator`, `Exception`, `StringUtils` | versioning, trigger e variabili | classi standard .NET o funzioni pure; niente framework interno generico |
| Diagnostica e finestre | `DebugLog/*`, `StyleSheetEditor/*`, `StringListEditorDialog/*` | debug log, editor di stringhe e stile | UI Avalonia e logging con redazione dei contenuti sensibili |
| Concorrenza/progress | `ThreadedOperation/*` | funzionalità secondarie di UI | `async/await`, `CancellationToken`, progress esplicito |

CMake: `XMiLib/CMakeLists.txt:1-20,30-42,44-94` richiede Qt 6, seleziona sorgenti Windows e imposta C++20; `CMAKE_PREFIX_PATH` contiene `QT_DIR` letterale alla riga 16 (possibile errore di configurazione, non confermato in build). Il progetto include test Qt (`Test/XMiLibTest/`) e GUI test (`Test/XMiLibGuiTest/`), ma non sono stati eseguiti; `qmake` non è disponibile nell'ambiente attuale.

## Evidenze tecniche con severità

### XM-01 — Evento di rilascio Unicode non marcato [Alta, osservazione statica]

In `XMiLib/SystemUtils.cpp:169-181`, la funzione `synthesizeUnicodeKeyDownAndUp()` prepara un evento `KEYEVENTF_UNICODE` per il key-down, ma il key-up assegna il flag `KEYEVENTF_KEYUP | KEYEVENTF_KEYUP`. L'OR ripete lo stesso flag e **non include `KEYEVENTF_UNICODE`**. La funzione è effettivamente usata dal percorso `Beeftext/BeeftextUtils.cpp:229-246`, che itera i `QChar` e chiama questo helper. Questo è un difetto testuale certo nella costruzione dei flag; il suo impatto sul destinatario va dimostrato su Windows. Non riprodurre il comportamento: testare coppie keydown/keyup, flag Unicode, output e conteggio eventi con più editor.

### XM-02 — L'invio non verifica il numero di eventi [Alta, da validare]

`XMiLib/SystemUtils.cpp:50-63,85-101,107-127,134-147,169-182`: le chiamate `SendInput` visualizzate non controllano il numero restituito. Questo impedisce a quel livello di sapere se l'iniezione sia avvenuta completamente. Nel prodotto nuovo l'adapter dovrà restituire success/failure/partial/unknown, con dettagli diagnostici privi del testo utente. Anche un ritorno positivo dell'API non prova che il testo *renderizzato* sia corretto: servono E2E editor.

### XM-03 — `QChar` e UTF-16 [Alta, rischio da esperimento]

Il percorso `Beeftext/BeeftextUtils.cpp:229-246` itera elementi `QChar` (unità UTF-16) e chiama l'helper con `quint16`. Emoji sopra U+FFFF richiedono più unità UTF-16, mentre le sequenze ZWJ ne richiedono molte. La corretta ricomposizione **non è garantita semplicemente da questa lettura**; né è dimostrato che fallisca sempre. Specificare test con U+1F600, ZWJ, modificatori tono, VS16, CJK e RTL.

### XM-04 — Hotkey fallita e atom Windows [Media, sospetto statico forte]

`XMiLib/GlobalShortcut/GlobalShortcut.cpp:28-35,49-52`: il costruttore crea un `GlobalAddAtom`, quindi, se `RegisterHotKey` fallisce, lancia un'eccezione. La liberazione dell'atom compare solo nel distruttore; per un costruttore che lancia, quel distruttore non è eseguito. Rischio di risorsa non liberata sui tentativi falliti (collisions / reserved shortcut). Verificare con test Win32 e progettare un handle scoped per l'atom anche durante la costruzione.

### XM-05 — Clipboard access e ripristino [Alta, rischio di perdita dati]

`Scoped/ScopedClipboardAccess.cpp:11-14,24-42`: apertura con max 20 tentativi e 50 ms fra tentativi; `isOpen()` comunica successo/fallimento. Nel chiamante `Beeftext/Clipboard/ClipboardManagerDefault.cpp:63-100`, `backupClipboard()` svuota prima i dati salvati e costruisce il guard, ma non controlla esplicitamente `sca.isOpen()` prima di enumerare. Ciò rende necessario studiare il comportamento con clipboard occupata. `restoreClipboard():110-148` controlla invece il guard e usa `EmptyClipboard` prima di reinserire i formati. Il nostro contratto: fallimento dell'apertura non deve invalidare una snapshot valida, ripristino condizionale alla versione degli appunti, test multi-formato e ownership.

### XM-06 — Single-instance con flag e polling [Media, limite di design]

`SingleInstanceApp.cpp:15-18,27-44,66-99`: messaggio tramite memoria condivisa intera e timer ogni 200 ms; non è una coda di comandi affidabile. Per il nuovo prodotto l'istanza secondaria dovrà richiamare la principale con un messaggio circoscritto, gestendo race, lock residui e assenza di tray.

### XM-07 — Portabilità apparente delle hotkey [Alta, scelta progettuale]

`GlobalShortcut/GlobalShortcutManager.h:20-49` è sotto `Q_OS_WIN`, e `GlobalShortcut.cpp:55-64` contiene metodi segnaposto per il ramo non-Windows. Non è una soluzione cross-platform riutilizzabile. Avalonia gestirà la GUI; hotkey ed eventi globali avranno backend separati per OS, con capability documentate.

## Cosa riutilizzare e cosa no

**Da apprendere:** ownership e teardown tramite RAII, distinzione tra hotkey ed eventi generici, protezione contro accesso concorrente alla clipboard, codice UI separato dai metodi Win32. **Da evitare nel prodotto:** singoli manager singleton accessibili dappertutto, successo implicito di `SendInput`, reset clipboard senza verifica del contenuto corrente, dipendenza Qt, astrazioni per ogni metodo senza uso reale.

## Verifiche ancora necessarie

- Acquisire gli SHA Git effettivi del submodule *pinned* da Beeftext; uno ZIP `master` non li contiene.
- Eseguire test Win32 controllati di Unicode release, partial input, hotkey conflicts, clipboard busy e distruzione delle risorse.
- Se si compila XMiLib per riprodurre il comportamento, usare ambiente Windows isolato con Qt 6 e specificare Qt / CMake / MSVC installati. **Nessuna build C++ eseguita in questo audit.**

Fonte primaria: snapshot locale consegnato dall'utente; tutti i riferimenti `XMiLib/...` sono relativi alla radice dello ZIP. Questo audit non introduce codice copiato nel nostro progetto.
