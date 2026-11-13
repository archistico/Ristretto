# Addendum v2.5 — Unicode end-to-end, segmentazione e IME

**Stato: PROPOSTA documentale.** Vedere [matcher](TRIGGER_ENGINE.md), [ADR-0004](adr/ADR-0004-UNICODE-MATCHING-AND-WORD-BOUNDARIES.md) e [test](MATCHER_TEST_PLAN.md).

- **Tre lunghezze diverse:** UTF-16 `string.Length`, code point `Rune` ed *extended grapheme cluster* UAX #29. Nessuna delle tre è automaticamente il numero di Backspace che un'app esterna accetta.
- **NFC e output:** `café` e `cafe` + U+0301 sono canonically equivalent come chiave NFC, ma il contenuto originale viene preservato. NFC non elimina skin tones/ZWJ/VS16; accent-insensitive e compatibility-normalization NFKC non sono predefinite.
- **Segmentazione:** UAX #29 per grapheme/word con versione Unicode definita; `StringInfo` .NET è uno strumento utile, non una prova di equivalenza dell'editor. `Regex \b` è un algoritmo differente e non va adottato senza profilo di conformità.
- **Case:** `OrdinalIgnoreCase` non deve essere pubblicizzato come *full Unicode case folding*; turco `İ/ı`, greco `Σ/ς` e tedesco `ß/SS` hanno casi particolari. Nessun folding dipendente automaticamente dalla lingua della GUI.
- **IME:** processare un evento `CommittedText` solo quando la fonte nativa ne garantisce la semantica; non espandere su sequenze pre-edit. In assenza di certezza, modalità manuale/picker.
- **Indicizzazione:** se la chiave NFC ha lunghezza diversa dalla digitazione originale, conservare `RawMatchedText` e gli eventi committati; evitare di ricostruire il testo target da un offset della chiave normalizzata.
- **RTL e bidirezionalità:** preservare U+200E/U+200F/U+2066–U+2069, mostrare rappresentazione tecnica opzionale; niente conversione silenziosa della sequenza.
- **Controllo validità:** stringhe .NET con surrogati isolati vanno rifiutate o segnalate come dato non valido negli import; il formato UTF-8 esportato deve essere validato. Nessuna perdita silenziosa tramite carattere sostitutivo.
- **Import catalogo emoji:** shortcode e keyword localizzate non sono trigger automatici di default. Varianti tono, gender, VS16 e ZWJ sono parti significative e preservate.

**Evidenza:** UAX #15 https://www.unicode.org/reports/tr15/ ; UAX #29 https://www.unicode.org/reports/tr29/ ; UTS #39 https://www.unicode.org/reports/tr39/ ; .NET StringInfo https://learn.microsoft.com/en-us/dotnet/api/system.globalization.stringinfo.gettextelementenumerator.

---

# Contratto Unicode/IME e testo internazionale

## Regola madre

**L'archivio Unicode e l'algoritmo di matching non bastano a garantire un'espansione corretta dentro tutte le app**. Devono essere qualificati separatamente storage, trigger, parser, clipboard, sequenze simulate, IME e posizionamento cursore.

## Unità di testo

- `char` in C# è una unità UTF-16, **non** un carattere visibile; `Rune` rappresenta uno scalar Unicode, ma **non** un intero grapheme cluster.
- Le operazioni di posizionamento visivo e parte delle cancellazioni richiedono segmentazione per *extended grapheme clusters* (UAX #29 / equivalente .NET dove applicabile). Tuttavia il tasto Backspace nel campo di destinazione **non** è garantito agire su uno stesso grapheme per tutti gli editor.
- Il contenuto salvato/ricopiato resta identico all'originale, senza NFC/NFKC implicita. Per il solo confronto di trigger valutare NFC con policy documentata e test per collisioni.
- Case-insensitive non è una banale conversione a minuscolo. Definire comportamento invariant/culture-aware per Turkish dotted/dotless I, greco sigma, tedesco ß.
- Non reinterpretare sequenze come `\n`, `{Enter}`, `#{...}` in modalità `Testo letterale`, opzione necessaria per SQL, JSON e codice.

## IME e testo bidirezionale

- La composizione in corso per giapponese/cinese/coreano non deve attivare sostituzioni premature; trigger ammessi solo quando testati commit/finalizzazione e confini.
- Supportare copia di testo arabo/ebraico e combinazioni LTR/RTL con parentesi, numeri e simboli; l'ordine visuale non coincide con quello logico.
- Inserimenti tramite tastiera sintetica hanno limiti diversi da paste/clipboard. Decidere per app e indicare possibili fallback.
- I font locali determinano visualizzazione emoji, non il valore del testo memorizzato. No normalizzazione forzata degli emoji variation selector.

## Suite di fixture P0 (salvare input, expected bytes e risultato visivo)

| Test | Payload | Rischio |
|---|---|---|
| U01 | `caffè déjà vu piñata Straße Łódź` | Latin Extended, diacritici |
| U02 | `e` + U+0301 | carattere combinato vs `é` |
| U03 | `👍🏽 🇮🇹 👨‍👩‍👧‍👦 ❤️` | ZWJ, skin tone, flag, VS16 |
| U04 | `こんにちは 世界 / 漢字 / 한글` | CJK e IME |
| U05 | `العربية עברית ABC 123` | RTL/BiDi |
| U06 | `İ ı I i Σ σ ς ß` | case folding per locale |
| U07 | `∑ ∞ ≥ ≤ ₿ € ₹ → °C` | simboli |
| U08 | `SELECT N'caffè😀';` | SQL e literal mode |
| U09 | multilinea + tab + CRLF/LF | paste, line ending |
| U10 | zero-width joiner, non-joiner, bidi controls | sicurezza e cursor |

## Differenziare gli esiti

`StorePass`, `MatchPass`, `OutputPass`, `CursorPass`, `UndoPass`, `PlatformBlocked`; un test che passa in `StorePass` non abilita il badge `Unicode supported` per quell'editor.

## Test obbligatori di concorrenza

- Clipboard con immagine e HTML che cambia dopo l'acquisizione.
- Due snippet attivati in successione; il secondo non deve contaminare il primo.
- Cambio focus dopo apertura picker o form; niente inserimento in finestra ignota.
- Backspace sotto IME, auto-correct degli editor, input multilinea e password field.

Fonti: https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction ; https://learn.microsoft.com/it-it/dotnet/api/system.globalization.stringinfo?view=net-10.0 ; https://docs.avaloniaui.net/docs/app-development/localizing.

I 54 scenari Unicode originari sono recuperati, con ID immutati e stato `NOT_RUN`, in [HISTORICAL_TEST_SCENARIOS.csv](../data/HISTORICAL_TEST_SCENARIOS.csv).

## Addendum v1.6 — committed text, trigger e grafemi

Le diverse fonti confermano che le regole word-boundary non sono uniformi tra applicazioni. Per la nostra app il riconoscimento riguarda **testo logico confermato**, non eventi hardware grezzi. `StringInfo` permette enumerazione di text elements ma non valida automaticamente la cancellazione con Backspace in un editor esterno. Conservare NFC/NFD esattamente nello storage; per il match si può valutare NFC opzionale solo con UI e collision detection. `CaseSensitive`, `CaseInsensitive` e `AccentInsensitive` sono politiche diverse. Mettere in pausa/negare matching su composizione IME incerta.

Test strutturati: [TRIGGER_TEST_DESIGN.md](../research/TRIGGER_TEST_DESIGN.md), [TRIGGER_SCENARIOS.csv](../data/TRIGGER_SCENARIOS.csv); fonti: https://learn.microsoft.com/en-us/dotnet/standard/base-types/character-encoding-introduction e https://wiki.keyboardmaestro.com/trigger/Typed_String.


## Raffinamento v1.7 — evento hardware ≠ testo confermato

Windows `WH_KEYBOARD_LL`, macOS Event Tap, X11 keycode ed evdev osservano eventi di input, non necessariamente il risultato finale di dead key/IME. Non chiamare `CommittedText` un evento ricostruito da solo scancode. Su Wayland `text-input-v3` gestisce la composizione per **il campo client**, non offre ad app terze una lettura globale dei commit. Un backend con composizione non verificabile deve rinunciare al trigger automatico in quel contesto.

`KEYEVENTF_UNICODE` è una possibile via di inserimento Windows ma un passaggio UTF-16/VK_PACKET non garantisce compatibilità con ogni editor. Qualificare emoji ZWJ e grapheme cursor per ciascun metodo. Fonte: [NATIVE_INPUT_OUTPUT.md](NATIVE_INPUT_OUTPUT.md), [NATIVE_SOURCE_EVIDENCE.md](../research/NATIVE_SOURCE_EVIDENCE.md).

## Evidenze dai sorgenti forniti (v2.1)

- Beeftext usa `QString::size()` per il numero di caratteri da cancellare (`Combo.cpp:433`) e itera `QChar` per digitazione simulata (`BeeftextUtils.cpp:232`). Queste operazioni contano/elaborano unità UTF-16, non necessariamente grafemi o testo composto.
- Espanso sceglie clipboard per testo non ASCII in modalità automatica su Linux (`text_inject.rs:85–95`); su macOS spezza in blocchi UTF-16 per l'API di iniezione (`native.mm:58–88`). Sono workaround reali da valutare, **non** garanzie di supporto emoji.
- Nuovi test in `data/LOCAL_SOURCE_AUDIT_SCENARIOS_v2.1.csv`. Nessun test runtime eseguito.

## Audit emojilib/XMiLib v2.2

`XMiLib/SystemUtils.cpp:169-181` prepara un key-up senza `KEYEVENTF_UNICODE` nell'helper che Beeftext usa per la digitazione simulata; la versione di input segue unità UTF-16 (`QChar`). Nessuna equivalenza generale tra `string.Length`, punti di codice, grafemi e numero di backspace. Test P0: surrogate pair, VS16, ZWJ, skin tone, CJK, RTL, IME e gestione di un evento di input incompleto. `emojilib` v4.0.3 è un dataset di keyword **en-US**, non il supporto multilingua completo. La stringa dell'utente deve essere preservata indipendentemente dal catalogo. [Specifica catalogo](EMOJI_CATALOG.md).

## Modello di dominio v2.4 e vincolo sul deletion span

[DOMAIN_MODEL.md](DOMAIN_MODEL.md) precisa che il payload originale viene preservato come **sequenza Unicode e terminatori di riga**, non necessariamente come byte di JSON serializzato; import Unicode invalidi devono essere segnalati, non “corretti” con U+FFFD. Il `MatchResult` restituisce uno span **logico** di testo committato, non un numero universale di Backspace. Il target decide se la rimozione sia verificabile; l'impossibilità di stabilirla provoca blocco/degrado manuale. `InputObservation` separa commit attestato, composizione e unknown. Test [DOMAIN_TEST_PLAN](DOMAIN_TEST_PLAN.md).
