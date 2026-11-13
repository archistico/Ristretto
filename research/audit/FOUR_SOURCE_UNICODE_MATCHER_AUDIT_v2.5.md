# Audit diretto — Unicode e riconoscimento trigger (v2.5)

**Stato:** verifica statica degli ZIP forniti; non sono stati compilati o eseguiti i progetti. Le righe sono riferite agli snapshot ZIP forniti, non a un commit Git pin-nato. Nessun codice di terzi è riprodotto nel dossier. Consultare anche [contratto trigger](../../docs/TRIGGER_ENGINE.md), [Unicode e IME](../../docs/UNICODE_IME.md), [piano test](../../docs/MATCHER_TEST_PLAN.md).

## 1. Beeftext — evidenze ripetibili

| File nello ZIP `Beeftext-master.zip` | Righe | Osservazione verificata staticamente | Conseguenza per il nuovo prodotto |
|---|---:|---|---|
| `Beeftext/Combo/Combo.cpp` | 377–384 | `matchesForInput` usa `Qt::CaseSensitivity`; modalità Loose confronta con `endsWith`, le altre usano `compare` | Il comportamento *suffix* non è una regola affidabile di parola intera; definire boundary separatamente |
| `Beeftext/Combo/ComboManager.cpp` | 194–217 | Opzione globale di espansione dopo spazio; rimozione dello spazio finale prima del confronto; in caso di candidati multipli viene scelto un indice tramite RNG | Separare delimitatore dalla sequenza; conflitti deterministici e osservabili |
| `Beeftext/Combo/Combo.cpp` | 406–449 | Prepara il testo e simula la cancellazione con `keyword_.size()` e, se necessario, un tasto per lo spazio; la posizione del cursore è gestita via frecce | `QString::size()` non rappresenta Backspace reali in tutti gli editor; output e deletion tramite capability |
| `Beeftext/Combo/ComboManager.cpp` | 230–264 | Emoji tramite delimitatori sinistro/destro, ricerca shortcode e `keyword.size()` per quantità da rimuovere | Shortcode emoji e trigger normali sono funzionalità distinte; ragionare su segmenti osservati, non lunghezza normalizzata |

**Non dimostrato da questi file:** supporto completo a IME, normalizzazione NFC nei confronti, affidabilità con grafemi ZWJ, comportamento reale su ogni applicazione. Non attribuire prove runtime alla sola lettura.

## 2. Espanso — evidenze ripetibili

| File nello ZIP `espanso-dev.zip` | Righe | Osservazione statica | Conseguenza |
|---|---:|---|---|
| `espanso-match/src/event.rs` | 20–24 | `Event::Key` distingue codice tasto e stringa `chars: Option<String>`; esiste `VirtualSeparator` | Buona separazione concettuale ma non prova che ogni backend esponga testo IME committato |
| `espanso-match/src/rolling/mod.rs` | 26–63 | `RollingItem` include `WordSeparator`, `Key`, `Char`, `CharInsensitive`; `from_string` itera su `string.chars()` | È un pattern da studiare, non una garanzia di segmentazione in grapheme cluster UAX #29 |
| `espanso-match/src/rolling/matcher.rs` | 34–63, 70–131 | Matcher mantiene percorsi incrementali e produce match senza effetti esterni | Condividere il modello rolling puro, non necessariamente la stessa struttura ad albero |
| `espanso-match/src/rolling/matcher.rs` | 152–205 | Riconoscimento di separatori tramite elenchi configurati di caratteri/tasti e `VirtualSeparator` | Separatore configurabile non equivale automaticamente a word boundary Unicode |
| `espanso-match/src/rolling/util.rs` | 24–51 | Risultato include trigger e separatori sinistro/destro ricostruiti dagli eventi | Preservare il delimitatore realmente committato, senza inventare uno spazio |
| `espanso-config/src/matches/group/loader/yaml/parse.rs` | 61–121 | Il formato dichiara `word`, `left_word`, `right_word`, `propagate_case`, regex e più trigger | Importare impostazioni con anteprima; semantica nuova esplicita, non clonare silentemente |

**Nota:** `UniCase` viene usato nel matcher, ma senza prove dedicate non concludiamo che i suoi risultati coincidano con *full Unicode case folding* in ogni lingua e versione Unicode. Espanso `dev` può differire dalle release distribuite.

## 3. XMiLib ed emojilib — ruolo in questa fase

- **XMiLib**: helper Win32 usati per output tastiera, clipboard e hotkey. Non costituiscono un motore di segmentazione Unicode affidabile per tutte le applicazioni. Vedere [audit XMiLib](XMILIB_DIRECT_SOURCE_AUDIT_v2.2.md).
- **emojilib**: catalogo emoji e parole chiave; il dataset non specifica come cancellare un trigger da un editor esterno né il significato di “parola”. Non usare parole chiave tradotte come trigger globali attivi implicitamente. Vedere [catalogo](../../docs/EMOJI_CATALOG.md).

## 4. Verifiche di standard e runtime

1. **UAX #15 (NFC)**: confrontare equivalenza canonica; mai trasformare il contenuto persistito o l'output dell'utente senza richiesta. Una concatenazione di stringhe NFC non è necessariamente NFC: normalizzare la sequenza completa (o usare algoritmo incrementale corretto). https://www.unicode.org/reports/tr15/
2. **UAX #29**: distingue grapheme cluster, word boundary e sentence boundary. Versionare i dati/algoritmi e testare con i file di conformance ufficiali. Non equiparare regole semplici `\b` .NET a tutte le regole UAX #29. https://www.unicode.org/reports/tr29/
3. **UTS #39**: warning di grafia confondibile opzionale, mai traduzione o fusione automatica di alfabeti simili. https://www.unicode.org/reports/tr39/
4. **.NET StringInfo**: utile per *text elements*; non implica che Backspace elimini un solo grapheme nel target. https://learn.microsoft.com/en-us/dotnet/api/system.globalization.stringinfo.gettextelementenumerator
5. **.NET RegEx**: le classi `\w` e `\b` hanno una semantica propria e cambiano con `ECMAScript`. Usare wrapper e conformance; non prendere questa scorciatoia per il motore P0. https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-options

## 5. Conclusioni documentali

- **Accertato nel sorgente:** Beeftext confronta exact/suffix e sceglie casualmente più candidati; Espanso usa rolling + separatori configurabili; emoji catalog è separato dall'output.
- **Decisione progettuale proposta:** core che consuma solo testo *committato* e calcola candidati in modo deterministico con NFC di confronto; `raw span` mantenuto per eventuale cancellazione.
- **Non accertato:** supporto IME cross-app da hook generici, `Undo` transazionale universale, conteggio Backspace universale, equivalenza casing completa e uniformità Wayland.
