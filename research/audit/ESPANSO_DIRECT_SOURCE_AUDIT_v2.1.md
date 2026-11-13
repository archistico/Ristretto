# Espanso — audit diretto dello snapshot `dev`

**Fonte:** ZIP utente `espanso-dev.zip`, workspace Rust con 15 crate dichiarati in `Cargo.toml`, `Cargo.lock` presente. Non è identificabile il commit Git. Non abbiamo compilato o eseguito Espanso. Il codice è GPLv3: **nessuna incorporazione diretta senza verifica legale della licenza scelta per il nostro prodotto**.

## Componenti e responsabilità osservate

| Modulo | Esempi | Concetto riutilizzabile |
|---|---|---|
| `espanso-detect` | `src/win32`, `mac`, `x11`, `evdev`, `layout` | confine netto degli input native |
| `espanso-match` | `rolling/matcher.rs`, `regex/mod.rs` | match e regex indipendenti dalla UI |
| `espanso-engine` | `process/middleware/`, `dispatch/executor/` | pipeline riconoscimento → effetti |
| `espanso-render` | `renderer/`, `extension/{date,form,shell,script}` | risoluzione di variabili separata dai trigger |
| `espanso-inject` | `win32`, `mac`, `x11`, `evdev` | backends OS di inserimento |
| `espanso-clipboard` | Win32, Cocoa, X11, Wayland | implementazioni native clipboard |
| `espanso-config` | parsing YAML, schemi, store | input configurazione non legato al motore |
| `espanso-ipc`, `espanso-ui` | IPC e tray/menu | processo e GUI possono dialogare senza dipendenza diretta dal matcher |
| altri crate | `kvs`, `package`, `info`, `modulo`, mac utils | **non** migrare per imitazione: aggiungono superficie e manutenzione |

## Evidenze di maggiore interesse

### Matcher rolling e regex

- `espanso-match/src/rolling/matcher.rs`: il matcher mantiene percorsi di stato su un trie di eventi; produce più `MatchResult` quando più ID condividono un nodo. Questo è preferibile a concatenare costantemente tutte le combinazioni, ma la gestione dei risultati deve essere deterministica.
- `espanso-match/src/regex/mod.rs`: matcher regex su buffer limitato (`max_buffer_size` 30 di default); contiene gestione delle capture con nome. La regola di dimensionamento del buffer è un parametro funzionale da testare anche con Unicode.
- `espanso-engine/src/process/middleware/matcher.rs`: la gestione del buffer è separata dall'esecuzione; va preservata come concetto, non necessariamente con una middleware pipeline generica.

### Inserimento adattivo

- `espanso-engine/src/dispatch/executor/text_inject.rs:31–110` definisce modalità `Event`, `Clipboard`, `Auto` e possibilità di forzatura per espansione.
- In `Auto`, oltre una soglia misurata in caratteri Rust viene scelta la clipboard; **su Linux in particolare, quando il testo non è ASCII, viene preferita la clipboard**. È evidenza di un limite pratico dell'input da tastiera in alcuni ambienti Linux, non prova di perfetta copertura Unicode con clipboard.
- L'executor registra errori dell'injector, ma ritorna comunque `true` come evento gestito: **non confondere 'evento consumato' con 'inserimento riuscito'**. Per la nostra app definire `ExpansionResult` con esito dettagliato.

### Windows/macOS/Linux

- `espanso-inject/src/mac/native.mm:58–88`: per l'inserimento Unicode via Quartz, il codice spezza la stringa UTF-16 in chunk di 20 unità (oppure 1 con ritardi personalizzati), per mitigare un limite osservato. Occorre testare che un confine di chunk non divida surrogate pair: rischio, non difetto runtime dimostrato.
- `espanso-inject/src/x11/mod.rs`: backend X11 principale con fallback xdotool.
- `espanso-detect/src/x11/native.cpp`: usa XRecord per osservare eventi; non si applica automaticamente a Wayland.
- `espanso-inject/src/evdev/uinput.rs` e `espanso-detect/src/evdev/`: supporto Linux evdev/uinput; implicazioni di permessi e distribuzione.
- `espanso-clipboard/src/lib.rs:94–108`: percorso Wayland fallback con `wl-copy`/`wl-paste`, documentato nel commento. Richiede tool esterni: disponibilità e permission model dipendono dal sistema.

## Qualità e test

Nello snapshot sono presenti test Rust per match, configurazione, rendering, rilevazione e altre parti. Si tratta di **test scritti**, non eseguiti in questo audit; il numero o la presenza di test non garantiscono correttezza. La separazione dei moduli aiuta a testare il core indipendentemente dall'OS.

## Trade-off per il nostro progetto

**Da prendere come modello concettuale:** trigger event-based, parser separato, due metodi di inserimento, strategia per-app, capability e fallback, error handling, test puri.

**Da non riprodurre meccanicamente:** 15 crate/assembly distinte, sistema completo di scripting, package manager remoto, dipendenze da tool Linux esterni obbligatorie, middleware/event bus quando bastano use case espliciti. Il prodotto Avalonia deve restare piccolo, comprensibile, offline-first e con il minimo privilegio.

## Rischi non chiusi

Supporto IME realmente composto, riacquisizione focus dopo picker, cancel/rollback su edit esterni, i diversi formati clipboard, permessi Wayland, emoji composte e modalità keyboard injection. Richiedono prove su editor reali.
