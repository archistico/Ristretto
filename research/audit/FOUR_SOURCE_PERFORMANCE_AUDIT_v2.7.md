# Audit diretto — prestazioni, threading e lifecycle sui 4 snapshot (v2.7)

**Metodo:** lettura statica degli archivi caricati `Beeftext-master.zip`, `espanso-dev.zip`, `XMiLib-master.zip`, `emojilib-main.zip`. I percorsi relativi con righe si riferiscono ai rispettivi snapshot, **non a un commit Git attestato**. Nessun benchmark/runtime eseguito. Le osservazioni negative indicano rischi o scelte del codice, non che il software sia necessariamente lento o difettoso in produzione.

## Beeftext — evidenze dirette

| Percorso e righe | Evidenza | Implicazione progettuale |
|---|---|---|
| `Beeftext/InputManager.cpp:369-400` | Hook low-level Windows registrato e liberato con `SetWindowsHookEx`, `UnhookWindowsHookEx` | trattare install/uninstall come risorse a ownership esplicita |
| `Beeftext/InputManager.cpp:238-275` | Conversione eventi input, generazione segnali per backspace, separatori, caratteri | callback e trasformazione testo richiedono budget separati |
| `Beeftext/MainWindow.cpp:...` e `Beeftext/main.cpp:63-103` | Inizializzazione QApplication, istanza unica, preferenze, ComboManager, UpdateManager ed EmojiManager; emoji abilitate vengono caricate prima della finestra | cold-start e lazy load sono candidati, misurare dipendenze inizializzazione |
| `Beeftext/main.cpp:65-76` | seconda istanza bloccata e `setQuitOnLastWindowClosed(false)` | lifecycle in tray distinto dalla chiusura finestra |
| `Beeftext/Picker/PickerWindow.cpp:225-232` | ad ogni cambio query viene aggiornato `proxyModel_.setFilterFixedString` | con D10/D50 valutare il costo ricerca, caching, cancellation e virtualizzazione nella nuova GUI |
| `Beeftext/Picker/PickerSortFilterProxyModel.cpp:30-62` | filtro visita campi (nome, keyword, snippet, gruppo, descrizione) e costruisce regex per termini della query | costo query può dipendere da numero/volume snippet; *non* è una misura di latenza |
| `Beeftext/Picker/PickerWindow.cpp:285-303` | dopo chiusura picker `QTimer::singleShot(200ms, ...)` invoca l'espansione | ritardo fisso dipendente dal recupero focus; nel nuovo prodotto richiedere capacità e misurazioni |
| `Beeftext/Snippet/SnippetFragment.cpp:121` | `QThread::msleep` tra frammenti | un delay nel percorso output influenza il tempo end-to-end, non il matcher puro |
| `Beeftext/Snippet/KeySnippetFragment.cpp:101` | ritardo simulazione fra keystroke configurabile | tempi iniezione differenti per editor/strategia |
| `Beeftext/Combo/ComboList.cpp:395-398` | `readFromJsonDocument` cancella la collezione prima di validare | proposta di staging/swap coerente per resilienza, non benchmark |
| `Beeftext/Combo/ComboList.cpp:475-489` | JSON scritto direttamente tramite QFile | possibile latenza I/O e ripristino da separare dal path input |
| `Beeftext/Update/UpdateManager.cpp:84-104` | thread Qt dedicato al controllo update | già separato dal lavoro GUI; non affermare che tutto Beeftext sia single-threaded |

## Espanso — evidenze dirette

| Percorso e righe | Evidenza | Implicazione progettuale |
|---|---|---|
| `espanso-engine/src/lib.rs:34-75` | motore `Funnel → Processor → Dispatcher`, ciclo di ricezione/dispatch | separare osservazione, matching e effetti, senza copiare 15 crate |
| `espanso-engine/src/funnel/default.rs:20-63` | `crossbeam::Select` aspetta evento da diverse sorgenti | la gestione dei canali può essere efficace ma richiede strategia shutdown |
| `espanso-engine/src/process/default.rs:60-132` | catena di middleware costruita una volta; `VecDeque` di eventi processati | buona separazione, ma evitare una catena estensibile per ogni piccolo use case |
| `espanso-engine/src/process/default.rs:173-185` | processa eventi e recupera quelli generati internamente | garantire ordine, budget e nessuna ricorsione/coda illimitata nel nostro percorso |
| `espanso-engine/src/process/middleware/matcher.rs:71-95,103-156` | stati del matcher in `VecDeque` limitato da `max_history_size` | esempio concreto di cronologia bounded, senza accettare automaticamente i limiti originari |
| `espanso-match/src/rolling/matcher.rs:59-65,134-140` | albero di match compilato in `new` | evitare scansioni lineari di tutti gli snippet ad ogni evento; valutare trie in core |
| `espanso-match/src/rolling/matcher.rs:78-132` | stato incrementale con percorsi e cloni di eventi | misurare allocazioni/crescita per worst case di prefissi comuni |
| `espanso/src/cli/worker/engine/funnel/mod.rs:53-54` e `espanso/src/cli/worker/mod.rs:125-128` | canali `crossbeam::channel::unbounded()` per sottosistemi | non dedurne un bug; non adottare una coda di input illimitata senza overflow policy |
| `espanso/src/cli/daemon/watcher.rs:31-47,107-121` | watcher configurazione e debouncer su thread distinti | il refresh può essere coalesced, mai il testo digitato |
| `espanso-engine/src/dispatch/executor/text_inject.rs:68-107` | selezione strategia di output a runtime e segnalazione errori | budget output dipende dal backend; misurare separatamente dall'evento di matching |

## XMiLib — componenti di lifecycle nativi

| Percorso e righe | Evidenza | Implicazione |
|---|---|---|
| `XMiLib/GlobalShortcut/GlobalShortcut.cpp:30-51` | `GlobalAddAtom`, `RegisterHotKey`, `UnregisterHotKey`, `GlobalDeleteAtom` | ownership di risorse e cleanup anche se fallisce una registrazione |
| `XMiLib/SystemUtils.cpp:59,97,123,143,181` | `SendInput` per differenti azioni/tasti | non fare iniezione nel callback di osservazione; contare successi parziali e tempi OS |

## emojilib — responsabilità dati, non motore

Dataset `dist/emoji-en-US.json` dell'archivio emojilib 4.0.3: è un catalogo dati che nel nostro progetto va **normalizzato/indicizzato e caricato on demand**, non trasformato in dipendenza del hook input o della compilazione continua del matcher. La differenza di formato con l'`emojis.json` legacy di Beeftext è documentata nell'[audit submodule](EMOJILIB_DIRECT_SOURCE_AUDIT_v2.2.md). Non dedurre tempi di caricamento senza misure.

## Conclusioni verificabili, non risultati prestazionali

1. La GUI originale Beeftext usa un filtro del picker che dipende da campi dello snippet: **rischio di costo crescente**, da profilare; non chiamarlo collo di bottiglia dimostrato.
2. Espanso possiede già una struttura incrementale e storia limitata, ma gli eventi interni possono essere incanalati in canali non bounded: il nostro progetto definisce overflow e resync espliciti; non lo chiama leak di Espanso.
3. Entrambi distinguono alcuni confini native/output ma non risolvono magicamente i problemi di GUI/Unicode e focus già documentati in v2.5/v2.6.
4. La scelta futura C# resta una soluzione leggera in 4 assembly con Core puro e interfacce *solo* ai veri effetti esterni; performance budgets come **ipotesi**, non misure.

## Fonti pubbliche complementari, controllate al 2026-11-13

- Avalonia UI thread e dispatcher: https://docs.avaloniaui.net/docs/app-development/threading
- Microsoft diagnostic tool `dotnet-counters`: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters
- Microsoft runtime metric catalog: https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-runtime
- .NET BackgroundService lifecycle: https://learn.microsoft.com/en-us/dotnet/core/extensions/hosted-services (non obbliga a usare Generic Host)

**Evidenza mancante:** commit esatti, tempi di CPU/RSS e profili reali, comportamento su Windows/macOS/Linux, qualità del callback sotto carico, spike Wayland, editor con IME. Tutto da testare.
