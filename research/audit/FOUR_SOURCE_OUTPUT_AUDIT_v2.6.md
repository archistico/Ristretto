# Audit diretto della pipeline di output v2.6 — Beeftext, Espanso, XMiLib, emojilib

**Metodo.** Lettura statica dei quattro ZIP forniti dall'utente; percorsi relativi alla radice degli archivi; numeri di riga riferiti agli snapshot locali `Beeftext-master` e `espanso-dev`. Nessuna compilazione o prova su editor esterni. Il termine *rischio* non implica difetto riprodotto. `emojilib` è rilevante per i dati emoji, non per l'output nativo.

## Evidenze direttamente verificabili

| ID | File e linee dello snapshot | Fatti visibili nel codice | Conseguenza / verifica proposta |
|---|---|---|---|
| BF-OUT-01 | `Beeftext/Combo/Combo.cpp:406-452` | `performSubstitution` prepara snippet, attende 300ms dopo dialog, disabilita hook, cancella keyword in base a `keyword_.size()`, invia frammenti e muove cursore; ripristina hook anche nel catch gestito | buon precedente preparazione prima output; `keyword_.size()` non è Backspace universale e delay non prova focus |
| BF-OUT-02 | `Beeftext/BeeftextUtils.cpp:179-183` | `eraseChars` invoca `synthesizeBackspaces(count)` | semantica rimessa al backend/editor; non ritorna numero di caratteri cancellati |
| BF-OUT-03 | `Beeftext/BeeftextUtils.cpp:193-219` | copia clipboard se impostato, `setText`, simula paste, chiama `QTimer::singleShot(1000, restore o clear)` | race con nuove copie dell'utente; timing non conferma che paste sia stato consumato |
| BF-OUT-04 | `Beeftext/BeeftextUtils.cpp:229-244` | simula unità `QChar`/UTF-16 e gestisce newline con VK_RETURN | test Unicode/IME necessari, una iterazione per `QChar` non è una iterazione per grapheme |
| BF-OUT-05 | `Beeftext/Clipboard/ClipboardManagerDefault.cpp:63-103` | enumera formati Win32 eccetto alcuni; tratta bitmap a parte; backup best-effort con eccezioni loggate | non garantisce conservazione integrale di formati/dati differiti |
| BF-OUT-06 | `Beeftext/Clipboard/ClipboardManagerDefault.cpp:110-153` | ripristino `EmptyClipboard` seguito da formati del backup, senza verifica di una nuova copia intercorsa nella funzione | possibile overwrite di clipboard altrui; test su copie concorrenti obbligatori |
| BF-OUT-07 | `Beeftext/Clipboard/ClipboardManagerDefault.cpp:159-170` | `clearClipboard` chiama `EmptyClipboard` senza controllo ownership | vietare pulizia incondizionata nel nuovo prodotto |
| ES-OUT-01 | `espanso-engine/src/dispatch/executor/text_inject.rs:27-115` | scelta `Event`/`Clipboard`/`Auto`; in `Auto` soglia sulla count dei `chars`, Linux non ASCII sceglie clipboard; errore loggato e `execute` restituisce `true` per evento gestito | separare `handled` da `injected`; metodo adattivo sì, outcome esplicito |
| ES-OUT-02 | `espanso-engine/src/process/middleware/action.rs:118-151` | cancellazione trigger usa `.chars().count()` meno separatore; undo conta `.chars().count() - 1` | codice scalares ≠ backspace garantiti; emoji composte/caret/IME e undo richiedono test |
| ES-OUT-03 | `espanso-engine/src/process/middleware/cause.rs:39-59` | emette evento di compensazione trigger e match compensato separati | buon precedente disaccoppiare rilevazione e output; correlazione evento da mantenere |
| ES-OUT-04 | `espanso-engine/src/process/middleware/undo.rs:55-102` | registra trigger+injected text e prova undo su prossimo Backspace; mouse/cursor hint invalidano record | annullamento interessante ma condizionato; non copiare senza verifica editor/Unicode |
| ES-OUT-05 | `espanso-engine/src/process/middleware/match_select.rs:89-123` | per più match chiede selezione, scarta eventi accumulati mentre la ricerca è aperta | utile evitare replay di input vecchio quando picker ha preso focus |
| ES-OUT-06 | `espanso-engine/src/dispatch/executor/key_inject.rs:41-49` | errore inject sequence loggato, esecutore riporta evento gestito | stesso rischio semantico: successo dispatch non successo inserimento |
| XM-OUT-01 | `XMiLib` → utility Win32 Unicode/shortcut, vedere [audit submodule](XMILIB_DIRECT_SOURCE_AUDIT_v2.2.md) | il progetto upstream incapsula input sintetico, il suo codice non va replicato letteralmente | definire `ITextOutput` con risultato e test Win32 per numero eventi inviati |
| EM-OUT-01 | `emojilib` → JSON dataset, vedere [audit catalogo](EMOJILIB_DIRECT_SOURCE_AUDIT_v2.2.md) | file di dati, non backend di output | emoji catalog e inserimento sono problemi indipendenti |

## Sintesi, non equiparazione con runtime

- **Beeftext** fornisce una sequenza diretta, comprensibile ma con verifiche limitate e timer di clipboard. La presenza di `try/catch` per il ripristino hook non dimostra che tutti gli errori nativi siano osservabili.
- **Espanso** separa dispatcher ed esecutori, ma `execute()->true` in questi file significa che l'evento è stato *gestito*, non che il target abbia ricevuto testo. Le regole di compensazione si basano sulla count di Unicode scalar values; non equivalgono a un editing remoto transazionale.
- **XMiLib** rende accessibile l'input sintetico Windows: miglioramento richiesto nella nuova app è la contabilizzazione di eventi spediti, errori e contesto, senza equiparazione a output effettivo.
- **emojilib** non può risolvere da sola Backspace Unicode, verifica caret o testo committato IME.

## Raccomandazioni progettuali (nostre, non comportamenti originali)

1. Preparazione immutabile e dialog senza effetti sul target.
2. Identità/epoch target riconvalidati immediatamente prima degli effetti; nessuna garanzia anti-race totale.
3. Cancellazione qualificata per editor e metodo, senza count basata solo su `QChar`/`char`/`Rune`/grapheme.
4. Pipeline seriale con esito `NoChange`, `SubmittedUnverified`, `PartialOrUnknown` e `VerifiedComplete` solo quando attestabile.
5. Clipboard lease best effort, restore se ownership invariata e tecnicamente verificabile; se cambia, *preservare* gli appunti attuali.
6. Annullamento automatico rimandato a post-MVP.
7. Insertion method per-target, diagnosi e manual fallback.

## Limiti

Gli snapshot non attestano commit esatti, né submodule gitlink compatibili. Non abbiamo riprodotto race, perdita testo, UiPI, scritture paste o IME. Le osservazioni sui rischi sono statiche, non report di bug confermati in esecuzione. Normative e API consultate sono registrate in [fonti output](../OUTPUT_SOURCE_REGISTER_v2.6.md).
